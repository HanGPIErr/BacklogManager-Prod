using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Linq;
using BacklogManager.Domain;
using BacklogManager.Services;

namespace BacklogManager.Views
{
    public partial class GuideUtilisateurView : UserControl
    {
        private readonly AuthenticationService _authService;
        private readonly Role _userRole;
        private readonly IDatabase _database;
        private readonly MainWindow _mainWindow;
        private Dictionary<string, string> _questionsReponses;
        private readonly GuideContentService _guideContentService;

        public GuideUtilisateurView(AuthenticationService authService, IDatabase database, MainWindow mainWindow)
        {
            InitializeComponent();
            _authService = authService;
            _database = database;
            _mainWindow = mainWindow;
            _userRole = _authService.GetCurrentUserRole();
            _guideContentService = new GuideContentService();
            
            // Initialize default message in RichTextBox
            InitialiserMessageDefaut();
            
            ChargerQuestionsSelonRole();
        }

        private void InitialiserMessageDefaut()
        {
            var flowDoc = new FlowDocument();
            flowDoc.PagePadding = new Thickness(0);
            
            var paragraph = new Paragraph
            {
                Margin = new Thickness(0, 0, 0, 12),
                LineHeight = 24
            };
            
            var run = new Run(LocalizationService.Instance.GetString("Guide_SelectQuestion"))
            {
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999")),
                FontStyle = FontStyles.Italic
            };
            
            paragraph.Inlines.Add(run);
            flowDoc.Blocks.Add(paragraph);
            RtbReponse.Document = flowDoc;
        }

        private void BtnChatIA_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.AfficherChatIA();
        }

        private void ChargerQuestionsSelonRole()
        {
            if (_userRole == null) return;

            TxtRole.Text = $"{LocalizationService.Instance.GetString("Guide_RolePrefix")} {_userRole.Nom}";
            _questionsReponses = _guideContentService.GetQuestionsForRole(_userRole.Type);

            AfficherQuestions();
        }

