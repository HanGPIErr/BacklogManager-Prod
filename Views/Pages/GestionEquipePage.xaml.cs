using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views.Pages
{
    public partial class GestionEquipePage : Page
    {
        private readonly IDatabase _database;
        private readonly BacklogService _backlogService;
        private List<EquipeGroupeViewModel> _tousLesGroupes;
        private string _filtreRecherche = "";

        public GestionEquipePage(IDatabase database)
        {
            InitializeComponent();
            _database = database;
            _backlogService = (Application.Current as App)?.BacklogService ?? new BacklogService(_database);
            ChargerEquipe();
        }

        private void ChargerEquipe()
        {
            try
            {
                // Charger tous les utilisateurs actifs
                var utilisateurs = _database.GetUtilisateurs().FindAll(u => u.Actif);
                var roles = _database.GetRoles().Where(r => r.Actif).ToList();
                
                // Grouper par rôle avec ordre spécifique
                _tousLesGroupes = new List<EquipeGroupeViewModel>();
                
                // Ordre prédéfini : Administrateurs, Chefs de Projet, Développeurs, Business Analysts
                var ordreRoles = new[] { "Administrateur", "Chef de Projet", "Développeur", "Business Analyst" };
                var rolesOrdonnes = ordreRoles
                    .Select(nomRole => roles.FirstOrDefault(r => r.Nom == nomRole))
                    .Where(r => r != null)
                    .ToList();
                
                // Ajouter les rôles non listés à la fin
                var autresRoles = roles.Where(r => !ordreRoles.Contains(r.Nom)).ToList();
                rolesOrdonnes.AddRange(autresRoles);
                
                foreach (var role in rolesOrdonnes)
                {
                    var membresRole = utilisateurs.Where(u => u.RoleId == role.Id)
                        .Select(u => new UtilisateurViewModel 
                        { 
                            Nom = $"{u.Prenom} {u.Nom}",
                            Initiales = GetInitiales(u.Prenom, u.Nom),
                            Actif = u.Actif,
                            RoleName = role.Nom
                        }).ToList();
                    
                    if (membresRole.Any())
                    {
                        var (icone, couleur) = GetRoleStyle(role.Nom);
                        _tousLesGroupes.Add(new EquipeGroupeViewModel
                        {
                            NomRole = $"Équipe des {GetRolePluralName(role.Nom)}",
                            Icone = icone,
                            CouleurRole = couleur,
                            Membres = membresRole,
                            RoleNom = role.Nom
                        });
                    }
                }
                
                AppliquerFiltre();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement de l'équipe: {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private (string icone, SolidColorBrush couleur) GetRoleStyle(string roleNom)
        {
            switch (roleNom)
            {
                case "Développeur":
                    return ("👨‍💻", new SolidColorBrush(Color.FromRgb(0, 145, 90))); // Vert BNP
                case "Business Analyst":
                    return ("📋", new SolidColorBrush(Color.FromRgb(33, 150, 243))); // Bleu
                case "Chef de Projet":
                    return ("👔", new SolidColorBrush(Color.FromRgb(156, 39, 176))); // Violet
                case "Administrateur":
                    return ("⚙️", new SolidColorBrush(Color.FromRgb(255, 87, 34))); // Orange
                default:
                    return ("👤", new SolidColorBrush(Color.FromRgb(96, 125, 139))); // Gris par défaut
            }
        }
        
        private string GetRolePluralName(string roleNom)
        {
            switch (roleNom)
            {
                case "Développeur":
                    return "Développeurs";
                case "Business Analyst":
                    return "Business Analysts";
                case "Chef de Projet":
                    return "Chefs de Projet";
                case "Administrateur":
                    return "Administrateurs";
                default:
                    return roleNom + "s";
            }
        }
        
        private void AppliquerFiltre()
        {
            if (_tousLesGroupes == null) return;
            
            var groupesFiltres = _tousLesGroupes;
            
            if (!string.IsNullOrWhiteSpace(_filtreRecherche))
            {
                var recherche = _filtreRecherche.ToLower();
                groupesFiltres = new List<EquipeGroupeViewModel>();
                
                foreach (var groupe in _tousLesGroupes)
                {
                    // Filtrer les membres
                    var membresFiltres = groupe.Membres.Where(m => 
                        m.Nom.ToLower().Contains(recherche) || 
                        m.RoleName.ToLower().Contains(recherche)).ToList();
                    
                    if (membresFiltres.Any())
                    {
                        groupesFiltres.Add(new EquipeGroupeViewModel
                        {
                            NomRole = groupe.NomRole,
                            Icone = groupe.Icone,
                            CouleurRole = groupe.CouleurRole,
                            Membres = membresFiltres,
                            RoleNom = groupe.RoleNom
                        });
                    }
                }
            }
            
            LstEquipeGroupee.ItemsSource = groupesFiltres;
        }
        
        private string GetInitiales(string prenom, string nom)
        {
            if (string.IsNullOrWhiteSpace(prenom) || string.IsNullOrWhiteSpace(nom))
                return "??";
            
            return $"{prenom[0]}{nom[0]}".ToUpper();
        }

        private void BtnGererEquipe_Click(object sender, RoutedEventArgs e)
        {
            var window = new GestionEquipeWindow(_backlogService);
            window.ShowDialog();
            ChargerEquipe();
        }

        private void BtnActualiser_Click(object sender, RoutedEventArgs e)
        {
            ChargerEquipe();
        }
        
        private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e)
        {
            _filtreRecherche = TxtRecherche.Text;
            AppliquerFiltre();
        }
        
        private void BtnEffacerRecherche_Click(object sender, RoutedEventArgs e)
        {
            TxtRecherche.Text = "";
        }
    }
    
    // ViewModel pour grouper l'équipe par rôle
    public class EquipeGroupeViewModel
    {
        public string NomRole { get; set; }
        public string Icone { get; set; }
        public Brush CouleurRole { get; set; }
        public List<UtilisateurViewModel> Membres { get; set; }
        public string RoleNom { get; set; }
        public int NombreMembres 
        { 
            get 
            {
                if (Membres == null) return 0;
                return Membres.Count;
            }
        }
    }
    
    // ViewModel pour afficher un utilisateur
    public class UtilisateurViewModel
    {
        public string Nom { get; set; }
        public string Initiales { get; set; }
        public bool Actif { get; set; }
        public string RoleName { get; set; }
    }
}
