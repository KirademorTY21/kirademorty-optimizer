param(
    [switch]$Dev
)

$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { throw 'csc.exe not found' }

$distDir = Join-Path $root 'dist'
New-Item -ItemType Directory -Path $distDir -Force | Out-Null

$name = if ($Dev) { 'KDT-dev.exe' } else { 'KiraDemorTY-Optimizer.exe' }
$out = Join-Path $distDir $name

$src = @(Get-ChildItem (Join-Path $root 'src') -Filter *.cs | ForEach-Object { $_.FullName })
if ($src.Count -eq 0) { throw 'No source files found in src\' }

$fw = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$refs = @(
    'mscorlib.dll',
    'System.dll',
    'System.Core.dll',
    'System.Drawing.dll',
    'System.Windows.Forms.dll',
    'System.Management.dll'
) | ForEach-Object { '/r:' + (Join-Path $fw $_) }

$argList = @(
    '/nologo',
    '/target:winexe',
    '/platform:anycpu',
    '/optimize+',
    '/codepage:65001',
    '/warn:4',
    ('/out:' + $out),
    ('/win32icon:' + (Join-Path $root 'app.ico'))
)
if (-not $Dev) {
    $argList += ('/win32manifest:' + (Join-Path $root 'app.manifest'))
}
$argList += $refs
$argList += $src

& $csc $argList
if ($LASTEXITCODE -ne 0) { throw "Build failed (exit $LASTEXITCODE)" }

$exe = Get-Item $out
Write-Host ''
Write-Host ('BUILD OK  ->  {0}' -f $exe.FullName) -ForegroundColor Green
Write-Host ('Size: {0} KB' -f [math]::Round($exe.Length / 1KB, 1)) -ForegroundColor Green
Write-Host ('Version: {0}' -f $exe.VersionInfo.FileVersion) -ForegroundColor Green