        private void ChargerQuestionsAdministrateur()
        {
            _questionsReponses = new Dictionary<string, string>
            {
                { "Comment gérer les utilisateurs ?", 
                    "📋 GESTION DES UTILISATEURS\n\n" +
                    "1. Accédez au menu ADMINISTRATION → Gérer l'équipe\n" +
                    "2. Vous verrez la liste complète des utilisateurs\n" +
                    "3. Double-cliquez sur un utilisateur pour modifier son profil\n" +
                    "4. Modifiez le rôle (Admin, Chef de Projet, Développeur, Business Analyst)\n" +
                    "5. Définissez les permissions d'accès aux modules\n" +
                    "6. Validez les changements avec le bouton Enregistrer\n\n" +
                    "💡 Les rôles déterminent les droits d'accès et les fonctionnalités disponibles." },
                
                { "Comment utiliser le Reporting ?",
                    "📊 REPORTING & PILOTAGE\n\n" +
                    "Menu ADMINISTRATION → Onglet Reporting\n\n" +
                    "Deux vues disponibles :\n\n" +
                    "📊 VUE PROGRAMME :\n" +
                    "• Analyse consolidée de tous les projets d'un programme\n" +
                    "• KPIs globaux : progression, vélocité, charge\n" +
                    "• Répartition des tâches par priorité et statut\n" +
                    "• Contributions des équipes et ressources\n" +
                    "• Vue comparative avant/pendant la période\n\n" +
                    "📁 VUE PROJET :\n" +
                    "• Analyse détaillée d'un projet spécifique\n" +
                    "• Santé du projet (On track / En retard / Critique)\n" +
                    "• Tâches terminées vs en cours\n" +
                    "• Temps réel vs estimé\n" +
                    "• Performance par développeur\n\n" +
                    "Filtrez par période pour des analyses temporelles précises." },
                
                { "Comment utiliser les analyses IA ?",
                    "🤖 ANALYSES INTELLIGENTES\n\n" +
                    "L'IA analyse automatiquement vos données :\n\n" +
                    "📊 ANALYSE PROJET (Reporting) :\n" +
                    "• Recommandations de pilotage\n" +
                    "• Détection des risques\n" +
                    "• Optimisation de la charge\n" +
                    "• Prévisions de livraison\n\n" +
                    "📈 ANALYSE DÉVELOPPEUR (Suivi CRA) :\n" +
                    "• Performance individuelle\n" +
                    "• Charge de travail\n" +
                    "• Suggestions d'amélioration\n\n" +
                    "📉 ANALYSE STATISTIQUES (Dashboard) :\n" +
                    "• Tendances globales\n" +
                    "• KPIs d'équipe\n" +
                    "• Points d'attention\n\n" +
                    "✅ VALIDATION CRA (Timeline) :\n" +
                    "• Analyse de conformité\n" +
                    "• Détection d'anomalies\n" +
                    "• Rapport de validation automatique\n\n" +
                    "L'IA nécessite un token API configuré dans Paramètres." },
                
                { "Comment valider les CRA ?",
                    "✅ VALIDATION DES CRA\n\n" +
                    "1. Menu ADMINISTRATION → Timeline (Suivi CRA)\n" +
                    "2. Sélectionnez le programme ou projet\n" +
                    "3. Vérifiez les temps saisis par chaque développeur\n" +
                    "4. Utilisez l'analyse IA pour détecter les anomalies\n" +
                    "5. Validez en masse avec le bouton 'Valider'\n" +
                    "6. Un rapport détaillé est généré automatiquement\n\n" +
                    "📌 Les CRA validés sont verrouillés et ne peuvent plus être modifiés.\n" +
                    "💡 L'IA vous signale les tâches en retard et les écarts de temps." },
                
                { "Comment gérer les archives ?",
                    "📦 GESTION DES ARCHIVES\n\n" +
                    "Menu ADMINISTRATION → Archives\n\n" +
                    "Fonctionnalités :\n" +
                    "• Archiver les demandes obsolètes ou terminées\n" +
                    "• Consulter l'historique des éléments archivés\n" +
                    "• Restaurer des éléments si nécessaire\n" +
                    "• Libérer de l'espace dans le backlog actif\n\n" +
                    "Pour archiver une demande :\n" +
                    "1. Ouvrez la demande dans DEMANDES\n" +
                    "2. Cliquez sur 'Archiver'\n" +
                    "3. Confirmez l'action\n\n" +
                    "⚠️ Seuls les administrateurs peuvent archiver et restaurer." },
                
                { "Comment gérer les permissions ?",
                    "🔐 GESTION DES PERMISSIONS\n\n" +
                    "Menu ADMINISTRATION → Utilisateurs & Rôles\n\n" +
                    "Permissions par rôle :\n\n" +
                    "• Administrateur : accès total, reporting, archives, gestion système\n" +
                    "• Chef de Projet : backlog, affectations, validation CRA, statistiques\n" +
                    "• Développeur : consultation tâches, saisie CRA, commentaires\n" +
                    "• Business Analyst : création demandes, consultation backlog\n\n" +
                    "Configuration des permissions :\n" +
                    "1. Onglet 'Rôles' dans Utilisateurs & Rôles\n" +
                    "2. Cochez les permissions pour chaque rôle\n" +
                    "3. Enregistrez les modifications\n" +
                    "4. Les utilisateurs reçoivent immédiatement les nouveaux droits" },
                
                { "Comment utiliser le chat IA ?",
                    "🤖 AGENT CONVERSATIONNEL IA\n\n" +
                    "1. Cliquez sur 'Discuter avec l'IA' en bas de ce panneau\n" +
                    "2. Première utilisation : configurez votre token API\n" +
                    "   - Allez sur https://genfactory-ai.analytics.cib.echonet\n" +
                    "   - Générez un token Bearer dans votre profil\n" +
                    "   - Collez-le dans la configuration (Settings)\n" +
                    "3. Posez vos questions sur BacklogManager\n" +
                    "4. L'IA connaît toutes les fonctionnalités\n\n" +
                    "💡 L'IA peut vous guider pas à pas dans vos tâches quotidiennes." }
            };
        }

