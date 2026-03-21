using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BacklogManager.Domain;
using BacklogManager.Services.Sync;

namespace BacklogManager.Services
{
    /// <summary>
    /// Service de test d'intégration multi-utilisateur pour la synchronisation.
    /// 
    /// Protocole :
    ///   1. Chaque poste écrit un fichier "signal" sur le NAS pour signaler sa présence
    ///   2. Les 2 postes attendent de voir le signal de l'autre (timeout 2 min)
    ///   3. Pendant ~5 minutes, chaque poste crée des entités préfixées par son clientId
    ///   4. Après création, on force un push + réconciliation complète
    ///   5. Le script de vérification compare les entités attendues vs présentes
    /// </summary>
    public class SyncTestService
    {
        private readonly IDatabase _database;
        private readonly SyncEngine _syncEngine;
        private readonly string _clientId;
        private readonly string _signalDir;

        private readonly List<string> _log = new List<string>();
        private readonly List<TestEntity> _expectedEntities = new List<TestEntity>();
        private readonly List<TestFailure> _failures = new List<TestFailure>();
        private CancellationTokenSource _cts;
        private bool _isRunning;

        public bool IsRunning => _isRunning;
        public IReadOnlyList<string> Log => _log;
        public IReadOnlyList<TestFailure> Failures => _failures;
        public IReadOnlyList<TestEntity> ExpectedEntities => _expectedEntities;

        public event Action<string> LogUpdated;
        public event Action<int> ProgressChanged;
        public event Action<SyncTestResult> TestCompleted;

        public SyncTestService(IDatabase database, SyncEngine syncEngine, string clientId, string nasSyncPath)
        {
            _database = database;
            _syncEngine = syncEngine;
            _clientId = clientId;
            _signalDir = System.IO.Path.Combine(nasSyncPath, "test_signals");
        }

        public void Cancel()
        {
            _cts?.Cancel();
        }

