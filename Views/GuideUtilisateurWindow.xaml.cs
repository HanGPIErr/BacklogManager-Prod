using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class GuideUtilisateurWindow : Window
    {
        private readonly AuthenticationService _authService;
        private readonly Role _userRole;
        private Dictionary<string, string> _questionsReponses;

        public GuideUtilisateurWindow(AuthenticationService authService)
        {
            InitializeComponent();
            _authService = authService;
            _userRole = _authService.GetCurrentUserRole();
            
            ChargerQuestionsSelonRole();
        }

        private void ChargerQuestionsSelonRole()
        {
            if (_userRole == null) return;

            TxtRole.Text = string.Format("Guide {0}", _userRole.Nom);
            _questionsReponses = new Dictionary<string, string>();

            switch (_userRole.Type)
            {
                case RoleType.Administrateur:
                    ChargerQuestionsAdministrateur();
                    break;
                case RoleType.ChefDeProjet:
                    ChargerQuestionsChefDeProjet();
                    break;
                case RoleType.Developpeur:
                    ChargerQuestionsDeveloppeur();
                    break;
                case RoleType.BusinessAnalyst:
                    ChargerQuestionsBusinessAnalyst();
                    break;
                default:
                    ChargerQuestionsGenerales();
                    break;
            }

            AfficherQuestions();
        }

        private void ChargerQuestionsAdministrateur()
        {
            _questionsReponses = new Dictionary<string, string>
            {
                { "Comment gérer les utilisateurs et leurs rôles ?", 
                    "En tant qu'administrateur, vous êtes le maître du système ! 👑\n\n" +
                    "**Accès Administration :**\n" +
                    "Seul vous avez accès à la section **ADMINISTRATION** du menu latéral.\n\n" +
                    "**Gestion des utilisateurs :**\n" +
                    "• Les utilisateurs sont créés automatiquement au démarrage\n" +
                    "• Vous pouvez modifier leurs rôles et permissions\n" +
                    "• 4 types de rôles : Administrateur, Chef de Projet, Business Analyst, Développeur\n\n" +
                    "**Permissions importantes :**\n" +
                    "• Vous seul pouvez **archiver** les demandes obsolètes\n" +
                    "• Vous seul pouvez **supprimer** tâches et demandes\n" +
                    "• Vous seul pouvez gérer les référentiels (projets, équipes)\n\n" +
                    "💡 *Astuce* : Chaque rôle a des permissions précises. Admin = tous pouvoirs !" },
                
                { "Comment utiliser le Dashboard et le Kanban ?",
                    "Le Dashboard et le Kanban sont vos outils de pilotage quotidien ! 📊\n\n" +
                    "**Dashboard (🏠) :**\n" +
                    "• Vue d'ensemble avec KPIs : tâches terminées, en cours, à prioriser\n" +
                    "• Productivité de l'équipe en pourcentage\n" +
                    "• Notifications importantes affichées avec Caramel & Flopy\n" +
                    "• Cliquez sur une notification pour l'envoyer par email au développeur\n\n" +
                    "**Kanban (🎯) :**\n" +
                    "• Colonnes : EN ATTENTE | A PRIORISER (zone admin) | À FAIRE | EN COURS | EN TEST | TERMINÉ\n" +
                    "• Drag & drop pour changer les statuts\n" +
                    "• Filtres par développeur et par projet\n" +
                    "• Vous pouvez supprimer des tâches (croix rouge sur les cartes)\n\n" +
                    "🎯 *Mon conseil* : Zone admin visible uniquement par vous pour gérer EN ATTENTE et A PRIORISER !" },
                
                { "Comment gérer le Backlog et les Demandes ?",
                    "Le Backlog et les Demandes sont au cœur de la planification ! 📋\n\n" +
                    "**Backlog (📋) :**\n" +
                    "• Liste de TOUTES les tâches du système\n" +
                    "• Créez de nouvelles tâches avec le bouton ➕\n" +
                    "• Assignez des développeurs et définissez les priorités\n" +
                    "• Double-cliquez sur une tâche pour l'éditer\n" +
                    "• Supprimez les tâches obsolètes (vous seul le pouvez)\n\n" +
                    "**Demandes (📝) :**\n" +
                    "• Créez des demandes métier avec ➕ Nouvelle demande\n" +
                    "• Assignez un Business Analyst pour spécifier\n" +
                    "• Archivez les demandes terminées (bouton Archiver)\n" +
                    "• Seul l'admin peut supprimer et archiver\n\n" +
                    "⚠️ *Important* : Utilisez Archiver au lieu de Supprimer pour garder l'historique !" },
                
                { "Comment fonctionne le suivi des CRA et du temps ?",
                    "Le CRA est crucial pour le suivi projet ! ⏱️\n\n" +
                    "**Saisir CRA (⏱️) :**\n" +
                    "• Les développeurs saisissent leur temps par tâche et par jour\n" +
                    "• Vous pouvez consulter mais pas saisir (c'est pour les devs)\n\n" +
                    "**Suivi CRA (📊) - Section ADMINISTRATION :**\n" +
                    "• Vue calendrier avec temps saisi par développeur\n" +
                    "• Validez les CRA pour les comptabiliser dans les stats\n" +
                    "• États : Prévisionnel (orange clair) → À valider (orange) → Validé (vert)\n" +
                    "• Seul le temps validé compte dans le 'Temps réel passé' du Kanban\n\n" +
                    "**Important pour les stats :**\n" +
                    "• Le Kanban affiche Temps réel = somme des CRA validés\n" +
                    "• Les estimations vs réalisé vous aident à ajuster les futurs chiffrages\n\n" +
                    "💡 *Conseil* : Validez les CRA chaque semaine pour des statistiques à jour !" },
                
                { "Comment utiliser les Notifications intelligentes ?",
                    "Les notifications, c'est votre système d'alerte proactif ! 🔔\n\n" +
                    "**Types de notifications avec Caramel & Flopy :**\n" +
                    "• 🔴 URGENT (grumpy) : Tâches en retard critique\n" +
                    "• ⚠️ ATTENTION (grumpy) : Échéance proche, attention requise\n" +
                    "• ✅ SUCCESS (happy) : Félicitations, tâche terminée\n" +
                    "• 📋 INFO (normal) : Informations générales\n\n" +
                    "**Fonctionnalité Email :**\n" +
                    "• Cliquez sur le bouton '📧 Envoyer par email' sur une notification\n" +
                    "• Outlook s'ouvre avec un email pré-rempli\n" +
                    "• Message formaté avec détails tâche, urgence, deadline\n" +
                    "• Pratique pour faire un follow-up rapide au développeur\n\n" +
                    "**Badge rouge :**\n" +
                    "Nombre de notifications non lues affiché dans le menu\n\n" +
                    "💡 *Astuce* : Traitez les notifications URGENT en priorité pour éviter les blocages !" }
            };
        }

        private void ChargerQuestionsChefDeProjet()
        {
            _questionsReponses = new Dictionary<string, string>
            {
                { "Quelles sont mes permissions en tant que Chef de Projet ?",
                    "En tant que Chef de Projet, vous avez de larges pouvoirs ! 👔\n\n" +
                    "**Ce que vous POUVEZ faire :**\n" +
                    "✅ Créer des demandes métier (📝 Demandes)\n" +
                    "✅ Prioriser les tâches dans le Backlog\n" +
                    "✅ Assigner des développeurs aux tâches\n" +
                    "✅ Modifier toutes les tâches (pas seulement les vôtres)\n" +
                    "✅ Supprimer des tâches et des demandes\n" +
                    "✅ Voir tous les KPI et statistiques\n" +
                    "✅ Changer les statuts dans le Kanban\n\n" +
                    "**Ce que vous NE POUVEZ PAS faire :**\n" +
                    "❌ Accéder à la section ADMINISTRATION\n" +
                    "❌ Gérer les utilisateurs et les rôles\n" +
                    "❌ Archiver des demandes (réservé à l'admin)\n\n" +
                    "🎯 *Votre rôle* : Orchestrer l'équipe et prioriser le travail !" },
                
                { "Comment organiser le Backlog et prioriser les tâches ?",
                    "La priorisation, c'est votre super-pouvoir ! 🎯\n\n" +
                    "**Dans le Backlog (📋) :**\n" +
                    "• Créez de nouvelles tâches avec le bouton ➕ Nouvelle tâche\n" +
                    "• Assignez un développeur dans le formulaire\n" +
                    "• Définissez la priorité : Urgent / Haute / Moyenne / Basse\n" +
                    "• Estimez la charge en jours\n" +
                    "• Double-cliquez pour modifier une tâche existante\n\n" +
                    "**Filtres disponibles :**\n" +
                    "• Par développeur pour équilibrer la charge\n" +
                    "• Par projet pour suivre un périmètre\n" +
                    "• Par statut pour identifier les blocages\n\n" +
                    "**Conseil stratégique :**\n" +
                    "Priorisez selon valeur métier + urgence. Les devs voient leurs tâches dans le Kanban !\n\n" +
                    "🎯 *Astuce* : Utilisez les priorités pour guider les devs, pas pour les stresser !" },
                
                { "Comment utiliser le Kanban et les Demandes ?",
                    "Le Kanban et les Demandes sont vos outils de pilotage visuel ! 📊\n\n" +
                    "**Kanban (🎯) :**\n" +
                    "• 4 colonnes principales : À FAIRE | EN COURS | EN TEST | TERMINÉ\n" +
                    "• Drag & drop pour changer les statuts (vous pouvez tout bouger)\n" +
                    "• Filtres par dev/projet en haut\n" +
                    "• Bouton ❌ pour supprimer une tâche obsolète\n" +
                    "• Temps réel passé affiché (basé sur CRA validés)\n\n" +
                    "**Demandes (📝) :**\n" +
                    "• Créez des demandes avec ➕ Nouvelle demande\n" +
                    "• Assignez un BA pour spécifier\n" +
                    "• Modifiez et supprimez les demandes (vous avez les droits)\n" +
                    "• Suivez le cycle : Brouillon → Spécification → Chiffrage → Acceptée\n\n" +
                    "**Dashboard (🏠) :**\n" +
                    "Vue synthétique des KPIs et notifications importantes\n\n" +
                    "📊 *Mon truc* : Daily stand-up de 10 min devant le Kanban !" }
            };
        }

        private void ChargerQuestionsDeveloppeur()
        {
            _questionsReponses = new Dictionary<string, string>
            {
                { "Quelles sont mes permissions en tant que Développeur ?",
                    "En tant que dev, vous avez des droits ciblés sur VOS tâches ! 💻\n\n" +
                    "**Ce que vous POUVEZ faire :**\n" +
                    "✅ Saisir vos heures dans le CRA (⏱️ Saisir CRA)\n" +
                    "✅ Modifier VOS propres tâches assignées\n" +
                    "✅ Changer le statut de VOS tâches dans le Kanban\n" +
                    "✅ Participer au Planning Poker (chiffrage)\n" +
                    "✅ Voir vos tâches dans le Dashboard et le Kanban\n\n" +
                    "**Ce que vous NE POUVEZ PAS faire :**\n" +
                    "❌ Créer des demandes (réservé aux BA, Chef, Admin)\n" +
                    "❌ Modifier ou supprimer les tâches des autres devs\n" +
                    "❌ Assigner des développeurs aux tâches\n" +
                    "❌ Prioriser les tâches\n" +
                    "❌ Voir les KPI globaux et statistiques\n" +
                    "❌ Accéder à l'Administration\n\n" +
                    "🎯 *Votre focus* : Exécuter vos tâches et saisir votre temps !" },
                
                { "Comment saisir mes heures dans le CRA ?",
                    "Le CRA, c'est votre feuille de temps quotidienne ! ⏱️\n\n" +
                    "**Accès : ⏱️ Saisir CRA dans le menu**\n\n" +
                    "**Saisie des heures :**\n" +
                    "1. Calendrier affiché avec le mois en cours\n" +
                    "2. Cliquez sur un jour pour saisir du temps\n" +
                    "3. Sélectionnez la tâche travaillée (dans la liste)\n" +
                    "4. Indiquez les heures : 4h (demi-journée) ou 8h (journée)\n" +
                    "5. Ajoutez un commentaire optionnel\n" +
                    "6. Validez\n\n" +
                    "**Saisie congés/absences :**\n" +
                    "• Bouton 'Saisir Congés' pour déclarer congés/RTT/absence\n" +
                    "• Le système décale automatiquement vos tâches planifiées\n" +
                    "• Apparaît différemment dans le calendrier\n\n" +
                    "**Important :**\n" +
                    "Votre temps est en 'prévisionnel' jusqu'à validation par l'admin. Seul le temps validé compte dans les stats du Kanban !\n\n" +
                    "💡 *Conseil* : Saisissez quotidiennement, c'est plus précis !" },
                
                { "Comment utiliser le Kanban et le Backlog ?",
                    "Kanban et Backlog sont vos outils de travail quotidiens ! 🎯\n\n" +
                    "**Dashboard (🏠) :**\n" +
                    "• Vue synthétique de VOS tâches assignées\n" +
                    "• Statuts : À faire, En cours, En test\n" +
                    "• Cliquez sur une tâche pour les détails\n\n" +
                    "**Kanban (🎯) :**\n" +
                    "• Colonnes : À FAIRE | EN COURS | EN TEST | TERMINÉ\n" +
                    "• Filtrez par votre nom pour voir uniquement VOS tâches\n" +
                    "• Glissez-déposez VOS cartes pour changer le statut\n" +
                    "• Vous NE POUVEZ PAS déplacer les tâches des autres\n" +
                    "• Temps réel = heures CRA validées par l'admin\n\n" +
                    "**Backlog (📋) :**\n" +
                    "• Liste de toutes les tâches (toute l'équipe)\n" +
                    "• Double-cliquez sur VOS tâches pour les éditer\n" +
                    "• Voyez les priorités définies par le Chef de Projet\n\n" +
                    "🎯 *Ma méthode* : Dashboard au réveil, Kanban en continu, CRA en fin de journée !" }
            };
        }

        private void ChargerQuestionsBusinessAnalyst()
        {
            _questionsReponses = new Dictionary<string, string>
            {
                { "Quelles sont mes permissions en tant que Business Analyst ?",
                    "En tant que BA, vous êtes le pont entre métier et technique ! 📐\n\n" +
                    "**Ce que vous POUVEZ faire :**\n" +
                    "✅ Créer des demandes métier (📝 Demandes)\n" +
                    "✅ Modifier les demandes que vous avez créées\n" +
                    "✅ Voir les KPI dans le Dashboard\n" +
                    "✅ Consulter le Backlog et le Kanban (lecture seule)\n\n" +
                    "**Ce que vous NE POUVEZ PAS faire :**\n" +
                    "❌ Chiffrer les tâches (réservé aux développeurs)\n" +
                    "❌ Prioriser les tâches (réservé Chef de Projet et Admin)\n" +
                    "❌ Modifier les tâches dans le Backlog\n" +
                    "❌ Supprimer des demandes (Chef de Projet et Admin)\n" +
                    "❌ Saisir des CRA (réservé aux développeurs)\n" +
                    "❌ Accéder à l'Administration\n\n" +
                    "🎯 *Votre rôle* : Exprimer le besoin métier clairement et créer les demandes !" },
                
                { "Comment créer et suivre mes demandes ?",
                    "Les demandes, c'est votre terrain de jeu ! 📝\n\n" +
                    "**Créer une demande (📝 Demandes) :**\n" +
                    "1. Cliquez sur ➕ Nouvelle demande\n" +
                    "2. Remplissez le titre (clair et précis)\n" +
                    "3. Décrivez le besoin dans la description\n" +
                    "4. Définissez la criticité : Basse / Moyenne / Haute / Critique\n" +
                    "5. Assignez à un projet si applicable\n" +
                    "6. Validez\n\n" +
                    "**Cycle de vie d'une demande :**\n" +
                    "• Brouillon : demande en cours de rédaction\n" +
                    "• Spécification : vous détaillez les besoins\n" +
                    "• Chiffrage : les devs estiment (vous ne chiffrez pas)\n" +
                    "• Acceptée : prête à être découpée en tâches\n\n" +
                    "**Suivi :**\n" +
                    "• Dashboard : vue synthétique\n" +
                    "• Backlog : voir les tâches créées depuis vos demandes\n" +
                    "• Kanban : avancement visuel (lecture seule pour vous)\n\n" +
                    "🎯 *Conseil* : Plus votre description est précise, plus l'équipe sera efficace !" }
            };
        }

        private void ChargerQuestionsGenerales()
        {
            _questionsReponses = new Dictionary<string, string>
            {
                { "Comment naviguer dans BacklogManager ?",
                    "Bienvenue dans BacklogManager BNP Paribas ! 🏠\n\n" +
                    "**Menu latéral gauche avec sections :**\n\n" +
                    "**VUES :**\n" +
                    "• 🏠 Dashboard : Tableau de bord personnel, KPIs, notifications\n" +
                    "• 📋 Backlog : Liste complète des tâches\n" +
                    "• 🎯 Kanban : Suivi visuel (À faire → En cours → Test → Terminé)\n\n" +
                    "**TEMPS & CRA :**\n" +
                    "• ⏱️ Saisir CRA : Saisie des heures par tâche (développeurs)\n\n" +
                    "**ADMINISTRATION :**\n" +
                    "• 📊 Suivi CRA : Validation des temps (admin uniquement)\n\n" +
                    "**ACTIONS :**\n" +
                    "• 📝 Demandes : Gestion des besoins métier\n" +
                    "• 🔔 Notifications : Alertes et suivis avec Caramel & Flopy\n\n" +
                    "💡 *Conseil* : Dashboard = point de départ quotidien !" },
                
                { "Qui sont Caramel et Flopy ? 🐱🐰",
                    "Nous sommes vos guides et compagnons dans BacklogManager ! \n\n" +
                    "**Caramel (chat orange) :**\n" +
                    "Le sage et l'organisé. Expert en planification et stratégie !\n\n" +
                    "**Flopy (lapin blanc) :**\n" +
                    "Le curieux et l'enthousiaste. Toujours prêt à aider !\n\n" +
                    "**Nos 3 états émotionnels :**\n" +
                    "😊 Normal : Réponse standard, tout va bien\n" +
                    "😄 Heureux : Félicitations, succès, bonnes nouvelles\n" +
                    "😠 Grognon : Attention, urgence, problème à traiter\n\n" +
                    "**Où nous trouver :**\n" +
                    "• Dans ce guide (vous y êtes !)\n" +
                    "• Sur les notifications du Dashboard\n" +
                    "• Dans la fenêtre Notifications complète\n" +
                    "• Sur les états vides (pas de données)\n\n" +
                    "**Sources d'aide :**\n" +
                    "• Ce guide adapté à votre rôle\n" +
                    "• Tooltips en survolant les boutons\n" +
                    "• Votre administrateur système\n\n" +
                    "🤝 *Notre devise* : Pas de question bête, que des réponses utiles !" }
            };
        }

        private void AfficherQuestions()
        {
            QuestionsPanel.Children.Clear();

            foreach (var question in _questionsReponses.Keys)
            {
                var button = new Button
                {
                    Content = question,
                    Background = new SolidColorBrush(Color.FromRgb(243, 243, 243)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(12, 10, 12, 10),
                    Margin = new Thickness(0, 0, 0, 8),
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = question
                };

                var textBlock = new TextBlock
                {
                    Text = question,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                    TextWrapping = TextWrapping.Wrap
                };

                button.Content = textBlock;
                button.Click += QuestionButton_Click;

                // Style hover
                button.MouseEnter += (s, e) =>
                {
                    button.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
                    button.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 145, 90));
                };
                button.MouseLeave += (s, e) =>
                {
                    button.Background = new SolidColorBrush(Color.FromRgb(243, 243, 243));
                    button.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                };

                QuestionsPanel.Children.Add(button);
            }
        }

        private void QuestionButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            var question = button.Tag as string;
            if (string.IsNullOrEmpty(question) || !_questionsReponses.ContainsKey(question)) return;

            var reponse = _questionsReponses[question];

            AfficherConversation(question, reponse);
        }

        private void AfficherConversation(string question, string reponse)
        {
            ConversationPanel.Children.Clear();

            // Déterminer l'état émotionnel selon le contenu de la réponse
            string imageSource = "/Images/caramel-flopy-normal.png"; // Par défaut

            if (reponse.Contains("✅") || reponse.Contains("🎉") || reponse.Contains("Bravo") || 
                reponse.Contains("Excellent") || reponse.Contains("félicitations") || reponse.Contains("Félicitations"))
            {
                imageSource = "/Images/caramel-flopy-happy.png";
            }
            else if (reponse.Contains("⚠️") || reponse.Contains("Attention") || reponse.Contains("Important") ||
                     reponse.Contains("erreur") || reponse.Contains("N'oubliez pas") || reponse.Contains("éviter"))
            {
                imageSource = "/Images/caramel-flopy-grumpy.png";
            }

            // Mettre à jour l'image
            ImgGuide.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(imageSource, UriKind.Relative));

            // Question de l'utilisateur
            var questionBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(232, 245, 233)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(15),
                Margin = new Thickness(40, 0, 0, 15),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var questionText = new TextBlock
            {
                Text = "🙋 Vous : " + question,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 145, 90)),
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            };

            questionBorder.Child = questionText;
            ConversationPanel.Children.Add(questionBorder);

            // Réponse de Caramel & Flopy
            var reponseBorder = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 40, 0)
            };

            var reponseText = new TextBlock
            {
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20
            };

            // Parser la réponse pour le formatage
            var lines = reponse.Split(new[] { "\n\n" }, StringSplitOptions.None);
            bool first = true;

            foreach (var line in lines)
            {
                if (!first)
                {
                    reponseText.Inlines.Add(new LineBreak());
                    reponseText.Inlines.Add(new LineBreak());
                }
                first = false;

                // Titre en gras
                if (line.StartsWith("**") && line.Contains("**"))
                {
                    var boldText = line.Replace("**", "");
                    reponseText.Inlines.Add(new Run(boldText) { FontWeight = FontWeights.Bold });
                }
                // Italique
                else if (line.Contains("*") && !line.StartsWith("•"))
                {
                    var parts = line.Split('*');
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (i % 2 == 1)
                            reponseText.Inlines.Add(new Run(parts[i]) { FontStyle = FontStyles.Italic, Foreground = new SolidColorBrush(Color.FromRgb(0, 145, 90)) });
                        else
                            reponseText.Inlines.Add(new Run(parts[i]));
                    }
                }
                else
                {
                    reponseText.Inlines.Add(new Run(line));
                }
            }

            reponseBorder.Child = reponseText;
            ConversationPanel.Children.Add(reponseBorder);
        }
    }
}
