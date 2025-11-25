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
        private readonly IDatabase _database;
        private Dictionary<string, string> _questionsReponses;

        public GuideUtilisateurWindow(AuthenticationService authService, IDatabase database)
        {
            InitializeComponent();
            _authService = authService;
            _database = database;
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
                    "En tant qu'administrateur, vous disposez de droits étendus sur le système.\n\n" +
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
                    "Chaque rôle dispose de permissions spécifiques selon son périmètre d'action." },
                
                { "Comment utiliser le Dashboard et le Kanban ?",
                    "Le Dashboard et le Kanban sont vos outils de pilotage quotidien.\n\n" +
                    "**Dashboard (🏠) :**\n" +
                    "• Vue d'ensemble avec KPIs : tâches terminées, en cours, à prioriser\n" +
                    "• Productivité de l'équipe en pourcentage\n" +
                    "• Notifications importantes avec Agent Project & Change\n" +
                    "• Cliquez sur une notification pour l'envoyer par email au développeur\n\n" +
                    "**Kanban (🎯) :**\n" +
                    "• Colonnes : EN ATTENTE | A PRIORISER (zone admin) | À FAIRE | EN COURS | EN TEST | TERMINÉ\n" +
                    "• Glissez-déposez les cartes pour changer les statuts\n" +
                    "• Filtres par développeur et par projet\n" +
                    "• Suppression de tâches possible (croix rouge sur les cartes)\n\n" +
                    "La zone admin (EN ATTENTE et A PRIORISER) est visible uniquement par les administrateurs." },
                
                { "Comment gérer le Backlog et les Demandes ?",
                    "Le Backlog et les Demandes structurent la planification de vos projets.\n\n" +
                    "**Backlog (📋) :**\n" +
                    "• Liste de TOUTES les tâches du système\n" +
                    "• Créez de nouvelles tâches avec le bouton ➕\n" +
                    "• Assignez des développeurs et définissez les priorités\n" +
                    "• Double-cliquez sur une tâche pour l'éditer\n" +
                    "• Supprimez les tâches obsolètes (droits administrateur uniquement)\n\n" +
                    "**Demandes (📝) :**\n" +
                    "• Créez des demandes métier avec ➕ Nouvelle demande\n" +
                    "• Assignez un Business Analyst pour spécifier\n" +
                    "• Archivez les demandes terminées (bouton Archiver)\n" +
                    "• Seul l'administrateur peut supprimer et archiver\n\n" +
                    "⚠️ Privilégiez l'archivage à la suppression pour conserver l'historique." },
                
                { "Comment fonctionne le suivi des CRA et du temps ?",
                    "Le CRA assure le suivi précis du temps passé sur les projets.\n\n" +
                    "**Saisir CRA (⏱️) :**\n" +
                    "• Les développeurs saisissent leur temps par tâche et par jour\n" +
                    "• Vous pouvez consulter les saisies mais pas en créer (réservé aux développeurs)\n\n" +
                    "**Suivi CRA (📊) - Section ADMINISTRATION :**\n" +
                    "• Vue calendrier avec temps saisi par développeur\n" +
                    "• Validez les CRA pour les comptabiliser dans les statistiques\n" +
                    "• États : Prévisionnel (orange clair) → À valider (orange) → Validé (vert)\n" +
                    "• Seul le temps validé compte dans le 'Temps réel passé' du Kanban\n\n" +
                    "**Impact sur les statistiques :**\n" +
                    "• Le Kanban affiche Temps réel = somme des CRA validés\n" +
                    "• Les estimations vs réalisé permettent d'ajuster les futurs chiffrages\n\n" +
                    "Validez les CRA régulièrement pour maintenir des statistiques à jour." },
                
                { "Comment utiliser les Notifications intelligentes ?",
                    "Les notifications vous aident à piloter efficacement vos projets.\n\n" +
                    "**Types de notifications :**\n" +
                    "• 🔴 URGENT : Tâches en retard critique - action immédiate requise\n" +
                    "• ⚠️ ATTENTION : Échéance proche - anticipez les risques\n" +
                    "• ✅ SUCCESS : Validation positive - progression du projet\n" +
                    "• 📋 INFO : Informations générales - restez informé\n\n" +
                    "**Fonctionnalité Email :**\n" +
                    "• Cliquez sur '📧 Envoyer par email' pour notifier l'équipe\n" +
                    "• Outlook s'ouvre avec un message structuré et contextualisé\n" +
                    "• Détails automatiques : tâche, urgence, échéance, actions requises\n" +
                    "• Facilite le suivi et la communication avec les équipes\n\n" +
                    "**Badge rouge :**\n" +
                    "Indique le nombre de notifications non lues dans le menu\n\n" +
                    "Traitez les notifications URGENT en priorité pour éviter les impacts sur le planning." }
            };
        }

        private void ChargerQuestionsChefDeProjet()
        {
            _questionsReponses = new Dictionary<string, string>
            {
                { "Quelles sont mes permissions en tant que Chef de Projet ?",
                    "En tant que Chef de Projet, vous disposez de droits étendus pour orchestrer votre équipe.\n\n" +
                    "**Ce que vous POUVEZ faire :**\n" +
                    "✅ Créer des demandes métier (📝 Demandes)\n" +
                    "✅ Prioriser les tâches dans le Backlog\n" +
                    "✅ Assigner des développeurs aux tâches\n" +
                    "✅ Modifier toutes les tâches (pas seulement les vôtres)\n" +
                    "✅ Supprimer des tâches et des demandes\n" +
                    "✅ Consulter tous les KPI et statistiques\n" +
                    "✅ Changer les statuts dans le Kanban\n\n" +
                    "**Ce que vous NE POUVEZ PAS faire :**\n" +
                    "❌ Accéder à la section ADMINISTRATION\n" +
                    "❌ Gérer les utilisateurs et les rôles\n" +
                    "❌ Archiver des demandes (réservé à l'administrateur)\n\n" +
                    "Votre rôle : orchestrer l'équipe et prioriser le travail selon les objectifs métier." },
                
                { "Comment organiser le Backlog et prioriser les tâches ?",
                    "La priorisation des tâches structure l'activité de votre équipe.\n\n" +
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
                    "**Approche stratégique :**\n" +
                    "Priorisez selon valeur métier et urgence. Les développeurs voient leurs tâches assignées dans le Kanban.\n\n" +
                    "Utilisez les priorités pour guider l'équipe vers les objectifs prioritaires." },
                
                { "Comment utiliser le Kanban et les Demandes ?",
                    "Le Kanban et les Demandes sont vos outils de pilotage visuel.\n\n" +
                    "**Kanban (🎯) :**\n" +
                    "• 4 colonnes principales : À FAIRE | EN COURS | EN TEST | TERMINÉ\n" +
                    "• Glissez-déposez les cartes pour changer les statuts\n" +
                    "• Filtres par développeur/projet disponibles en haut\n" +
                    "• Bouton ❌ pour supprimer une tâche obsolète\n" +
                    "• Temps réel passé affiché (basé sur CRA validés)\n\n" +
                    "**Demandes (📝) :**\n" +
                    "• Créez des demandes avec ➕ Nouvelle demande\n" +
                    "• Assignez un Business Analyst pour spécifier\n" +
                    "• Modifiez et supprimez les demandes (selon vos droits)\n" +
                    "• Suivez le cycle : Brouillon → Spécification → Chiffrage → Acceptée\n\n" +
                    "**Dashboard (🏠) :**\n" +
                    "Vue synthétique des KPIs et notifications importantes\n\n" +
                    "Organisez des points de synchronisation réguliers avec le Kanban comme support visuel." }
            };
        }

        private void ChargerQuestionsDeveloppeur()
        {
            _questionsReponses = new Dictionary<string, string>
            {
                { "Quelles sont mes permissions en tant que Développeur ?",
                    "En tant que développeur, vous disposez de droits ciblés sur vos tâches.\n\n" +
                    "**Ce que vous POUVEZ faire :**\n" +
                    "✅ Saisir vos heures dans le CRA (⏱️ Saisir CRA)\n" +
                    "✅ Modifier VOS propres tâches assignées\n" +
                    "✅ Changer le statut de VOS tâches dans le Kanban\n" +
                    "✅ Participer au Planning Poker (chiffrage)\n" +
                    "✅ Consulter vos tâches dans le Dashboard et le Kanban\n\n" +
                    "**Ce que vous NE POUVEZ PAS faire :**\n" +
                    "❌ Créer des demandes (réservé aux BA, Chef de Projet, Admin)\n" +
                    "❌ Modifier ou supprimer les tâches des autres développeurs\n" +
                    "❌ Assigner des développeurs aux tâches\n" +
                    "❌ Prioriser les tâches\n" +
                    "❌ Consulter les KPI globaux et statistiques\n" +
                    "❌ Accéder à l'Administration\n\n" +
                    "Votre focus : exécuter vos tâches assignées et saisir votre temps avec précision." },
                
                { "Comment saisir mes heures dans le CRA ?",
                    "Le CRA permet de suivre précisément votre temps de travail sur les tâches.\n\n" +
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
                    "Votre temps est en 'prévisionnel' jusqu'à validation par l'administrateur. Seul le temps validé compte dans les statistiques du Kanban.\n\n" +
                    "Saisissez quotidiennement pour plus de précision." },
                
                { "Comment utiliser le Kanban et le Backlog ?",
                    "Le Kanban et le Backlog sont vos outils de travail quotidiens.\n\n" +
                    "**Dashboard (🏠) :**\n" +
                    "• Vue synthétique de VOS tâches assignées\n" +
                    "• Statuts : À faire, En cours, En test\n" +
                    "• Cliquez sur une tâche pour accéder aux détails\n\n" +
                    "**Kanban (🎯) :**\n" +
                    "• Colonnes : À FAIRE | EN COURS | EN TEST | TERMINÉ\n" +
                    "• Filtrez par votre nom pour voir uniquement VOS tâches\n" +
                    "• Glissez-déposez VOS cartes pour changer le statut\n" +
                    "• Vous NE POUVEZ PAS déplacer les tâches des autres développeurs\n" +
                    "• Temps réel = heures CRA validées par l'administrateur\n\n" +
                    "**Backlog (📋) :**\n" +
                    "• Liste de toutes les tâches (toute l'équipe)\n" +
                    "• Double-cliquez sur VOS tâches pour les éditer\n" +
                    "• Consultez les priorités définies par le Chef de Projet\n\n" +
                    "Approche recommandée : Dashboard le matin, Kanban en continu, CRA en fin de journée." }
            };
        }

        private void ChargerQuestionsBusinessAnalyst()
        {
            _questionsReponses = new Dictionary<string, string>
            {
                { "Quelles sont mes permissions en tant que Business Analyst ?",
                    "En tant que Business Analyst, vous assurez le lien entre métier et technique.\n\n" +
                    "**Ce que vous POUVEZ faire :**\n" +
                    "✅ Créer des demandes métier (📝 Demandes)\n" +
                    "✅ Modifier les demandes que vous avez créées\n" +
                    "✅ Consulter les KPI dans le Dashboard\n" +
                    "✅ Consulter le Backlog et le Kanban (lecture seule)\n\n" +
                    "**Ce que vous NE POUVEZ PAS faire :**\n" +
                    "❌ Chiffrer les tâches (réservé aux développeurs)\n" +
                    "❌ Prioriser les tâches (réservé Chef de Projet et Admin)\n" +
                    "❌ Modifier les tâches dans le Backlog\n" +
                    "❌ Supprimer des demandes (Chef de Projet et Admin)\n" +
                    "❌ Saisir des CRA (réservé aux développeurs)\n" +
                    "❌ Accéder à l'Administration\n\n" +
                    "Votre rôle : exprimer le besoin métier avec clarté et précision dans les demandes." },
                
                { "Comment créer et suivre mes demandes ?",
                    "Les demandes structurent l'expression des besoins métier.\n\n" +
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
                    "• Chiffrage : les développeurs estiment (vous ne chiffrez pas)\n" +
                    "• Acceptée : prête à être découpée en tâches\n\n" +
                    "**Suivi :**\n" +
                    "• Dashboard : vue synthétique\n" +
                    "• Backlog : tâches créées depuis vos demandes\n" +
                    "• Kanban : avancement visuel (lecture seule)\n\n" +
                    "Plus votre description est précise, plus l'équipe pourra estimer et implémenter efficacement." }
            };
        }

        private void ChargerQuestionsGenerales()
        {
            _questionsReponses = new Dictionary<string, string>
            {
                { "Comment naviguer dans BacklogManager ?",
                    "BacklogManager BNP Paribas structure votre pilotage de projets.\n\n" +
                    "**Menu latéral gauche avec sections :**\n\n" +
                    "**VUES :**\n" +
                    "• 🏠 Dashboard : Tableau de bord personnel, KPIs, notifications\n" +
                    "• 📋 Backlog : Liste complète des tâches\n" +
                    "• 🎯 Kanban : Suivi visuel (À faire → En cours → Test → Terminé)\n\n" +
                    "**TEMPS & CRA :**\n" +
                    "• ⏱️ Saisir CRA : Saisie des heures par tâche (développeurs)\n\n" +
                    "**ADMINISTRATION :**\n" +
                    "• 📊 Suivi CRA : Validation des temps (administrateur uniquement)\n\n" +
                    "**ACTIONS :**\n" +
                    "• 📝 Demandes : Gestion des besoins métier\n" +
                    "• 🔔 Notifications : Alertes et suivis avec Agent Project & Change\n\n" +
                    "Le Dashboard constitue votre point de départ quotidien pour consulter l'activité." },
                
                { "Qu'est-ce qu'Agent Project & Change ?",
                    "Agent Project & Change est votre assistant de pilotage dans BacklogManager.\n\n" +
                    "**Rôle :**\n" +
                    "Vous accompagner dans vos projets avec des conseils structurés et un discours adapté à votre rôle.\n\n" +
                    "**Où le trouver :**\n" +
                    "• Dans ce guide (vous y êtes actuellement)\n" +
                    "• Sur les notifications du Dashboard\n" +
                    "• Dans la fenêtre Notifications complète\n" +
                    "• Sur les états vides (pas de données)\n\n" +
                    "**Sources d'aide :**\n" +
                    "• Ce guide adapté à votre rôle\n" +
                    "• Tooltips en survolant les boutons\n" +
                    "• Votre administrateur système\n\n" +
                    "Agent Project & Change vous fournit des informations claires et actionnables pour piloter efficacement." },

                { "Comment utiliser le Chat avec l'IA (Agent Project & Change) ?",
                    "Le Chat IA est accessible via l'icône 🔔 Notifications en haut de l'écran.\n\n" +
                    "**Configuration initiale (première utilisation) :**\n" +
                    "1. Cliquez sur l'icône 🔔 Notifications\n" +
                    "2. Cliquez sur le bouton 💬 'Discuter avec l'Agent IA'\n" +
                    "3. Un écran vous demande de configurer votre token\n" +
                    "4. Collez votre token d'accès API (Bearer token)\n" +
                    "5. Cliquez sur 'Valider'\n\n" +
                    "**Où obtenir le token ?**\n" +
                    "Le token est fourni par votre administrateur système ou l'équipe IT.\n" +
                    "Format : Bearer token pour l'API GenFactory AI\n" +
                    "URL API : https://genfactory-ai.analytics.cib.echonet/genai/api/v2/chat/completions\n\n" +
                    "**Utiliser le chat :**\n" +
                    "• Posez vos questions directement dans la zone de texte\n" +
                    "• L'IA connaît toutes les fonctionnalités de BacklogManager\n" +
                    "• Elle adapte ses réponses selon votre rôle\n" +
                    "• Exemples : 'Comment créer une tâche ?', 'Explique-moi le Kanban', 'Comment valider un CRA ?'\n\n" +
                    "**Historique des conversations (Admin uniquement) :**\n" +
                    "• Administration > Historique des chats IA\n" +
                    "• Voir toutes les conversations par utilisateur\n" +
                    "• Historique complet de tous les échanges\n\n" +
                    "Le token est stocké localement et sécurisé. Vous pouvez le changer à tout moment." }
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

            // Utiliser toujours Agent Project & Change
            string imageSource = "/Images/agent-project-and-change.png";

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

            // Réponse de l'Agent Project & Change
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

        private void OpenAgentChat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var chatHistoryService = new ChatHistoryService(_database);
                var currentUser = _authService.CurrentUser;
                
                var chatWindow = new AgentChatWindow(chatHistoryService, currentUser);
                chatWindow.Owner = this;
                chatWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture du chat : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
