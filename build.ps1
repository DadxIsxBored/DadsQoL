param([switch]$Package)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$projectPath = Join-Path $root 'DadsQoL.csproj'
$packageRoot = Join-Path $root 'package'
$dllPath = Join-Path $root 'bin\Release\net48\DadsQoL.dll'
$manifestPath = Join-Path $packageRoot 'manifest.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json

dotnet build $projectPath -c Release
if ($LASTEXITCODE -ne 0) { throw "DadsQoL source build exited with code $LASTEXITCODE" }

$assembly = [System.Reflection.Assembly]::ReflectionOnlyLoadFrom($dllPath)
if ($assembly.GetName().Name -ne 'DadsQoL') {
    throw "Unexpected assembly name: $($assembly.GetName().Name)"
}

if ($Package) {
    $packageEntries = [ordered]@{
        'DadsQoL.dll' = $dllPath
        'manifest.json' = $manifestPath
        'README.md' = (Join-Path $packageRoot 'README.md')
        'icon.png' = (Join-Path $packageRoot 'icon.png')
        'CHANGELOG.md' = (Join-Path $root 'CHANGELOG.md')
        'LICENSE' = (Join-Path $root 'LICENSE')
        'THIRD_PARTY.md' = (Join-Path $root 'THIRD_PARTY.md')
        'MassFarming-LICENSE.txt' = (Join-Path $root 'THIRD_PARTY_LICENSES\MassFarming-LICENSE.txt')
    }

    foreach ($sourcePath in $packageEntries.Values) {
        if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
            throw "Required package file is missing: $sourcePath"
        }
    }

    Add-Type -AssemblyName System.Drawing
    $icon = [System.Drawing.Image]::FromFile($packageEntries['icon.png'])
    try {
        if ($icon.Width -ne 256 -or $icon.Height -ne 256) {
            throw "Thunderstore icon.png must be exactly 256x256; found $($icon.Width)x$($icon.Height)."
        }
        if ($icon.RawFormat.Guid -ne [System.Drawing.Imaging.ImageFormat]::Png.Guid) {
            throw 'Thunderstore icon.png is not a PNG image.'
        }
    }
    finally {
        $icon.Dispose()
    }

    $assemblyVersion = $assembly.GetName().Version
    $assemblySemVer = "$($assemblyVersion.Major).$($assemblyVersion.Minor).$($assemblyVersion.Build)"
    if ($assemblySemVer -ne $manifest.version_number) {
        throw "Assembly version $assemblySemVer does not match manifest version $($manifest.version_number)."
    }

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $distRoot = Join-Path $root 'dist'
    $artifactArchiveRoot = Join-Path $root 'Archive\package-builds'
    New-Item -ItemType Directory -Path $distRoot -Force | Out-Null
    New-Item -ItemType Directory -Path $artifactArchiveRoot -Force | Out-Null

    $zipPath = Join-Path $distRoot "DadsQoL-$($manifest.version_number).zip"
    foreach ($previousZip in Get-ChildItem -LiteralPath $distRoot -File -Filter 'DadsQoL-*.zip') {
        $stamp = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
        Move-Item -LiteralPath $previousZip.FullName -Destination (Join-Path $artifactArchiveRoot "$($previousZip.BaseName)-$stamp.zip")
    }

    $zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($entry in $packageEntries.GetEnumerator()) {
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $zip,
                $entry.Value,
                $entry.Key,
                [System.IO.Compression.CompressionLevel]::Optimal
            ) | Out-Null
        }
    }
    finally {
        $zip.Dispose()
    }

    $zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        $entryNames = @($zip.Entries | ForEach-Object FullName)
        foreach ($requiredEntry in $packageEntries.Keys) {
            if ($requiredEntry -notin $entryNames) {
                throw "Package ZIP is missing required root entry: $requiredEntry"
            }
        }
        if ($entryNames | Where-Object { $_ -match '[/\\]' }) {
            throw 'Package ZIP contains a nested directory.'
        }
    }
    finally {
        $zip.Dispose()
    }

    Write-Host "Thunderstore package: $zipPath"
    Write-Host "SHA256: $((Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash)"
}
