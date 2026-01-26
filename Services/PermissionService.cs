using BacklogManager.Domain;

namespace BacklogManager.Services
{
    public class PermissionService
    {
        private readonly Utilisateur _currentUser;
        private readonly Role _currentRole;

        public PermissionService(Utilisateur currentUser, Role currentRole)
        {
            _currentUser = currentUser;
            _currentRole = currentRole;
        }

        // Permissions de création
        public bool PeutCreerDemandes => _currentRole?.PeutCreerDemandes ?? false;
        public bool PeutCreerTaches => _currentRole?.PeutCreerDemandes ?? false; // Pour Admin/BA/CP
        public bool PeutCreerTachesSpeciales => true; // Tous les utilisateurs peuvent créer congés/support
        public bool PeutCreerProjets => _currentRole?.PeutGererReferentiels ?? false;

        // Permissions de modification
        public bool PeutModifierTaches => _currentRole?.PeutModifierTaches ?? false;
        public bool PeutChiffrer => _currentRole?.PeutChiffrer ?? false;
        public bool PeutPrioriser => _currentRole?.PeutPrioriser ?? false;

        // Permissions de suppression
        public bool PeutSupprimerTaches => _currentRole?.PeutSupprimerTaches ?? false;
        public bool PeutSupprimerProjets => _currentRole?.PeutGererReferentiels ?? false;

        // Permissions administratives
        public bool PeutGererUtilisateurs => _currentRole?.PeutGererUtilisateurs ?? false;
        public bool PeutGererRoles => _currentRole?.PeutGererUtilisateurs ?? false;
        public bool PeutGererReferentiels => _currentRole?.PeutGererReferentiels ?? false;
        public bool PeutGererEquipe => _currentRole?.PeutGererReferentiels ?? false;

        // Permissions de visualisation
        public bool PeutVoirKPI => _currentRole?.PeutVoirKPI ?? false;
        public bool PeutVoirToutesLesTaches => IsAdmin || (_currentRole?.PeutPrioriser ?? false);
        public bool PeutVoirTousLesProjets => IsAdmin || (_currentRole?.PeutPrioriser ?? false);

        // Rôles spéciaux
        public bool IsAdmin => _currentRole?.Type == RoleType.Administrateur;
        public bool EstAdministrateur => IsAdmin; // Alias pour compatibilité
        public bool IsChefDeProjet => _currentRole?.Type == RoleType.ChefDeProjet;
        public bool IsDeveloppeur => _currentRole?.Type == RoleType.Developpeur;
        public bool IsBusinessAnalyst => _currentRole?.Type == RoleType.BusinessAnalyst;

        // Permissions combinées
        public bool PeutAccederAdministration => PeutGererUtilisateurs || PeutGererReferentiels;
        public bool PeutModifierProjet(Projet projet) => IsAdmin || PeutGererReferentiels;
        public bool PeutModifierTache(BacklogItem tache)
        {
            // Admin peut tout modifier
            if (IsAdmin) return true;
            
            // Un chef de projet peut modifier toutes les tâches
            if (IsChefDeProjet && PeutModifierTaches) return true;
            
            // Un dev peut modifier ses propres tâches s'il a la permission
            if (IsDeveloppeur && PeutModifierTaches && tache.DevAssigneId == _currentUser.Id) return true;
            
            // Sinon vérifier la permission générale
            return PeutModifierTaches;
        }

        public bool PeutSupprimerTache(BacklogItem tache)
        {
            // Admin peut tout supprimer
            if (IsAdmin) return true;
            
            // Un chef de projet peut supprimer les tâches
            if (IsChefDeProjet && PeutSupprimerTaches) return true;
            
            // Sinon vérifier la permission générale
            return PeutSupprimerTaches;
        }

        public bool PeutAssignerDev => IsAdmin || IsChefDeProjet || PeutPrioriser;
        public bool PeutChangerPriorite => PeutPrioriser;
        public bool PeutChangerStatut(BacklogItem tache)
        {
            if (IsAdmin || IsChefDeProjet) return true;
            
            // Un dev peut changer le statut de ses propres tâches
            if (IsDeveloppeur && tache.DevAssigneId == _currentUser.Id) return true;
            
            return false;
        }

        // Permissions pour les demandes
        public bool PeutModifierDemande(Demande demande)
        {
            // Admin peut tout modifier
            if (IsAdmin) return true;
            
            // Chef de projet peut modifier toutes les demandes
            if (IsChefDeProjet) return true;
            
            // Business Analyst peut modifier les demandes qu'il a créées ou dont il est responsable
            if (IsBusinessAnalyst && (demande.DemandeurId == _currentUser.Id || demande.BusinessAnalystId == _currentUser.Id))
                return true;
            
            // Le créateur peut modifier sa propre demande
            if (demande.DemandeurId == _currentUser.Id)
                return true;
            
            return false;
        }

        public bool PeutSupprimerDemande(Demande demande)
        {
            // Seuls Admin et Chef de projet peuvent supprimer
            return IsAdmin || IsChefDeProjet;
        }

        // Permissions CRA
        public bool PeutValiderCRA => IsAdmin || IsChefDeProjet;

        // Accès à l'utilisateur connecté
        public Utilisateur UtilisateurConnecte => _currentUser;
        public int? EquipeIdUtilisateur => _currentUser?.EquipeId;
    }
}

