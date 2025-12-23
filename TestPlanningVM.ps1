# Script pour tester le Planning VM
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test du Planning VM - Tactical Solutions" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Vérifier que l'application existe
$appPath = ".\bin\Release\BacklogManager.exe"

if (-not (Test-Path $appPath)) {
    Write-Host "ERREUR: L'application n'existe pas à: $appPath" -ForegroundColor Red
    Write-Host "Veuillez compiler l'application en mode Release d'abord." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Pour compiler:" -ForegroundColor Yellow
    Write-Host "  1. Ouvrez le projet dans Visual Studio" -ForegroundColor Yellow
    Write-Host "  2. Sélectionnez 'Release' dans la barre d'outils" -ForegroundColor Yellow
    Write-Host "  3. Allez dans Build > Build Solution (ou F6)" -ForegroundColor Yellow
    exit 1
}

Write-Host "✓ Application trouvée" -ForegroundColor Green
Write-Host ""

# Vérifier la base de données
$dbPath = "data\backlog.db"

if (Test-Path $dbPath) {
    Write-Host "✓ Base de données trouvée: $dbPath" -ForegroundColor Green
    
    # Afficher la taille
    $dbSize = (Get-Item $dbPath).Length / 1KB
    Write-Host "  Taille: $([math]::Round($dbSize, 2)) KB" -ForegroundColor Gray
} else {
    Write-Host "! Base de données non trouvée à: $dbPath" -ForegroundColor Yellow
    Write-Host "  Elle sera créée au premier lancement" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Nouvelles fonctionnalités ajoutées:" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. 🖥️  Planning VM pour Tactical Solutions" -ForegroundColor Green
Write-Host "   - Calendrier mensuel avec navigation" -ForegroundColor Gray
Write-Host "   - Assignation des membres sur les jours ouvrés" -ForegroundColor Gray
Write-Host "   - Jours fériés et weekends grisés" -ForegroundColor Gray
Write-Host "   - Demandes d'échange entre membres" -ForegroundColor Gray
Write-Host "   - Notifications pour les demandes" -ForegroundColor Gray
Write-Host ""
Write-Host "2. 📊 Tables de base de données créées:" -ForegroundColor Green
Write-Host "   - PlanningVM" -ForegroundColor Gray
Write-Host "   - DemandeEchangeVM" -ForegroundColor Gray
Write-Host ""
Write-Host "3. 🎯 Accès au Planning VM:" -ForegroundColor Green
Write-Host "   - Aller dans Dashboard > Équipes" -ForegroundColor Gray
Write-Host "   - Cliquer sur 'Tactical Solutions'" -ForegroundColor Gray
Write-Host "   - Un bouton 'Planning VM' apparaît en haut à droite" -ForegroundColor Gray
Write-Host "   - Ce bouton n'est visible QUE pour Tactical Solutions" -ForegroundColor Gray
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Comment tester:" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Lancez l'application" -ForegroundColor Yellow
Write-Host "2. Connectez-vous avec un compte" -ForegroundColor Yellow
Write-Host "3. Allez dans la vue 'Équipes'" -ForegroundColor Yellow
Write-Host "4. Cliquez sur 'Tactical Solutions / Rapid Delivery'" -ForegroundColor Yellow
Write-Host "5. Vous verrez le bouton '🖥️ Planning VM' en haut à droite" -ForegroundColor Yellow
Write-Host "6. Cliquez dessus pour accéder au calendrier" -ForegroundColor Yellow
Write-Host ""
Write-Host "Fonctionnalités à tester:" -ForegroundColor Cyan
Write-Host "  ✓ Navigation entre les mois" -ForegroundColor Gray
Write-Host "  ✓ Clic sur un jour ouvré pour s'assigner" -ForegroundColor Gray
Write-Host "  ✓ Vérifier que les weekends et jours fériés sont grisés" -ForegroundColor Gray
Write-Host "  ✓ Se désister d'un jour assigné" -ForegroundColor Gray
Write-Host "  ✓ Demander un échange à un autre membre" -ForegroundColor Gray
Write-Host "  ✓ Recevoir une notification de demande d'échange" -ForegroundColor Gray
Write-Host ""
Write-Host "Voulez-vous lancer l'application maintenant? (O/N): " -ForegroundColor Cyan -NoNewline
$response = Read-Host

if ($response -eq "O" -or $response -eq "o") {
    Write-Host ""
    Write-Host "Lancement de l'application..." -ForegroundColor Green
    Start-Process $appPath
} else {
    Write-Host ""
    Write-Host "Test annulé." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Terminé!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
