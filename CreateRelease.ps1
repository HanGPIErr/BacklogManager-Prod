# Script de création du package de release
$zipPath = "BacklogManager-Release.zip"

# Supprimer l'ancien ZIP si existant
if (Test-Path $zipPath) { 
    Remove-Item $zipPath -Force 
}

$sourceDir = "bin\Release"
$excludePatterns = @('*.log', '*.pdb', 'Logs', 'Backups', 'data', 'permissions_log.txt')

# Fichiers d'installation à inclure depuis la racine
$installFiles = @(
    'Install.ps1',
    'InstallBacklogManager.vbs',
    'Start-BacklogManager.ps1',
    'StartBacklogManager.vbs',
    'Uninstall.ps1'
)

# Créer répertoire temporaire
$tempDir = Join-Path $env:TEMP ("BacklogRelease_" + [Guid]::NewGuid().ToString())
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

Write-Host "Copie des fichiers depuis bin\Release..." -ForegroundColor Cyan

# Copier les fichiers de bin\Release avec exclusions
Get-ChildItem -Path $sourceDir -Recurse | ForEach-Object {
    $shouldInclude = $true
    $relPath = $_.FullName.Substring((Resolve-Path $sourceDir).Path.Length + 1)
    
    foreach ($pattern in $excludePatterns) {
        if ($pattern -notlike '*\*' -and $pattern -notlike '*/*') {
            if ($_.Name -like $pattern -or $relPath -like "*\$pattern\*" -or $relPath -like "*\$pattern") {
                $shouldInclude = $false
                break
            }
        } else {
            if ($relPath -like $pattern) {
                $shouldInclude = $false
                break
            }
        }
    }
    
    if ($shouldInclude -and -not $_.PSIsContainer) {
        $destPath = Join-Path $tempDir $relPath
        $destDir = Split-Path $destPath
        if (-not (Test-Path $destDir)) {
            New-Item -ItemType Directory -Path $destDir -Force | Out-Null
        }
        Copy-Item $_.FullName -Destination $destPath -Force
    }
}

Write-Host "Copie des fichiers d'installation..." -ForegroundColor Cyan

# Copier les fichiers d'installation à la racine du ZIP
foreach ($file in $installFiles) {
    if (Test-Path $file) {
        Copy-Item $file -Destination $tempDir -Force
        Write-Host "  + $file" -ForegroundColor Gray
    } else {
        Write-Host "  ! $file introuvable" -ForegroundColor Yellow
    }
}

Write-Host "Compression..." -ForegroundColor Cyan

# Créer le ZIP
Compress-Archive -Path "$tempDir\*" -DestinationPath $zipPath -CompressionLevel Optimal

# Nettoyer
Remove-Item $tempDir -Recurse -Force

# Info finale
$zipInfo = Get-Item $zipPath
Write-Host "`nZIP cree: $($zipInfo.Name) - Taille: $([math]::Round($zipInfo.Length / 1MB, 2)) MB" -ForegroundColor Green
Write-Host "Contenu:" -ForegroundColor Cyan
Add-Type -Assembly System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($zipInfo.FullName)
$zip.Entries | Select-Object -First 20 | ForEach-Object { Write-Host "  - $($_.FullName)" -ForegroundColor Gray }
if ($zip.Entries.Count -gt 20) {
    Write-Host "  ... et $($zip.Entries.Count - 20) autres fichiers" -ForegroundColor Gray
}
$zip.Dispose()
