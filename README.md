# Backlog Manager - BNP Paribas

Application de gestion de backlog et de suivi de projet développée pour BNP Paribas. Permet la gestion des tâches, le suivi en Kanban, la planification des sprints, le compte-rendu d'activité (CRA) et l'analyse des KPI.

## 🔐 Authentification
- Connexion avec **code d'authentification BNP** (format: JXXXXX)
- L'application identifie automatiquement l'utilisateur et ses permissions
- Pas de mot de passe requis (simulation d'authentification Windows)

## 👥 Rôles et Permissions

### 👨‍💼 Administrateur
- ✅ Accès complet à toutes les fonctionnalités
- ✅ Gestion des utilisateurs et de l'équipe
- ✅ Gestion des projets et référentiels
- ✅ Consultation des logs d'audit
- ✅ Accès aux paramètres système (sauvegarde, export/import)
- ✅ Création de tâches normales et spéciales (congés/support)

### 📊 Chef de Projet (CP)
- ✅ Création et gestion de projets
- ✅ Priorisation des tâches
- ✅ Assignation des développeurs aux tâches
- ✅ Consultation des KPI et statistiques
- ✅ Suivi du planning et des sprints
- ✅ Création de tâches normales et spéciales
- ❌ Pas d'accès aux paramètres système

### 🧑‍💻 Business Analyst (BA)
- ✅ Création de demandes et user stories
- ✅ Création de tâches normales
- ✅ Consultation du backlog et des KPI
- ✅ Suivi des tâches
- ✅ Création de congés/support
- ❌ Pas de priorisation
- ❌ Pas de gestion d'équipe

### 💻 Développeur
- ✅ Consultation du backlog
- ✅ Mise à jour du statut des tâches assignées
- ✅ Saisie du CRA (temps passé)
- ✅ Création de congés et support uniquement
- ✅ Vue Kanban pour le suivi quotidien
- ❌ Pas de création de tâches normales
- ❌ Pas de priorisation
- ❌ Pas d'accès administration

## ✨ Fonctionnalités principales

### 🏠 Dashboard
- Vue d'ensemble avec indicateurs clés
- **Activités récentes** : affichage dynamique des dernières actions (création/modification de tâches, congés, support, temps saisi)
- Activités cliquables : navigation vers la tâche concernée (Backlog ou Archives)
- Tâches urgentes avec échéances
- Notifications importantes
- Actions rapides (nouvelle tâche, Kanban, Timeline)
- Guide utilisateur intégré

### 📋 Backlog
- Liste complète des tâches et demandes
- Filtres avancés (type, priorité, statut, développeur, projet)
- Recherche par titre
- 3 vues : Tâches, Projets, Archives
- **Permissions adaptées** :
  - Développeurs : voient uniquement "➕ Congés/Support"
  - Admin/BA/CP : voient "➕ Nouvelle Tâche" + "➕ Congés/Support"
- Édition des détails d'une tâche (selon permissions)

### 📊 Kanban Board
- Vue en colonnes : À Faire → En Attente → À Prioriser → En Cours → Test → Terminé
- Drag & drop pour changer le statut
- Alertes visuelles selon les délais (URGENT, ATTENTION, OK)
- Filtres par développeur et projet
- Cartes compactes : titre, priorité, type, développeur, temps restant, progression

### 📁 Projets
- Création et gestion des projets
- Association des tâches aux projets
- Activation/désactivation des projets
- Suivi de l'avancement par projet

### ⏱️ Timeline / Planning
- Vue Gantt du planning des tâches
- Visualisation des sprints
- Suivi des échéances
- Planning des congés et disponibilités

### 📝 CRA (Compte-Rendu d'Activité)
- **Vue Calendrier** : saisie mensuelle du temps passé
- **Vue Historique** : consultation des CRA passés avec filtres
- Saisie en jours (1j = 8h)
- Types d'activité : Run, Dev, Autre, Congés, Non Travaillé, Support
- Calcul automatique des jours fériés français
- Validation et corrections des saisies
- Export des données

### 📈 Statistiques & KPI
- Vélocité de l'équipe
- Taux de complétion
- Répartition par priorité
- Analyse des délais
- Graphiques et métriques de performance
- Temps passé vs estimé

### 🔔 Notifications
- Alertes sur les tâches urgentes
- Rappels de deadlines
- Notifications des changements de statut
- Centre de notifications centralisé

### 🧑‍💼 Gestion d'équipe (Admin uniquement)
- Liste des membres de l'équipe
- Attribution des rôles
- Gestion des capacités (jours disponibles par sprint)
- Activation/désactivation des utilisateurs
- Modification des informations utilisateur

### 🔍 Audit (Admin uniquement)
- Traçabilité complète des actions
- Logs avec : date, utilisateur, action, type d'entité, détails
- Filtres par date, utilisateur et type d'action
- Export des logs
- Intégration dans le Dashboard (activités récentes)

### ⚙️ Paramètres (Admin uniquement)
- **Sauvegarde automatique** : 
  - Activation/désactivation par checkbox
  - Intervalle configurable (5-120+ minutes)
  - Affichage de la prochaine sauvegarde
  - Nettoyage automatique (garde les 10 dernières)
  - Fichiers : `backup_auto_YYYYMMDD_HHMMSS.db`
- **Sauvegarde manuelle** :
  - Bouton de création manuelle
  - Fichiers : `backup_manual_YYYYMMDD_HHMMSS.db`
  - Affichage de la dernière sauvegarde
- **Export de données** :
  - Export SQLite (.db) : copie complète de la base
  - Export JSON : données structurées (BacklogItems, Projets, Utilisateurs)
  - Export Complet : ZIP contenant SQLite + JSON + README
  - Export CSV : backlog uniquement (compatibilité)
- **Import de données** :
  - Import SQLite : remplacement de la base (avec backup automatique)
  - Import JSON : préparé pour import futur
- Affichage du chemin de la base de données
- Gestion des thèmes (préparé pour futur)

## 📦 Types de tâches

### Tâches normales (Admin/BA/CP uniquement)
- **User Story** : Fonctionnalité métier
- **Bug** : Correction d'anomalie
- **Amélioration** : Optimisation existante
- **Technique** : Dette technique, refactoring
- **Run** : Tâche de production/maintenance

### Tâches spéciales (Tous les utilisateurs)
- **Congés** : Vacances, RTT, congés payés
- **Non Travaillé** : Absences diverses
- **Support** : Aide à un collègue développeur

## 🎯 Niveaux de priorité
- **🔴 Urgente** : Traitement immédiat requis
- **🟠 Haute** : Important, à traiter rapidement
- **🟡 Moyenne** : Priorité standard
- **🟢 Basse** : Peut attendre

## 🔄 Workflow des tâches
1. **À Faire** : Tâche créée, prête à être démarrée
2. **En Attente** : Bloquée, en attente de dépendances
3. **À Prioriser** : Nécessite une décision de priorité
4. **En Cours** : Développement en cours
5. **Test** : En phase de validation/tests
6. **Terminé** : Tâche complétée et validée

## 💾 Stockage des données

**Base de données** : SQLite (`backlog.db`)
- Localisation : `bin/Debug/data/backlog.db` ou `bin/Release/data/backlog.db`
- Création automatique au premier lancement
- **Sauvegardes automatiques** (si activées dans Paramètres)
- **Sauvegardes manuelles** disponibles
- Dossier des backups : `Backups/` (même répertoire que l'exécutable)

**Tables principales** :
- BacklogItems (tâches)
- Projets
- Utilisateurs
- Roles
- CRA (compte-rendu d'activité)
- Sprints
- AuditLogs (traçabilité)
- Demandes

## 🛠️ Technologies

- **Framework** : WPF (.NET Framework 4.8)
- **Base de données** : SQLite (System.Data.SQLite)
- **Architecture** : MVVM (Model-View-ViewModel)
- **Langage** : C# 8.0
- **Sérialisation** : System.Text.Json
- **Compression** : System.IO.Compression (export ZIP)

## 🎨 Branding
- **Couleur principale** : BNP Green (#00915A)
- Interface claire avec accents verts
- Design moderne et épuré
- Logo BNP Paribas en header
- Expérience utilisateur optimisée

## 🚀 Compilation et Exécution

### Prérequis

- .NET Framework 4.8 SDK
- MSBuild (fourni avec Visual Studio ou .NET Framework SDK)
- Windows 7 ou supérieur

### Commandes de compilation

Ouvrir PowerShell dans le répertoire du projet :

```powershell
# Compilation en mode Release
& "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe" "BacklogManager.sln" /t:Rebuild /p:Configuration=Release /v:minimal

# Ou en mode Debug
& "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe" "BacklogManager.sln" /t:Rebuild /p:Configuration=Debug /v:minimal
```

### Lancement de l'application

```powershell
# Mode Release
.\bin\Release\BacklogManager.exe

# Mode Debug
.\bin\Debug\BacklogManager.exe
```

### Premier lancement

Au premier démarrage, l'application :
1. Crée automatiquement la base de données SQLite
2. Initialise les 4 rôles par défaut
3. Crée 9 utilisateurs de test (voir `UTILISATEURS_TEST.txt`)
4. Prépare le dossier `Backups/` pour les sauvegardes

## 📂 Structure du projet

```
BacklogManager/
├── Domain/              # Modèles de domaine
│   ├── BacklogItem.cs   # Tâche
│   ├── Projet.cs        # Projet
│   ├── Utilisateur.cs   # Utilisateur
│   ├── Role.cs          # Rôle et permissions
│   ├── CRA.cs           # Compte-rendu d'activité
│   ├── Sprint.cs        # Sprint
│   ├── AuditLog.cs      # Log d'audit
│   ├── Demande.cs       # Demande métier
│   └── Enums.cs         # Énumérations
├── Services/            # Logique métier et accès données
│   ├── IDatabase.cs     # Interface base de données
│   ├── SqliteDatabase.cs    # Implémentation SQLite
│   ├── JsonDatabase.cs      # Implémentation JSON (legacy)
│   ├── BacklogService.cs    # Service principal
│   ├── CRAService.cs        # Service CRA
│   ├── AuthenticationService.cs  # Authentification
│   ├── PermissionService.cs      # Gestion permissions
│   ├── AuditLogService.cs        # Traçabilité
│   ├── NotificationService.cs    # Notifications
│   ├── JoursFeriesService.cs     # Jours fériés français
│   └── InitializationService.cs  # Initialisation données
├── ViewModels/          # MVVM ViewModels
│   ├── MainViewModel.cs
│   ├── BacklogViewModel.cs
│   ├── KanbanViewModel.cs
│   ├── CRAViewModel.cs
│   ├── CRACalendrierViewModel.cs
│   ├── CRAHistoriqueViewModel.cs
│   └── ArchivesViewModel.cs
├── Views/               # Vues XAML
│   ├── DashboardView.xaml      # Dashboard
│   ├── BacklogView.xaml        # Backlog
│   ├── KanbanView.xaml         # Kanban
│   ├── CRAView.xaml            # CRA
│   ├── TimelineView.xaml       # Planning
│   ├── AdminView.xaml          # Administration
│   ├── ParametresWindow.xaml   # Paramètres système
│   └── GuideUtilisateurWindow.xaml  # Guide
├── Converters/          # Convertisseurs WPF
├── Shared/              # Utilitaires
│   ├── RelayCommand.cs
│   └── BooleanToVisibilityConverter.cs
├── Images/              # Ressources graphiques
├── App.xaml
├── MainWindow.xaml      # Fenêtre principale
└── README.md
```

## 📖 Utilisation

### Connexion
1. Lancer l'application
2. Entrer un code utilisateur (ex: J04831 pour un développeur, J00001 pour admin)
3. Cliquer sur "Se connecter"

### Dashboard
- Vue d'ensemble de votre activité
- Cliquez sur une activité récente pour naviguer vers la tâche
- Accédez rapidement aux fonctionnalités via les boutons d'actions

### Backlog
- Créez des tâches avec "➕ Nouvelle Tâche" (si autorisé)
- Créez des congés/support avec "➕ Congés/Support" (tous les utilisateurs)
- Utilisez les filtres pour affiner la vue
- Double-cliquez sur une tâche pour l'éditer

### Kanban
- Glissez-déposez les cartes entre les colonnes
- Filtrez par développeur ou projet
- Les changements sont sauvegardés automatiquement

### CRA (Compte-Rendu d'Activité)
- **Onglet Calendrier** : saisissez votre temps par jour
- **Onglet Historique** : consultez vos saisies passées
- Les jours fériés sont détectés automatiquement
- 1 jour = 8 heures

### Paramètres (Admin uniquement)
- Activez la sauvegarde automatique
- Configurez l'intervalle (minutes)
- Exportez vos données (SQLite, JSON, Complet)
- Importez une base de données de backup