        private void ChargerQuestionsChefDeProjet()
        {
            _questionsReponses = new Dictionary<string, string>
            {
                { "Comment créer une tâche ?", 
                    "➕ CRÉATION D'UNE TÂCHE\n\n" +
                    "1. Accédez au menu BACKLOG\n" +
                    "2. Cliquez sur le bouton ➕ Nouvelle tâche\n" +
                    "3. Remplissez les informations :\n" +
                    "   • Titre clair et concis\n" +
                    "   • Description détaillée\n" +
                    "   • Développeur assigné\n" +
                    "   • Priorité (Urgente, Haute, Moyenne, Basse)\n" +
                    "   • Charge estimée en heures\n" +
                    "   • Projet associé\n" +
                    "4. Cliquez sur Enregistrer\n\n" +
                    "La tâche apparaît immédiatement dans le backlog et le Kanban." },
                
                { "Comment utiliser le Kanban ?",
                    "📋 TABLEAU KANBAN\n\n" +
                    "Menu KANBAN pour visualiser votre workflow :\n\n" +
                    "• Colonnes : À faire, En attente, À prioriser, En cours, Test, Terminé\n" +
                    "• Glissez-déposez les cartes entre colonnes\n" +
                    "• Filtrez par projet ou développeur\n" +
                    "• Double-cliquez sur une carte pour voir les détails\n" +
                    "• Les couleurs indiquent la priorité\n" +
                    "• Compteurs en temps réel par colonne\n\n" +
                    "💡 Le Kanban se met à jour en temps réel pour toute l'équipe." },
                
                { "Comment consulter les statistiques ?",
                    "📊 STATISTIQUES & ANALYSES\n\n" +
                    "Menu STATISTIQUES pour piloter votre équipe :\n\n" +
                    "📈 INDICATEURS CLÉS :\n" +
                    "• Tâches terminées vs en cours\n" +
                    "• Productivité de l'équipe (%)\n" +
                    "• Vélocité moyenne\n" +
                    "• Charge de travail par développeur\n\n" +
                    "📉 GRAPHIQUES :\n" +
                    "• Évolution des tâches dans le temps\n" +
                    "• Répartition par priorité et statut\n" +
                    "• Temps réel vs estimé\n\n" +
                    "🤖 ANALYSE IA :\n" +
                    "Demandez une analyse intelligente pour obtenir :\n" +
                    "• Recommandations de pilotage\n" +
                    "• Détection des risques\n" +
                    "• Suggestions d'amélioration\n\n" +
                    "Exportez les statistiques en PDF pour vos reportings." },
                
                { "Comment affecter des tâches ?",
                    "👥 AFFECTATION DES TÂCHES\n\n" +
                    "Plusieurs méthodes :\n\n" +
                    "1. Depuis le BACKLOG :\n" +
                    "   • Sélectionnez une tâche\n" +
                    "   • Choisissez le développeur dans la liste\n" +
                    "   • Sauvegardez\n\n" +
                    "2. Depuis le KANBAN :\n" +
                    "   • Double-clic sur une carte\n" +
                    "   • Modifiez l'assignation\n\n" +
                    "Le développeur reçoit une notification automatiquement." },
                
                { "Comment suivre l'avancement ?",
                    "📈 SUIVI DE L'AVANCEMENT\n\n" +
                    "Plusieurs indicateurs disponibles :\n\n" +
                    "• Dashboard : Vue d'ensemble KPIs et graphiques\n" +
                    "• Kanban : État temps réel des tâches\n" +
                    "• Timeline : Suivi CRA par projet et équipe\n" +
                    "• Statistiques : Graphiques détaillés et analyses\n" +
                    "• Notifications : Alertes sur tâches urgentes\n\n" +
                    "💡 Exportez les statistiques en PDF pour vos reportings." },
                
                { "Comment gérer les demandes ?",
                    "📝 GESTION DES DEMANDES\n\n" +
                    "1. Menu DEMANDES pour voir toutes les demandes\n" +
                    "2. Triez par criticité ou date\n" +
                    "3. Ouvrez une demande pour l'analyser\n" +
                    "4. Transformez-la en tâches du backlog\n" +
                    "5. Assignez aux développeurs\n" +
                    "6. Mettez à jour le statut\n\n" +
                    "Les Business Analysts sont notifiés des changements d'état." },
                
                { "Comment gérer les notifications ?",
                    "🔔 CENTRE DE NOTIFICATIONS\n\n" +
                    "Menu NOTIFICATIONS (icône cloche) :\n\n" +
                    "Vous êtes notifié pour :\n" +
                    "• Nouvelles demandes créées\n" +
                    "• Tâches bloquées ou en retard\n" +
                    "• Commentaires sur vos projets\n" +
                    "• CRA en attente de validation\n" +
                    "• Changements de statut importants\n\n" +
                    "Actions possibles :\n" +
                    "• Marquer comme lue\n" +
                    "• Accéder directement à l'élément\n" +
                    "• Filtrer par type de notification\n\n" +
                    "Le compteur rouge indique le nombre de notifications non lues." }
            };
        }

