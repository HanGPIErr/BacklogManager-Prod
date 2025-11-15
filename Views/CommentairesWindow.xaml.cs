using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
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
            
            ChargerCommentaires();
            BtnAjouter.Click += BtnAjouter_Click;
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
                TxtNbCommentaires.Text = string.Format("{0} commentaire(s)", commentaires.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lors du chargement des commentaires : {0}", ex.Message), 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string ObtenirNomUtilisateur(int userId, List<Utilisateur> utilisateurs)
        {
            var user = utilisateurs.FirstOrDefault(u => u.Id == userId);
            return user != null ? string.Format("{0} {1}", user.Prenom, user.Nom) : "Utilisateur inconnu";
        }

        private void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNouveauCommentaire.Text))
            {
                MessageBox.Show("Veuillez saisir un commentaire.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var utilisateur = _authService.CurrentUser;
                if (utilisateur == null)
                {
                    MessageBox.Show("Utilisateur non connecté.", "Erreur", 
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
                MessageBox.Show(string.Format("Erreur lors de l'ajout du commentaire : {0}", ex.Message), 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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