        /// <summary>
        /// Lance le test complet (à exécuter sur un thread background).
        /// </summary>
        public async Task RunAsync()
        {
            _isRunning = true;
            _cts = new CancellationTokenSource();
            _log.Clear();
            _expectedEntities.Clear();
            _failures.Clear();

            try
            {
                // ══════════════════════════════════════════════════════════
                // PHASE 1 : Attente du second poste
                // ══════════════════════════════════════════════════════════
                AddLog("═══ PHASE 1 : Synchronisation des postes ═══");
                bool partnerFound = await WaitForPartner(_cts.Token);
                if (!partnerFound)
                {
                    AddLog("❌ Timeout : le second poste n'a pas été détecté après 2 minutes.");
                    Complete(false);
                    return;
                }

                // Petit décalage pour éviter les collisions exactes de timestamp
                await Task.Delay(new Random().Next(100, 500), _cts.Token);

                // ══════════════════════════════════════════════════════════
                // PHASE 2 : Création de données (~5 minutes)
                // ══════════════════════════════════════════════════════════
                AddLog("═══ PHASE 2 : Création de données de test ═══");
                int totalSteps = 12;
                int step = 0;

                // --- Utilisateurs & Rôles ---
                step++; ReportProgress(step, totalSteps);
                CreateTestUtilisateurs();
                await SyncAndWait(5000, _cts.Token);

                // --- Devs ---
                step++; ReportProgress(step, totalSteps);
                CreateTestDevs();
                await SyncAndWait(5000, _cts.Token);

                // --- Programmes ---
                step++; ReportProgress(step, totalSteps);
                CreateTestProgrammes();
                await SyncAndWait(5000, _cts.Token);

                // --- Équipes ---
                step++; ReportProgress(step, totalSteps);
                CreateTestEquipes();
                await SyncAndWait(5000, _cts.Token);

                // --- Projets ---
                step++; ReportProgress(step, totalSteps);
                CreateTestProjets();
                await SyncAndWait(5000, _cts.Token);

                // --- Sprints ---
                step++; ReportProgress(step, totalSteps);
                CreateTestSprints();
                await SyncAndWait(5000, _cts.Token);

                // --- Demandes ---
                step++; ReportProgress(step, totalSteps);
                CreateTestDemandes();
                await SyncAndWait(10000, _cts.Token);

                // --- BacklogItems ---
                step++; ReportProgress(step, totalSteps);
                CreateTestBacklogItems();
                await SyncAndWait(10000, _cts.Token);

                // --- Commentaires ---
                step++; ReportProgress(step, totalSteps);
                CreateTestCommentaires();
                await SyncAndWait(5000, _cts.Token);

                // --- CRA ---
                step++; ReportProgress(step, totalSteps);
                CreateTestCRAs();
                await SyncAndWait(5000, _cts.Token);

                // --- Disponibilités ---
                step++; ReportProgress(step, totalSteps);
                CreateTestDisponibilites();
                await SyncAndWait(5000, _cts.Token);

                // --- Modifications & Mises à jour ---
                step++; ReportProgress(step, totalSteps);
                CreateTestModifications();
                await SyncAndWait(10000, _cts.Token);

                // ══════════════════════════════════════════════════════════
                // PHASE 3 : Sync finale + attente convergence
                // ══════════════════════════════════════════════════════════
                AddLog("═══ PHASE 3 : Synchronisation finale ═══");
                AddLog("Force push + réconciliation complète...");
                _syncEngine.ForceFullReconciliation();
                await Task.Delay(30000, _cts.Token); // 30s d'attente pour convergence
                _syncEngine.ForceFullReconciliation();
                await Task.Delay(15000, _cts.Token); // 15s supplémentaires

                // ══════════════════════════════════════════════════════════
                // PHASE 4 : Vérification
                // ══════════════════════════════════════════════════════════
                AddLog("═══ PHASE 4 : Vérification des données ═══");
                VerifyAllEntities();

                bool success = _failures.Count == 0;
                Complete(success);
            }
            catch (OperationCanceledException)
            {
                AddLog("⚠️ Test annulé par l'utilisateur.");
                Complete(false);
            }
            catch (Exception ex)
            {
                AddLog($"❌ Erreur fatale : {ex.Message}");
                Complete(false);
            }
            finally
            {
                CleanupSignals();
                _isRunning = false;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  SIGNAL DE SYNCHRONISATION INTER-POSTES
        // ═══════════════════════════════════════════════════════════════════

        private async Task<bool> WaitForPartner(CancellationToken ct)
        {
            try
            {
                if (!System.IO.Directory.Exists(_signalDir))
                    System.IO.Directory.CreateDirectory(_signalDir);
            }
            catch { AddLog("⚠️ Impossible de créer le dossier signal sur le NAS."); return false; }

            string mySignal = System.IO.Path.Combine(_signalDir, $"ready_{_clientId}.signal");
            System.IO.File.WriteAllText(mySignal, DateTime.UtcNow.ToString("o"));
            AddLog($"📡 Signal envoyé ({_clientId}). En attente du second poste...");

            var deadline = DateTime.UtcNow.AddMinutes(2);
            while (DateTime.UtcNow < deadline)
            {
                ct.ThrowIfCancellationRequested();

                var signals = System.IO.Directory.GetFiles(_signalDir, "ready_*.signal");
                var others = signals.Where(s => !s.Contains(_clientId)).ToArray();
                if (others.Length > 0)
                {
                    string partner = System.IO.Path.GetFileNameWithoutExtension(others[0])
                        .Replace("ready_", "");
                    AddLog($"✅ Second poste détecté : {partner}");
                    return true;
                }

                await Task.Delay(2000, ct);
            }

            return false;
        }

        private void CleanupSignals()
        {
            try
            {
                string mySignal = System.IO.Path.Combine(_signalDir, $"ready_{_clientId}.signal");
                if (System.IO.File.Exists(mySignal))
                    System.IO.File.Delete(mySignal);
            }
            catch { }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  CRÉATION DE DONNÉES DE TEST
        // ═══════════════════════════════════════════════════════════════════

        private string Tag => $"[TEST_{_clientId}]";

        private void CreateTestUtilisateurs()
        {
            AddLog("👥 Création de 3 utilisateurs de test...");
            for (int i = 1; i <= 3; i++)
            {
                var u = new Utilisateur
                {
                    UsernameWindows = $"TEST_{_clientId}_{i}",
                    Nom = $"TestNom_{_clientId}_{i}",
                    Prenom = $"TestPrenom_{i}",
                    Email = $"test_{_clientId}_{i}@test.com",
                    RoleId = 1,
                    Actif = true,
                    DateCreation = DateTime.Now
                };
                var result = _database.AddOrUpdateUtilisateur(u);
                Track("Utilisateurs", result.Id, u.UsernameWindows);
                AddLog($"   ✓ Utilisateur {u.UsernameWindows} (Id={result.Id})");
            }
        }

        private void CreateTestDevs()
        {
            AddLog("🔧 Création de 2 devs de test...");
            for (int i = 1; i <= 2; i++)
            {
                var d = new Dev
                {
                    Nom = $"{Tag}_Dev{i}",
                    Initiales = $"T{_clientId.Substring(0, Math.Min(1, _clientId.Length))}{i}",
                    Actif = true
                };
                var result = _database.AddOrUpdateDev(d);
                Track("Devs", result.Id, d.Nom);
                AddLog($"   ✓ Dev {d.Nom} (Id={result.Id})");
            }
        }

        private void CreateTestProgrammes()
        {
            AddLog("📊 Création de 2 programmes de test...");
            for (int i = 1; i <= 2; i++)
            {
                var p = new Programme
                {
                    Nom = $"{Tag}_Programme{i}",
                    Code = $"PGM_{_clientId}_{i}",
                    Description = $"Programme de test {i} créé par {_clientId}",
                    Actif = true,
                    DateCreation = DateTime.Now,
                    StatutGlobal = "On Track"
                };
                _database.AjouterProgramme(p);
                // Relire pour obtenir l'Id
                var all = _database.GetAllProgrammes();
                var created = all.LastOrDefault(x => x.Code == p.Code);
                if (created != null) Track("Programmes", created.Id, p.Nom);
                AddLog($"   ✓ Programme {p.Nom}");
            }
        }

        private void CreateTestEquipes()
        {
            AddLog("🏢 Création de 2 équipes de test...");
            for (int i = 1; i <= 2; i++)
            {
                var e = new Equipe
                {
                    Nom = $"{Tag}_Equipe{i}",
                    Code = $"EQ_{_clientId}_{i}",
                    Description = $"Équipe de test {i}",
                    Actif = true,
                    DateCreation = DateTime.Now
                };
                _database.AjouterEquipe(e);
                var all = _database.GetAllEquipes();
                var created = all.LastOrDefault(x => x.Code == e.Code);
                if (created != null) Track("Equipes", created.Id, e.Nom);
                AddLog($"   ✓ Équipe {e.Nom}");
            }
        }

        private void CreateTestProjets()
        {
            AddLog("📁 Création de 3 projets de test...");
            string[] types = { "Data", "Digital", "Regulatory" };
            for (int i = 1; i <= 3; i++)
            {
                var p = new Projet
                {
                    Nom = $"{Tag}_Projet{i}",
                    Description = $"Projet de test #{i} par {_clientId}",
                    DateCreation = DateTime.Now,
                    Actif = true,
                    CouleurHex = "#00915A",
                    TypeProjet = types[(i - 1) % types.Length],
                    Categorie = i % 2 == 0 ? "BAU" : "TRANSFO",
                    Priorite = "Medium",
                    StatutRAG = "Green",
                    Phase = "Implementation"
                };
                var result = _database.AddOrUpdateProjet(p);
                Track("Projets", result.Id, p.Nom);
                AddLog($"   ✓ Projet {p.Nom} (Id={result.Id})");
            }
        }

        private void CreateTestSprints()
        {
            AddLog("🏃 Création de 2 sprints de test...");
            var projetIds = _expectedEntities.Where(e => e.Table == "Projets").Select(e => e.EntityId).ToList();
            if (projetIds.Count == 0)
                throw new InvalidOperationException("Aucun projet créé avant les sprints");

            for (int i = 1; i <= 2; i++)
            {
                var s = new Sprint
                {
                    Nom = $"{Tag}_Sprint{i}",
                    ProjetId = projetIds[(i - 1) % projetIds.Count],
                    DateDebut = DateTime.Now.AddDays(i * 14),
                    DateFin = DateTime.Now.AddDays(i * 14 + 13),
                    Objectif = $"Objectif test sprint {i} ({_clientId})",
                    EstActif = i == 1,
                    EstCloture = false
                };
                var result = _database.AddOrUpdateSprint(s);
                Track("Sprints", result.Id, s.Nom);
                AddLog($"   ✓ Sprint {s.Nom} (Id={result.Id})");
            }
        }

        private void CreateTestDemandes()
        {
            AddLog("📋 Création de 5 demandes de test...");
            var users = _database.GetUtilisateurs();
            int demandeurId = users.FirstOrDefault()?.Id ?? 1;

            StatutDemande[] statuts = {
                StatutDemande.EnAttenteSpecification,
                StatutDemande.EnAttenteChiffrage,
                StatutDemande.Acceptee,
                StatutDemande.EnCours,
                StatutDemande.Livree
            };
            Criticite[] criticites = { Criticite.Basse, Criticite.Moyenne, Criticite.Haute, Criticite.Bloquante, Criticite.Moyenne };

            for (int i = 1; i <= 5; i++)
            {
                var d = new Demande
                {
                    Titre = $"{Tag}_Demande{i}",
                    Description = $"Description demande test #{i} par {_clientId}",
                    Specifications = $"Spécifications détaillées pour la demande {i}",
                    ContexteMetier = "Test de synchronisation multi-poste",
                    BeneficesAttendus = "Validation de la synchronisation",
                    DemandeurId = demandeurId,
                    Type = i % 2 == 0 ? TypeDemande.Dev : TypeDemande.Run,
                    Criticite = criticites[(i - 1) % criticites.Length],
                    Statut = statuts[(i - 1) % statuts.Length],
                    DateCreation = DateTime.Now,
                    ChiffrageEstimeJours = i * 0.5,
                    EstArchivee = false,
                    Priorite = i <= 2 ? "High" : "Medium"
                };
                var result = _database.AddOrUpdateDemande(d);
                Track("Demandes", result.Id, d.Titre);
                AddLog($"   ✓ Demande {d.Titre} (Id={result.Id})");
            }
        }

        private void CreateTestBacklogItems()
        {
            AddLog("📝 Création de 8 backlog items de test...");
            var projets = _database.GetProjets().Where(p => p.Actif).ToList();
            var sprints = _database.GetSprints().Where(s => !s.EstCloture).ToList();
            int? projetId = projets.FirstOrDefault()?.Id;
            int? sprintId = sprints.FirstOrDefault()?.Id;

            Statut[] statuts = { Statut.Afaire, Statut.EnAttente, Statut.APrioriser, Statut.EnCours, Statut.Test, Statut.Termine, Statut.Afaire, Statut.EnCours };
            Priorite[] priorites = { Priorite.Urgent, Priorite.Haute, Priorite.Moyenne, Priorite.Basse, Priorite.Haute, Priorite.Moyenne, Priorite.Basse, Priorite.Urgent };
            TypeDemande[] types = { TypeDemande.Dev, TypeDemande.Run, TypeDemande.Support, TypeDemande.Dev, TypeDemande.Autre, TypeDemande.Dev, TypeDemande.Run, TypeDemande.Support };

            for (int i = 1; i <= 8; i++)
            {
                var item = new BacklogItem
                {
                    Titre = $"{Tag}_Item{i}",
                    Description = $"Description backlog item #{i} par {_clientId}. Test de synchronisation complet.",
                    TypeDemande = types[(i - 1) % types.Length],
                    Statut = statuts[(i - 1) % statuts.Length],
                    Priorite = priorites[(i - 1) % priorites.Length],
                    ProjetId = projetId,
                    SprintId = sprintId,
                    Complexite = i * 2,
                    ChiffrageHeures = i * 4.0,
                    DateCreation = DateTime.Now,
                    DateDerniereMaj = DateTime.Now,
                    EstArchive = false
                };
                var result = _database.AddOrUpdateBacklogItem(item);
                Track("BacklogItems", result.Id, item.Titre);
                AddLog($"   ✓ BacklogItem {item.Titre} (Id={result.Id})");
            }
        }

        private void CreateTestCommentaires()
        {
            AddLog("💬 Création de 4 commentaires de test...");
            var items = _database.GetBacklogItems().Where(b => b.Titre.Contains(Tag)).Take(4).ToList();
            var users = _database.GetUtilisateurs();
            int auteurId = users.FirstOrDefault()?.Id ?? 1;

            for (int i = 0; i < Math.Min(4, items.Count); i++)
            {
                var c = new Commentaire
                {
                    BacklogItemId = items[i].Id,
                    AuteurId = auteurId,
                    Contenu = $"{Tag} Commentaire test #{i + 1} sur {items[i].Titre}",
                    DateCreation = DateTime.Now
                };
                var result = _database.AddCommentaire(c);
                Track("Commentaires", result.Id, c.Contenu.Substring(0, Math.Min(50, c.Contenu.Length)));
                AddLog($"   ✓ Commentaire #{i + 1} sur {items[i].Titre}");
            }
        }

        private void CreateTestCRAs()
        {
            AddLog("⏱️ Création de 4 CRA de test...");
            var items = _database.GetBacklogItems().Where(b => b.Titre.Contains(Tag)).Take(4).ToList();
            var devs = _database.GetDevs().Where(d => d.Actif).ToList();
            int devId = devs.FirstOrDefault()?.Id ?? 1;

            for (int i = 0; i < Math.Min(4, items.Count); i++)
            {
                var cra = new CRA
                {
                    BacklogItemId = items[i].Id,
                    DevId = devId,
                    Date = DateTime.Now.AddDays(-i),
                    HeuresTravaillees = (i + 1) * 2.0,
                    Commentaire = $"{Tag} CRA test #{i + 1}",
                    DateCreation = DateTime.Now,
                    EstPrevisionnel = false,
                    EstValide = true
                };
                _database.SaveCRA(cra);
                // CRA n'a pas de retour direct — relire
                var all = _database.GetAllCRAs();
                var created = all.LastOrDefault(x => x.Commentaire != null && x.Commentaire.Contains($"{Tag} CRA test #{i + 1}"));
                if (created != null) Track("CRA", created.Id, cra.Commentaire);
                AddLog($"   ✓ CRA #{i + 1} ({cra.HeuresTravaillees}h)");
            }
        }

        private void CreateTestDisponibilites()
        {
            AddLog("📅 Création de 3 disponibilités de test...");
            var users = _database.GetUtilisateurs();
            int userId = users.FirstOrDefault()?.Id ?? 1;

            TypeIndisponibilite[] types = { TypeIndisponibilite.Conges, TypeIndisponibilite.Formation, TypeIndisponibilite.Absence };
            for (int i = 1; i <= 3; i++)
            {
                var d = new Disponibilite
                {
                    UtilisateurId = userId,
                    DateDebut = DateTime.Now.AddDays(i * 7),
                    DateFin = DateTime.Now.AddDays(i * 7 + 4),
                    Type = types[(i - 1) % types.Length],
                    Motif = $"{Tag} Dispo test #{i}",
                    EstValide = true
                };
                var result = _database.AddOrUpdateDisponibilite(d);
                Track("Disponibilites", result.Id, d.Motif);
                AddLog($"   ✓ Disponibilité #{i} ({d.Type})");
            }
        }

        private void CreateTestModifications()
        {
            AddLog("✏️ Modifications sur les entités existantes...");

            // Modifier des BacklogItems créés par ce poste
            var myItems = _database.GetBacklogItems()
                .Where(b => b.Titre != null && b.Titre.Contains(Tag))
                .Take(3).ToList();

            foreach (var item in myItems)
            {
                item.Description = $"{item.Description} [MODIFIÉ par {_clientId} à {DateTime.Now:HH:mm:ss}]";
                item.DateDerniereMaj = DateTime.Now;
                if (item.Statut == Statut.Afaire) item.Statut = Statut.EnCours;
                else if (item.Statut == Statut.EnCours) item.Statut = Statut.Test;
                _database.AddOrUpdateBacklogItem(item);
                AddLog($"   ✏️ BacklogItem {item.Titre} → statut={item.Statut}");
            }

            // Modifier des Demandes
            var myDemandes = _database.GetDemandes()
                .Where(d => d.Titre != null && d.Titre.Contains(Tag))
                .Take(2).ToList();

            foreach (var dem in myDemandes)
            {
                dem.Description = $"{dem.Description} [MàJ {_clientId}]";
                if (dem.Statut == StatutDemande.EnAttenteSpecification)
                    dem.Statut = StatutDemande.EnAttenteChiffrage;
                _database.AddOrUpdateDemande(dem);
                AddLog($"   ✏️ Demande {dem.Titre} → statut={dem.Statut}");
            }

            // Modifier des Projets
            var myProjets = _database.GetProjets()
                .Where(p => p.Nom != null && p.Nom.Contains(Tag))
                .Take(1).ToList();

            foreach (var proj in myProjets)
            {
                proj.Description = $"{proj.Description} [MAJ {_clientId}]";
                proj.StatutRAG = "Amber";
                _database.AddOrUpdateProjet(proj);
                AddLog($"   ✏️ Projet {proj.Nom} → RAG=Amber");
            }

            AddLog($"   ✓ {myItems.Count + myDemandes.Count + myProjets.Count} entités modifiées");
        }

        // ═══════════════════════════════════════════════════════════════════
        //  VÉRIFICATION
        // ═══════════════════════════════════════════════════════════════════

        private void VerifyAllEntities()
        {
            AddLog("🔍 Vérification de toutes les entités de test...");

            // Vérifier par type
            VerifyTable("Utilisateurs", () => _database.GetUtilisateurs().Cast<dynamic>().ToList(), e => e.UsernameWindows);
            VerifyTable("Devs", () => _database.GetDevs().Cast<dynamic>().ToList(), e => e.Nom);
            VerifyTable("Programmes", () => _database.GetAllProgrammes().Cast<dynamic>().ToList(), e => e.Nom);
            VerifyTable("Equipes", () => _database.GetAllEquipes().Cast<dynamic>().ToList(), e => e.Nom);
            VerifyTable("Projets", () => _database.GetProjets().Cast<dynamic>().ToList(), e => e.Nom);
            VerifyTable("Sprints", () => _database.GetSprints().Cast<dynamic>().ToList(), e => e.Nom);
            VerifyTable("Demandes", () => _database.GetDemandes().Cast<dynamic>().ToList(), e => e.Titre);
            VerifyTable("BacklogItems", () => _database.GetBacklogItems().Cast<dynamic>().ToList(), e => e.Titre);
            VerifyTable("Commentaires", () => _database.GetCommentaires().Cast<dynamic>().ToList(), e => e.Contenu);
            VerifyTable("CRA", () => _database.GetAllCRAs().Cast<dynamic>().ToList(), e => e.Commentaire);
            VerifyTable("Disponibilites", () => _database.GetDisponibilites().Cast<dynamic>().ToList(), e => e.Motif);

            // Résumé
            int total = _expectedEntities.Count;
            int found = _expectedEntities.Count(e => e.Found);
            int missing = total - found;

            AddLog($"");
            AddLog($"═══ RÉSUMÉ VÉRIFICATION ═══");
            AddLog($"   Total entités attendues : {total}");
            AddLog($"   ✅ Trouvées : {found}");
            AddLog($"   ❌ Manquantes : {missing}");

            if (_failures.Count > 0)
            {
                AddLog($"");
                AddLog($"═══ ÉCHECS DÉTAILLÉS ({_failures.Count}) ═══");
                foreach (var f in _failures)
                {
                    AddLog($"   ❌ [{f.Table}] {f.SearchKey} — {f.Reason}");
                }
            }
        }

        private void VerifyTable(string table, Func<List<dynamic>> getData, Func<dynamic, string> getKey)
        {
            var expected = _expectedEntities.Where(e => e.Table == table).ToList();
            if (expected.Count == 0) return;

            try
            {
                var data = getData();
                var keys = new HashSet<string>();
                foreach (var item in data)
                {
                    try
                    {
                        string k = getKey(item);
                        if (k != null) keys.Add(k);
                    }
                    catch { }
                }

                int found = 0;
                foreach (var exp in expected)
                {
                    if (keys.Contains(exp.SearchKey))
                    {
                        exp.Found = true;
                        found++;
                    }
                    else
                    {
                        _failures.Add(new TestFailure
                        {
                            Table = table,
                            SearchKey = exp.SearchKey,
                            Reason = "Entité non trouvée après synchronisation"
                        });
                    }
                }

                string status = found == expected.Count ? "✅" : "⚠️";
                AddLog($"   {status} {table}: {found}/{expected.Count} trouvées");
            }
            catch (Exception ex)
            {
                AddLog($"   ❌ {table}: erreur de lecture — {ex.Message}");
                foreach (var exp in expected)
                {
                    _failures.Add(new TestFailure { Table = table, SearchKey = exp.SearchKey, Reason = ex.Message });
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════════════════

        private async Task SyncAndWait(int delayMs, CancellationToken ct)
        {
            // Force un push immédiat puis attend la propagation
            _syncEngine.NotifyNewOperation();
            await Task.Delay(delayMs, ct);
        }

        private void Track(string table, int id, string searchKey)
        {
            _expectedEntities.Add(new TestEntity
            {
                Table = table,
                EntityId = id,
                SearchKey = searchKey,
                ClientId = _clientId,
                Found = false
            });
        }

        private void AddLog(string message)
        {
            string timestamped = $"[{DateTime.Now:HH:mm:ss}] {message}";
            _log.Add(timestamped);
            LogUpdated?.Invoke(timestamped);
        }

        private void ReportProgress(int step, int total)
        {
            int pct = (int)(step * 100.0 / total);
            ProgressChanged?.Invoke(pct);
            AddLog($"── Étape {step}/{total} ({pct}%) ──");
        }

        private void Complete(bool success)
        {
            var result = new SyncTestResult
            {
                Success = success,
                TotalExpected = _expectedEntities.Count,
                TotalFound = _expectedEntities.Count(e => e.Found),
                Failures = _failures.ToList(),
                Log = _log.ToList(),
                ClientId = _clientId,
                CompletedAtUtc = DateTime.UtcNow
            };

            AddLog(success
                ? "🎉 TEST RÉUSSI — Toutes les entités sont synchronisées !"
                : $"❌ TEST ÉCHOUÉ — {_failures.Count} entité(s) manquante(s)");

            TestCompleted?.Invoke(result);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  DTOs
    // ═══════════════════════════════════════════════════════════════════════

    public class TestEntity
    {
        public string Table { get; set; }
        public int EntityId { get; set; }
        public string SearchKey { get; set; }
        public string ClientId { get; set; }
        public bool Found { get; set; }
    }

    public class TestFailure
    {
        public string Table { get; set; }
        public string SearchKey { get; set; }
        public string Reason { get; set; }
    }

    public class SyncTestResult
    {
        public bool Success { get; set; }
        public int TotalExpected { get; set; }
        public int TotalFound { get; set; }
        public List<TestFailure> Failures { get; set; }
        public List<string> Log { get; set; }
        public string ClientId { get; set; }
        public DateTime CompletedAtUtc { get; set; }
    }
}
