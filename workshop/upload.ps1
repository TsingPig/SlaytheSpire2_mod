# ============================================================
# NinjaMod Steam Workshop 上传脚本
# ============================================================
# 把打包好的模组发布到 Steam 创意工坊。
#
# 准备工作：
#   1. Steam 账号必须拥有 Slay the Spire 2
#   2. 账号必须启用了 Steam 令牌
#   3. 脚本会自动下载 steamcmd.exe（如果不存在的话）
#
# 使用方法：
#   直接运行即可，按提示输入 Steam 账号密码和令牌验证码。
#
# 也可通过 .env 文件免交互（复制 .env.example 为 .env 并填入凭据）：
#   STEAM_USER=your_username
#   STEAM_PASS=your_password
# ============================================================

param(
    [switch]$ValidateOnly,    # 只校验不上传
    [string]$SteamUser,       # 可选：Steam 登录账号
    [string]$SteamPass,       # 可选：Steam 密码；更推荐写入 workshop/.env
    [string]$SteamCmdPath,    # 可选：steamcmd.exe 路径
    [int]$MaxRetries = 3      # 上传因网络超时失败时的自动重试次数
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true)
$steamPreviewMaxBytes = 950KB

# ------------------------------------------------------------------
# 辅助函数：读写 VDF 配置
# ------------------------------------------------------------------

function ConvertTo-VdfValue([string]$Value) {
    return $Value.Replace('\', '\\').Replace('"', '\"').Replace("`r`n", '\n').Replace("`n", '\n')
}

function Read-Utf8Text([string]$Path) {
    return [System.IO.File]::ReadAllText($Path, $utf8Strict)
}

function Read-Utf8Lines([string]$Path) {
    return [System.IO.File]::ReadAllLines($Path, $utf8Strict)
}

function Set-VdfValue([string]$Text, [string]$Key, [string]$Value) {
    $escapedKey = [regex]::Escape($Key)
    $escapedValue = ConvertTo-VdfValue $Value
    $pattern = '(?m)^(\s*"' + $escapedKey + '"\s*)".*"'
    $replacement = '${1}"' + $escapedValue + '"'
    if ([regex]::IsMatch($Text, $pattern)) {
        return [regex]::Replace($Text, $pattern, $replacement, 1)
    }
    return [regex]::Replace($Text, '(?m)^}', "`t`"$Key`"`t`t`"$escapedValue`"`r`n}", 1)
}

function Remove-VdfValue([string]$Text, [string]$Key) {
    $escapedKey = [regex]::Escape($Key)
    return [regex]::Replace($Text, '(?m)^\s*"' + $escapedKey + '"\s*".*"\r?\n?', '', 1)
}

function Test-PlaceholderCredential([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) { return $true }
    $normalized = $Value.Trim().ToLowerInvariant()
    return $normalized -in @(
        "your_username",
        "your_password",
        "your_steam_username",
        "your_steam_password"
    )
}

function Test-LikelyShellCommand([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) { return $false }
    return $Value -match '(?i)(^|\s)(powershell|pwsh|cmd)(\.exe)?(\s|$)' -or
           $Value -match '(?i)(executionpolicy|upload\.ps1|\.ps1\b)'
}

function Test-SteamUserValue([string]$Value) {
    if (Test-PlaceholderCredential $Value) { return $false }
    $trimmed = $Value.Trim()
    if ($trimmed -match '\s') { return $false }
    if (Test-LikelyShellCommand $trimmed) { return $false }
    return $true
}

function Resolve-SteamUser([string]$ExplicitUser) {
    foreach ($candidate in @($ExplicitUser, $env:STEAM_USER)) {
        if (Test-PlaceholderCredential $candidate) { continue }
        if (Test-SteamUserValue $candidate) { return $candidate.Trim() }
        throw "Steam 用户名看起来不像账号：`"$candidate`"。请只输入 Steam 登录账号，不要把 powershell -ExecutionPolicy ... 命令粘到用户名提示里。"
    }

    for ($attempt = 1; $attempt -le 3; $attempt++) {
        $candidate = Read-Host "Steam 用户名"
        if (Test-SteamUserValue $candidate) { return $candidate.Trim() }
        Write-Host "这不像 Steam 登录账号。请只输入账号名；不要在这里粘贴启动 upload.ps1 的命令。" -ForegroundColor Yellow
    }

    throw "没有获得有效 Steam 用户名，已停止上传。"
}

function Resolve-SteamPass([string]$ExplicitPass) {
    foreach ($candidate in @($ExplicitPass, $env:STEAM_PASS)) {
        if (-not (Test-PlaceholderCredential $candidate)) { return $candidate }
    }

    $secure = Read-Host "Steam 密码" -AsSecureString
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }
}

