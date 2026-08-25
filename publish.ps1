[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Write-Error "'git' command not found in PATH."
    exit 1
}

$currentBranch = (git rev-parse --abbrev-ref HEAD 2>$null)
if (-not $currentBranch) {
    Write-Error "Unable to determine Git branch. Is this a Git repository?"
    exit 1
}
$currentBranch = $currentBranch.Trim()

Write-Host "==> Current Git branch: $currentBranch" -ForegroundColor Cyan

if ($currentBranch -ne "main") {
    Write-Warning "You are on branch '$currentBranch', not 'main'."
    Write-Warning "Publishing from a non-main branch may generate prerelease or development packages."
    
    $response = Read-Host "Do you want to proceed with packaging anyway? [y/N]"
    $response = if ($response) { $response.Trim().ToLowerInvariant() } else { "" }
    
    if ($response -ne "y" -and $response -ne "yes") {
        Write-Host "==> Packaging aborted by user." -ForegroundColor Yellow
        exit 1
    }
}

Write-Host "`n==> Executing: dotnet pack -c Release -o release --include-symbols" -ForegroundColor Green
dotnet pack -c Release -o release --include-symbols

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet pack failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "`n==> Packaging completed successfully. Artifacts available in ./release/" -ForegroundColor Green
