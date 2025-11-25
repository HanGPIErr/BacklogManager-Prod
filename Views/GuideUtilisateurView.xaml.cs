using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
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

        public GuideUtilisateurView(AuthenticationService authService, IDatabase database, MainWindow mainWindow)
        {
            InitializeComponent();
            _authService = authService;
            _database = database;
            _mainWindow = mainWindow;
            _userRole = _authService.GetCurrentUserRole();
            
            ChargerQuestionsSelonRole();
        }

        private void BtnChatIA_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.AfficherChatIA();
        }

        private void ChargerQuestionsSelonRole()
        {
            if (_userRole == null) return;

            TxtRole.Text = $"Guide {_userRole.Nom}";
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
                { "Comment gérer les utilisateurs ?", 
                    "📋 GESTION DES UTILISATEURS\n\n" +
                    "1. Accédez au menu ADMINISTRATION → Gérer l'équipe\n" +
                    "2. Vous verrez la liste complète des utilisateurs\n" +
                    "3. Double-cliquez sur un utilisateur pour modifier son profil\n" +
                    "4. Modifiez le rôle (Admin, Chef de Projet, Développeur, Business Analyst)\n" +
                    "5. Définissez les permissions d'accès aux modules\n" +
                    "6. Validez les changements avec le bouton Enregistrer\n\n" +
                    "💡 Les rôles déterminent les droits d'accès et les fonctionnalités disponibles." },
                
                { "Comment utiliser le Dashboard ?",
                    "📊 TABLEAU DE BORD\n\n" +
                    "Le Dashboard vous donne une vue d'ensemble complète :\n\n" +
                    "• KPIs en temps réel (tâches, vélocité, charge)\n" +
                    "• Graphiques de progression des projets\n" +
                    "• Liste des tâches urgentes et en retard\n" +
                    "• Notifications importantes\n" +
                    "• Activité récente de l'équipe\n\n" +
                    "Utilisez les filtres pour affiner l'affichage par période ou projet." },
                
                { "Comment valider les CRA ?",
                    "✅ VALIDATION DES CRA\n\n" +
                    "1. Menu ADMINISTRATION → Suivi CRA\n" +
                    "2. Sélectionnez le mois et l'utilisateur\n" +
                    "3. Vérifiez les temps saisis pour chaque tâche\n" +
                    "4. Validez ou rejetez avec commentaire si nécessaire\n" +
                    "5. Le statut est mis à jour automatiquement\n\n" +
                    "📌 Les CRA validés sont verrouillés et ne peuvent plus être modifiés." },
                
                { "Comment configurer les projets ?",
                    "⚙️ CONFIGURATION DES PROJETS\n\n" +
                    "1. Menu ADMINISTRATION → Projets\n" +
                    "2. Créez un nouveau projet avec le bouton ➕\n" +
                    "3. Définissez : nom, code, client, budget, dates\n" +
                    "4. Assignez un chef de projet\n" +
                    "5. Ajoutez les membres de l'équipe\n" +
                    "6. Configurez les sprints et jalons\n\n" +
                    "Les projets structurent votre backlog et votre suivi de temps." },
                
                { "Comment gérer les permissions ?",
                    "🔐 GESTION DES PERMISSIONS\n\n" +
                    "Permissions par rôle :\n\n" +
                    "• Administrateur : accès total, gestion utilisateurs et configuration\n" +
                    "• Chef de Projet : gestion backlog, affectation tâches, validation CRA\n" +
                    "• Développeur : consultation tâches, saisie CRA, commentaires\n" +
                    "• Business Analyst : création demandes, consultation backlog\n\n" +
                    "Les permissions sont automatiques selon le rôle attribué." },
                
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
                    "   • Priorité (Basse, Normale, Haute, Critique)\n" +
                    "   • Charge estimée en heures\n" +
                    "   • Projet associé\n" +
                    "4. Cliquez sur Enregistrer\n\n" +
                    "La tâche apparaît immédiatement dans le backlog et le Kanban." },
                
                { "Comment utiliser le Kanban ?",
                    "📋 TABLEAU KANBAN\n\n" +
                    "Menu KANBAN pour visualiser votre workflow :\n\n" +
                    "• Colonnes : À faire, En cours, En test, Terminé\n" +
                    "• Glissez-déposez les cartes entre colonnes\n" +
                    "• Filtrez par projet, sprint ou développeur\n" +
                    "• Double-cliquez sur une carte pour voir les détails\n" +
                    "• Les couleurs indiquent la priorité\n\n" +
                    "💡 Le Kanban se met à jour en temps réel pour toute l'équipe." },
                
                { "Comment planifier un sprint ?",
                    "🎯 PLANIFICATION DE SPRINT\n\n" +
                    "1. Menu SPRINTS → Nouveau sprint\n" +
                    "2. Définissez les dates de début et fin\n" +
                    "3. Fixez l'objectif du sprint\n" +
                    "4. Sélectionnez les tâches du backlog à inclure\n" +
                    "5. Vérifiez la charge totale vs capacité de l'équipe\n" +
                    "6. Validez le sprint\n\n" +
                    "📊 Le burndown chart suit automatiquement l'avancement." },
                
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
                    "• Burndown Chart : Avancement sprint vs idéal\n" +
                    "• Kanban : État temps réel des tâches\n" +
                    "• Rapports CRA : Temps réel passé vs estimé\n" +
                    "• Vélocité : Points story terminés par sprint\n\n" +
                    "Exportez les rapports en PDF pour vos reportings." },
                
                { "Comment gérer les demandes ?",
                    "📝 GESTION DES DEMANDES\n\n" +
                    "1. Menu DEMANDES pour voir toutes les demandes\n" +
                    "2. Triez par criticité ou date\n" +
                    "3. Ouvrez une demande pour l'analyser\n" +
                    "4. Transformez-la en tâches du backlog\n" +
                    "5. Assignez aux développeurs\n" +
                    "6. Mettez à jour le statut\n\n" +
                    "Les Business Analysts sont notifiés des changements d'état." }
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
                    "2. Modifiez le statut (À faire → En cours → En test → Terminé)\n" +
                    "3. Ajoutez des commentaires sur votre progression\n" +
                    "4. Mettez à jour le temps restant si nécessaire\n" +
                    "5. Sauvegardez les modifications\n\n" +
                    "🔔 Le chef de projet est notifié des changements importants." },
                
                { "Comment signaler un blocage ?",
                    "🚨 SIGNALEMENT DE BLOCAGE\n\n" +
                    "1. Ouvrez la tâche bloquée\n" +
                    "2. Changez le statut en 'Bloqué'\n" +
                    "3. Ajoutez un commentaire détaillé expliquant :\n" +
                    "   • Nature du blocage\n" +
                    "   • Impact sur le planning\n" +
                    "   • Solution envisagée\n" +
                    "4. Sauvegardez\n\n" +
                    "Le chef de projet reçoit une notification immédiate." },
                
                { "Comment consulter mes CRA passés ?",
                    "📊 HISTORIQUE CRA\n\n" +
                    "1. Menu SAISIR CRA\n" +
                    "2. Utilisez les flèches pour naviguer entre les mois\n" +
                    "3. Les jours avec temps saisi sont en vert\n" +
                    "4. Les jours validés sont verrouillés\n" +
                    "5. Cliquez sur un jour pour voir le détail\n\n" +
                    "💡 Exportez vos CRA en PDF pour vos archives personnelles." },
                
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
                    TxtReponse.Text = _questionsReponses[question];
                }
            }
        }
    }
}