function Import-DotEnv([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return }

    foreach ($line in (Read-Utf8Lines $Path)) {
        if ($line -match '^\s*(#|$)') { continue }
        if ($line -notmatch '^\s*([A-Za-z_]\w*)\s*=\s*(.*?)\s*$') { continue }

        $key = $matches[1]
        $value = $matches[2].Trim()
        if (($value.StartsWith('"') -and $value.EndsWith('"')) -or
            ($value.StartsWith("'") -and $value.EndsWith("'"))) {
            $value = $value.Substring(1, $value.Length - 2)
        }
        Set-Item -LiteralPath "env:$key" -Value $value
    }
}

function Save-JpegImage([System.Drawing.Image]$Image, [string]$Path, [int]$Quality) {
    $jpegCodec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() |
        Where-Object { $_.MimeType -eq "image/jpeg" } |
        Select-Object -First 1
    if (-not $jpegCodec) { throw "当前系统找不到 JPEG 编码器，无法生成 Steam 预览图。" }

    $encoderParams = [System.Drawing.Imaging.EncoderParameters]::new(1)
    $encoderParams.Param[0] = [System.Drawing.Imaging.EncoderParameter]::new(
        [System.Drawing.Imaging.Encoder]::Quality,
        [long]$Quality
    )
    try {
        $Image.Save($Path, $jpegCodec, $encoderParams)
    } finally {
        $encoderParams.Dispose()
    }
}

