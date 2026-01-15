using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class CommentairesWindow : Window
    {
        private readonly int _demandeId;
        private readonly IDatabase _database;
        private readonly AuthenticationService _authService;

        public CommentairesWindow(int demandeId, IDatabase database, AuthenticationService authService)
        {
            InitializeComponent();
            _demandeId = demandeId;
            _database = database;
            _authService = authService;
            
            InitialiserTextes();
            ChargerCommentaires();
            BtnAjouter.Click += BtnAjouter_Click;
            
            LocalizationService.Instance.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(LocalizationService.CurrentCulture))
                {
                    InitialiserTextes();
                    ChargerCommentaires(); // Refresh pour les dates formatées
                }
            };
        }

        private void InitialiserTextes()
        {
            Title = LocalizationService.Instance["Comments_Title"];
            
            // Traductions directes via x:Name
            TxtCommentsTitle.Text = LocalizationService.Instance["Comments_Title"];
            TxtAddCommentTitle.Text = LocalizationService.Instance["Comments_AddComment"];
            BtnFermer.Content = LocalizationService.Instance["Common_Close"];
            BtnAjouter.Content = LocalizationService.Instance["Common_Add"];
        }

        private IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void OnFermer(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ChargerCommentaires()
        {
            try
            {
                var commentaires = _database.GetCommentaires()
                    .Where(c => c.DemandeId == _demandeId)
                    .OrderByDescending(c => c.DateCreation)
                    .ToList();

                var utilisateurs = _database.GetUtilisateurs();
                
                var commentairesVM = commentaires.Select(c => new CommentaireViewModel
                {
                    Auteur = ObtenirNomUtilisateur(c.AuteurId, utilisateurs),
                    DateCreation = c.DateCreation.ToString("dd/MM/yyyy HH:mm"),
                    Texte = c.Contenu
                }).ToList();

                ListeCommentaires.ItemsSource = commentairesVM;
                
                // Update comment count with localized text
                var countText = commentaires.Count == 0 
                    ? LocalizationService.Instance["Comments_NoComments"]
                    : commentaires.Count == 1 
                        ? LocalizationService.Instance["Comments_OneComment"]
                        : string.Format(LocalizationService.Instance["Comments_MultipleComments"], commentaires.Count);
                        
                TxtNbCommentaires.Text = countText;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LocalizationService.Instance["Comments_LoadError"], ex.Message), 
                    LocalizationService.Instance["Common_Error"], MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string ObtenirNomUtilisateur(int userId, List<Utilisateur> utilisateurs)
        {
            var user = utilisateurs.FirstOrDefault(u => u.Id == userId);
            return user != null ? string.Format("{0} {1}", user.Prenom, user.Nom) : LocalizationService.Instance["Comments_UnknownUser"];
        }

        private void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNouveauCommentaire.Text))
            {
                MessageBox.Show(LocalizationService.Instance["Comments_EnterComment"], LocalizationService.Instance["Common_Validation"], 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var utilisateur = _authService.CurrentUser;
                if (utilisateur == null)
                {
                    MessageBox.Show(LocalizationService.Instance["Comments_UserNotLoggedIn"], LocalizationService.Instance["Common_Error"], 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var commentaire = new Commentaire
                {
                    DemandeId = _demandeId,
                    AuteurId = utilisateur.Id,
                    Contenu = TxtNouveauCommentaire.Text.Trim(),
                    DateCreation = DateTime.Now
                };

                _database.AddCommentaire(commentaire);
                TxtNouveauCommentaire.Clear();
                ChargerCommentaires();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LocalizationService.Instance["Comments_AddError"], ex.Message), 
                    LocalizationService.Instance["Common_Error"], MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class CommentaireViewModel
    {
        public string Auteur { get; set; }
        public string DateCreation { get; set; }
        public string Texte { get; set; }
    }
}
