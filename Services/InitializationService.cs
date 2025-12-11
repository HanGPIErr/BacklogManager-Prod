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
            var equipes = _database.GetAllEquipes();

            var roleAdmin = roles.FirstOrDefault(r => r.Type == RoleType.Administrateur);
            var roleBA = roles.FirstOrDefault(r => r.Type == RoleType.BusinessAnalyst);
            var roleCP = roles.FirstOrDefault(r => r.Type == RoleType.ChefDeProjet);
            var roleDev = roles.FirstOrDefault(r => r.Type == RoleType.Developpeur);

            var equipeTransfo = equipes.FirstOrDefault(e => e.Code == "TRANSFO_IMPLEM");
            var equipeTactical = equipes.FirstOrDefault(e => e.Code == "TACTICAL_SOLUTIONS");
            var equipeData = equipes.FirstOrDefault(e => e.Code == "DATA_OFFICE");
            var equipePCC = equipes.FirstOrDefault(e => e.Code == "PCC");
            var equipeTCSIM = equipes.FirstOrDefault(e => e.Code == "TCS_IM");
            var equipeL1 = equipes.FirstOrDefault(e => e.Code == "L1_SUPPORT");
            var equipeWatchtower = equipes.FirstOrDefault(e => e.Code == "WATCHTOWER");
            var equipeChangeBau = equipes.FirstOrDefault(e => e.Code == "CHANGE_BAU");
            var equipeITAssets = equipes.FirstOrDefault(e => e.Code == "IT_ASSETS");

            // Créer les utilisateurs de test s'ils n'existent pas déjà
            
            // ADMINISTRATEUR PAR DÉFAUT (connexion automatique)
            // TOUJOURS créer/mettre à jour l'admin pour garantir qu'il existe
            var adminExistant = utilisateurs.FirstOrDefault(u => u.UsernameWindows == "admin");
            if (adminExistant == null)
            {
                // Créer l'admin s'il n'existe pas
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "admin",
                    Nom = "Admin",
                    Prenom = "System",
                    Email = "admin@bnpparibas.com",
                    RoleId = roleAdmin?.Id ?? 1,
                    EquipeId = equipeTransfo?.Id,
                    Actif = true,
                    DateCreation = DateTime.Now,
                    Statut = "BAU"
                });
            }
            else
            {
                // S'assurer que l'admin existant est actif et a le bon rôle
                adminExistant.Actif = true;
                adminExistant.RoleId = roleAdmin?.Id ?? 1;
                _database.AddOrUpdateUtilisateur(adminExistant);
            }
            
            // ADMINISTRATEUR DE TEST
            if (!utilisateurs.Any(u => u.UsernameWindows == "admin.test"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "admin.test",
                    Nom = "Administrateur",
                    Prenom = "Test",
                    Email = "admin.test@bnpparibas.com",
                    RoleId = roleAdmin?.Id ?? 1,
                    EquipeId = equipeTransfo?.Id,
                    Actif = true,
                    DateCreation = DateTime.Now,
                    Statut = "BAU"
                });
            }

            // CHEFS DE PROJET
            if (!utilisateurs.Any(u => u.UsernameWindows == "thomas.bernard"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "thomas.bernard",
                    Nom = "Bernard",
                    Prenom = "Thomas",
                    Email = "thomas.bernard@bnpparibas.com",
                    RoleId = roleCP?.Id ?? 3,
                    EquipeId = equipeTransfo?.Id,
                    Actif = true,
                    DateCreation = DateTime.Now,
                    Statut = "BAU"
                });
            }

            if (!utilisateurs.Any(u => u.UsernameWindows == "marie.lefevre"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "marie.lefevre",
                    Nom = "Lefevre",
                    Prenom = "Marie",
                    Email = "marie.lefevre@bnpparibas.com",
                    RoleId = roleCP?.Id ?? 3,
                    EquipeId = equipeChangeBau?.Id,
                    Actif = true,
                    DateCreation = DateTime.Now,
                    Statut = "PROJECTS"
                });
            }

            if (!utilisateurs.Any(u => u.UsernameWindows == "julien.moreau"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "julien.moreau",
                    Nom = "Moreau",
                    Prenom = "Julien",
                    Email = "julien.moreau@bnpparibas.com",
                    RoleId = roleCP?.Id ?? 3,
                    EquipeId = equipeTCSIM?.Id,
                    Actif = true,
                    DateCreation = DateTime.Now,
                    Statut = "BAU"
                });
            }

            // BUSINESS ANALYSTS
            if (!utilisateurs.Any(u => u.UsernameWindows == "sophie.dubois"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "sophie.dubois",
                    Nom = "Dubois",
                    Prenom = "Sophie",
                    Email = "sophie.dubois@bnpparibas.com",
                    RoleId = roleBA?.Id ?? 2,
                    EquipeId = equipeTransfo?.Id,
                    Actif = true,
                    DateCreation = DateTime.Now,
                    Statut = "BAU"
                });
            }

            if (!utilisateurs.Any(u => u.UsernameWindows == "nicolas.martin"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "nicolas.martin",
                    Nom = "Martin",
                    Prenom = "Nicolas",
                    Email = "nicolas.martin@bnpparibas.com",
                    RoleId = roleBA?.Id ?? 2,
                    EquipeId = equipeTCSIM?.Id,
                    Actif = true,
                    DateCreation = DateTime.Now,
                    Statut = "PROJECTS"
                });
            }

            if (!utilisateurs.Any(u => u.UsernameWindows == "laura.petit"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "laura.petit",
                    Nom = "Petit",
                    Prenom = "Laura",
                    Email = "laura.petit@bnpparibas.com",
                    RoleId = roleBA?.Id ?? 2,
                    EquipeId = equipeL1?.Id,
                    Actif = true,
                    DateCreation = DateTime.Now,
                    Statut = "BAU"
                });
            }

            if (!utilisateurs.Any(u => u.UsernameWindows == "alexandre.roux"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "alexandre.roux",
                    Nom = "Roux",
                    Prenom = "Alexandre",
                    Email = "alexandre.roux@bnpparibas.com",
                    RoleId = roleBA?.Id ?? 2,
                    EquipeId = equipePCC?.Id,
                    Actif = true,
                    DateCreation = DateTime.Now,
                    Statut = "BAU"
                });
            }

            // DÉVELOPPEURS
            if (!utilisateurs.Any(u => u.UsernameWindows == "pierre.durand"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "pierre.durand",
                    Nom = "Durand",
                    Prenom = "Pierre",
                    Email = "pierre.durand@bnpparibas.com",
                    RoleId = roleDev?.Id ?? 4,
                    EquipeId = equipeTransfo?.Id,
                    Actif = true,
                    DateCreation = DateTime.Now,
                    Statut = "BAU"
                });
            }

            if (!utilisateurs.Any(u => u.UsernameWindows == "julie.garnier"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "julie.garnier",
                    Nom = "Garnier",
                    Prenom = "Julie",
                    Email = "julie.garnier@bnpparibas.com",
                    RoleId = roleDev?.Id ?? 4,
                    EquipeId = equipeTransfo?.Id,
                    Actif = true,
                    DateCreation = DateTime.Now,
                    Statut = "BAU"
                });
            }

            if (!utilisateurs.Any(u => u.UsernameWindows == "antoine.laurent"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "antoine.laurent",
                    Nom = "Laurent",
                    Prenom = "Antoine",
                    Email = "antoine.laurent@bnpparibas.com",
                    RoleId = roleDev?.Id ?? 4,
                    EquipeId = equipeData?.Id,
                    Actif = true,
                    DateCreation = DateTime.Now,
                    Statut = "PROJECTS"
                });
            }

            if (!utilisateurs.Any(u => u.UsernameWindows == "camille.bonnet"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "camille.bonnet",
                    Nom = "Bonnet",
                    Prenom = "Camille",
                    Email = "camille.bonnet@bnpparibas.com",
                    RoleId = roleDev?.Id ?? 4,
                    EquipeId = equipeChangeBau?.Id,
                    Actif = true,
                    DateCreation = DateTime.Now,
                    Statut = "PROJECTS"
                });
            }

            if (!utilisateurs.Any(u => u.UsernameWindows == "maxime.richard"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "maxime.richard",
                    Nom = "Richard",
                    Prenom = "Maxime",
                    Email = "maxime.richard@bnpparibas.com",
                    RoleId = roleDev?.Id ?? 4,
                    EquipeId = equipeData?.Id,
                    Actif = true,
                    DateCreation = DateTime.Now,
                    Statut = "BAU"
                });
            }

            if (!utilisateurs.Any(u => u.UsernameWindows == "claire.simon"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "claire.simon",
                    Nom = "Simon",
                    Prenom = "Claire",
                    Email = "claire.simon@bnpparibas.com",
                    RoleId = roleDev?.Id ?? 4,
                    EquipeId = equipeWatchtower?.Id,
                    Actif = true,
                    DateCreation = DateTime.Now,
                    Statut = "BAU"
                });
            }

            if (!utilisateurs.Any(u => u.UsernameWindows == "lucas.blanc"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "lucas.blanc",
                    Nom = "Blanc",
                    Prenom = "Lucas",
                    Email = "lucas.blanc@bnpparibas.com",
                    RoleId = roleDev?.Id ?? 4,
                    EquipeId = equipeTCSIM?.Id,
                    Actif = true,
                    DateCreation = DateTime.Now,
                    Statut = "BAU"
                });
            }

            if (!utilisateurs.Any(u => u.UsernameWindows == "emma.girard"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "emma.girard",
                    Nom = "Girard",
                    Prenom = "Emma",
                    Email = "emma.girard@bnpparibas.com",
                    RoleId = roleDev?.Id ?? 4,
                    EquipeId = equipeTactical?.Id,
                    Actif = true,
                    DateCreation = DateTime.Now,
                    Statut = "BAU"
                });
            }

            // UTILISATEURS SUPPLÉMENTAIRES pour atteindre les totaux de l'image
            
            // Transfo & Implem: besoin de 3 de plus (total 7: 5 BAU + 2 PROJECTS) 
            if (!utilisateurs.Any(u => u.UsernameWindows == "francois.lambert"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "francois.lambert",
                    Nom = "Lambert",
                    Prenom = "François",
                    Email = "francois.lambert@bnpparibas.com",
                    RoleId = roleDev?.Id ?? 4,
                    EquipeId = equipeTransfo?.Id,
                    Actif = true,
                    DateCreation = DateTime.Now,
                    Statut = "BAU"
                });
            }

            if (!utilisateurs.Any(u => u.UsernameWindows == "isabelle.rousseau"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "isabelle.rousseau",
                    Nom = "Rousseau",
                    Prenom = "Isabelle",
                    Email = "isabelle.rousseau@bnpparibas.com",
                    RoleId = roleDev?.Id ?? 4,
                    EquipeId = equipeTransfo?.Id,
                    Actif = true,
                    DateCreation = DateTime.Now,
                    Statut = "BAU"
                });
            }

            if (!utilisateurs.Any(u => u.UsernameWindows == "vincent.morel"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "vincent.morel",
                    Nom = "Morel",
                    Prenom = "Vincent",
                    Email = "vincent.morel@bnpparibas.com",
                    RoleId = roleBA?.Id ?? 2,
                    EquipeId = equipeTransfo?.Id,
                    Actif = true,
                    DateCreation = DateTime.Now,
                    Statut = "PROJECTS"
                });
            }

            // TCS/IM: besoin de 3 de plus (total 6: 5 BAU + 1 PROJECTS)
            if (!utilisateurs.Any(u => u.UsernameWindows == "stephanie.andre"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "stephanie.andre",
                    Nom = "André",
                    Prenom = "Stéphanie",
                    Email = "stephanie.andre@bnpparibas.com",
                    RoleId = roleDev?.Id ?? 4,
                    EquipeId = equipeTCSIM?.Id,
                    Actif = true,
                    DateCreation = DateTime.Now,
                    Statut = "BAU"
                });
            }

            if (!utilisateurs.Any(u => u.UsernameWindows == "marc.fontaine"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "marc.fontaine",
                    Nom = "Fontaine",
                    Prenom = "Marc",
                    Email = "marc.fontaine@bnpparibas.com",
                    RoleId = roleDev?.Id ?? 4,
                    EquipeId = equipeTCSIM?.Id,
                    Actif = true,
                    DateCreation = DateTime.Now,
                    Statut = "BAU"
                });
            }

            if (!utilisateurs.Any(u => u.UsernameWindows == "valerie.chevalier"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "valerie.chevalier",
                    Nom = "Chevalier",
                    Prenom = "Valérie",
                    Email = "valerie.chevalier@bnpparibas.com",
                    RoleId = roleBA?.Id ?? 2,
                    EquipeId = equipeTCSIM?.Id,
                    Actif = true,
                    DateCreation = DateTime.Now,
                    Statut = "Temporary"
                });
            }

            // Data Office: besoin de 3 de plus (total 5: 3 BAU + 2 PROJECTS)
            if (!utilisateurs.Any(u => u.UsernameWindows == "david.gauthier"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "david.gauthier",
                    Nom = "Gauthier",
                    Prenom = "David",
                    Email = "david.gauthier@bnpparibas.com",
                    RoleId = roleDev?.Id ?? 4,
                    EquipeId = equipeData?.Id,
                    Actif = true,
                    DateCreation = DateTime.Now,
                    Statut = "BAU"
                });
            }

            if (!utilisateurs.Any(u => u.UsernameWindows == "nathalie.perrin"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "nathalie.perrin",
                    Nom = "Perrin",
                    Prenom = "Nathalie",
                    Email = "nathalie.perrin@bnpparibas.com",
                    RoleId = roleBA?.Id ?? 2,
                    EquipeId = equipeData?.Id,
                    Actif = true,
                    DateCreation = DateTime.Now,
                    Statut = "Hiring ongoing"
                });
            }

            if (!utilisateurs.Any(u => u.UsernameWindows == "olivier.michel"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "olivier.michel",
                    Nom = "Michel",
                    Prenom = "Olivier",
                    Email = "olivier.michel@bnpparibas.com",
                    RoleId = roleDev?.Id ?? 4,
                    EquipeId = equipeData?.Id,
                    Actif = true,
                    DateCreation = DateTime.Now,
                    Statut = "BAU"
                });
            }

            // Tactical Solutions: besoin de 1 de plus (total 3: 2 BAU + 1 Temporary)
            if (!utilisateurs.Any(u => u.UsernameWindows == "patrick.roy"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "patrick.roy",
                    Nom = "Roy",
                    Prenom = "Patrick",
                    Email = "patrick.roy@bnpparibas.com",
                    RoleId = roleDev?.Id ?? 4,
                    EquipeId = equipeTactical?.Id,
                    Actif = true,
                    DateCreation = DateTime.Now,
                    Statut = "Temporary"
                });
            }

            // L1 Support: besoin de 1 de plus (total 2: 2 BAU)
            if (!utilisateurs.Any(u => u.UsernameWindows == "sandrine.bertrand"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "sandrine.bertrand",
                    Nom = "Bertrand",
                    Prenom = "Sandrine",
                    Email = "sandrine.bertrand@bnpparibas.com",
                    RoleId = roleDev?.Id ?? 4,
                    EquipeId = equipeL1?.Id,
                    Actif = true,
                    DateCreation = DateTime.Now,
                    Statut = "BAU"
                });
            }

            // PCC: besoin de 1 de plus (total 2: 1 BAU + 1 Hiring ongoing)
            if (!utilisateurs.Any(u => u.UsernameWindows == "christophe.renard"))
            {
                _database.AddOrUpdateUtilisateur(new Utilisateur
                {
                    UsernameWindows = "christophe.renard",
                    Nom = "Renard",
                    Prenom = "Christophe",
                    Email = "christophe.renard@bnpparibas.com",
                    RoleId = roleDev?.Id ?? 4,
                    EquipeId = equipePCC?.Id,
                    Actif = true,
                    DateCreation = DateTime.Now,
                    Statut = "Hiring ongoing"
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
