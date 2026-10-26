# Builds and runs the engine self test.
# Applies and reverts a harmless tweak (startup delay), checks the journal,
# generates a report, verifies the power-setting readers, then restores your
# original journal file. Nothing else on the system is modified.

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { throw 'csc.exe not found (is .NET Framework 4 installed?)' }

$fw = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$refs = @(
    'mscorlib.dll',
    'System.dll',
    'System.Core.dll',
    'System.Drawing.dll',
    'System.Windows.Forms.dll',
    'System.Management.dll'
) | ForEach-Object { '/r:' + (Join-Path $fw $_) }

$src = @(Get-ChildItem (Join-Path $root 'src') -Filter *.cs |
    Where-Object { $_.Name -ne 'Program.cs' } |
    ForEach-Object { $_.FullName })
$src += (Join-Path $PSScriptRoot 'SelfTest.cs')

$exe = Join-Path $PSScriptRoot 'selftest.exe'
& $csc /nologo /target:exe /platform:anycpu /codepage:65001 /main:KDT.SelfTest "/out:$exe" $refs $src
if ($LASTEXITCODE -ne 0) { throw "Build failed (exit $LASTEXITCODE)" }

& $exe
exit $LASTEXITCODE
