param(
    [string]$RepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path,
    [string]$PluginHub,
    [switch]$AllowDirty
)

$ErrorActionPreference = 'Stop'

$PluginId = 'TomasServo/atomic.fm'
$RepoId = 'TomasServo/atomic.fm'
$FriendlyName = 'atomic.fm'
$Author = 'TomasServo'
$Tooltip = 'Streams atomic.fm from Icecast into the Space Engineers client.'
$SourceDirectory = 'ClientPlugin'
$NAudioVersion = '2.2.1'
$DescriptionLimit = 1000
$Description = @'
All Ultralounge all the time.

atomic.fm adds client-side internet radio to Space Engineers through Pulsar. Mark any terminal block as a radio source by adding atomic.fm=true to Custom Data. Optional values: atomic.fm.range=35 and atomic.fm.volume=1.0.

The stream starts automatically near marked blocks with distance fade. Plain armor blocks do not work because they do not have terminal Custom Data. Audio is client-side; each player chooses whether to install and enable the plugin.
'@

function Invoke-Git {
    param([string[]]$Arguments)

    $output = & git @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw ($output -join [Environment]::NewLine)
    }

    return ($output -join [Environment]::NewLine).Trim()
}

function Get-OrCreateElement {
    param(
        [xml]$Document,
        [System.Xml.XmlElement]$Parent,
        [string]$Name
    )

    $node = $Parent.SelectSingleNode($Name)
    if ($null -eq $node) {
        $node = $Document.CreateElement($Name)
        [void]$Parent.AppendChild($node)
    }

    return [System.Xml.XmlElement]$node
}

function Set-ElementText {
    param(
        [xml]$Document,
        [System.Xml.XmlElement]$Parent,
        [string]$Name,
        [string]$Value
    )

    $node = Get-OrCreateElement -Document $Document -Parent $Parent -Name $Name
    $node.InnerText = $Value
}

function Update-Descriptor {
    param(
        [string]$Path,
        [string]$Commit
    )

    [xml]$xml = Get-Content -Raw -LiteralPath $Path
    $root = $xml.PluginData

    $xsi = 'http://www.w3.org/2001/XMLSchema-instance'
    [void]$root.SetAttribute('type', $xsi, 'GitHubPlugin')

    Set-ElementText -Document $xml -Parent $root -Name 'Id' -Value $PluginId
    Set-ElementText -Document $xml -Parent $root -Name 'RepoId' -Value $RepoId
    Set-ElementText -Document $xml -Parent $root -Name 'FriendlyName' -Value $FriendlyName
    Set-ElementText -Document $xml -Parent $root -Name 'Author' -Value $Author
    Set-ElementText -Document $xml -Parent $root -Name 'Tooltip' -Value $Tooltip
    Set-ElementText -Document $xml -Parent $root -Name 'Description' -Value $Description.Trim()
    Set-ElementText -Document $xml -Parent $root -Name 'Runtimes' -Value 'CLR;Mono'
    Set-ElementText -Document $xml -Parent $root -Name 'Platforms' -Value 'Windows'
    Set-ElementText -Document $xml -Parent $root -Name 'Hidden' -Value 'false'
    Set-ElementText -Document $xml -Parent $root -Name 'Commit' -Value $Commit

    $sourceDirectories = Get-OrCreateElement -Document $xml -Parent $root -Name 'SourceDirectories'
    $sourceDirectories.RemoveAll()
    $directory = $xml.CreateElement('Directory')
    $directory.InnerText = $SourceDirectory
    [void]$sourceDirectories.AppendChild($directory)

    $nugetReferences = Get-OrCreateElement -Document $xml -Parent $root -Name 'NuGetReferences'
    $nugetReferences.RemoveAll()
    $package = $xml.CreateElement('PackageReference')
    $package.SetAttribute('Include', 'NAudio')
    $package.SetAttribute('Version', $NAudioVersion)
    [void]$nugetReferences.AppendChild($package)

    $settings = New-Object System.Xml.XmlWriterSettings
    $settings.Indent = $true
    $settings.Encoding = New-Object System.Text.UTF8Encoding($false)

    $writer = [System.Xml.XmlWriter]::Create($Path, $settings)
    try {
        $xml.Save($writer)
    }
    finally {
        $writer.Dispose()
    }

    Add-Content -LiteralPath $Path -Value ''
}

function Test-Descriptor {
    param(
        [string]$Path,
        [string]$Commit
    )

    [xml]$xml = Get-Content -Raw -LiteralPath $Path
    $root = $xml.PluginData

    $checks = @{
        Id = $PluginId
        RepoId = $RepoId
        FriendlyName = $FriendlyName
        Author = $Author
        Hidden = 'false'
        Commit = $Commit
    }

    foreach ($name in $checks.Keys) {
        $actual = ($root.$name | Select-Object -First 1).'#text'
        if ([string]::IsNullOrWhiteSpace($actual)) {
            $actual = [string]($root.$name)
        }
        if ($actual.Trim() -ne $checks[$name]) {
            throw "$Path expected $name=$($checks[$name]) but found '$actual'"
        }
    }

    if ($root.Description.Length -gt $DescriptionLimit) {
        throw "$Path description is $($root.Description.Length) characters; limit is $DescriptionLimit"
    }

    if ($root.SourceDirectories.Directory -ne $SourceDirectory) {
        throw "$Path missing SourceDirectories/Directory=$SourceDirectory"
    }

    $package = $root.NuGetReferences.PackageReference |
        Where-Object { $_.Include -eq 'NAudio' -and $_.Version -eq $NAudioVersion } |
        Select-Object -First 1
    if ($null -eq $package) {
        throw "$Path missing NAudio $NAudioVersion NuGet reference"
    }
}

$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
$Descriptor = Join-Path $RepoRoot 'AtomicRadio.xml'

if (-not (Test-Path -LiteralPath $Descriptor)) {
    throw "Could not find descriptor: $Descriptor"
}

Push-Location $RepoRoot
try {
    if (-not $AllowDirty) {
        $status = Invoke-Git -Arguments @('status', '--short')
        if (-not [string]::IsNullOrWhiteSpace($status)) {
            throw 'Working tree has uncommitted changes. Commit or stash them, or rerun with -AllowDirty.'
        }
    }

    $commit = Invoke-Git -Arguments @('rev-parse', 'HEAD')
    if ($commit.Length -ne 40) {
        throw "Unexpected git commit hash: $commit"
    }

    Update-Descriptor -Path $Descriptor -Commit $commit
    Test-Descriptor -Path $Descriptor -Commit $commit

    Write-Host "Prepared $Descriptor"
    Write-Host "PluginHub commit: $commit"
    Write-Host "Description length: $($Description.Trim().Length)"

    if (-not [string]::IsNullOrWhiteSpace($PluginHub)) {
        $destination = Join-Path (Resolve-Path -LiteralPath $PluginHub).Path 'Plugins\atomic.fm.xml'
        New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
        Copy-Item -LiteralPath $Descriptor -Destination $destination -Force
        Write-Host "Copied submission XML to $destination"
    }
    else {
        Write-Host 'Submit AtomicRadio.xml as Plugins/atomic.fm.xml in StarCpt/PluginHub.'
    }
}
finally {
    Pop-Location
}
