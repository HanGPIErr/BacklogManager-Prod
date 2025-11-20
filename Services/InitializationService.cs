using System;
using System.Linq;
using BacklogManager.Domain;

namespace BacklogManager.Services
{
    public class InitializationService
    {
        private readonly IDatabase _database;

        public InitializationService(IDatabase database)
        {
            _database = database;
        }

        public void InitializeDefaultData()
        {
            InitializeRoles();
            InitializeUsers();
            InitializeDefaultProjet();
            InitializeDefaultTasks();
        }

        private void InitializeRoles()
        {
            var roles = _database.GetRoles();
            
            if (!roles.Any())
            {
                // Rôle Administrateur
                _database.AddOrUpdateRole(new Role
                {
                    Nom = "Administrateur",
                    Type = RoleType.Administrateur,
                    PeutCreerDemandes = true,
                    PeutChiffrer = true,
                    PeutPrioriser = true,
                    PeutGererUtilisateurs = true,
                    PeutVoirKPI = true,
                    PeutGererReferentiels = true,
                    Actif = true
                });

                // Rôle Business Analyst
                _database.AddOrUpdateRole(new Role
                {
                    Nom = "Business Analyst",
                    Type = RoleType.BusinessAnalyst,
                    PeutCreerDemandes = true,
                    PeutChiffrer = false,
                    PeutPrioriser = false,
                    PeutGererUtilisateurs = false,
                    PeutVoirKPI = true,
                    PeutGererReferentiels = false,
                    Actif = true
                });

                // Rôle Chef de Projet / PO
                _database.AddOrUpdateRole(new Role
                {
                    Nom = "Chef de Projet / PO",
                    Type = RoleType.ChefDeProjet,
                    PeutCreerDemandes = true,
                    PeutChiffrer = false,
                    PeutPrioriser = true,
                    PeutGererUtilisateurs = false,
                    PeutVoirKPI = true,
                    PeutGererReferentiels = false,
                    Actif = true
                });

                // Rôle Développeur
                _database.AddOrUpdateRole(new Role
                {
                    Nom = "Développeur",
                    Type = RoleType.Developpeur,
                    PeutCreerDemandes = false,
                    PeutChiffrer = true,
                    PeutPrioriser = false,
                    PeutGererUtilisateurs = false,
                    PeutVoirKPI = false,
                    PeutGererReferentiels = false,
                    Actif = true
                });
            }
        }

        private void InitializeUsers()
        {
            var utilisateurs = _database.GetUtilisateurs();
            var roles = _database.GetRoles();

            if (!utilisateurs.Any())
            {
                var roleAdmin = roles.FirstOrDefault(r => r.Type == RoleType.Administrateur);

                // Créer un utilisateur administrateur par défaut
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "ADMIN",
                    Nom = "Administrateur",
                    Prenom = "Système",
                    Email = "admin@company.com",
                    RoleId = roleAdmin?.Id ?? 1,
                    Actif = true,
                    DateCreation = DateTime.Now
                });
            }
        }

        private void InitializeDefaultProjet()
        {
            var projets = _database.GetProjets();
            
            // Créer le projet "Tâches administratives" s'il n'existe pas
            bool hasProjetAdmin = projets.Any(p => p.Nom == "Tâches administratives");
            
            if (!hasProjetAdmin)
            {
                _database.AddOrUpdateProjet(new Projet
                {
                    Nom = "Tâches administratives",
                    Description = "Projet générique pour les congés, absences et autres tâches administratives",
                    DateCreation = DateTime.Now,
                    Actif = true
                });
            }
        }

        private void InitializeDefaultTasks()
        {
            // Ne plus créer de tâches génériques partagées
            // Les devs créeront leurs propres instances de congés/support/etc.
            // Le système auto-assignera le projet "Tâches administratives" selon le type
        }
    }
}
