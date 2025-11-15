# BacklogManager - WPF Application

A complete WPF application for managing backlogs with MVVM architecture, built with .NET Framework 4.8.

## Features

- **Backlog Management**: Create, edit, and search backlog items with filtering capabilities
- **Kanban Board**: Visual workflow management with drag-and-drop between status columns
- **Planning Poker**: Team-based complexity estimation with consensus voting
- **JSON Storage**: All data stored in a single JSON file (no database required)

## Data Storage

All data is stored in: `C:\Users\HanGP\BacklogManager\backlog-db.json`

The file is created automatically on first run.

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
