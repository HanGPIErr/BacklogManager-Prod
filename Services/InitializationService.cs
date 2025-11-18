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
                var roleBA = roles.FirstOrDefault(r => r.Type == RoleType.BusinessAnalyst);
                var roleCP = roles.FirstOrDefault(r => r.Type == RoleType.ChefDeProjet);
                var roleDev = roles.FirstOrDefault(r => r.Type == RoleType.Developpeur);

                // Admin
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "J00001",
                    Nom = "Admin",
                    Prenom = "Système",
                    Email = "admin@bnpparibas.com",
                    RoleId = roleAdmin?.Id ?? 1,
                    Actif = true,
                    DateCreation = DateTime.Now
                });

                // Business Analysts
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "J10001",
                    Nom = "Martin",
                    Prenom = "Sophie",
                    Email = "sophie.martin@bnpparibas.com",
                    RoleId = roleBA?.Id ?? 2,
                    Actif = true,
                    DateCreation = DateTime.Now
                });

                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "J10002",
                    Nom = "Dubois",
                    Prenom = "Marc",
                    Email = "marc.dubois@bnpparibas.com",
                    RoleId = roleBA?.Id ?? 2,
                    Actif = true,
                    DateCreation = DateTime.Now
                });

                // Chef de Projet
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "J20001",
                    Nom = "Leroy",
                    Prenom = "Catherine",
                    Email = "catherine.leroy@bnpparibas.com",
                    RoleId = roleCP?.Id ?? 3,
                    Actif = true,
                    DateCreation = DateTime.Now
                });

                // Développeurs
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "J04831",
                    Nom = "HanGP",
                    Prenom = "Pierre-Romain",
                    Email = "pierre-romain.hangp@bnpparibas.com",
                    RoleId = roleDev?.Id ?? 4,
                    Actif = true,
                    DateCreation = DateTime.Now
                });

                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "J30001",
                    Nom = "Bernard",
                    Prenom = "Thomas",
                    Email = "thomas.bernard@bnpparibas.com",
                    RoleId = roleDev?.Id ?? 4,
                    Actif = true,
                    DateCreation = DateTime.Now
                });

                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "J30002",
                    Nom = "Petit",
                    Prenom = "Julie",
                    Email = "julie.petit@bnpparibas.com",
                    RoleId = roleDev?.Id ?? 4,
                    Actif = true,
                    DateCreation = DateTime.Now
                });

                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "J30003",
                    Nom = "Robert",
                    Prenom = "Alexandre",
                    Email = "alexandre.robert@bnpparibas.com",
                    RoleId = roleDev?.Id ?? 4,
                    Actif = true,
                    DateCreation = DateTime.Now
                });

                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "J30004",
                    Nom = "Moreau",
                    Prenom = "Émilie",
                    Email = "emilie.moreau@bnpparibas.com",
                    RoleId = roleDev?.Id ?? 4,
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