        private void ChargerQuestionsDeveloppeur()
        {
            _questionsReponses = new Dictionary<string, string>
            {
                { "Comment saisir mon CRA ?", 
                    "⏱️ SAISIE DU CRA\n\n" +
                    "1. Menu SAISIR CRA → Vue calendrier mensuel\n" +
                    "2. Cliquez sur un jour pour ajouter du temps\n" +
                    "3. Sélectionnez la tâche dans la liste déroulante\n" +
                    "4. Indiquez les heures travaillées\n" +
                    "5. Ajoutez un commentaire si nécessaire\n" +
                    "6. Cliquez sur Enregistrer\n\n" +
                    "💡 Le total journalier s'affiche en bas. Visez 7-8h par jour.\n" +
                    "📌 Validez votre CRA en fin de mois avant soumission au chef de projet." },
                
                { "Comment voir mes tâches ?",
                    "📋 VUE DES TÂCHES\n\n" +
                    "Plusieurs vues disponibles :\n\n" +
                    "• DASHBOARD : Vos tâches en cours et urgentes\n" +
                    "• BACKLOG : Toutes vos tâches assignées avec filtres\n" +
                    "• KANBAN : Vue workflow avec glisser-déposer\n\n" +
                    "Double-cliquez sur une tâche pour :\n" +
                    "• Voir description complète\n" +
                    "• Ajouter des commentaires\n" +
                    "• Changer le statut\n" +
                    "• Consulter l'historique" },
                
                { "Comment mettre à jour une tâche ?",
                    "✏️ MISE À JOUR DE TÂCHE\n\n" +
                    "1. Ouvrez la tâche depuis BACKLOG ou KANBAN\n" +
                    "2. Modifiez le statut (À faire → En cours → Test → Terminé)\n" +
                    "3. Ajoutez des commentaires sur votre progression\n" +
                    "4. Mettez à jour le temps restant si nécessaire\n" +
                    "5. Sauvegardez les modifications\n\n" +
                    "🔔 Le chef de projet est notifié des changements importants." },
                
                { "Comment créer une tâche spéciale ?",
                    "✨ TÂCHES SPÉCIALES\n\n" +
                    "Pour créer un congé, support ou autre :\n\n" +
                    "1. Menu BACKLOG → Bouton ➕\n" +
                    "2. Sélectionnez le type :\n" +
                    "   🏖️ CONGÉS : Jours de repos, RTT, CP\n" +
                    "   🆘 SUPPORT : Assistance utilisateur, hotline\n" +
                    "   📝 AUTRE : Réunions, formations, administratif\n" +
                    "3. Remplissez les dates et la durée\n" +
                    "4. Ajoutez une description si nécessaire\n" +
                    "5. Enregistrez\n\n" +
                    "Ces tâches apparaissent dans votre CRA et sont comptabilisées\n" +
                    "dans votre charge de travail." },
                
                { "Comment signaler un blocage ?",
                    "🚨 SIGNALEMENT DE BLOCAGE\n\n" +
                    "1. Ouvrez la tâche bloquée\n" +
                    "2. Ajoutez un commentaire détaillé expliquant :\n" +
                    "   • Nature du blocage\n" +
                    "   • Impact sur le planning\n" +
                    "   • Solution envisagée\n" +
                    "3. Changez la priorité en 'Urgente' si nécessaire\n" +
                    "4. Sauvegardez\n\n" +
                    "💡 Contactez votre chef de projet via les commentaires\n" +
                    "ou directement pour résoudre rapidement le blocage." },
                
                { "Comment consulter mes CRA passés ?",
                    "📊 HISTORIQUE CRA\n\n" +
                    "1. Menu SAISIR CRA\n" +
                    "2. Utilisez les flèches pour naviguer entre les mois\n" +
                    "3. Les jours avec temps saisi sont en couleur\n" +
                    "4. Les jours validés sont verrouillés (cadenas)\n" +
                    "5. Cliquez sur un jour pour voir le détail\n\n" +
                    "Les indicateurs visuels :\n" +
                    "• ✅ Vert : jour complet et validé\n" +
                    "• 🟡 Orange : jour partiel\n" +
                    "• ⚠️ Rouge : jour sans saisie\n" +
                    "• 🔒 Cadenas : CRA validé, non modifiable" },
                
                { "Comment utiliser le chat IA ?",
                    "🤖 ASSISTANCE IA\n\n" +
                    "1. Cliquez sur 'Discuter avec l'IA'\n" +
                    "2. Configurez votre token API si première utilisation\n" +
                    "3. Posez des questions comme :\n" +
                    "   • 'Comment saisir mon CRA ?'\n" +
                    "   • 'Où voir mes tâches en retard ?'\n" +
                    "   • 'Comment signaler un bug ?'\n\n" +
                    "L'IA vous guide pas à pas dans vos actions quotidiennes." }
            };
        }

