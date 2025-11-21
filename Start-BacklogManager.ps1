# ========================================================
# BacklogManager - Lanceur intelligent avec mapping réseau
# ========================================================

param(
    [string]$NetworkPath = "",  # Chemin UNC à mapper (ex: \\serveur\partage\Data)
    [switch]$NoMapping          # Ne pas mapper de lecteur réseau
)

$AppPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$ExePath = Join-Path $AppPath "BacklogManager.exe"
$ConfigPath = Join-Path $AppPath "config.ini"

# Fonction pour trouver un lecteur mappé vers un chemin UNC
function Get-MappedDrive {
    param([string]$UncPath)
    
    $drives = Get-PSDrive -PSProvider FileSystem | Where-Object { $_.DisplayRoot -like "$UncPath*" }
    return $drives | Select-Object -First 1
}

# Fonction pour mapper un nouveau lecteur
function New-NetworkDrive {
    param([string]$UncPath)
    
    # Trouver une lettre disponible (de Z vers M)
    $usedDrives = (Get-PSDrive -PSProvider FileSystem).Name
    $availableLetter = $null
    
    for ($i = 90; $i -ge 77; $i--) {  # Z=90, M=77
        $letter = [char]$i
        if ($letter -notin $usedDrives) {
            $availableLetter = $letter
            break
        }
    }
    
    if (-not $availableLetter) {
        Write-Host "Aucune lettre de lecteur disponible" -ForegroundColor Red
        return $null
    }
    
    try {
        net use "${availableLetter}:" "$UncPath" /PERSISTENT:NO | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Lecteur ${availableLetter}: mappe vers $UncPath" -ForegroundColor Green
            return $availableLetter
        } else {
            Write-Host "Echec du mapping vers $UncPath" -ForegroundColor Red
            return $null
        }
    } catch {
        Write-Host "Erreur: $_" -ForegroundColor Red
        return $null
    }
}

# Lire le chemin de la base depuis config.ini
function Get-DatabasePath {
    if (Test-Path $ConfigPath) {
        $content = Get-Content $ConfigPath -Encoding UTF8
        foreach ($line in $content) {
            if ($line -match "^DatabasePath=(.+)$") {
                return $matches[1].Trim().Trim('"').Trim("'")
            }
        }
    }
    return ""
}

# Mettre à jour config.ini avec un nouveau chemin
function Set-DatabasePath {
    param([string]$NewPath)
    
    if (Test-Path $ConfigPath) {
        $content = Get-Content $ConfigPath -Encoding UTF8
        $newContent = @()
        $found = $false
        
        foreach ($line in $content) {
            if ($line -match "^DatabasePath=") {
                $newContent += "DatabasePath=$NewPath"
                $found = $true
            } else {
                $newContent += $line
            }
        }
        
        if ($found) {
            $newContent | Out-File $ConfigPath -Encoding UTF8 -Force
            Write-Host "config.ini mis a jour: DatabasePath=$NewPath" -ForegroundColor Green
        }
    }
}

# ===== MAIN =====

Write-Host "==================================" -ForegroundColor Cyan
Write-Host "  BacklogManager - Demarrage" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $ExePath)) {
    Write-Host "ERREUR: BacklogManager.exe introuvable!" -ForegroundColor Red
    Read-Host "Appuyez sur Entree pour quitter"
    exit 1
}

# Si un chemin réseau est spécifié
if ($NetworkPath -and -not $NoMapping) {
    Write-Host "Chemin reseau specifie: $NetworkPath" -ForegroundColor Yellow
    
    # Vérifier si déjà mappé
    $existingDrive = Get-MappedDrive $NetworkPath
    
    if ($existingDrive) {
        Write-Host "Lecteur deja mappe: $($existingDrive.Name):" -ForegroundColor Green
        $driveLetter = $existingDrive.Name
    } else {
        Write-Host "Mapping du lecteur reseau..." -ForegroundColor Yellow
        $driveLetter = New-NetworkDrive $NetworkPath
        
        if (-not $driveLetter) {
            Write-Host "Impossible de mapper le lecteur. Lancement avec chemin UNC..." -ForegroundColor Yellow
            Start-Process $ExePath -WorkingDirectory $AppPath
            exit 0
        }
    }
    
    # Mettre à jour config.ini
    $currentDbPath = Get-DatabasePath
    if ($currentDbPath -like "\\*") {
        # Chemin UNC actuel, le remplacer par le lecteur mappé
        $fileName = Split-Path -Leaf $currentDbPath
        $newPath = "${driveLetter}:\$fileName"
        Set-DatabasePath $newPath
    }
}

# Lancer l'application
Write-Host "Lancement de BacklogManager..." -ForegroundColor Green
Start-Process $ExePath -WorkingDirectory $AppPath

Write-Host ""
Write-Host "BacklogManager demarre!" -ForegroundColor Green
