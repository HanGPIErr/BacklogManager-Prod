# =====================================
# Script de publication d'une nouvelle version
# =====================================
param(
    [Parameter(Mandatory=$true)]
    [string]$Version,
    
    [Parameter(Mandatory=$false)]
    [string]$Changelog = "Nouvelle version",
    
    [Parameter(Mandatory=$false)]
    [bool]$Mandatory = $false
)

Write-Host "`n=== PUBLICATION VERSION $Version ===" -ForegroundColor Cyan

# 1. Créer le ZIP avec CreateRelease.ps1
Write-Host "`n[1/4] Création du package..." -ForegroundColor Yellow
& "$PSScriptRoot\CreateRelease.ps1"
$releaseZip = Join-Path $projectRoot "BacklogManager-Release.zip"
if (-not (Test-Path $releaseZip)) {
    Write-Host "ERREUR: Le ZIP n'a pas été créé!" -ForegroundColor Red
    exit 1
}

# 2. Renommer le ZIP avec le numéro de version
$versionedZip = Join-Path $projectRoot "BacklogManager-v$Version.zip"
if (Test-Path $versionedZip) {
    Remove-Item $versionedZip -Force
}
Rename-Item $releaseZip $versionedZip
Write-Host "Package créé: $versionedZip" -ForegroundColor Green

# 3. Lire le chemin du serveur de mise à jour depuis config.ini
Write-Host "`n[2/4] Lecture de la configuration..." -ForegroundColor Yellow
$projectRoot = Resolve-Path "$PSScriptRoot\.."
$configPath = Join-Path $projectRoot "config.ini"
$updateServerPath = $null

if (Test-Path $configPath) {
    $lines = Get-Content $configPath -Encoding UTF8
    foreach ($line in $lines) {
        if ($line -match '^UpdateServerPath\s*=\s*(.+)$') {
            $updateServerPath = $Matches[1].Trim().Trim('"').Trim("'")
            break
        }
    }
}

if ([string]::IsNullOrEmpty($updateServerPath)) {
    Write-Host "ERREUR: UpdateServerPath non trouvé dans config.ini!" -ForegroundColor Red
    exit 1
}

Write-Host "Serveur de mise à jour: $updateServerPath" -ForegroundColor Gray

# Créer le dossier s'il n'existe pas
if (-not (Test-Path $updateServerPath)) {
    Write-Host "Création du dossier: $updateServerPath" -ForegroundColor Gray
    New-Item -ItemType Directory -Path $updateServerPath -Force | Out-Null
}

# 4. Créer le fichier version.json
Write-Host "`n[3/4] Création de version.json..." -ForegroundColor Yellow
$versionInfo = @{
    version = $Version
    releaseDate = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss")
    downloadUrl = Join-Path $updateServerPath $versionedZipName
    mandatory = $Mandatory
    changelog = $Changelog
    minimumVersion = "0.1.0"
}

$jsonContent = $versionInfo | ConvertTo-Json -Depth 10
$jsonPath = Join-Path $updateServerPath "version.json"
$jsonContent | Out-File -FilePath $jsonPath -Encoding UTF8 -Force

Write-Host "version.json créé:" -ForegroundColor Green
Write-Host $jsonContent -ForegroundColor Gray

# 5. Copier le ZIP vers le serveur de mise à jour
Write-Host "`n[4/4] Copie du package..." -ForegroundColor Yellow
$versionedZipName = Split-Path $versionedZip -Leaf
$destZip = Join-Path $updateServerPath $versionedZipName
Copy-Item $versionedZip -Destination $destZip -Force

Write-Host "`n=== PUBLICATION TERMINÉE ===" -ForegroundColor Green
Write-Host "`nFichiers publiés:" -ForegroundColor Cyan
Write-Host "  - $destZip" -ForegroundColor Gray
Write-Host "  - $jsonPath" -ForegroundColor Gray
Write-Host "`nLes utilisateurs recevront la mise à jour au prochain démarrage." -ForegroundColor White

# Nettoyage local
if (Test-Path $versionedZip) {
    Write-Host "`nNettoyage du ZIP local..." -ForegroundColor Gray
    Remove-Item $versionedZip -Force
}

Write-Host "`nPour annuler cette version, supprimez:" -ForegroundColor Yellow
Write-Host "  $jsonPath" -ForegroundColor Gray
