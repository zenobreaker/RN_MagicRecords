param()
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$packageRoot = Join-Path $projectRoot 'Packages/com.thewheeler.toolkit'
$manifest = Get-Content -LiteralPath (Join-Path $packageRoot 'package.json') -Raw | ConvertFrom-Json
if ($manifest.name -ne 'com.thewheeler.toolkit' -or $manifest.version -notmatch '^\d+\.\d+\.\d+$') {
    throw 'Unexpected package name or version.'
}
Get-Command tar.exe -ErrorAction Stop | Out-Null
$releaseRoot = Join-Path $projectRoot 'Releases'
$stagingRoot = Join-Path $projectRoot ('Library/TheWheelerPackaging/' + [guid]::NewGuid().ToString('N'))
foreach ($targetPath in @($releaseRoot, $stagingRoot)) {
    if (-not ([IO.Path]::GetFullPath($targetPath)).StartsWith($projectRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Output path must remain inside the project.'
    }
    New-Item -ItemType Directory -Path $targetPath -Force | Out-Null
}
try {
    Copy-Item -LiteralPath $packageRoot -Destination (Join-Path $stagingRoot 'package') -Recurse
    $archive = Join-Path $releaseRoot ("TheWheeler-{0}.tgz" -f $manifest.version)
    & tar.exe -czf $archive -C $stagingRoot package
    if ($LASTEXITCODE -ne 0) { throw 'Package archive creation failed.' }
    $hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
    [IO.File]::WriteAllText($archive + '.sha256', $hash + '  ' + [IO.Path]::GetFileName($archive) + "`n", [Text.UTF8Encoding]::new($false))
    Write-Output "Created $archive"
    Write-Output "SHA-256: $hash"
}
finally {
    $resolvedStaging = [IO.Path]::GetFullPath($stagingRoot)
    $allowedStaging = [IO.Path]::GetFullPath((Join-Path $projectRoot 'Library/TheWheelerPackaging')) + '\'
    if (-not $resolvedStaging.StartsWith($allowedStaging, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Staging cleanup path is outside the intended directory.'
    }
    Remove-Item -LiteralPath $resolvedStaging -Recurse -Force
}
