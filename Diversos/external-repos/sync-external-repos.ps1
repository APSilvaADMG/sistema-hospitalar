# Clone (shallow) or download ZIP of reference repos listed in manifest.json
$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Manifest = Join-Path $Root "manifest.json"
$Data = Get-Content $Manifest -Raw | ConvertFrom-Json

function Resolve-GitExe {
    $cmd = Get-Command git -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    $candidates = @(
        "$env:ProgramFiles\Git\bin\git.exe",
        "${env:ProgramFiles(x86)}\Git\bin\git.exe",
        "$env:LOCALAPPDATA\Programs\Git\bin\git.exe"
    )
    foreach ($p in $candidates) {
        if (Test-Path $p) { return $p }
    }
    return $null
}

function Download-RepoZip {
    param(
        [string]$RepoUrl,
        [string]$Dest
    )
    $name = ($RepoUrl -replace '.*/', '')
    $branches = @('master', 'main')
    foreach ($branch in $branches) {
        $zipUrl = "$RepoUrl/archive/refs/heads/$branch.zip"
        $zipPath = Join-Path $env:TEMP "$name-$branch.zip"
        try {
            Write-Host "DOWNLOAD $zipUrl"
            Invoke-WebRequest -Uri $zipUrl -OutFile $zipPath -UseBasicParsing
            $extractRoot = Join-Path $env:TEMP "$name-$branch"
            if (Test-Path $extractRoot) { Remove-Item $extractRoot -Recurse -Force }
            Expand-Archive -Path $zipPath -DestinationPath $extractRoot -Force
            $inner = Get-ChildItem $extractRoot -Directory | Select-Object -First 1
            if (-not $inner) { throw "Empty ZIP archive" }
            $parent = Split-Path -Parent $Dest
            if (-not (Test-Path $parent)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
            Move-Item $inner.FullName $Dest
            Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
            Remove-Item $extractRoot -Recurse -Force -ErrorAction SilentlyContinue
            return $true
        }
        catch {
            Write-Host "  branch $branch unavailable"
        }
    }
    return $false
}

$git = Resolve-GitExe

foreach ($repo in $Data.repositories) {
    $dest = Join-Path $Root $repo.clonePath
    if (Test-Path $dest) {
        Write-Host "SKIP $($repo.id) - already at $dest"
        continue
    }

    if ($git) {
        $parent = Split-Path -Parent $dest
        if (-not (Test-Path $parent)) {
            New-Item -ItemType Directory -Path $parent -Force | Out-Null
        }
        Write-Host "CLONE $($repo.url) -> $dest"
        & $git clone --depth 1 $repo.url $dest
        continue
    }

    Write-Host "Git not found - trying ZIP for $($repo.id)"
    $ok = Download-RepoZip -RepoUrl $repo.url -Dest $dest
    if (-not $ok) {
        Write-Warning "Failed to fetch $($repo.id). Install Git or download manually from $($repo.url)"
    }
}

Write-Host "Done. Field mappings: ReportFieldMappings.cs and web/src/data/reportFieldMappings.ts"
Write-Host "See template-snapshots/README.md for quick reference."
