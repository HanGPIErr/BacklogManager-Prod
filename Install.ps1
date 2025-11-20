# =====================================
# BacklogManager - Script d'installation
# =====================================

param(
    [string]$InstallPath = "C:\SGI_SUPPORT\APPLICATIONS\BacklogManager",
    [switch]$Silent
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  BacklogManager - Installation" -ForegroundColor Cyan
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

# Chemin du script et des fichiers sources
$ScriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$SourcePath = $ScriptPath

Write-Host "Chemin d'installation: $InstallPath" -ForegroundColor Green
Write-Host ""

# Créer le dossier d'installation
if (-not (Test-Path $InstallPath)) {
    Write-Host "Création du dossier d'installation..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
} else {
    if (-not $Silent) {
        $response = Read-Host "Le dossier existe déjà. Voulez-vous continuer? (O/N)"
        if ($response -ne "O" -and $response -ne "o") {
            Write-Host "Installation annulée." -ForegroundColor Red
            exit 0
        }
    }
}

# Copier les fichiers
Write-Host "Copie des fichiers..." -ForegroundColor Yellow

$FilesToCopy = @(
    "BacklogManager.exe",
    "*.dll",
    "*.config",
    "config.ini",
    "README.txt",
    "GUIDE_MISE_A_JOUR.txt"
)

foreach ($pattern in $FilesToCopy) {
    $files = Get-ChildItem -Path $SourcePath -Filter $pattern -File -ErrorAction SilentlyContinue
    foreach ($file in $files) {
        Copy-Item -Path $file.FullName -Destination $InstallPath -Force
        Write-Host "  ✓ $($file.Name)" -ForegroundColor Gray
    }
}

# Copier les dossiers x64 et x86
if (Test-Path "$SourcePath\x64") {
    Copy-Item -Path "$SourcePath\x64" -Destination $InstallPath -Recurse -Force
    Write-Host "  ✓ Dossier x64" -ForegroundColor Gray
}

if (Test-Path "$SourcePath\x86") {
    Copy-Item -Path "$SourcePath\x86" -Destination $InstallPath -Recurse -Force
    Write-Host "  ✓ Dossier x86" -ForegroundColor Gray
}

# Créer le dossier data pour la base de données
$DataPath = Join-Path $InstallPath "data"
if (-not (Test-Path $DataPath)) {
    New-Item -ItemType Directory -Path $DataPath -Force | Out-Null
    Write-Host "  ✓ Dossier data créé" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Création du raccourci bureau..." -ForegroundColor Yellow

# Créer le raccourci sur le bureau
$WshShell = New-Object -ComObject WScript.Shell
$DesktopPath = [Environment]::GetFolderPath("CommonDesktopDirectory")
$ShortcutPath = Join-Path $DesktopPath "BacklogManager.lnk"
$Shortcut = $WshShell.CreateShortcut($ShortcutPath)
$Shortcut.TargetPath = Join-Path $InstallPath "BacklogManager.exe"
$Shortcut.WorkingDirectory = $InstallPath
$Shortcut.Description = "BacklogManager - Gestion de projets Agile"
$Shortcut.IconLocation = Join-Path $InstallPath "BacklogManager.exe"
$Shortcut.Save()

Write-Host "  ✓ Raccourci créé sur le bureau" -ForegroundColor Gray

# Créer le raccourci dans le menu Démarrer
$StartMenuPath = [Environment]::GetFolderPath("CommonPrograms")
$StartMenuFolder = Join-Path $StartMenuPath "BacklogManager"
if (-not (Test-Path $StartMenuFolder)) {
    New-Item -ItemType Directory -Path $StartMenuFolder -Force | Out-Null
}
$StartMenuShortcutPath = Join-Path $StartMenuFolder "BacklogManager.lnk"
$StartMenuShortcut = $WshShell.CreateShortcut($StartMenuShortcutPath)
$StartMenuShortcut.TargetPath = Join-Path $InstallPath "BacklogManager.exe"
$StartMenuShortcut.WorkingDirectory = $InstallPath
$StartMenuShortcut.Description = "BacklogManager - Gestion de projets Agile"
$StartMenuShortcut.IconLocation = Join-Path $InstallPath "BacklogManager.exe"
$StartMenuShortcut.Save()

Write-Host "  ✓ Raccourci créé dans le menu Démarrer" -ForegroundColor Gray

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Installation terminée avec succès!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "BacklogManager a été installé dans:" -ForegroundColor White
Write-Host "  $InstallPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "Lancez l'application via:" -ForegroundColor White
Write-Host "  - Le raccourci sur le bureau" -ForegroundColor Gray
Write-Host "  - Le menu Démarrer > BacklogManager" -ForegroundColor Gray
Write-Host ""

if (-not $Silent) {
    $launch = Read-Host "Voulez-vous lancer l'application maintenant? (O/N)"
    if ($launch -eq "O" -or $launch -eq "o") {
        Start-Process -FilePath (Join-Path $InstallPath "BacklogManager.exe")
    }
}

Write-Host ""
Write-Host "Appuyez sur Entrée pour terminer..." -ForegroundColor Yellow
Read-Host
