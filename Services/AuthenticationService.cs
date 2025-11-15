using System;
using System.Linq;
using System.Security.Principal;
using BacklogManager.Domain;

namespace BacklogManager.Services
{
    public class AuthenticationService
    {
        private readonly IDatabase _database;
        private Utilisateur _currentUser;

        public AuthenticationService(IDatabase database)
        {
            _database = database;
        }

        public Utilisateur CurrentUser
        {
            get { return _currentUser; }
        }

        public bool EstConnecte
        {
            get { return _currentUser != null; }
        }

        public bool Login()
        {
            try
            {
                // Récupérer le username Windows (ex: J12222, J04831)
                string windowsUsername = WindowsIdentity.GetCurrent().Name;
                
                // Extraire juste le username sans le domaine
                if (windowsUsername.Contains("\\"))
                {
                    windowsUsername = windowsUsername.Split('\\')[1];
                }

                // Chercher l'utilisateur dans la base
                var utilisateurs = _database.GetUtilisateurs();
                _currentUser = utilisateurs.FirstOrDefault(u => 
                    u.UsernameWindows.Equals(windowsUsername, StringComparison.OrdinalIgnoreCase) && u.Actif);

                if (_currentUser != null)
                {
                    // Mettre à jour la date de dernière connexion
                    _currentUser.DateDerniereConnexion = DateTime.Now;
                    _database.AddOrUpdateUtilisateur(_currentUser);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public bool LoginWithUsername(string username)
        {
            try
            {
                // Chercher l'utilisateur dans la base avec le username fourni
                var utilisateurs = _database.GetUtilisateurs();
                _currentUser = utilisateurs.FirstOrDefault(u => 
                    u.UsernameWindows.Equals(username, StringComparison.OrdinalIgnoreCase) && u.Actif);

                if (_currentUser != null)
                {
                    // Mettre à jour la date de dernière connexion
                    _currentUser.DateDerniereConnexion = DateTime.Now;
                    _database.AddOrUpdateUtilisateur(_currentUser);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public Role GetCurrentUserRole()
        {
            if (_currentUser == null) return null;
            
            var roles = _database.GetRoles();
            return roles.FirstOrDefault(r => r.Id == _currentUser.RoleId);
        }

        public bool HasPermission(string permission)
        {
            var role = GetCurrentUserRole();
            if (role == null) return false;

            switch (permission)
            {
                case "CreerDemandes":
                    return role.PeutCreerDemandes;
                case "Chiffrer":
                    return role.PeutChiffrer;
                case "Prioriser":
                    return role.PeutPrioriser;
                case "GererUtilisateurs":
                    return role.PeutGererUtilisateurs;
                case "VoirKPI":
                    return role.PeutVoirKPI;
                case "GererReferentiels":
                    return role.PeutGererReferentiels;
                default:
                    return false;
            }
        }

        public bool IsAdmin()
        {
            var role = GetCurrentUserRole();
            return role?.Type == RoleType.Administrateur;
        }

        public bool IsBusinessAnalyst()
        {
            var role = GetCurrentUserRole();
            return role?.Type == RoleType.BusinessAnalyst;
        }

        public bool IsChefDeProjet()
        {
            var role = GetCurrentUserRole();
            return role?.Type == RoleType.ChefDeProjet;
        }

        public bool IsDeveloppeur()
        {
            var role = GetCurrentUserRole();
            return role?.Type == RoleType.Developpeur;
        }
    }
}