        private void ChargerQuestionsBusinessAnalyst()
        {
            _questionsReponses = new Dictionary<string, string>
            {
                { "Comment créer une demande ?", 
                    "➕ CRÉATION DE DEMANDE\n\n" +
                    "1. Menu DEMANDES → Bouton ➕ Nouvelle demande\n" +
                    "2. Remplissez le formulaire :\n" +
                    "   • Titre explicite\n" +
                    "   • Description détaillée du besoin\n" +
                    "   • Criticité (Basse, Normale, Haute, Urgente)\n" +
                    "   • Projet ou service concerné\n" +
                    "   • Bénéfices attendus\n" +
                    "3. Ajoutez des pièces jointes si nécessaire\n" +
                    "4. Soumettez la demande\n\n" +
                    "Le chef de projet reçoit une notification et analyse la demande." },
                
                { "Comment suivre mes demandes ?",
                    "📊 SUIVI DES DEMANDES\n\n" +
                    "Menu DEMANDES affiche toutes vos demandes avec :\n\n" +
                    "• Statut actuel (Nouvelle, En analyse, Acceptée, En cours, Terminée)\n" +
                    "• Date de création et dernière mise à jour\n" +
                    "• Chef de projet assigné\n" +
                    "• Commentaires et historique\n\n" +
                    "Filtrez par statut ou projet pour retrouver rapidement vos demandes.\n" +
                    "🔔 Vous recevez des notifications à chaque changement d'état." },
                
                { "Comment prioriser les demandes ?",
                    "🎯 PRIORISATION\n\n" +
                    "Utilisez la criticité pour indiquer l'urgence :\n\n" +
                    "• URGENTE : Impact business immédiat, blocage utilisateurs\n" +
                    "• HAUTE : Important pour les opérations, deadline proche\n" +
                    "• NORMALE : Amélioration standard, pas de deadline stricte\n" +
                    "• BASSE : Nice to have, peut attendre\n\n" +
                    "Le chef de projet prend ces éléments en compte pour planifier." },
                
                { "Comment collaborer avec l'équipe ?",
                    "👥 COLLABORATION\n\n" +
                    "1. Ajoutez des commentaires détaillés sur vos demandes\n" +
                    "2. Répondez rapidement aux questions du chef de projet\n" +
                    "3. Participez aux réunions de refinement du backlog\n" +
                    "4. Validez les solutions proposées\n" +
                    "5. Testez les développements livrés\n\n" +
                    "💡 Plus vous êtes précis, plus vite votre demande sera traitée." },
                
                { "Comment consulter le backlog ?",
                    "📋 CONSULTATION DU BACKLOG\n\n" +
                    "Menu BACKLOG pour voir :\n\n" +
                    "• Toutes les tâches planifiées\n" +
                    "• Leur statut d'avancement\n" +
                    "• Les développeurs assignés\n" +
                    "• Les estimations et temps passé\n\n" +
                    "Utilisez les filtres pour voir uniquement :\n" +
                    "• Vos demandes transformées en tâches\n" +
                    "• Un projet spécifique\n" +
                    "• Un sprint donné" },
                
                { "Comment utiliser le chat IA ?",
                    "🤖 ASSISTANT IA\n\n" +
                    "L'IA peut vous aider à :\n\n" +
                    "• Rédiger des demandes claires et complètes\n" +
                    "• Comprendre le statut de vos demandes\n" +
                    "• Savoir comment suivre l'avancement\n" +
                    "• Naviguer dans l'application\n\n" +
                    "Cliquez sur 'Discuter avec l'IA' et configurez votre token API.\n" +
                    "Posez vos questions en langage naturel !" }
            };
        }

