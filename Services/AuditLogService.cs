using System;
using BacklogManager.Domain;

namespace BacklogManager.Services
{
    public class AuditLogService
    {
        private readonly IDatabase _database;
        private readonly Utilisateur _currentUser;

        public AuditLogService(IDatabase database, Utilisateur currentUser)
        {
            _database = database;
            _currentUser = currentUser;
        }

        public void LogAction(string action, string entityType, int? entityId, string oldValue, string newValue, string details = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    Action = action,
                    UserId = _currentUser.Id,
                    Username = _currentUser.UsernameWindows,
                    EntityType = entityType,
                    EntityId = entityId,
                    OldValue = oldValue,
                    NewValue = newValue,
                    DateAction = DateTime.Now,
                    Details = details
                };

                _database.AddAuditLog(auditLog);
            }
            catch (Exception)
            {
                // Ne pas bloquer l'application si le logging échoue
                // On pourrait logger dans un fichier de fallback ici
            }
        }

        public void LogCreate(string entityType, int entityId, string newValue, string details = null)
        {
            LogAction("CREATE", entityType, entityId, null, newValue, details);
        }

        public void LogUpdate(string entityType, int entityId, string oldValue, string newValue, string details = null)
        {
            LogAction("UPDATE", entityType, entityId, oldValue, newValue, details);
        }

        public void LogDelete(string entityType, int entityId, string oldValue, string details = null)
        {
            LogAction("DELETE", entityType, entityId, oldValue, null, details);
        }

        public void LogLogin()
        {
            LogAction("LOGIN", "Utilisateur", _currentUser.Id, null, null, $"Connexion de {_currentUser.Nom}");
        }

        public void LogLogout()
        {
            LogAction("LOGOUT", "Utilisateur", _currentUser.Id, null, null, $"Déconnexion de {_currentUser.Nom}");
        }
    }
}
