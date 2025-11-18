# Script PowerShell pour vérifier les archives dans SQLite
$dbPath = "C:\Users\HanGP\BacklogManager\bin\Release\data\backlog.db"

if (-not (Test-Path $dbPath)) {
    Write-Host "Base de données non trouvée à: $dbPath" -ForegroundColor Red
    exit 1
}

Write-Host "Base de données trouvée: $dbPath" -ForegroundColor Green
Write-Host "Taille: $((Get-Item $dbPath).Length) bytes"
Write-Host ""

# Charger l'assembly SQLite depuis le répertoire de sortie
$sqliteDll = "C:\Users\HanGP\BacklogManager\bin\Release\System.Data.SQLite.dll"
if (Test-Path $sqliteDll) {
    [System.Reflection.Assembly]::LoadFrom($sqliteDll) | Out-Null
    Write-Host "Assembly SQLite chargé" -ForegroundColor Green
} else {
    Write-Host "System.Data.SQLite.dll non trouvé à: $sqliteDll" -ForegroundColor Red
    exit 1
}

try {
    $connectionString = "Data Source=$dbPath;Version=3;"
    $connection = New-Object System.Data.SQLite.SQLiteConnection($connectionString)
    $connection.Open()
    Write-Host "Connexion ouverte" -ForegroundColor Green
    Write-Host ""
    
    # Requête pour compter les archives
    $cmdCount = $connection.CreateCommand()
    $cmdCount.CommandText = "SELECT COUNT(*) FROM BacklogItems WHERE EstArchive = 1;"
    $count = $cmdCount.ExecuteScalar()
    Write-Host "Nombre de tâches archivées: $count" -ForegroundColor Cyan
    Write-Host ""
    
    # Requête pour lister les archives
    $cmd = $connection.CreateCommand()
    $cmd.CommandText = "SELECT Id, Titre, EstArchive, Statut, DateDerniereMaj FROM BacklogItems WHERE EstArchive = 1 ORDER BY DateDerniereMaj DESC;"
    $reader = $cmd.ExecuteReader()
    
    if ($reader.HasRows) {
        Write-Host "Tâches archivées trouvées:" -ForegroundColor Yellow
        Write-Host "-------------------------------------------"
        while ($reader.Read()) {
            Write-Host "ID: $($reader['Id'])"
            Write-Host "  Titre: $($reader['Titre'])"
            Write-Host "  Statut: $($reader['Statut'])"
            Write-Host "  Date MAJ: $($reader['DateDerniereMaj'])"
            Write-Host ""
        }
    } else {
        Write-Host "Aucune tâche archivée trouvée dans la base" -ForegroundColor Yellow
        Write-Host ""
        
        # Vérifier toutes les tâches
        $cmdAll = $connection.CreateCommand()
        $cmdAll.CommandText = "SELECT COUNT(*) FROM BacklogItems;"
        $totalCount = $cmdAll.ExecuteScalar()
        Write-Host "Nombre total de tâches: $totalCount" -ForegroundColor Cyan
    }
    
    $reader.Close()
    $connection.Close()
    Write-Host ""
    Write-Host "Connexion fermée" -ForegroundColor Green
}
catch {
    Write-Host "Erreur: $_" -ForegroundColor Red
}
