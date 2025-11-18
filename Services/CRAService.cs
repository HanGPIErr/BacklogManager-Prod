using System;
using System.Collections.Generic;
using System.Linq;
using BacklogManager.Domain;

namespace BacklogManager.Services
{
    public class CRAService
    {
        private readonly IDatabase _db;

        public CRAService(IDatabase db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>
        /// Sauvegarde un CRA avec validations
        /// </summary>
        public void SaveCRA(CRA cra)
        {
            // Validations
            ValidateCRA(cra);

            // Vérification charge quotidienne
            var chargeJour = GetChargeParJour(cra.DevId, cra.Date);
            if (chargeJour + cra.HeuresTravaillees > 24)
            {
                throw new InvalidOperationException($"Impossible de saisir {cra.HeuresTravaillees}h : le total du jour dépasserait 24h (actuellement {chargeJour}h).");
            }

            _db.SaveCRA(cra);

            // Mise à jour automatique de DateDebut si première saisie
            var backlogItem = _db.GetBacklogItems().FirstOrDefault(b => b.Id == cra.BacklogItemId);
            if (backlogItem != null && !backlogItem.DateDebut.HasValue)
            {
                backlogItem.DateDebut = cra.Date;
                _db.AddOrUpdateBacklogItem(backlogItem);
            }
        }

        /// <summary>
        /// Récupère tous les CRA pour une tâche
        /// </summary>
        public List<CRA> GetCRAsByBacklogItem(int backlogItemId)
        {
            return _db.GetCRAs(backlogItemId: backlogItemId);
        }

        /// <summary>
        /// Récupère les CRA d'un développeur sur une période
        /// </summary>
        public List<CRA> GetCRAsByDev(int devId, DateTime? dateDebut = null, DateTime? dateFin = null)
        {
            return _db.GetCRAs(devId: devId, dateDebut: dateDebut, dateFin: dateFin);
        }

        /// <summary>
        /// Récupère tous les CRA sur une période (tous devs)
        /// </summary>
        public List<CRA> GetCRAsByPeriod(DateTime dateDebut, DateTime dateFin)
        {
            return _db.GetCRAs(dateDebut: dateDebut, dateFin: dateFin);
        }

        /// <summary>
        /// Calcule le temps réel total passé sur une tâche
        /// </summary>
        public double GetTempsReelTache(int backlogItemId)
        {
            var cras = GetCRAsByBacklogItem(backlogItemId);
            // Exclure les CRA prévisionnels (dates futures) du calcul du temps réel
            var aujourdhui = DateTime.Now.Date;
            return cras.Where(c => c.Date <= aujourdhui && !c.EstPrevisionnel).Sum(c => c.HeuresTravaillees);
        }

        /// <summary>
        /// Calcule la charge totale d'un dev pour une journée
        /// </summary>
        public double GetChargeParJour(int devId, DateTime date)
        {
            var dateOnly = date.Date;
            var cras = _db.GetCRAs(devId: devId, dateDebut: dateOnly, dateFin: dateOnly);
            return cras.Sum(c => c.HeuresTravaillees);
        }

        /// <summary>
        /// Calcule l'écart entre chiffrage et temps réel (en heures)
        /// </summary>
        public double GetEcartTache(int backlogItemId)
        {
            var backlogItem = _db.GetBacklogItems().FirstOrDefault(b => b.Id == backlogItemId);
            if (backlogItem == null || !backlogItem.ChiffrageHeures.HasValue)
                return 0;

            var tempsReel = GetTempsReelTache(backlogItemId);
            return tempsReel - backlogItem.ChiffrageHeures.Value;
        }

        /// <summary>
        /// Calcule l'écart en pourcentage (tempsReel / chiffrage * 100)
        /// </summary>
        public double GetEcartPourcentage(int backlogItemId)
        {
            var backlogItem = _db.GetBacklogItems().FirstOrDefault(b => b.Id == backlogItemId);
            if (backlogItem == null || !backlogItem.ChiffrageHeures.HasValue || backlogItem.ChiffrageHeures.Value == 0)
                return 0;

            var tempsReel = GetTempsReelTache(backlogItemId);
            return (tempsReel / backlogItem.ChiffrageHeures.Value) * 100;
        }

        /// <summary>
        /// Indique si une tâche est en dépassement (> 110% du chiffrage)
        /// </summary>
        public bool EstEnDepassement(int backlogItemId)
        {
            var ecartPct = GetEcartPourcentage(backlogItemId);
            return ecartPct > 110;
        }

        /// <summary>
        /// Indique si une tâche est en risque (90-110% du chiffrage)
        /// </summary>
        public bool EstEnRisque(int backlogItemId)
        {
            var ecartPct = GetEcartPourcentage(backlogItemId);
            return ecartPct > 90 && ecartPct <= 110;
        }

        /// <summary>
        /// Supprime un CRA (avec vérification permissions)
        /// </summary>
        public void DeleteCRA(int id, int currentUserId, bool isAdmin)
        {
            var cra = _db.GetCRAs().FirstOrDefault(c => c.Id == id);
            if (cra == null)
                throw new InvalidOperationException("CRA introuvable.");

            // Vérification permissions
            if (!isAdmin)
            {
                if (cra.DevId != currentUserId)
                    throw new UnauthorizedAccessException("Vous ne pouvez supprimer que vos propres CRA.");

                if ((DateTime.Now - cra.DateCreation).TotalDays > 7)
                    throw new InvalidOperationException("Impossible de supprimer un CRA de plus de 7 jours.");
            }

            _db.DeleteCRA(id);
        }

        /// <summary>
        /// Récupère les tâches actives d'un développeur (pour saisie CRA)
        /// </summary>
        public List<BacklogItem> GetTachesActivesDev(int devId)
        {
            return _db.GetBacklogItems()
                .Where(b => b.DevAssigneId == devId && 
                           (b.Statut == Statut.EnCours || b.Statut == Statut.Test))
                .OrderBy(b => b.Titre)
                .ToList();
        }

        /// <summary>
        /// Génère un rapport hebdomadaire pour un dev
        /// </summary>
        public Dictionary<DateTime, double> GetRapportHebdomadaire(int devId, DateTime dateDebut)
        {
            var dateFin = dateDebut.AddDays(6);
            var cras = GetCRAsByDev(devId, dateDebut, dateFin);

            var rapport = new Dictionary<DateTime, double>();
            for (int i = 0; i < 7; i++)
            {
                var jour = dateDebut.AddDays(i);
                rapport[jour] = cras.Where(c => c.Date.Date == jour.Date)
                                   .Sum(c => c.HeuresTravaillees);
            }

            return rapport;
        }

        /// <summary>
        /// Récupère les tâches en dépassement
        /// </summary>
        public List<BacklogItem> GetTachesEnDepassement()
        {
            return _db.GetBacklogItems()
                .Where(b => b.ChiffrageHeures.HasValue && b.ChiffrageHeures.Value > 0)
                .Where(b => EstEnDepassement(b.Id))
                .ToList();
        }

        /// <summary>
        /// Récupère les tâches en risque
        /// </summary>
        public List<BacklogItem> GetTachesEnRisque()
        {
            return _db.GetBacklogItems()
                .Where(b => b.ChiffrageHeures.HasValue && b.ChiffrageHeures.Value > 0)
                .Where(b => EstEnRisque(b.Id))
                .ToList();
        }

        /// <summary>
        /// Calcule la vélocité moyenne d'un dev (temps réel / chiffrage)
        /// </summary>
        public double GetVelociteMoyenne(int devId, int nombreTaches = 10)
        {
            var tachesTerminees = _db.GetBacklogItems()
                .Where(b => b.DevAssigneId == devId && 
                           b.Statut == Statut.Termine && 
                           b.ChiffrageHeures.HasValue && 
                           b.ChiffrageHeures.Value > 0)
                .OrderByDescending(b => b.Id)
                .Take(nombreTaches)
                .ToList();

            if (!tachesTerminees.Any())
                return 1.0;

            var ratios = tachesTerminees.Select(t => 
            {
                var tempsReel = GetTempsReelTache(t.Id);
                return tempsReel / t.ChiffrageHeures.Value;
            }).Where(r => r > 0);

            return ratios.Any() ? ratios.Average() : 1.0;
        }

        /// <summary>
        /// Génère un rapport mensuel par projet
        /// </summary>
        public Dictionary<int, ProjectReport> GetRapportMensuelProjets(int mois, int annee)
        {
            var dateDebut = new DateTime(annee, mois, 1);
            var dateFin = dateDebut.AddMonths(1).AddDays(-1);
            var cras = _db.GetCRAs(dateDebut: dateDebut, dateFin: dateFin);
            var backlogItems = _db.GetBacklogItems();
            var projets = _db.GetProjets();

            var rapport = new Dictionary<int, ProjectReport>();

            foreach (var projet in projets)
            {
                var tachesProjets = backlogItems.Where(b => b.ProjetId == projet.Id).Select(b => b.Id).ToList();
                var crasProjets = cras.Where(c => tachesProjets.Contains(c.BacklogItemId)).ToList();

                rapport[projet.Id] = new ProjectReport
                {
                    ProjetId = projet.Id,
                    ProjetNom = projet.Nom,
                    TotalHeures = crasProjets.Sum(c => c.HeuresTravaillees),
                    NombreTaches = tachesProjets.Count(),
                    NombreCRA = crasProjets.Count
                };
            }

            return rapport;
        }

        /// <summary>
        /// Valide un CRA avant sauvegarde
        /// </summary>
        private void ValidateCRA(CRA cra)
        {
            if (cra == null)
                throw new ArgumentNullException(nameof(cra));

            if (cra.HeuresTravaillees <= 0)
                throw new InvalidOperationException("Le nombre d'heures doit être supérieur à 0.");

            if (cra.HeuresTravaillees > 24)
                throw new InvalidOperationException("Le nombre d'heures ne peut pas dépasser 24.");

            var backlogItem = _db.GetBacklogItems().FirstOrDefault(b => b.Id == cra.BacklogItemId);
            if (backlogItem == null)
                throw new InvalidOperationException("Tâche introuvable.");

            var dev = _db.GetUtilisateurs().FirstOrDefault(u => u.Id == cra.DevId);
            if (dev == null)
                throw new InvalidOperationException("Développeur introuvable.");
        }

        /// <summary>
        /// Récupère tous les CRA (pour historique admin)
        /// </summary>
        public List<CRA> GetAllCRAs(DateTime? dateDebut = null, DateTime? dateFin = null)
        {
            return _db.GetCRAs(dateDebut: dateDebut, dateFin: dateFin);
        }

        /// <summary>
        /// Exporte les CRA au format CSV
        /// </summary>
        public string ExportToCSV(List<CRA> cras)
        {
            var backlogItems = _db.GetBacklogItems().ToDictionary(b => b.Id);
            var devs = _db.GetUtilisateurs().ToDictionary(u => u.Id);
            var projets = _db.GetProjets().ToDictionary(p => p.Id);

            var csv = "Date;Dev;Projet;Tâche;Heures;Commentaire\n";

            foreach (var cra in cras.OrderBy(c => c.Date))
            {
                var backlogItem = backlogItems.ContainsKey(cra.BacklogItemId) 
                    ? backlogItems[cra.BacklogItemId] 
                    : null;
                var dev = devs.ContainsKey(cra.DevId) 
                    ? devs[cra.DevId] 
                    : null;
                var projet = backlogItem != null && backlogItem.ProjetId.HasValue && projets.ContainsKey(backlogItem.ProjetId.Value) 
                    ? projets[backlogItem.ProjetId.Value] 
                    : null;

                csv += $"{cra.Date:yyyy-MM-dd};";
                csv += $"{dev?.Nom ?? "Inconnu"};";
                csv += $"{projet?.Nom ?? "Inconnu"};";
                csv += $"{backlogItem?.Titre ?? "Inconnu"};";
                csv += $"{cra.HeuresTravaillees:F2};";
                csv += $"{cra.Commentaire?.Replace(";", ",") ?? ""}\n";
            }

            return csv;
        }
    }

    /// <summary>
    /// Rapport mensuel par projet
    /// </summary>
    public class ProjectReport
    {
        public int ProjetId { get; set; }
        public string ProjetNom { get; set; }
        public double TotalHeures { get; set; }
        public int NombreTaches { get; set; }
        public int NombreCRA { get; set; }
    }
}
