using System;
using System.Collections.Generic;
using System.Linq;
using BacklogManager.Domain;

namespace BacklogManager.Services
{
    public class BacklogService
    {
        private readonly IDatabase _database;

        public BacklogService(IDatabase database)
        {
            _database = database;
        }

        public List<BacklogItem> GetAllBacklogItems()
        {
            return _database.GetBacklog().Where(x => !x.EstArchive).ToList();
        }

        public BacklogItem GetBacklogItemById(int id)
        {
            return _database.GetBacklog().FirstOrDefault(x => x.Id == id);
        }

        public BacklogItem SaveBacklogItem(BacklogItem item)
        {
            return _database.AddOrUpdateBacklogItem(item);
        }

        public List<BacklogItem> SearchBacklog(string searchText, TypeDemande? type, Priorite? priorite, Statut? statut, int? devId)
        {
            var items = GetAllBacklogItems();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                searchText = searchText.ToLower();
                items = items.Where(x => 
                    (x.Titre != null && x.Titre.ToLower().Contains(searchText)) ||
                    (x.Description != null && x.Description.ToLower().Contains(searchText))
                ).ToList();
            }

            if (type.HasValue)
            {
                items = items.Where(x => x.TypeDemande == type.Value).ToList();
            }

            if (priorite.HasValue)
            {
                items = items.Where(x => x.Priorite == priorite.Value).ToList();
            }

            if (statut.HasValue)
            {
                items = items.Where(x => x.Statut == statut.Value).ToList();
            }

            if (devId.HasValue)
            {
                items = items.Where(x => x.DevAssigneId == devId.Value).ToList();
            }

            return items;
        }

        public List<BacklogItem> GetBacklogItemsByStatus(Statut statut)
        {
            return GetAllBacklogItems().Where(x => x.Statut == statut).ToList();
        }

        public void UpdateBacklogItemStatus(int itemId, Statut newStatus)
        {
            var item = GetBacklogItemById(itemId);
            if (item != null)
            {
                item.Statut = newStatus;
                SaveBacklogItem(item);
            }
        }

        public List<Utilisateur> GetAllUtilisateurs()
        {
            var roles = _database.GetRoles();
            var devRole = roles.FirstOrDefault(r => r.Type == RoleType.Developpeur);
            if (devRole == null) return new List<Utilisateur>();
            
            return _database.GetUtilisateurs()
                .Where(x => x.Actif && x.RoleId == devRole.Id)
                .ToList();
        }

        public List<Dev> GetAllDevs()
        {
            return _database.GetDevs().Where(x => x.Actif).ToList();
        }

        public Dev SaveDev(Dev dev)
        {
            return _database.AddOrUpdateDev(dev);
        }

        public List<Projet> GetAllProjets()
        {
            return _database.GetProjets().Where(x => x.Actif).ToList();
        }

        public Projet SaveProjet(Projet projet)
        {
            return _database.AddOrUpdateProjet(projet);
        }

        public void DeleteDev(int devId)
        {
            var dev = _database.GetDevs().FirstOrDefault(d => d.Id == devId);
            if (dev != null)
            {
                dev.Actif = false;
                _database.AddOrUpdateDev(dev);
            }
        }

        public void DeleteProjet(int projetId)
        {
            var projet = _database.GetProjets().FirstOrDefault(p => p.Id == projetId);
            if (projet != null)
            {
                projet.Actif = false;
                _database.AddOrUpdateProjet(projet);
            }
        }

        public void DeleteBacklogItem(int itemId)
        {
            var item = GetBacklogItemById(itemId);
            if (item != null)
            {
                item.EstArchive = true;
                SaveBacklogItem(item);
            }
        }
    }
}
