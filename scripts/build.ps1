<#
.SYNOPSIS
    Restore and build the Ninja mod (Debug by default).
.PARAMETER Configuration
    Build configuration: Debug (default) or Release.
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'  # 编译模式：Debug（默认）或 Release
)


$ErrorActionPreference = 'Stop' # 遇到错误立刻停，不要闷头往下跑
# 脚本所在目录的上一级就是仓库根目录
$RepoRoot = Split-Path -Parent $PSScriptRoot
# NinjaMod 的项目文件路径
$proj = Join-Path $RepoRoot 'NinjaMod.csproj'

Write-Host "=== 正在编译 NinjaMod（$Configuration） ===" -ForegroundColor Cyan

# 第一步：还原 NuGet 依赖包
& dotnet restore $proj
if ($LASTEXITCODE -ne 0) { throw "还原依赖包失败（退出码 $LASTEXITCODE）。" }

# 第二步：编译项目
& dotnet build $proj -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "编译失败（退出码 $LASTEXITCODE）。" }

Write-Host '=== 编译成功 ===' -ForegroundColor Green