        private void ChargerQuestionsGenerales()
        {
            _questionsReponses = new Dictionary<string, string>
            {
                { "Comment naviguer dans l'application ?", 
                    "Utilisez le menu latéral gauche pour accéder aux différentes sections." }
            };
        }

        private void AfficherQuestions()
        {
            ListeQuestions.ItemsSource = _questionsReponses.Keys;
        }

        private void Question_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;
            
            // Le Content est maintenant un TextBlock
            if (button.Content is TextBlock textBlock)
            {
                var question = textBlock.Text;
                if (_questionsReponses.ContainsKey(question))
                {
                    AfficherReponseFormatee(_questionsReponses[question]);
                }
            }
        }

        private void AfficherReponseFormatee(string contenu)
        {
            var flowDoc = new FlowDocument();
            flowDoc.PagePadding = new Thickness(0);
            
            var lines = contenu.Split(new[] { "\n" }, StringSplitOptions.None);
            Paragraph currentParagraph = null;
            bool inList = false;
            
            foreach (var line in lines)
            {
                // Ligne vide = nouveau paragraphe
                if (string.IsNullOrWhiteSpace(line))
                {
                    if (currentParagraph != null && currentParagraph.Inlines.Count > 0)
                    {
                        flowDoc.Blocks.Add(currentParagraph);
                        currentParagraph = null;
                    }
                    inList = false;
                    continue;
                }
                
                var trimmedLine = line.Trim();
                
                // Titre principal (MAJUSCULES avec emojis au début)
                if (trimmedLine.Length > 3 && trimmedLine == trimmedLine.ToUpper() && 
                    !trimmedLine.StartsWith("•") && !char.IsDigit(trimmedLine[0]))
                {
                    if (currentParagraph != null && currentParagraph.Inlines.Count > 0)
                    {
                        flowDoc.Blocks.Add(currentParagraph);
                    }
                    
                    currentParagraph = new Paragraph 
                    { 
                        Margin = new Thickness(0, 8, 0, 16),
                        LineHeight = 28
                    };
                    
                    var titleRun = new Run(trimmedLine)
                    {
                        FontSize = 18,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00915A"))
                    };
                    
                    currentParagraph.Inlines.Add(titleRun);
                    flowDoc.Blocks.Add(currentParagraph);
                    currentParagraph = null;
                    inList = false;
                    continue;
                }
                
                // Sous-titre (contient : à la fin et commence par emoji/texte en MAJ)
                if (trimmedLine.EndsWith(":") && trimmedLine.Length > 5)
                {
                    if (currentParagraph != null && currentParagraph.Inlines.Count > 0)
                    {
                        flowDoc.Blocks.Add(currentParagraph);
                    }
                    
                    currentParagraph = new Paragraph 
                    { 
                        Margin = new Thickness(0, 12, 0, 8),
                        LineHeight = 24
                    };
                    
                    var subtitleRun = new Run(trimmedLine)
                    {
                        FontSize = 15,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C5F2D"))
                    };
                    
                    currentParagraph.Inlines.Add(subtitleRun);
                    flowDoc.Blocks.Add(currentParagraph);
                    currentParagraph = null;
                    inList = false;
                    continue;
                }
                
                // Points de liste avec •
                if (trimmedLine.StartsWith("•"))
                {
                    if (!inList || currentParagraph == null)
                    {
                        if (currentParagraph != null && currentParagraph.Inlines.Count > 0)
                        {
                            flowDoc.Blocks.Add(currentParagraph);
                        }
                        currentParagraph = new Paragraph 
                        { 
                            Margin = new Thickness(20, 0, 0, 8),
                            LineHeight = 22
                        };
                        inList = true;
                    }
                    
                    var listRun = new Run(trimmedLine)
                    {
                        FontSize = 14,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555555"))
                    };
                    
                    currentParagraph.Inlines.Add(listRun);
                    currentParagraph.Inlines.Add(new LineBreak());
                    continue;
                }
                
                // Lignes numérotées (1. 2. 3. etc)
                if (trimmedLine.Length > 2 && char.IsDigit(trimmedLine[0]) && trimmedLine[1] == '.')
                {
                    if (!inList || currentParagraph == null)
                    {
                        if (currentParagraph != null && currentParagraph.Inlines.Count > 0)
                        {
                            flowDoc.Blocks.Add(currentParagraph);
                        }
                        currentParagraph = new Paragraph 
                        { 
                            Margin = new Thickness(0, 0, 0, 8),
                            LineHeight = 22
                        };
                        inList = true;
                    }
                    
                    var numberRun = new Run(trimmedLine)
                    {
                        FontSize = 14,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"))
                    };
                    
                    currentParagraph.Inlines.Add(numberRun);
                    currentParagraph.Inlines.Add(new LineBreak());
                    continue;
                }
                
                // Lignes avec indentation (sous-points)
                if (line.StartsWith("   ") && trimmedLine.Length > 0)
                {
                    if (currentParagraph == null)
                    {
                        currentParagraph = new Paragraph 
                        { 
                            Margin = new Thickness(30, 0, 0, 4),
                            LineHeight = 20
                        };
                    }
                    
                    var indentRun = new Run(trimmedLine)
                    {
                        FontSize = 13,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666"))
                    };
                    
                    currentParagraph.Inlines.Add(indentRun);
                    currentParagraph.Inlines.Add(new LineBreak());
                    continue;
                }
                
                // Texte normal ou notes avec emojis
                if (currentParagraph == null)
                {
                    currentParagraph = new Paragraph 
                    { 
                        Margin = new Thickness(0, 0, 0, 8),
                        LineHeight = 22
                    };
                    inList = false;
                }
                
                // Détection des notes importantes (lignes commençant par des marqueurs spéciaux)
                bool isImportant = false;
                if (trimmedLine.Length > 0)
                {
                    // Détection via les premiers caractères (emojis ou codes mal encodés)
                    string firstChars = trimmedLine.Length >= 2 ? trimmedLine.Substring(0, 2) : trimmedLine;
                    isImportant = firstChars.Contains("💡") || firstChars.Contains("📌") || 
                                  firstChars.Contains("⚠") || firstChars.Contains("ð") || 
                                  firstChars.Contains("â");
                }
                
                var normalRun = new Run(trimmedLine)
                {
                    FontSize = 14,
                    Foreground = isImportant ? 
                        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF8C00")) :
                        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555555")),
                    FontStyle = isImportant ? FontStyles.Italic : FontStyles.Normal,
                    FontWeight = isImportant ? FontWeights.SemiBold : FontWeights.Normal
                };
                
                currentParagraph.Inlines.Add(normalRun);
                currentParagraph.Inlines.Add(new LineBreak());
            }
            
            // Ajouter le dernier paragraphe
            if (currentParagraph != null && currentParagraph.Inlines.Count > 0)
            {
                flowDoc.Blocks.Add(currentParagraph);
            }
            
            RtbReponse.Document = flowDoc;
        }
    }
}
