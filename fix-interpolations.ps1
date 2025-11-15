# Script pour remplacer toutes les string interpolations
$replacements = @{
    'Views\LoginWindow.xaml.cs' = @(
        @('$"Utilisateur Windows: {windowsUsername}"', 'string.Format("Utilisateur Windows: {0}", windowsUsername)'),
        @('$"Connecté en tant que {user.Nom} {user.Prenom} ({role?.Nom})"', 'string.Format("Connecté en tant que {0} {1} ({2})", user.Nom, user.Prenom, role != null ? role.Nom : "")'),
        @('$"Erreur lors de la connexion: {ex.Message}"', 'string.Format("Erreur lors de la connexion: {0}", ex.Message)')
    )
    'Views\DemandesView.xaml.cs' = @(
        @('$"Erreur lors du chargement des demandes: {ex.Message}"', 'string.Format("Erreur lors du chargement des demandes: {0}", ex.Message)'),
        @('$"{user.Prenom} {user.Nom}"', 'string.Format("{0} {1}", user.Prenom, user.Nom)'),
        @('$"Erreur lors de l''ouverture du formulaire: {ex.Message}"', 'string.Format("Erreur lors de l''ouverture du formulaire: {0}", ex.Message)'),
        @('$"Erreur lors de l''affichage des détails: {ex.Message}"', 'string.Format("Erreur lors de l''affichage des détails: {0}", ex.Message)'),
        @('$"Erreur lors de la modification: {ex.Message}"', 'string.Format("Erreur lors de la modification: {0}", ex.Message)'),
        @('$"Erreur lors de l''affichage des commentaires: {ex.Message}"', 'string.Format("Erreur lors de l''affichage des commentaires: {0}", ex.Message)')
    )
    'Views\EditionDemandeWindow.xaml.cs' = @(
        @('$"{u.Prenom} {u.Nom}"', 'string.Format("{0} {1}", u.Prenom, u.Nom)'),
        @('$"Demande modifiée : {_demandeActuelle.Titre}"', 'string.Format("Demande modifiée : {0}", _demandeActuelle.Titre)'),
        @('$"Demande créée : {_demandeActuelle.Titre}"', 'string.Format("Demande créée : {0}", _demandeActuelle.Titre)'),
        @('$"Erreur lors de l''enregistrement: {ex.Message}"', 'string.Format("Erreur lors de l''enregistrement: {0}", ex.Message)')
    )
    'Views\DetailsDemandeWindow.xaml.cs' = @(
        @('$"{user.Prenom} {user.Nom}"', 'string.Format("{0} {1}", user.Prenom, user.Nom)')
    )
    'Views\CommentairesWindow.xaml.cs' = @(
        @('$"{commentaires.Count} commentaire(s)"', 'string.Format("{0} commentaire(s)", commentaires.Count)'),
        @('$"Erreur lors du chargement des commentaires: {ex.Message}"', 'string.Format("Erreur lors du chargement des commentaires: {0}", ex.Message)'),
        @('$"{user.Prenom} {user.Nom}"', 'string.Format("{0} {1}", user.Prenom, user.Nom)'),
        @('$"Erreur lors de l''ajout du commentaire: {ex.Message}"', 'string.Format("Erreur lors de l''ajout du commentaire: {0}", ex.Message)')
    )
    'ViewModels\PokerViewModel.cs' = @(
        @('$"Consensus atteint ! Complexité: {consensus}, Jours planifiés: {JoursPlanifies:F2}"', 'string.Format("Consensus atteint ! Complexité: {0}, Jours planifiés: {1:F2}", consensus, JoursPlanifies)')
    )
    'ViewModels\TimelineViewModel.cs' = @(
        @('$"{DaysRemaining} jours restants"', 'string.Format("{0} jours restants", DaysRemaining)')
    )
}

foreach ($file in $replacements.Keys) {
    $content = Get-Content $file -Raw -ErrorAction SilentlyContinue
    if ($content) {
        foreach ($pair in $replacements[$file]) {
            $content = $content.Replace($pair[0], $pair[1])
        }
        Set-Content $file $content -NoNewline
        Write-Host "Updated: $file"
    }
}

Write-Host "Done!"
