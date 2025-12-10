using BacklogManager.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BacklogManager.Services
{
    public interface IEquipeService
    {
        List<Equipe> GetAllEquipes();
        Equipe GetEquipeById(int id);
        void AjouterEquipe(Equipe equipe);
        void ModifierEquipe(Equipe equipe);
        void SupprimerEquipe(int id);
        
        List<Utilisateur> GetMembresByEquipe(int equipeId);
        List<Projet> GetProjetsByEquipe(int equipeId);
        
        // Statistiques
        int GetNombreMembres(int equipeId);
        int GetNombreProjetsActifs(int equipeId);
        double GetChargeGlobale(int equipeId);  // En jours/homme
    }

    public class EquipeService : IEquipeService
    {
        private readonly IDatabase _database;

        public EquipeService(IDatabase database)
        {
            _database = database;
        }

        public List<Equipe> GetAllEquipes()
        {
            return _database.GetAllEquipes();
        }

        public Equipe GetEquipeById(int id)
        {
            return _database.GetEquipeById(id);
        }

        public void AjouterEquipe(Equipe equipe)
        {
            if (string.IsNullOrWhiteSpace(equipe.Nom))
                throw new ArgumentException("Le nom de l'équipe est obligatoire");
            
            if (string.IsNullOrWhiteSpace(equipe.Code))
                throw new ArgumentException("Le code de l'équipe est obligatoire");
            
            equipe.DateCreation = DateTime.Now;
            equipe.Actif = true;
            
            _database.AjouterEquipe(equipe);
        }

        public void ModifierEquipe(Equipe equipe)
        {
            if (string.IsNullOrWhiteSpace(equipe.Nom))
                throw new ArgumentException("Le nom de l'équipe est obligatoire");
            
            if (string.IsNullOrWhiteSpace(equipe.Code))
                throw new ArgumentException("Le code de l'équipe est obligatoire");
            
            _database.ModifierEquipe(equipe);
        }

        public void SupprimerEquipe(int id)
        {
            // Soft delete : désactiver l'équipe au lieu de la supprimer
            var equipe = GetEquipeById(id);
            if (equipe != null)
            {
                equipe.Actif = false;
                ModifierEquipe(equipe);
            }
        }

        public List<Utilisateur> GetMembresByEquipe(int equipeId)
        {
            return _database.GetMembresByEquipe(equipeId);
        }

        public List<Projet> GetProjetsByEquipe(int equipeId)
        {
            return _database.GetProjetsByEquipe(equipeId);
        }

        public int GetNombreMembres(int equipeId)
        {
            return GetMembresByEquipe(equipeId).Count;
        }

        public int GetNombreProjetsActifs(int equipeId)
        {
            return GetProjetsByEquipe(equipeId).Count(p => p.Actif);
        }

        public double GetChargeGlobale(int equipeId)
        {
            var membres = GetMembresByEquipe(equipeId);
            if (!membres.Any())
                return 0;

            // Calculer la charge totale de tous les membres de l'équipe
            double chargeTotal = 0;
            
            foreach (var membre in membres)
            {
                var tachesActives = _database.GetBacklogItemsByDevId(membre.Id)
                    .Where(t => t.Statut != Statut.Termine && !t.EstArchive)
                    .ToList();
                
                foreach (var tache in tachesActives)
                {
                    double tempsRestant = tache.ChiffrageHeures.HasValue ? tache.ChiffrageHeures.Value : 0;
                    if (tache.TempsReelHeures.HasValue)
                    {
                        tempsRestant = Math.Max(0, tempsRestant - tache.TempsReelHeures.Value);
                    }
                    chargeTotal += tempsRestant;
                }
            }
            
            // Convertir en jours (1 jour = 8 heures)
            return chargeTotal / 8.0;
        }
    }
}
