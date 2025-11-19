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
                { "Comment ajouter un nouvel utilisateur ?", 
                    "Excellente question ! Voici comment procéder de manière simple :\n\n" +
                    "1️⃣ Rendez-vous dans l'onglet **Administration** en haut de l'application\n" +
                    "2️⃣ Cliquez sur **Gestion des utilisateurs**\n" +
                    "3️⃣ Appuyez sur le bouton **➕ Nouvel utilisateur**\n" +
                    "4️⃣ Remplissez les informations : nom, prénom, email\n" +
                    "5️⃣ Choisissez le rôle approprié (Dev, Chef de projet, etc.)\n" +
                    "6️⃣ Validez et voilà ! L'utilisateur peut maintenant se connecter.\n\n" +
                    "💡 *Astuce* : Pensez à bien choisir le rôle dès le départ, cela définit les permissions !" },
                
                { "Comment gérer les projets ?",
                    "Ah, la gestion de projets ! C'est un peu comme diriger un orchestre 🎼\n\n" +
                    "**Pour créer un projet :**\n" +
                    "• Allez dans **Projets & Équipes**\n" +
                    "• Cliquez sur **Créer un projet**\n" +
                    "• Définissez le nom, la durée des sprints\n" +
                    "• Assignez un chef de projet\n" +
                    "• Ajoutez les développeurs à l'équipe\n\n" +
                    "**Pour suivre l'avancement :**\n" +
                    "• Le **Dashboard** vous donne une vue globale\n" +
                    "• Le **Kanban** montre les tâches en temps réel\n" +
                    "• Le **Suivi CRA** indique le temps passé\n\n" +
                    "🎯 *Mon conseil* : Revoyez régulièrement les projets pour ajuster les équipes si besoin !" },
                
                { "Que faire avec les demandes obsolètes ?",
                    "Bonne question ! Les demandes obsolètes, c'est comme les vieux papiers : il faut les ranger 📦\n\n" +
                    "**Pourquoi archiver ?**\n" +
                    "• Garde la liste des demandes actives propre et lisible\n" +
                    "• Préserve l'historique sans encombrer\n" +
                    "• Améliore les performances de l'application\n\n" +
                    "**Comment faire ?**\n" +
                    "1. Allez dans **Demandes**\n" +
                    "2. Sélectionnez la demande obsolète\n" +
                    "3. Cliquez sur **Archiver** (seuls les admins peuvent le faire)\n" +
                    "4. La demande disparaît de la vue principale\n\n" +
                    "**Pour retrouver une demande archivée ?**\n" +
                    "Rendez-vous dans **Archives** ! Tout y est conservé.\n\n" +
                    "⚠️ *Important* : N'archivez que les demandes vraiment terminées ou annulées !" },
                
                { "Comment valider les CRA ?",
                    "Ah, les CRA ! Le suivi du temps, c'est essentiel pour mesurer la productivité 📊\n\n" +
                    "**Pourquoi valider les CRA ?**\n" +
                    "• Permet de distinguer le temps prévisionnel du temps réel\n" +
                    "• Donne des statistiques précises\n" +
                    "• Aide à mieux estimer les futures tâches\n\n" +
                    "**La procédure est simple :**\n" +
                    "1. Allez dans **CRA Calendrier** ou **Suivi CRA**\n" +
                    "2. Vous voyez des journées en orange ? Elles sont à valider\n" +
                    "3. Cliquez sur le bouton **orange de validation**\n" +
                    "4. La journée passe en vert ✅\n\n" +
                    "**Les 3 états :**\n" +
                    "• 🟠 Orange clair : prévisionnel futur\n" +
                    "• 🟠 Orange vif : passé, à valider\n" +
                    "• 🟢 Vert : validé, compte dans les stats\n\n" +
                    "💡 *Conseil d'Einstein* : Validez régulièrement (chaque fin de semaine par exemple) !" },
                
                { "Comment voir les statistiques globales ?",
                    "Les statistiques, c'est mon dada ! J'adore les chiffres 📈\n\n" +
                    "**Le Dashboard est votre ami :**\n" +
                    "• Vue d'ensemble avec les KPIs principaux\n" +
                    "• Nombre de tâches terminées, en cours, à faire\n" +
                    "• Productivité du jour en pourcentage\n" +
                    "• Charge de travail des développeurs\n\n" +
                    "**Le Suivi CRA pour le détail :**\n" +
                    "• Temps passé par développeur\n" +
                    "• Séparation travail / congés / non-travaillé\n" +
                    "• Statistiques par période (mois, année)\n" +
                    "• Export possible vers Excel\n\n" +
                    "**Cliquez sur un dev dans les stats :**\n" +
                    "Une fenêtre s'ouvre avec toutes ses métriques détaillées !\n\n" +
                    "🔬 *Ma méthode* : Consultez le Dashboard tous les matins, ça donne le pouls du projet !" }
            };
        }

        private void ChargerQuestionsChefDeProjet()
        {
            _questionsReponses = new Dictionary<string, string>
            {
                { "Comment créer une nouvelle demande ?",
                    "Créer une demande, c'est le point de départ de tout projet ! 🚀\n\n" +
                    "**Étapes simples :**\n" +
                    "1. Allez dans l'onglet **Demandes**\n" +
                    "2. Cliquez sur **➕ Nouvelle demande**\n" +
                    "3. Remplissez le titre (clair et concis)\n" +
                    "4. Décrivez le besoin dans la description\n" +
                    "5. Définissez la criticité (Basse, Moyenne, Haute)\n" +
                    "6. Assignez un projet si vous en avez un\n" +
                    "7. Validez !\n\n" +
                    "**Après création :**\n" +
                    "• Vous pouvez assigner un Business Analyst pour détailler\n" +
                    "• La demande passe par différents statuts (Brouillon → Spécification → Chiffrage → Acceptée)\n\n" +
                    "💡 *Conseil* : Plus la description est précise, plus le chiffrage sera juste !" },
                
                { "Comment planifier les tâches ?",
                    "Planifier, c'est l'art de l'organisation ! Comme une partie d'échecs 🎲\n\n" +
                    "**Dans le Backlog :**\n" +
                    "• Créez des tâches depuis les demandes acceptées\n" +
                    "• Assignez des développeurs selon leurs compétences\n" +
                    "• Définissez une priorité (drag & drop pour réordonner)\n" +
                    "• Estimez la charge en jours\n\n" +
                    "**Utilisez le Kanban :**\n" +
                    "• Visualisez l'avancement en temps réel\n" +
                    "• Déplacez les cartes : À faire → En cours → Test → Terminé\n" +
                    "• Surveillez que rien ne reste bloqué\n\n" +
                    "🎯 *Stratégie d'Einstein* : Ne surchargez pas vos devs ! Mieux vaut livrer régulièrement que bloquer sur trop de tâches." },
                
                { "Comment suivre l'avancement du projet ?",
                    "Le suivi, c'est votre tableau de bord quotidien ! 🎛️\n\n" +
                    "**Dashboard - Vue rapide :**\n" +
                    "• KPIs essentiels en un coup d'œil\n" +
                    "• Productivité de l'équipe\n" +
                    "• Tâches terminées vs à faire\n\n" +
                    "**Kanban - Vue détaillée :**\n" +
                    "• Chaque tâche visible avec son statut\n" +
                    "• Filtrez par développeur ou projet\n" +
                    "• Identifiez les blocages rapidement\n\n" +
                    "**Suivi CRA - Vue temporelle :**\n" +
                    "• Temps passé par tâche\n" +
                    "• Comparez estimé vs réalisé\n" +
                    "• Ajustez vos futures estimations\n\n" +
                    "📊 *Mon truc* : Daily meeting de 10 min devant le Kanban, ça fait des miracles !" }
            };
        }

        private void ChargerQuestionsDeveloppeur()
        {
            _questionsReponses = new Dictionary<string, string>
            {
                { "Comment saisir mes heures de travail ?",
                    "Saisir vos heures, c'est crucial pour les statistiques ! ⏱️\n\n" +
                    "**Méthode simple :**\n" +
                    "1. Allez dans **CRA Calendrier**\n" +
                    "2. Cliquez sur un jour dans le calendrier\n" +
                    "3. Sélectionnez la tâche travaillée\n" +
                    "4. Indiquez le nombre d'heures (ou demi-journées)\n" +
                    "5. Ajoutez un commentaire si besoin\n" +
                    "6. Validez !\n\n" +
                    "**Astuces :**\n" +
                    "• Utilisez 4h pour une demi-journée, 8h pour une journée\n" +
                    "• Le système calcule automatiquement si vous dépassez la charge\n" +
                    "• Orange = à valider, Vert = validé et comptabilisé\n\n" +
                    "💡 *Important* : Saisissez régulièrement, pas tout en fin de mois !" },
                
                { "Comment poser mes congés ?",
                    "Ah, les vacances ! Tout le monde a besoin de repos 🏖️\n\n" +
                    "**C'est très simple :**\n" +
                    "1. Dans **CRA Calendrier**, cliquez sur **Saisir Congés**\n" +
                    "2. Choisissez entre :\n" +
                    "   • Journée simple (1 jour)\n" +
                    "   • Période (plusieurs jours d'affilée)\n" +
                    "3. Sélectionnez le type : Congés ou Non travaillé\n" +
                    "4. Validez\n\n" +
                    "**Le système intelligent :**\n" +
                    "• Décale automatiquement vos tâches planifiées\n" +
                    "• Ne compte pas dans votre charge de travail\n" +
                    "• Apparaît en bleu dans le calendrier 🔵\n\n" +
                    "**Repositionner une tâche :**\n" +
                    "Si une tâche tombe pendant vos congés, cliquez sur **Repositionner** pour la déplacer automatiquement !\n\n" +
                    "🌴 *Conseil* : Posez vos congés dès que possibles pour que l'équipe puisse s'organiser." },
                
                { "Comment voir mes tâches en cours ?",
                    "Vos tâches, c'est votre to-do list quotidienne ! ✅\n\n" +
                    "**Dans le Dashboard :**\n" +
                    "• Section \"Mes tâches\" avec tout ce qui vous est assigné\n" +
                    "• Statuts visibles : À faire, En cours, Test\n" +
                    "• Cliquez pour voir les détails\n\n" +
                    "**Dans le Kanban :**\n" +
                    "• Vue d'ensemble de toutes les tâches de l'équipe\n" +
                    "• Filtrez sur votre nom pour voir uniquement les vôtres\n" +
                    "• Glissez-déposez pour changer le statut\n\n" +
                    "**Dans le Backlog :**\n" +
                    "• Liste complète avec priorités\n" +
                    "• Double-cliquez pour éditer\n" +
                    "• Voyez la charge restante\n\n" +
                    "🎯 *Ma méthode* : Dashboard le matin pour voir le jour, Kanban pour updater l'avancement." }
            };
        }

        private void ChargerQuestionsBusinessAnalyst()
        {
            _questionsReponses = new Dictionary<string, string>
            {
                { "Comment bien rédiger une spécification ?",
                    "Les spécifications, c'est la fondation de tout ! Comme un plan d'architecte 📐\n\n" +
                    "**Les éléments clés :**\n" +
                    "1. **Contexte métier** : Pourquoi ce besoin existe ?\n" +
                    "2. **Spécifications fonctionnelles** : Que doit faire le système ?\n" +
                    "3. **Critères d'acceptation** : Comment valider que c'est bon ?\n" +
                    "4. **Bénéfices attendus** : Quel est le ROI ?\n\n" +
                    "**Règles d'or :**\n" +
                    "• Soyez précis et sans ambiguïté\n" +
                    "• Utilisez des exemples concrets\n" +
                    "• Ajoutez des schémas si nécessaire\n" +
                    "• Pensez aux cas limites et erreurs\n\n" +
                    "💡 *Astuce* : Faites relire par quelqu'un qui ne connaît pas le projet. S'il comprend, c'est bon !" },
                
                { "Comment participer au chiffrage ?",
                    "Le chiffrage, c'est l'art de l'estimation ! Pas toujours facile 🎲\n\n" +
                    "**Votre rôle en tant que BA :**\n" +
                    "• Clarifier les zones d'ombre pour les devs\n" +
                    "• Découper la demande en sous-tâches si elle est grosse\n" +
                    "• Participer aux sessions de Planning Poker\n" +
                    "• Valider que l'estimation correspond au scope\n\n" +
                    "**Dans l'application :**\n" +
                    "• Consultez la demande dans **Demandes**\n" +
                    "• Cliquez sur **Détails** puis **Chiffrage**\n" +
                    "• Les développeurs saisissent leurs estimations\n" +
                    "• Vous pouvez commenter et ajuster le périmètre\n\n" +
                    "🎯 *Conseil d'Einstein* : Un bon chiffrage vient d'une bonne spec. CQFD !" }
            };
        }

        private void ChargerQuestionsGenerales()
        {
            _questionsReponses = new Dictionary<string, string>
            {
                { "Comment naviguer dans l'application ?",
                    "Bienvenue dans BacklogManager ! Laissez-moi vous faire le tour du propriétaire 🏠\n\n" +
                    "**Les onglets principaux :**\n" +
                    "• 📊 **Dashboard** : Votre tableau de bord personnel\n" +
                    "• 📋 **Backlog** : Liste de toutes les tâches\n" +
                    "• 🎯 **Kanban** : Vue visuelle de l'avancement\n" +
                    "• 📝 **Demandes** : Gestion des besoins\n" +
                    "• ⏱️ **CRA Calendrier** : Saisie des temps\n" +
                    "• 📈 **Suivi CRA** : Statistiques temporelles\n\n" +
                    "💡 *Astuce* : Commencez toujours par le Dashboard, c'est votre point de départ quotidien !" },
                
                { "Comment obtenir de l'aide ?",
                    "Vous êtes déjà au bon endroit ! 🎓\n\n" +
                    "**Sources d'aide :**\n" +
                    "• Ce guide Einstein (vous y êtes !)\n" +
                    "• Les tooltips : survolez les boutons pour des infos\n" +
                    "• La documentation technique\n" +
                    "• Votre administrateur système\n\n" +
                    "**En cas de bug :**\n" +
                    "• Notez ce que vous faisiez\n" +
                    "• Prenez une capture d'écran si possible\n" +
                    "• Contactez le support technique\n\n" +
                    "🤝 *Rappel* : Il n'y a pas de question bête. Demandez toujours !" }
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

            // Réponse d'Einstein
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
