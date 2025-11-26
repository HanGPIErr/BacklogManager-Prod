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
            InitializeDevs();
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
                    PeutModifierTaches = true,
                    PeutSupprimerTaches = true,
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
                    PeutModifierTaches = false,
                    PeutSupprimerTaches = false,
                    Actif = true
                });

                // Rôle Chef de Projet / PO
                _database.AddOrUpdateRole(new Role
                {
                    Nom = "Chef de Projet / PO",
                    Type = RoleType.ChefDeProjet,
                    PeutCreerDemandes = true,
                    PeutChiffrer = true,
                    PeutPrioriser = true,
                    PeutGererUtilisateurs = false,
                    PeutVoirKPI = true,
                    PeutGererReferentiels = false,
                    PeutModifierTaches = true,
                    PeutSupprimerTaches = true,
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
                    PeutModifierTaches = false,
                    PeutSupprimerTaches = false,
                    Actif = true
                });
            }
        }

        private void InitializeUsers()
        {
            var utilisateurs = _database.GetUtilisateurs();
            var roles = _database.GetRoles();

            var roleAdmin = roles.FirstOrDefault(r => r.Type == RoleType.Administrateur);
            var roleBA = roles.FirstOrDefault(r => r.Type == RoleType.BusinessAnalyst);
            var roleCP = roles.FirstOrDefault(r => r.Type == RoleType.ChefDeProjet);
            var roleDev = roles.FirstOrDefault(r => r.Type == RoleType.Developpeur);

            // Créer les utilisateurs de test s'ils n'existent pas déjà
            if (!utilisateurs.Any(u => u.UsernameWindows == "admin.test"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "admin.test",
                    Nom = "Administrateur",
                    Prenom = "Test",
                    Email = "admin.test@bnpparibas.com",
                    RoleId = roleAdmin?.Id ?? 1,
                    Actif = true,
                    DateCreation = DateTime.Now
                });
            }

            if (!utilisateurs.Any(u => u.UsernameWindows == "ba.test"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "ba.test",
                    Nom = "Analyst",
                    Prenom = "Business",
                    Email = "ba.test@bnpparibas.com",
                    RoleId = roleBA?.Id ?? 2,
                    Actif = true,
                    DateCreation = DateTime.Now
                });
            }

            if (!utilisateurs.Any(u => u.UsernameWindows == "po.test"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "po.test",
                    Nom = "Owner",
                    Prenom = "Product",
                    Email = "po.test@bnpparibas.com",
                    RoleId = roleCP?.Id ?? 3,
                    Actif = true,
                    DateCreation = DateTime.Now
                });
            }

            if (!utilisateurs.Any(u => u.UsernameWindows == "dev.test"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "dev.test",
                    Nom = "Développeur",
                    Prenom = "Test",
                    Email = "dev.test@bnpparibas.com",
                    RoleId = roleDev?.Id ?? 4,
                    Actif = true,
                    DateCreation = DateTime.Now
                });
            }
        }

        private void InitializeDevs()
        {
            var devs = _database.GetDevs();

            // Créer des devs de test s'ils n'existent pas déjà
            if (!devs.Any(d => d.Nom == "Admin Test"))
            {
                _database.AddOrUpdateDev(new Dev
                {
                    Nom = "Admin Test",
                    Initiales = "AT",
                    Actif = true
                });
            }

            if (!devs.Any(d => d.Nom == "Dev Test"))
            {
                _database.AddOrUpdateDev(new Dev
                {
                    Nom = "Dev Test",
                    Initiales = "DT",
                    Actif = true
                });
            }

            if (!devs.Any(d => d.Nom == "BA Test"))
            {
                _database.AddOrUpdateDev(new Dev
                {
                    Nom = "BA Test",
                    Initiales = "BA",
                    Actif = true
                });
            }

            if (!devs.Any(d => d.Nom == "PO Test"))
            {
                _database.AddOrUpdateDev(new Dev
                {
                    Nom = "PO Test",
                    Initiales = "PO",
                    Actif = true
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
            var backlogItems = _database.GetBacklogItems();
            var projetAdmin = _database.GetProjets().FirstOrDefault(p => p.Nom == "Tâches administratives");
            
            if (projetAdmin == null) return;

            // Créer la tâche "Congés" si elle n'existe pas
            if (!backlogItems.Any(t => t.TypeDemande == TypeDemande.Conges))
            {
                _database.AddOrUpdateBacklogItem(new BacklogItem
                {
                    Titre = "Congés",
                    Description = "Congés / Vacances",
                    TypeDemande = TypeDemande.Conges,
                    Statut = Statut.EnCours,
                    Priorite = Priorite.Moyenne,
                    ProjetId = projetAdmin.Id,
                    DateCreation = DateTime.Now,
                    DateDerniereMaj = DateTime.Now,
                    EstArchive = false
                });
            }

            // Créer la tâche "Non travaillé" si elle n'existe pas
            if (!backlogItems.Any(t => t.TypeDemande == TypeDemande.NonTravaille))
            {
                _database.AddOrUpdateBacklogItem(new BacklogItem
                {
                    Titre = "Non travaillé",
                    Description = "Absences, maladie, etc.",
                    TypeDemande = TypeDemande.NonTravaille,
                    Statut = Statut.EnCours,
                    Priorite = Priorite.Moyenne,
                    ProjetId = projetAdmin.Id,
                    DateCreation = DateTime.Now,
                    DateDerniereMaj = DateTime.Now,
                    EstArchive = false
                });
            }

            // Créer la tâche "Support" si elle n'existe pas
            var tachesSupport = backlogItems.Where(t => t.TypeDemande == TypeDemande.Support).ToList();
            if (tachesSupport.Count == 0)
            {
                _database.AddOrUpdateBacklogItem(new BacklogItem
                {
                    Titre = "Support / Aide",
                    Description = "Support général",
                    TypeDemande = TypeDemande.Support,
                    Statut = Statut.EnCours,
                    Priorite = Priorite.Moyenne,
                    ProjetId = projetAdmin.Id,
                    DateCreation = DateTime.Now,
                    DateDerniereMaj = DateTime.Now,
                    EstArchive = false
                });
            }
            else if (tachesSupport.Count > 1)
            {
                // Nettoyer les doublons - garder seulement le premier, archiver les autres
                var premiereSupport = tachesSupport.OrderBy(t => t.Id).First();
                foreach (var doublon in tachesSupport.Where(t => t.Id != premiereSupport.Id))
                {
                    doublon.EstArchive = true;
                    _database.AddOrUpdateBacklogItem(doublon);
                }
            }
        }
    }
}
