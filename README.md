# Backlog Manager - BNP Paribas

Application de gestion de backlog et de suivi de projet développée pour BNP Paribas. Permet la gestion des tâches, le suivi en Kanban, la planification des sprints et l'analyse des KPI.

## Authentification
- Connexion avec **code d'authentification BNP** (format: JXXXXX)
- L'application identifie automatiquement l'utilisateur et ses permissions

## Rôles et Permissions

### 👨‍💼 Administrateur
- Accès complet à toutes les fonctionnalités
- Gestion des utilisateurs et de l'équipe
- Gestion des projets
- Consultation des logs d'audit
- Accès à l'administration complète

### 📊 Chef de Projet (CP)
- Création et gestion de projets
- Priorisation des tâches
- Assignation des développeurs aux tâches
- Consultation des KPI et statistiques
- Suivi du planning et des sprints

### 🧑‍💻 Business Analyst (BA)
- Création de demandes et user stories
- Chiffrage de la complexité (story points)
- Consultation du backlog
- Suivi des tâches assignées

### 💻 Développeur
- Consultation du backlog
- Mise à jour du statut des tâches assignées
- Modification de l'avancement (pourcentage)
- Vue Kanban pour le suivi quotidien
- Saisie du temps passé

## Fonctionnalités principales

### 📋 Backlog
- Liste complète des tâches et demandes
- Filtres avancés (type, priorité, statut, développeur, projet)
- Recherche par titre
- Édition des détails d'une tâche (selon permissions)

### 📊 Kanban Board
- Vue en colonnes : À Faire → En Cours → En Test → Terminé
- Drag & drop pour changer le statut
- Alertes visuelles selon les délais (URGENT, ATTENTION, OK)
- Filtres par développeur et projet
- Cartes compactes affichant : titre, priorité, type, développeur, temps restant, progression

### 📁 Projets
- Création et gestion des projets
- Association des tâches aux projets
- Activation/désactivation des projets

### ⏱️ Timeline / Planning
- Vue Gantt du planning des tâches
- Visualisation des sprints
- Suivi des échéances

### 📈 Statistiques & KPI
- Vélocité de l'équipe
- Taux de complétion
- Répartition par priorité
- Analyse des délais
- Graphiques et métriques de performance

### 🔔 Notifications
- Alertes sur les tâches urgentes
- Rappels de deadlines
- Notifications des changements de statut

### 🧑‍💼 Gestion d'équipe
- Liste des membres de l'équipe
- Attribution des rôles
- Gestion des capacités (jours disponibles par sprint)
- Activation/désactivation des utilisateurs

### 📝 Audit
- Traçabilité complète des actions
- Logs avec : date, utilisateur, action, détails
- Filtres par date, utilisateur et type d'action
- Export des logs

## Types de demandes
- **User Story** : Fonctionnalité métier
- **Bug** : Correction d'anomalie
- **Amélioration** : Optimisation existante
- **Technique** : Dette technique, refactoring

## Niveaux de priorité
- **Urgente** (rouge)
- **Haute** (orange)
- **Moyenne** (jaune)
- **Basse** (vert)

## Workflow des tâches
1. **À Faire** : Tâche créée, en attente
2. **En Cours** : Développement en cours
3. **En Test** : En phase de validation
4. **Terminé** : Tâche complétée

## Chiffrage
- Utilisation des **Story Points** (complexité)
- Échelle : 1, 2, 3, 5, 8, 13, 21, 34
- Le chiffrage est réservé aux BA et CP

## Technologies
- **Framework** : WPF (.NET Framework 4.8)
- **Base de données** : SQLite
- **Architecture** : MVVM (Model-View-ViewModel)
- **Langage** : C# 8.0

## Branding
- Couleur principale : **BNP Green** (#00915A)
- Interface sombre avec accents verts
- Logo BNP Paribas en header

## Data Storage

All data is stored in SQLite database: `backlog.db`

The database is created automatically on first run.

## Building the Application

### Prerequisites

- .NET Framework 4.8 SDK
- MSBuild (comes with Visual Studio or .NET Framework SDK)

### Build Commands

Open PowerShell in the project directory and run:

```powershell
# Restore and build
msbuild BacklogManager.csproj /t:Restore
msbuild BacklogManager.csproj /p:Configuration=Release
```

Or for Debug build:

```powershell
msbuild BacklogManager.csproj /p:Configuration=Debug
```

### Run the Application

After building, run:

```powershell
.\bin\Release\BacklogManager.exe
```

Or for Debug:

```powershell
.\bin\Debug\BacklogManager.exe
```

## Project Structure

```
BacklogManager/
├── Domain/              # Domain models and enums
│   ├── BacklogItem.cs
│   ├── Dev.cs
│   ├── PokerSession.cs
│   ├── PokerVote.cs
│   └── Enums.cs
├── Services/            # Business logic and data access
│   ├── JsonDatabase.cs
│   ├── BacklogService.cs
│   └── PokerService.cs
├── ViewModels/          # MVVM ViewModels
│   ├── MainViewModel.cs
│   ├── BacklogViewModel.cs
│   ├── KanbanViewModel.cs
│   └── PokerViewModel.cs
├── Views/               # XAML Views
│   ├── BacklogView.xaml
│   ├── KanbanView.xaml
│   └── PokerView.xaml
├── Shared/              # Utilities
│   └── RelayCommand.cs
├── App.xaml
└── MainWindow.xaml
```

## Usage

### Backlog View
- Search and filter backlog items
- Create new tasks
- Edit task details (title, description, priority, status, etc.)
- Assign developers to tasks
- Set complexity values

### Kanban View
- Visual board with 4 columns: À faire, En cours, Test, Terminé
- Move items between columns using arrow buttons
- Changes are saved immediately

### Planning Poker
1. Select a backlog item
2. Start voting session
3. Each developer votes on complexity (1-5)
4. System detects vote gaps and prompts for second round if needed
5. Consensus is calculated and applied to the backlog item
6. Planning days = Consensus × 1.25

## Sample Data

The application initializes with sample developers and backlog items on first run:
- 3 sample developers (Alice, Bob, Charlie)
- 3 sample backlog items with different statuses

## Architecture

- **MVVM Pattern**: Clean separation of concerns
- **No Database**: JSON file-based storage using System.Text.Json
- **Thread-Safe**: Lock-based synchronization for data access
- **Auto-increment IDs**: Automatic ID generation for all entities
- **ObservableCollection**: Real-time UI updates

## Technologies

- .NET Framework 4.8
- WPF (Windows Presentation Foundation)
- System.Text.Json
- MVVM Architecture Pattern