function New-WorkshopPreviewFile([string]$SourcePath, [string]$OutputPath, [int]$MaxBytes) {
    if (-not (Test-Path -LiteralPath $SourcePath)) { throw "Workshop 预览图不存在：$SourcePath" }

    $sourceItem = Get-Item -LiteralPath $SourcePath
    if ($sourceItem.Length -le $MaxBytes) {
        return $sourceItem.FullName
    }

    Add-Type -AssemblyName System.Drawing
    $sourceImage = [System.Drawing.Image]::FromFile($sourceItem.FullName)
    try {
        $width = $sourceImage.Width
        $height = $sourceImage.Height
        $scale = 1.0

        while ($scale -ge 0.45) {
            $targetWidth = [Math]::Max(1, [int][Math]::Round($width * $scale))
            $targetHeight = [Math]::Max(1, [int][Math]::Round($height * $scale))
            $bitmap = [System.Drawing.Bitmap]::new($targetWidth, $targetHeight, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
            try {
                $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
                try {
                    $graphics.Clear([System.Drawing.Color]::White)
                    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
                    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
                    $graphics.DrawImage($sourceImage, 0, 0, $targetWidth, $targetHeight)
                } finally {
                    $graphics.Dispose()
                }

                foreach ($quality in @(92, 88, 84, 80, 76, 72, 68, 64, 60, 56)) {
                    Save-JpegImage $bitmap $OutputPath $quality
                    $outputItem = Get-Item -LiteralPath $OutputPath
                    if ($outputItem.Length -le $MaxBytes) {
                        Write-Host "预览图超过 Steam 1 MB 限制，已生成上传用副本：$($outputItem.FullName) ($([Math]::Round($outputItem.Length / 1KB)) KB)" -ForegroundColor Yellow
                        return $outputItem.FullName
                    }
                }
            } finally {
                $bitmap.Dispose()
            }

            $scale -= 0.10
        }
    } finally {
        $sourceImage.Dispose()
    }

    throw "无法把 Workshop 预览图压到 Steam 要求的 1 MB 以下：$SourcePath"
}

# ==================================================================
# 第一步：从 dist/NinjaMod/ 同步到 content 目录
# ==================================================================

$distMod = Join-Path $repoRoot "dist\NinjaMod"

# 检查 dist 里三个核心文件是否齐全
$requiredFiles = @("NinjaMod.dll", "NinjaMod.json", "NinjaMod.pck")
foreach ($name in $requiredFiles) {
    $path = Join-Path $distMod $name
    if (-not (Test-Path -LiteralPath $path)) {
        throw "dist 里缺少 $name，请先执行 scripts\build.ps1。"
    }
}

# 清空旧的 content 目录，用最新文件替换
$contentRoot = Join-Path $PSScriptRoot "content"
$contentFolder = Join-Path $contentRoot "NinjaMod"
if (Test-Path -LiteralPath $contentFolder) {
    Remove-Item -LiteralPath $contentFolder -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $contentFolder | Out-Null
foreach ($name in $requiredFiles) {
    Copy-Item -LiteralPath (Join-Path $distMod $name) -Destination $contentFolder -Force
}
Write-Host "已同步 dist\NinjaMod -> $contentFolder" -ForegroundColor Cyan

# ==================================================================
# 第二步：更新 upload.vdf（路径 + 更新说明）
# ==================================================================

$vdfPath = Join-Path $PSScriptRoot "upload.vdf"
if (-not (Test-Path $vdfPath)) {
    Write-Host "[ERROR] 找不到 upload.vdf：$vdfPath" -ForegroundColor Red
    exit 1
}

$vdfText = Read-Utf8Text $vdfPath

# 填入 contentfolder 和 previewfile 的实际路径
$vdfText = Set-VdfValue $vdfText "contentfolder" ([System.IO.Path]::GetFullPath($contentRoot))
$previewSourceFile = Join-Path $PSScriptRoot "preview.png"
$previewUploadFile = Join-Path $PSScriptRoot "preview.upload.jpg"
$previewFile = New-WorkshopPreviewFile $previewSourceFile $previewUploadFile $steamPreviewMaxBytes
$vdfText = Set-VdfValue $vdfText "previewfile" ([System.IO.Path]::GetFullPath($previewFile))

# 从 NinjaMod.json 读版本号，从 CHANGELOG.md 自动生成更新说明
$manifest = Read-Utf8Text (Join-Path $contentFolder "NinjaMod.json") | ConvertFrom-Json
$changelogPath = Join-Path $repoRoot "CHANGELOG.md"
$versionHeader = "## v$($manifest.version.TrimStart('vV'))"
$pattern = [regex]::Escape($versionHeader) + "\s*—\s*\S+(.*?)(?=\r?\n## |\z)"
$changelogText = if (Test-Path -LiteralPath $changelogPath) { Read-Utf8Text $changelogPath } else { "" }
$match = [regex]::Match($changelogText, $pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)
$changeNote = $env:WORKSHOP_CHANGE_NOTE
if ([string]::IsNullOrWhiteSpace($changeNote) -and $match.Success) {
    $changeNote = $match.Groups[1].Value.Trim() -replace '^\s*[-*]\s*', ''
    $changeNote = ($changeNote -split "`r?`n" | Where-Object { $_.Trim() -ne '' } | Select-Object -First 5) -join '；'
    $changeNote = "$($manifest.version) - $changeNote"
}
if ([string]::IsNullOrWhiteSpace($changeNote)) {
    $changeNote = "$($manifest.version) - 更新 NinjaMod 模组。"
}
$vdfText = Set-VdfValue $vdfText "changenote" $changeNote

[System.IO.File]::WriteAllText($vdfPath, $vdfText, $utf8NoBom)

$publishedIdMatch = [regex]::Match($vdfText, '"publishedfileid"\s+"([^"]+)"')
$isExistingWorkshopItem = $publishedIdMatch.Success -and $publishedIdMatch.Groups[1].Value -ne "0"
$steamUploadVdfPath = $vdfPath
if ($isExistingWorkshopItem) {
    $steamUploadVdfPath = Join-Path $PSScriptRoot "upload.generated.vdf"
    $steamUploadVdfText = Remove-VdfValue (Remove-VdfValue $vdfText "title") "description"
    [System.IO.File]::WriteAllText($steamUploadVdfPath, $steamUploadVdfText, $utf8NoBom)
}

# ==================================================================
# 第三步：校验
# ==================================================================

# 确认 content 目录和预览图存在
if (-not (Test-Path -LiteralPath $contentFolder)) { throw "Workshop 内容目录不存在：$contentFolder" }
if (-not (Test-Path -LiteralPath $previewFile)) { throw "Workshop 预览图不存在：$previewFile" }
foreach ($name in $requiredFiles) {
    $path = Join-Path $contentFolder $name
    if (-not (Test-Path -LiteralPath $path)) { throw "Workshop 内容缺少 $name" }
}

# ==================================================================
# 第四步：打印上传信息
# ==================================================================

Write-Host "======== NinjaMod Workshop 上传 ========" -ForegroundColor Cyan
Write-Host "游戏：   Slay the Spire 2（App ID: 2868840）" -ForegroundColor Cyan
Write-Host "版本：   $($manifest.version)" -ForegroundColor Cyan
Write-Host "配置：   $steamUploadVdfPath" -ForegroundColor Cyan
if ($isExistingWorkshopItem) {
    Write-Host "保护：   本次不会覆盖 Steam 网页上的标题和描述" -ForegroundColor Cyan
}
Write-Host ""

# 首次上传提醒
if ($publishedIdMatch.Success -and $publishedIdMatch.Groups[1].Value -eq "0") {
    Write-Host "首次上传：将创建新的 Workshop 条目。" -ForegroundColor Yellow
    Write-Host "上传成功后，请把返回的 Workshop ID 填回 workshop/upload.vdf 中。" -ForegroundColor Yellow
    Write-Host ""
}

# 只校验模式
if ($ValidateOnly) {
    Write-Host "校验通过，Workshop 内容已就绪。" -ForegroundColor Green
    exit 0
}

# ==================================================================
# 第五步：准备 SteamCMD
# ==================================================================

$steamcmd = if ($SteamCmdPath) { $SteamCmdPath } elseif ($env:STEAMCMD_PATH) { $env:STEAMCMD_PATH } else { "D:\steamcmd\steamcmd.exe" }
if (-not (Test-Path $steamcmd)) {
    $steamcmdDir = Split-Path -Parent $steamcmd
    $zipPath = Join-Path $env:TEMP "steamcmd.zip"
    Write-Host "没找到 steamcmd.exe，正在下载 SteamCMD..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Force -Path $steamcmdDir | Out-Null
    Invoke-WebRequest "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip" -OutFile $zipPath
    Expand-Archive $zipPath $steamcmdDir -Force
    Remove-Item -LiteralPath $zipPath -Force
}
if (-not (Test-Path $steamcmd)) { throw "下载后仍然找不到 steamcmd.exe：$steamcmd" }

# ==================================================================
# 第六步：获取 Steam 账号密码
# ==================================================================

# 优先从 .env 文件读取（与脚本同目录），支持引号包裹的值。
# .env 格式：KEY=VALUE，每行一条，# 开头为注释。
$envFile = Join-Path $PSScriptRoot ".env"
Import-DotEnv $envFile

$user = Resolve-SteamUser $SteamUser
$pass = Resolve-SteamPass $SteamPass

# ==================================================================
# 第七步：运行 SteamCMD 上传
# ==================================================================

Write-Host ""
Write-Host "正在启动 SteamCMD..." -ForegroundColor Yellow
Write-Host "如果提示输入 Steam Guard 验证码，输完按回车即可。" -ForegroundColor Yellow
Write-Host ""

$cmdArgs = @(
    "+login", $user, $pass,
    "+workshop_build_item", $steamUploadVdfPath,
    "+quit"
)
Write-Host "执行：steamcmd +login $user *** +workshop_build_item $(Split-Path $steamUploadVdfPath -Leaf) +quit" -ForegroundColor DarkGray
Write-Host ""

# SteamCMD 连接内容分发服务器（steamcontent.com）时经常因网络波动超时，
# 这类失败大多是间歇性的，自动重试几次通常能成功。
$uploadExitCode = 1
for ($attempt = 1; $attempt -le $MaxRetries; $attempt++) {
    if ($attempt -gt 1) {
        Write-Host ""
        Write-Host "第 $attempt/$MaxRetries 次尝试上传（上一次因网络超时失败）..." -ForegroundColor Yellow
        Start-Sleep -Seconds 5
    }

    & $steamcmd @cmdArgs
    $uploadExitCode = $LASTEXITCODE
    if ($uploadExitCode -eq 0) { break }

    Write-Host "上传失败（退出码 $uploadExitCode）。" -ForegroundColor Yellow
}

# ------------------------------------------------------------------
# 检查上传结果
# ------------------------------------------------------------------

if ($uploadExitCode -eq 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  上传成功！" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "你的 mod 已经发布到 Steam Workshop 了。" -ForegroundColor Green
    Write-Host "访问地址：https://steamcommunity.com/app/2868840/workshop/" -ForegroundColor Green
    Write-Host ""
    Write-Host "【重要】首次上传后，记下返回的 PublishedFileID，" -ForegroundColor Yellow
    Write-Host "填到 workshop\upload.vdf 的 publishedfileid 字段里（把 0 替换掉），" -ForegroundColor Yellow
    Write-Host "以后更新就不用重新创建条目了。" -ForegroundColor Yellow
} else {
    Write-Host ""
    Write-Host "[失败] 上传返回错误码 $uploadExitCode（已重试 $MaxRetries 次）" -ForegroundColor Red
    Write-Host "常见原因：" -ForegroundColor Red
    Write-Host "  1. 密码或 Steam Guard 验证码错误" -ForegroundColor Red
    Write-Host "  2. Steam Guard 验证码过期了" -ForegroundColor Red
    Write-Host "  3. 该 Steam 账号没有购买 Slay the Spire 2" -ForegroundColor Red
    Write-Host "  4. 网络连不上 Steam 内容服务器（steamcontent.com）——" -ForegroundColor Red
    Write-Host "     日志里若出现 'Timeout uploading manifest' 或 'Failed to download manifest'," -ForegroundColor Red
    Write-Host "     说明是网络/CDN 问题（国内网络常见），建议挂加速器/代理后重试。" -ForegroundColor Red
    Write-Host "  详细日志见：D:\steamcmd\logs\workshop_log.txt" -ForegroundColor DarkGray
    exit $uploadExitCode
}
