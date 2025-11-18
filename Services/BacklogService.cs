using System;
using System.Collections.Generic;
using System.Linq;
using BacklogManager.Domain;

namespace BacklogManager.Services
{
    public class BacklogService
    {
        private readonly IDatabase _database;
        private readonly AuditLogService _auditLogService;
        
        public IDatabase Database => _database;

        public BacklogService(IDatabase database, AuditLogService auditLogService = null)
        {
            _database = database;
            _auditLogService = auditLogService;
        }

        public List<BacklogItem> GetAllBacklogItems()
        {
            return _database.GetBacklog().Where(x => !x.EstArchive).ToList();
        }

        public List<BacklogItem> GetAllBacklogItemsIncludingArchived()
        {
            return _database.GetAllBacklogItemsIncludingArchived();
        }

        public BacklogItem GetBacklogItemById(int id)
        {
            return _database.GetBacklog().FirstOrDefault(x => x.Id == id);
        }

        public BacklogItem SaveBacklogItem(BacklogItem item)
        {
            // Capture de l'état avant modification
            BacklogItem oldItem = null;
            bool isUpdate = item.Id > 0;
            
            if (isUpdate)
            {
                oldItem = GetBacklogItemById(item.Id);
            }

            // Sauvegarde de l'item
            var savedItem = _database.AddOrUpdateBacklogItem(item);

            // Audit log
            if (_auditLogService != null && savedItem != null)
            {
                try
                {
                    if (isUpdate && oldItem != null)
                    {
                        var oldValue = $"Titre: {oldItem.Titre}, Statut: {oldItem.Statut}, Priorité: {oldItem.Priorite}, Complexité: {oldItem.Complexite}";
                        var newValue = $"Titre: {savedItem.Titre}, Statut: {savedItem.Statut}, Priorité: {savedItem.Priorite}, Complexité: {savedItem.Complexite}";
                        _auditLogService.LogUpdate("BacklogItem", savedItem.Id, oldValue, newValue, 
                            $"Modification de la tâche #{savedItem.Id}");
                    }
                    else
                    {
                        var newValue = $"Titre: {savedItem.Titre}, Statut: {savedItem.Statut}, Priorité: {savedItem.Priorite}";
                        _auditLogService.LogCreate("BacklogItem", savedItem.Id, newValue, 
                            $"Création de la tâche #{savedItem.Id}");
                    }
                }
                catch
                {
                    // Ne pas bloquer l'opération si l'audit échoue
                }
            }

            return savedItem;
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
            // Récupérer l'ancien état si c'est une modification
            Projet oldProjet = null;
            if (projet.Id > 0)
            {
                oldProjet = _database.GetProjets().FirstOrDefault(p => p.Id == projet.Id);
            }

            var result = _database.AddOrUpdateProjet(projet);

            // Audit log
            if (_auditLogService != null)
            {
                if (oldProjet == null)
                {
                    // Création
                    string details = $"Nom: {projet.Nom}, Description: {projet.Description ?? "N/A"}, Actif: {projet.Actif}";
                    _auditLogService.LogCreate("Projet", result.Id, details);
                }
                else
                {
                    // Modification
                    string oldValue = $"Nom: {oldProjet.Nom}, Description: {oldProjet.Description ?? "N/A"}, Actif: {oldProjet.Actif}";
                    string newValue = $"Nom: {projet.Nom}, Description: {projet.Description ?? "N/A"}, Actif: {projet.Actif}";
                    _auditLogService.LogUpdate("Projet", projet.Id, $"Projet: {projet.Nom}", oldValue, newValue);
                }
            }

            return result;
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
                // Audit log avant suppression
                if (_auditLogService != null)
                {
                    string details = $"Nom: {projet.Nom}, Description: {projet.Description ?? "N/A"}";
                    _auditLogService.LogDelete("Projet", projetId, details);
                }

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
