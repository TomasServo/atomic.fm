param(
    [string]$RepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path,
    [string]$Version = '0.1.0'
)

$ErrorActionPreference = 'Stop'

$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$name = "atomic.fm-v$Version-$stamp"
$releaseRoot = Join-Path $RepoRoot 'Release'
$outDir = Join-Path $releaseRoot $name
$zip = "$outDir.zip"
$bin = Join-Path $RepoRoot 'ClientPlugin\bin\Release'

New-Item -ItemType Directory -Path $outDir -Force | Out-Null

Copy-Item -LiteralPath (Join-Path $RepoRoot 'LICENSE') -Destination (Join-Path $outDir 'LICENSE') -Force
Copy-Item -LiteralPath (Join-Path $RepoRoot 'README.md') -Destination (Join-Path $outDir 'README.md') -Force
Copy-Item -LiteralPath (Join-Path $RepoRoot 'AtomicRadio.xml') -Destination (Join-Path $outDir 'plugin.xml') -Force
Copy-Item -LiteralPath (Join-Path $bin 'atomic.fm.dll') -Destination (Join-Path $outDir 'plugin.dll') -Force
Copy-Item -LiteralPath (Join-Path $bin 'atomic.fm.dll.config') -Destination (Join-Path $outDir 'plugin.dll.config') -Force

$dependencies = @(
    'Microsoft.Win32.Registry.dll',
    'NAudio.Asio.dll',
    'NAudio.Core.dll',
    'NAudio.dll',
    'NAudio.Midi.dll',
    'NAudio.Wasapi.dll',
    'NAudio.WinForms.dll',
    'NAudio.WinMM.dll',
    'System.Security.AccessControl.dll',
    'System.Security.Principal.Windows.dll'
)

foreach ($dependency in $dependencies) {
    Copy-Item -LiteralPath (Join-Path $bin $dependency) -Destination (Join-Path $outDir $dependency) -Force
}

Compress-Archive -Path (Join-Path $outDir '*') -DestinationPath $zip -Force

[pscustomobject]@{
    ReleaseFolder = $outDir
    Zip = $zip
    ZipLength = (Get-Item -LiteralPath $zip).Length
}
