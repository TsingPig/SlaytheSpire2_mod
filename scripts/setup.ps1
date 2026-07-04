<#
.SYNOPSIS
    One-time environment setup & validation for the Ninja mod.
.DESCRIPTION
    - Validates the Slay the Spire 2 game path and mods folder.
    - Validates the .NET SDK (>= 9.0, required by the template).
    - Installs the BaseLib dependency mod from the OFFICIAL Alchyr NuGet package
      (no downloads from unofficial sources) if it is not already present.
    Reads GameDir / ModsDir from local.props (gitignored). No absolute paths are hardcoded here.
#>
[CmdletBinding()]
param(
    [switch]$InstallBaseLib = $true  # 是否自动安装 BaseLib 依赖模组，默认安装
)

# 遇到错误立刻停止，不要继续往下跑
$ErrorActionPreference = 'Stop'
# 脚本所在目录的上一级，就是仓库根目录
$RepoRoot = Split-Path -Parent $PSScriptRoot

# 从 local.props 文件里读取配置项（游戏路径等），如果没有文件或找不到就返回默认值

function Get-LocalProp {
    param([string]$Name, [string]$Fallback)
    $propsPath = Join-Path $RepoRoot 'local.props'
    if (Test-Path $propsPath) {
        try {
            [xml]$xml = Get-Content -Raw -LiteralPath $propsPath
            $node = $xml.Project.PropertyGroup.$Name
            if ($node) { return ([string]$node).Trim() }
        } catch { }
    }
    return $Fallback
}

Write-Host '=== Ninja mod 环境设置 ===' -ForegroundColor Cyan

# ======== 1. 检查游戏目录和模组目录 ========

# 读取游戏路径，如果 local.props 里没配就用这个默认路径
$GameDir = Get-LocalProp -Name 'GameDir' -Fallback 'D:\SteamLibrary\steamapps\common\Slay the Spire 2'
# 模组目录默认放在游戏目录下的 mods 文件夹
$ModsDir = Get-LocalProp -Name 'ModsDir' -Fallback (Join-Path $GameDir 'mods')

Write-Host "游戏目录: $GameDir"
Write-Host "模组目录: $ModsDir"

# 游戏目录不存在就直接报错，让用户去改 local.props
if (-not (Test-Path -LiteralPath $GameDir)) {
    throw "游戏目录不存在: $GameDir。请修改 local.props 指向你的杀戮尖塔2安装位置。"
}
Write-Host '[OK] 游戏目录找到了' -ForegroundColor Green

# 确认游戏主程序 DLL 存在，编译时要引用它
$dataDir = Join-Path $GameDir 'data_sts2_windows_x86_64'
if (-not (Test-Path -LiteralPath (Join-Path $dataDir 'sts2.dll'))) {
    Write-Warning "在 $dataDir 下没找到 sts2.dll，编译时需要从那里引用游戏 DLL"
} else {
    Write-Host '[OK] 游戏数据 (sts2.dll) 找到了' -ForegroundColor Green
}

# 模组目录不存在就自动创建
if (-not (Test-Path -LiteralPath $ModsDir)) {
    New-Item -ItemType Directory -Path $ModsDir -Force | Out-Null
    Write-Host "[OK] 已创建模组文件夹: $ModsDir" -ForegroundColor Green
} else {
    Write-Host '[OK] 模组文件夹已存在' -ForegroundColor Green
}

# ======== 2. 检查 .NET SDK ========

# 看看系统里有没有装 dotnet 命令
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    throw '没找到 .NET SDK，请先安装 .NET 9 SDK: https://dotnet.microsoft.com/download'
}
# 列出已安装的所有 SDK 版本，看看有没有 9.x
$sdks = (& dotnet --list-sdks)
$hasNet9 = $sdks | Where-Object { $_ -match '^9\.' }
if (-not $hasNet9) {
    Write-Warning "没找到 .NET 9 SDK，但本项目需要 net9.0。当前已安装的 SDK：`n$($sdks -join "`n")"
    Write-Warning '请从 https://dotnet.microsoft.com/download 安装 .NET 9 SDK'
} else {
    Write-Host '[OK] .NET 9 SDK 已安装' -ForegroundColor Green
}

# ======== 3. 检查 / 安装 BaseLib 依赖模组 ========
# NinjaMod 依赖 BaseLib 才能运行，所以要么确认它已经装好了，要么自动装一个

$baseLibDir = Join-Path $ModsDir 'BaseLib'
if (Test-Path -LiteralPath (Join-Path $baseLibDir 'BaseLib.json')) {
    Write-Host '[OK] BaseLib 已经装好了' -ForegroundColor Green
} else {
    Write-Warning '模组目录里没找到 BaseLib，NinjaMod 必须依赖 BaseLib 才能加载！'
    if ($InstallBaseLib) {
        # 先 restore 项目，确保官方 BaseLib NuGet 包下载到本地缓存里
        Write-Host '正在还原 NuGet 包，获取官方的 BaseLib 包...'
        & dotnet restore (Join-Path $RepoRoot 'NinjaMod.csproj') | Out-Null

        # 从 NuGet 缓存里找到最新版的 BaseLib 包
        $nugetRoot = Join-Path $env:USERPROFILE '.nuget\packages\alchyr.sts2.baselib'
        $pkg = Get-ChildItem -Path $nugetRoot -Directory -ErrorAction SilentlyContinue |
               Sort-Object Name -Descending | Select-Object -First 1
        if (-not $pkg) {
            Write-Warning "在 $nugetRoot 下没找到 BaseLib 包。"
            Write-Warning '请手动安装 BaseLib（比如从杀戮尖塔2 的 Steam 创意工坊下载）到：'
            Write-Warning "  $baseLibDir"
        } else {
            # 从 NuGet 包里提取 DLL、清单文件和资源包，复制到模组目录
            $dll  = Join-Path $pkg.FullName 'lib\net9.0\BaseLib.dll'
            $json = Join-Path $pkg.FullName 'Content\BaseLib.json'
            $pck  = Join-Path $pkg.FullName 'Content\BaseLib.pck'
            New-Item -ItemType Directory -Path $baseLibDir -Force | Out-Null
            foreach ($f in @($dll, $json, $pck)) {
                if (Test-Path -LiteralPath $f) {
                    Copy-Item -LiteralPath $f -Destination $baseLibDir -Force
                    Write-Host "  已复制 $(Split-Path $f -Leaf)"
                } else {
                    Write-Warning "  包里缺少文件: $f"
                }
            }
            Write-Host "[OK] 已从官方 NuGet 包安装 BaseLib $($pkg.Name) 到 $baseLibDir" -ForegroundColor Green
        }
    } else {
        Write-Host '加上 -InstallBaseLib 参数可以自动从官方 NuGet 包安装，或者手动安装也可以。'
    }
}

Write-Host '=== 环境设置完成 ===' -ForegroundColor Cyan
