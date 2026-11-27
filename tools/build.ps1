# Liuli 构建脚本：补 UTF-8 BOM（csc 对无 BOM 文件按 ANSI 解析，中文会乱码），再编译成单文件 exe。
# 用法：pwsh -File tools\build.ps1 [-Run] [-Clean]
param(
    [switch]$Run,
    [switch]$Clean
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (-not $root) { $root = 'E:\meihuaDesktop' }
$src = Join-Path $root 'src'
$bin = Join-Path $root 'bin'
$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { throw "找不到 csc.exe: $csc" }
New-Item -ItemType Directory -Force -Path $bin | Out-Null

$utf8bom = New-Object System.Text.UTF8Encoding($true)
$files = Get-ChildItem -Path $src -Filter *.cs | Sort-Object Name
foreach ($f in $files) {
    $bytes = [System.IO.File]::ReadAllBytes($f.FullName)
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { continue }
    $text = [System.IO.File]::ReadAllText($f.FullName, [System.Text.Encoding]::UTF8)
    [System.IO.File]::WriteAllText($f.FullName, $text, $utf8bom)
    Write-Host "  + BOM $($f.Name)"
}

if ($Clean) { Remove-Item -Path (Join-Path $bin 'Liuli.exe') -Force -ErrorAction SilentlyContinue }

$list = $files | ForEach-Object { $_.FullName }
$args = @(
    '/nologo', '/target:winexe', '/optimize+', '/platform:anycpu', '/utf8output',
    "/out:$bin\Liuli.exe",
    '/r:System.dll', '/r:System.Drawing.dll', '/r:System.Windows.Forms.dll',
    '/r:System.IO.Compression.dll', '/r:System.IO.Compression.FileSystem.dll', '/r:System.Management.dll'
) + $list
$sw = [Diagnostics.Stopwatch]::StartNew()
& $csc @args
$code = $LASTEXITCODE
$sw.Stop()
if ($code -ne 0) {
    Write-Host "编译失败，退出码 $code，用时 $([math]::Round($sw.Elapsed.TotalSeconds,1))s" -ForegroundColor Red
    exit $code
}
$exe = Join-Path $bin 'Liuli.exe'
$size = [math]::Round((Get-Item $exe).Length / 1KB, 1)
Write-Host "编译成功 -> $exe ($size KB, $([math]::Round($sw.Elapsed.TotalSeconds,1))s)" -ForegroundColor Green
if ($Run) { Start-Process $exe }
