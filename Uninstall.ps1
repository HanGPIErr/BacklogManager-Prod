# =====================================
# BacklogManager - Script de désinstallation
# =====================================

param(
    [string]$InstallPath = "C:\SGI_SUPPORT\APPLICATIONS\BacklogManager",
    [switch]$Silent
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  BacklogManager - Désinstallation" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Vérifier les droits administrateur
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "ERREUR: Ce script nécessite des droits administrateur." -ForegroundColor Red
    Write-Host "Relancez PowerShell en tant qu'administrateur." -ForegroundColor Yellow
    Write-Host ""
    Read-Host "Appuyez sur Entrée pour quitter"
    exit 1
}

if (-not (Test-Path $InstallPath)) {
    Write-Host "BacklogManager n'est pas installé dans: $InstallPath" -ForegroundColor Yellow
    Write-Host ""
    Read-Host "Appuyez sur Entrée pour quitter"
    exit 0
}

Write-Host "Chemin d'installation: $InstallPath" -ForegroundColor Yellow
Write-Host ""

if (-not $Silent) {
    Write-Host "ATTENTION: Cette action va supprimer BacklogManager." -ForegroundColor Red
    Write-Host "La base de données sera conservée dans le dossier 'data'." -ForegroundColor Yellow
    Write-Host ""
    $response = Read-Host "Voulez-vous continuer? (O/N)"
    if ($response -ne "O" -and $response -ne "o") {
        Write-Host "Désinstallation annulée." -ForegroundColor Green
        exit 0
    }
}

Write-Host ""
Write-Host "Fermeture de l'application si elle est en cours d'exécution..." -ForegroundColor Yellow
Get-Process -Name "BacklogManager" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

Write-Host "Suppression des raccourcis..." -ForegroundColor Yellow

# Supprimer le raccourci bureau
$DesktopPath = [Environment]::GetFolderPath("CommonDesktopDirectory")
$ShortcutPath = Join-Path $DesktopPath "BacklogManager.lnk"
if (Test-Path $ShortcutPath) {
    Remove-Item -Path $ShortcutPath -Force
    Write-Host "  ✓ Raccourci bureau supprimé" -ForegroundColor Gray
}

# Supprimer le dossier du menu Démarrer
$StartMenuPath = [Environment]::GetFolderPath("CommonPrograms")
$StartMenuFolder = Join-Path $StartMenuPath "BacklogManager"
if (Test-Path $StartMenuFolder) {
    Remove-Item -Path $StartMenuFolder -Recurse -Force
    Write-Host "  ✓ Raccourci menu Démarrer supprimé" -ForegroundColor Gray
}

Write-Host "Suppression des fichiers..." -ForegroundColor Yellow

# Conserver la base de données
$BackupData = $false
$DataPath = Join-Path $InstallPath "data"
$TempDataPath = Join-Path $env:TEMP "BacklogManager_data_backup"

if (Test-Path $DataPath) {
    if (-not $Silent) {
        $keepData = Read-Host "Voulez-vous conserver la base de données? (O/N)"
        if ($keepData -eq "O" -or $keepData -eq "o") {
            $BackupData = $true
            Copy-Item -Path $DataPath -Destination $TempDataPath -Recurse -Force
            Write-Host "  ✓ Base de données sauvegardée temporairement" -ForegroundColor Gray
        }
    }
}

# Supprimer le dossier d'installation
Remove-Item -Path $InstallPath -Recurse -Force
Write-Host "  ✓ Fichiers supprimés" -ForegroundColor Gray

# Restaurer la base de données si demandé
if ($BackupData) {
    New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
    New-Item -ItemType Directory -Path $DataPath -Force | Out-Null
    Copy-Item -Path "$TempDataPath\*" -Destination $DataPath -Recurse -Force
    Remove-Item -Path $TempDataPath -Recurse -Force
    Write-Host "  ✓ Base de données restaurée dans: $DataPath" -ForegroundColor Green
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Désinstallation terminée!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

if ($BackupData) {
    Write-Host "La base de données a été conservée dans:" -ForegroundColor Yellow
    Write-Host "  $DataPath" -ForegroundColor Cyan
    Write-Host ""
}

Write-Host "Appuyez sur Entrée pour terminer..." -ForegroundColor Yellow
Read-Host
