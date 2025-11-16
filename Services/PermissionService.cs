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
        public bool PeutCreerTaches => _currentRole?.PeutCreerDemandes ?? false; // Même logique
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
        public bool IsChefDeProjet => _currentRole?.Type == RoleType.ChefDeProjet;
        public bool IsDeveloppeur => _currentRole?.Type == RoleType.Developpeur;
        public bool IsBusinessAnalyst => _currentRole?.Type == RoleType.BusinessAnalyst;

        // Permissions combinées
        public bool PeutAccederAdministration => PeutGererUtilisateurs || PeutGererReferentiels;
        public bool PeutModifierProjet(Projet projet) => IsAdmin || PeutGererReferentiels;
        public bool PeutModifierTache(BacklogItem tache)
        {
            if (IsAdmin) return true;
            if (!PeutModifierTaches) return false;
            
            // Un dev peut modifier ses propres tâches
            if (IsDeveloppeur && tache.DevAssigneId == _currentUser.Id) return true;
            
            // Un chef de projet peut modifier toutes les tâches
            if (IsChefDeProjet) return true;
            
            return false;
        }

        public bool PeutSupprimerTache(BacklogItem tache)
        {
            if (IsAdmin) return true;
            if (!PeutSupprimerTaches) return false;
            
            // Un chef de projet peut supprimer les tâches
            if (IsChefDeProjet) return true;
            
            return false;
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
    }
}
