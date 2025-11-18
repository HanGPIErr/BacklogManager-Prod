using System;
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

        public GuideUtilisateurWindow(AuthenticationService authService)
        {
            InitializeComponent();
            _authService = authService;
            _userRole = _authService.GetCurrentUserRole();
            
            ChargerGuideSelonRole();
        }

        private void ChargerGuideSelonRole()
        {
            if (_userRole == null) return;

            TxtRole.Text = string.Format("Guide pour le rôle : {0}", _userRole.Nom);

            switch (_userRole.Type)
            {
                case RoleType.Administrateur:
                    ChargerGuideAdministrateur();
                    break;
                case RoleType.ChefDeProjet:
                    ChargerGuideChefDeProjet();
                    break;
                case RoleType.Developpeur:
                    ChargerGuideDeveloppeur();
                    break;
                case RoleType.BusinessAnalyst:
                    ChargerGuideBusinessAnalyst();
                    break;
                default:
                    ChargerGuideGeneral();
                    break;
            }
        }

        private void ChargerGuideAdministrateur()
        {
            AjouterSection("🎯 Vue d'ensemble", 
                "En tant qu'Administrateur, vous avez un accès complet à toutes les fonctionnalités de BacklogManager. Vous gérez l'équipe, les projets et supervisez l'ensemble des activités.");

            AjouterSection("👥 Gestion de l'équipe",
                "• Accédez à l'onglet Administration > Gestion des utilisateurs\n" +
                "• Créez, modifiez ou désactivez des utilisateurs\n" +
                "• Attribuez les rôles appropriés à chaque membre\n" +
                "• Gérez les équipes par rôle (Admins, Chefs de projet, Développeurs, BA)",
                CreerBoutonAction("Aller à Administration", "Administration"));

            AjouterSection("📁 Gestion des projets",
                "• Naviguez vers Projets & Équipes pour créer de nouveaux projets\n" +
                "• Définissez les objectifs et la durée des sprints\n" +
                "• Assignez des développeurs aux projets\n" +
                "• Suivez l'avancement global de chaque projet",
                CreerBoutonAction("Voir les projets", "Backlog"));

            AjouterSection("📝 Gestion des demandes",
                "• Consultez toutes les demandes dans l'onglet Demandes\n" +
                "• Archivez les demandes obsolètes (uniquement pour les admins)\n" +
                "• Validez et priorisez les demandes en cours\n" +
                "• Créez des tâches à partir des demandes acceptées",
                CreerBoutonAction("Voir les demandes", "Demandes"));

            AjouterSection("📊 Suivi et monitoring",
                "• Dashboard : Vue synthétique des KPIs\n" +
                "• Kanban : Visualisation de l'avancement des tâches\n" +
                "• Suivi CRA : Consultation du temps passé par développeur\n" +
                "• Archives : Accès aux tâches et demandes archivées");

            AjouterSection("🔐 Bonnes pratiques",
                "✓ Révisez régulièrement les rôles et permissions\n" +
                "✓ Archivez les demandes traitées pour maintenir une base propre\n" +
                "✓ Validez les CRA pour assurer un suivi précis du temps\n" +
                "✓ Communiquez les changements importants à l'équipe");
        }

        private void ChargerGuideChefDeProjet()
        {
            AjouterSection("🎯 Vue d'ensemble",
                "En tant que Chef de Projet, vous pilotez les projets et coordonnez les développements. Vous gérez les demandes, planifiez les tâches et suivez l'avancement de votre équipe.");

            AjouterSection("📝 Gestion des demandes",
                "• Créez de nouvelles demandes pour collecter les besoins métier\n" +
                "• Assignez un Business Analyst pour l'analyse détaillée\n" +
                "• Validez les spécifications et le chiffrage\n" +
                "• Acceptez ou refusez les demandes selon les priorités",
                CreerBoutonAction("Créer une demande", "Demandes"));

            AjouterSection("📋 Gestion des tâches",
                "• Accédez au Backlog pour voir toutes les tâches\n" +
                "• Créez des tâches à partir des demandes acceptées\n" +
                "• Assignez les tâches aux développeurs disponibles\n" +
                "• Définissez les priorités et dates limites\n" +
                "• Utilisez le Kanban pour visualiser l'avancement",
                CreerBoutonAction("Voir le Backlog", "Backlog"));

            AjouterSection("📁 Suivi des projets",
                "• Consultez l'onglet Projets & Équipes\n" +
                "• Suivez l'avancement de chaque sprint\n" +
                "• Vérifiez la charge de travail des développeurs\n" +
                "• Ajustez les ressources si nécessaire");

            AjouterSection("⏱️ Validation des CRA",
                "• Accédez à Suivi CRA pour consulter les temps saisis\n" +
                "• Vérifiez la cohérence avec les tâches assignées\n" +
                "• Identifiez les dépassements ou blocages\n" +
                "• Communiquez avec l'équipe si besoin",
                CreerBoutonAction("Consulter les CRA", "CRA"));

            AjouterSection("🎯 Bonnes pratiques",
                "✓ Priorisez les demandes selon la valeur métier\n" +
                "✓ Communiquez régulièrement avec les développeurs\n" +
                "✓ Anticipez les risques de dépassement de délais\n" +
                "✓ Validez les spécifications avant création de tâches");
        }

        private void ChargerGuideDeveloppeur()
        {
            AjouterSection("🎯 Vue d'ensemble",
                "En tant que Développeur, vous réalisez les tâches qui vous sont assignées et saisissez votre temps de travail quotidien.");

            AjouterSection("📋 Mes tâches",
                "• Consultez vos tâches dans l'onglet Backlog\n" +
                "• Utilisez le Kanban pour voir les tâches À faire, En cours, En test\n" +
                "• Cliquez sur une tâche pour voir ses détails\n" +
                "• Mettez à jour le statut au fur et à mesure de l'avancement",
                CreerBoutonAction("Voir mes tâches", "Kanban"));

            AjouterSection("⏱️ Saisie CRA (quotidienne)",
                "• Accédez à l'onglet Saisir CRA tous les jours\n" +
                "• Sélectionnez le jour dans le calendrier\n" +
                "• Pour chaque tâche travaillée, saisissez le temps\n" +
                "• Ajoutez un commentaire décrivant ce qui a été fait\n" +
                "• Le temps est en jours : 0.5j = 4h, 1j = 8h",
                CreerBoutonAction("Saisir mon CRA", "CRA"));

            AjouterSection("🔄 Workflow des tâches",
                "1. À faire → Commencez la tâche, passez-la \"En cours\"\n" +
                "2. En cours → Développement en cours, saisissez votre temps quotidien\n" +
                "3. En test → Une fois terminé, passez en test pour validation\n" +
                "4. Terminé → Le chef valide et clôture la tâche");

            AjouterSection("💬 Communication",
                "• Utilisez les commentaires sur les tâches pour signaler un problème\n" +
                "• Prévenez votre chef si vous êtes bloqué\n" +
                "• Indiquez si vous estimez dépasser le temps prévu\n" +
                "• Mettez à jour régulièrement le statut de vos tâches");

            AjouterSection("✅ Bonnes pratiques",
                "✓ Saisissez votre CRA quotidiennement (ne pas attendre la fin de semaine)\n" +
                "✓ Soyez précis dans vos commentaires CRA\n" +
                "✓ Alertez rapidement en cas de blocage technique\n" +
                "✓ Consultez le Kanban régulièrement pour voir vos priorités");
        }

        private void ChargerGuideBusinessAnalyst()
        {
            AjouterSection("🎯 Vue d'ensemble",
                "En tant que Business Analyst, vous collectez les besoins métier, rédigez les spécifications et facilitez le chiffrage des demandes.");

            AjouterSection("📝 Création de demandes",
                "• Accédez à l'onglet Demandes\n" +
                "• Cliquez sur \"Nouvelle demande\" pour créer une demande\n" +
                "• Renseignez le titre, description et contexte métier\n" +
                "• Définissez la criticité selon l'urgence\n" +
                "• Assignez un projet si applicable",
                CreerBoutonAction("Créer une demande", "Demandes"));

            AjouterSection("📋 Spécifications détaillées",
                "• Pour chaque demande, cliquez sur \"Détails\"\n" +
                "• Complétez les spécifications fonctionnelles\n" +
                "• Décrivez les bénéfices attendus pour le métier\n" +
                "• Ajoutez des critères d'acceptation clairs\n" +
                "• Joignez des maquettes ou documents si nécessaire");

            AjouterSection("💼 Suivi des demandes",
                "• Consultez régulièrement vos demandes en cours\n" +
                "• Filtrez par statut pour voir l'avancement\n" +
                "• Répondez aux questions des développeurs dans les commentaires\n" +
                "• Participez au chiffrage avec l'équipe technique");

            AjouterSection("🤝 Collaboration",
                "• Travaillez avec le Chef de Projet pour prioriser\n" +
                "• Clarifiez les besoins avec les développeurs\n" +
                "• Validez que les développements correspondent aux attentes\n" +
                "• Participez aux sessions de planning poker si nécessaire");

            AjouterSection("✅ Bonnes pratiques",
                "✓ Rédigez des spécifications claires et sans ambiguïté\n" +
                "✓ Définissez des critères d'acceptation mesurables\n" +
                "✓ Priorisez les demandes avec le métier\n" +
                "✓ Restez disponible pour répondre aux questions techniques");
        }

        private void ChargerGuideGeneral()
        {
            AjouterSection("🎯 Bienvenue sur BacklogManager",
                "BacklogManager est votre outil de gestion de projets et de suivi d'activité. Naviguez dans les différents onglets pour accéder aux fonctionnalités.");

            AjouterSection("📊 Dashboard",
                "Votre tableau de bord personnel affiche vos statistiques et tâches en cours.",
                CreerBoutonAction("Voir Dashboard", "Dashboard"));

            AjouterSection("📋 Backlog & Kanban",
                "Consultez et gérez les tâches du projet.",
                CreerBoutonAction("Voir Backlog", "Backlog"));

            AjouterSection("📝 Demandes",
                "Gérez les demandes de développement.",
                CreerBoutonAction("Voir Demandes", "Demandes"));
        }

        private void AjouterSection(string titre, string contenu, UIElement actionButton = null)
        {
            // Card container
            var border = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(20, 18, 20, 18),
                Margin = new Thickness(0, 0, 0, 15),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 10,
                    ShadowDepth = 0,
                    Opacity = 0.1,
                    Color = Colors.Gray
                }
            };

            var stackPanel = new StackPanel();

            // Titre
            var txtTitre = new TextBlock
            {
                Text = titre,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(26, 25, 25)),
                Margin = new Thickness(0, 0, 0, 12)
            };
            stackPanel.Children.Add(txtTitre);

            // Contenu
            var txtContenu = new TextBlock
            {
                Text = contenu,
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 22,
                Margin = new Thickness(0, 0, 0, actionButton != null ? 15 : 0)
            };
            stackPanel.Children.Add(txtContenu);

            // Bouton d'action si fourni
            if (actionButton != null)
            {
                stackPanel.Children.Add(actionButton);
            }

            border.Child = stackPanel;
            ContentPanel.Children.Add(border);
        }

        private Button CreerBoutonAction(string texte, string cible)
        {
            var button = new Button
            {
                Content = string.Format("→ {0}", texte),
                Height = 38,
                Padding = new Thickness(18, 0, 18, 0),
                Background = new SolidColorBrush(Color.FromRgb(0, 145, 90)),
                Foreground = Brushes.White,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Left,
                Tag = cible
            };

            button.Click += BoutonAction_Click;

            // Style avec template
            var style = new Style(typeof(Button));
            var template = new ControlTemplate(typeof(Button));
            
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            borderFactory.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));
            
            var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            
            borderFactory.AppendChild(contentFactory);
            template.VisualTree = borderFactory;
            
            var hoverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0, 120, 67))));
            template.Triggers.Add(hoverTrigger);
            
            style.Setters.Add(new Setter(Button.TemplateProperty, template));
            button.Style = style;

            return button;
        }

        private void BoutonAction_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is string cible)
            {
                // Fermer la fenêtre et indiquer quelle action effectuer
                this.Tag = cible;
                this.DialogResult = true;
                this.Close();
            }
        }
    }
}
