# Documentation Technique - BacklogManager
## Application de gestion de backlog avec système de permissions BNP Paribas

---

## 📋 Table des matières
1. [Vue d'ensemble](#vue-densemble)
2. [Architecture](#architecture)
3. [Fonctionnalités implémentées](#fonctionnalités-implémentées)
4. [Système de permissions](#système-de-permissions)
5. [Modules principaux](#modules-principaux)
6. [Base de données](#base-de-données)
7. [Guide d'utilisation](#guide-dutilisation)
8. [Axes d'amélioration](#axes-damélioration)

---

## 🎯 Vue d'ensemble

**BacklogManager** est une application WPF (.NET Framework 4.8) de gestion de backlog Scrum/Agile développée pour BNP Paribas. Elle permet de gérer des tâches, projets, équipes de développement avec un système de permissions granulaire basé sur les rôles.

### Technologies utilisées
- **Framework**: .NET Framework 4.8, C# 7.3
- **Interface**: WPF (Windows Presentation Foundation)
- **Architecture**: MVVM (Model-View-ViewModel)
- **Base de données**: SQLite (avec fallback JSON)
- **Branding**: Couleurs BNP Paribas (#00915A - Vert signature)

### Utilisateurs cibles
- **Administrateurs système**: Gestion complète (utilisateurs, rôles, projets)
- **Chefs de projet**: Pilotage des projets, priorisation, assignation
- **Développeurs**: Chiffrage, modification de leurs tâches
- **Business Analysts**: Création de demandes, consultation KPI

---

## 🏗️ Architecture

### Structure des dossiers
```
BacklogManager/
├── Domain/                    # Modèles métier
│   ├── BacklogItem.cs         # Tâche du backlog
│   ├── Utilisateur.cs         # Utilisateur système
│   ├── Role.cs                # Rôle avec 8 permissions
│   ├── Projet.cs              # Projet
│   ├── Dev.cs                 # Développeur (membre d'équipe)
│   ├── AuditLog.cs            # Log d'audit
│   ├── Demande.cs             # Demande utilisateur
│   └── Enums.cs               # Statut, Priorité, Complexité
│
├── Services/                  # Couche métier
│   ├── BacklogService.cs      # CRUD tâches/projets avec audit
│   ├── PermissionService.cs   # Gestion des permissions
│   ├── AuditLogService.cs     # Journalisation des actions
│   ├── NotificationService.cs # Système d'alertes
│   ├── AuthenticationService.cs # Authentification Windows
│   ├── SqliteDatabase.cs      # Implémentation SQLite
│   └── JsonDatabase.cs        # Implémentation JSON (fallback)
│
├── ViewModels/                # ViewModels MVVM
│   ├── BacklogViewModel.cs    # Vue backlog avec permissions
│   ├── KanbanViewModel.cs     # Vue Kanban drag & drop
│   ├── ProjetsViewModel.cs    # Gestion projets
│   ├── TimelineViewModel.cs   # Timeline Gantt
│   └── PokerViewModel.cs      # Planning Poker
│
├── Views/                     # Vues WPF
│   ├── MainWindow.xaml        # Fenêtre principale
│   ├── BacklogView.xaml       # Vue backlog
│   ├── KanbanView.xaml        # Tableau Kanban
│   ├── TimelineView.xaml      # Timeline Gantt
│   ├── AdministrationWindow.xaml # Admin (5 onglets)
│   ├── StatistiquesWindow.xaml   # KPI & Statistiques
│   ├── NotificationsWindow.xaml  # Centre de notifications
│   ├── AuditLogWindow.xaml       # Journal d'audit
│   ├── ParametresWindow.xaml     # Paramètres système
│   └── Pages/                    # Pages d'administration
│       ├── GestionUtilisateursPage.xaml
│       ├── GestionRolesPage.xaml
│       ├── GestionProjetsPage.xaml
│       └── GestionEquipePage.xaml
│
└── Converters/                # Convertisseurs XAML
    ├── BooleanToVisibilityConverter.cs
    └── TimelineBarMarginConverter.cs
```

### Pattern MVVM
- **Models (Domain)**: Entités métier pures sans logique UI
- **ViewModels**: Logique de présentation, commandes ICommand, ObservableCollections
- **Views**: XAML pur avec DataBinding, pas de code-behind (sauf événements drag & drop)

---

## ✨ Fonctionnalités implémentées

### 1. Gestion du Backlog
#### Vue Backlog (BacklogView.xaml)
- **DataGrid** avec tri/filtrage par projet, développeur, statut
- **Champs éditables** en ligne: Titre, Description, Priorité, Complexité, Dev assigné
- **Boutons contextuels** selon permissions (Nouvelle tâche, Enregistrer, Supprimer)
- **Double-clic** sur ligne pour ouvrir EditTacheWindow
- **Indicateurs visuels**: Couleurs par priorité, icônes de statut

#### Vue Kanban (KanbanView.xaml)
- **4 colonnes**: À faire, En cours, En test, Terminé
- **Drag & Drop** entre colonnes avec:
  - Effets visuels BNP (bordure verte #00915A, opacité)
  - Animation de succès (flash vert 200ms)
  - Sauvegarde automatique en base de données
  - Mise à jour temps réel des colonnes
- **Cartes de tâches** avec: Titre, Priorité (badge coloré), Dev assigné, Complexité

#### Vue Timeline (TimelineView.xaml)
- **Timeline Gantt** horizontale par développeur
- **Barres colorées** selon statut (Bleu: À faire, Orange: En cours, Vert: Terminé)
- **Alertes visuelles**: Bordure rouge si retard
- **Légendes**: Statuts, Priorités, Alertes avec codes couleur
- **Filtrage** par développeur et projet

### 2. Système de permissions

#### Rôles prédéfinis
| Rôle | Permissions |
|------|-------------|
| **Administrateur** | Toutes (8/8) |
| **Chef de Projet** | Créer demandes, Chiffrer, Prioriser, Voir KPI, Modifier/Supprimer tâches |
| **Développeur** | Créer demandes, Chiffrer, Modifier tâches (les siennes uniquement) |
| **Business Analyst** | Créer demandes, Voir KPI |

#### 8 Permissions définies (Role.cs)
1. `PeutCreerDemandes` - Créer des demandes utilisateur
2. `PeutChiffrer` - Participer au Planning Poker
3. `PeutPrioriser` - Modifier la priorité des tâches
4. `PeutGererUtilisateurs` - Accéder à l'administration (utilisateurs/rôles)
5. `PeutVoirKPI` - Consulter les statistiques et KPI
6. `PeutGererReferentiels` - Gérer projets et équipe
7. `PeutModifierTaches` - Modifier les tâches (contextuel: ses tâches ou toutes)
8. `PeutSupprimerTaches` - Supprimer des tâches

#### PermissionService.cs
Service centralisé qui encapsule toute la logique de permissions:
```csharp
// Méthodes contextuelles
bool PeutModifierTache(BacklogItem tache)  // Vérifie si tâche assignée à l'utilisateur
bool PeutSupprimerTache(BacklogItem tache) // Admin/Chef de projet ou tâche assignée
bool PeutChangerStatut(BacklogItem tache)  // Pour Kanban drag & drop

// Propriétés de rôle
bool IsAdmin
bool IsChefDeProjet
bool IsDeveloppeur
bool IsBusinessAnalyst
bool PeutAccederAdministration  // GererUtilisateurs || GererReferentiels
```

#### Application des permissions
- **Visibilité des boutons**: `Visibility="{Binding PeutCreerTachesVisibility}"`
- **Activation des commandes**: `CanExecute` des ICommand basé sur permissions
- **Champs en lecture seule**: `IsReadOnly="{Binding IsReadOnly}"`
- **Filtrage DataGrid**: Colonnes masquées si pas de permission

### 3. Administration

#### AdministrationWindow.xaml (5 onglets)
**Onglet 1 - 👥 Utilisateurs**
- Liste complète avec Nom, Prénom, Email, Rôle, UsernameWindows
- Boutons: Ajouter, Modifier, Supprimer (avec confirmation)
- EditUtilisateurWindow modale avec validation
- **Audit**: Création/Modification/Suppression journalisée

**Onglet 2 - 🎭 Rôles**
- 4 rôles prédéfinis non supprimables
- Édition des 8 permissions via checkboxes
- Sauvegarde immédiate avec capture avant/après
- **Audit**: Changements de permissions détaillés

**Onglet 3 - 📊 Projets**
- CRUD complet (Create, Read, Update, Delete)
- DataGrid avec Nom, Chef de projet, Dates, Statut
- Validation: Nom unique, dates cohérentes
- **Audit**: Actions sur projets journalisées

**Onglet 4 - 🧑‍💼 Équipe**
- Gestion des développeurs (Dev.cs)
- Champs: Nom, Prénom, Disponibilité (%), Taux journalier
- Édition en ligne dans DataGrid
- Suppression avec confirmation

**Onglet 5 - 📈 Statistiques**
- Tâches par statut (diagramme ASCII)
- Projets actifs
- Utilisateurs par rôle (Admin, CP, BA, Dev)
- Taux de complétion global

### 4. Audit Log

#### AuditLog.cs (Domain)
```csharp
public class AuditLog
{
    public int Id { get; set; }
    public string Action { get; set; }          // CREATE, UPDATE, DELETE, LOGIN, LOGOUT
    public int UserId { get; set; }
    public string Username { get; set; }
    public string EntityType { get; set; }      // BacklogItem, Utilisateur, Role, Projet
    public int? EntityId { get; set; }
    public string EntityName { get; set; }
    public string OldValue { get; set; }        // JSON avant modification
    public string NewValue { get; set; }        // JSON après modification
    public DateTime DateAction { get; set; }
}
```

#### AuditLogService.cs
Service centralisé pour la journalisation:
```csharp
void LogCreate(string entityType, int entityId, string entityName, string details)
void LogUpdate(string entityType, int entityId, string entityName, string oldValue, string newValue)
void LogDelete(string entityType, int entityId, string entityName)
void LogLogin(string username, bool success)
void LogLogout(string username)
```

#### AuditLogWindow.xaml
- **DataGrid** avec colonnes colorées selon action
- **Filtres**: Utilisateur, Dates (début/fin), Type d'action
- **Export CSV**: Bouton pour exporter les logs filtrés
- **Détails**: OldValue/NewValue affichés dans colonnes séparées

#### Points de journalisation
| Action | Hook | Détails capturés |
|--------|------|------------------|
| **Connexion/Déconnexion** | AuthenticationService.Login/Logout | Username, succès/échec |
| **Tâche créée** | BacklogService.SaveBacklogItem | Titre, Statut, Priorité, Dev assigné |
| **Tâche modifiée** | BacklogService.SaveBacklogItem | Avant/Après (JSON complet) |
| **Tâche supprimée** | BacklogView (bouton supprimer) | Titre, Statut final |
| **Statut changé (Kanban)** | KanbanViewModel.ChangerStatutTache | Ancien/Nouveau statut |
| **Utilisateur créé** | EditUtilisateurWindow | Nom, Prénom, Rôle |
| **Utilisateur modifié** | EditUtilisateurWindow | Changements (rôle, email, etc.) |
| **Utilisateur supprimé** | GestionUtilisateursPage | Nom complet |
| **Rôle modifié** | GestionRolesPage | 8 permissions avant/après |
| **Projet créé/modifié** | BacklogService.SaveProjet | Nom, Chef de projet, Dates |
| **Projet supprimé** | BacklogService.DeleteProjet | Nom, Chef de projet |

### 5. Notifications

#### NotificationService.cs
Service d'analyse automatique des tâches:
```csharp
// Analyse toutes les 5 minutes via timer
List<Notification> AnalyserTaches()

// Types de notifications
Urgent    - Tâche en retard (échéance passée)
Attention - Échéance < 2 jours
Info      - Tâche non assignée
Success   - Tâche terminée récemment
```

#### NotificationsWindow.xaml
- **Badge** dans MainWindow avec compteur (ex: 🔔 3)
- **Filtres**: Par type (Urgent/Attention/Info/Success), Par statut (Lues/Non lues)
- **Actions**: Marquer comme lu, Supprimer, Actualiser
- **Design BNP**: Couleurs cohérentes, icônes émoji, animations

#### Calcul automatique
- **Timer** dans MainWindow analyse toutes les 5 minutes
- **Badge** mis à jour en temps réel
- **Notifications** stockées en base (table Notifications)

### 6. Statistiques et KPI

#### StatistiquesWindow.xaml
Accessible via bouton "📊 KPI" (si PeutVoirKPI):

**Section 1 - Cartes KPI rapides**
- Total tâches
- Taux de complétion (%)
- Tâches en cours
- Projets actifs

**Section 2 - Graphiques (ASCII/texte)**
- Tâches par statut (barre horizontale)
- Charge par développeur (nombre de tâches assignées)

**Section 3 - Tableaux**
- Taux de complétion par projet
- Temps moyen par complexité (S/M/L/XL)

**Export HTML**
- Bouton "Exporter PDF" génère un fichier HTML avec:
  - Branding BNP Paribas (#00915A)
  - Toutes les statistiques formatées
  - CSS optimisé pour impression
  - Instructions: Ouvrir dans navigateur → Ctrl+P → Enregistrer en PDF

### 7. Paramètres système

#### ParametresWindow.xaml
4 sections principales:

**1. Base de données**
- Chemin actuel de la BDD SQLite
- Bouton "Modifier" avec avertissement de redémarrage

**2. Export / Import**
- Export complet JSON (toutes les tables)
- Export CSV du backlog uniquement
- Import JSON (placeholder avec avertissement)

**3. Sauvegarde**
- Affichage de la dernière sauvegarde
- Bouton "Sauvegarder maintenant" (copie .db avec timestamp)
- Bouton "Restaurer" avec sélection de fichier .db
- **Redémarrage automatique** après restauration

**4. Affichage** (placeholders pour versions futures)
- Thème (Clair/Sombre)
- Langue (Français/Anglais)

### 8. Planning Poker

#### PokerView.xaml
- **Sessions de chiffrage** collaboratif
- **Votes** des développeurs sur la complexité
- **Révélation** simultanée des votes
- **Historique** des sessions
- **Permissions**: Accessible uniquement si PeutChiffrer

### 9. Demandes utilisateur

#### DemandesView.xaml
- **Création** de nouvelles demandes (si PeutCreerDemandes)
- **Suivi** du statut (Nouvelle, En cours, Terminée)
- **Conversion** en tâches du backlog
- **Commentaires** et historique

---

## 🗄️ Base de données

### Structure SQLite

#### Table: BacklogItems
| Colonne | Type | Description |
|---------|------|-------------|
| Id | INTEGER PRIMARY KEY | Auto-increment |
| Titre | TEXT | Titre de la tâche |
| Description | TEXT | Description détaillée |
| Statut | INTEGER | 0=Afaire, 1=EnCours, 2=Test, 3=Termine |
| Priorite | INTEGER | 0=Basse, 1=Normale, 2=Haute, 3=Urgente |
| Complexite | TEXT | S, M, L, XL |
| ProjetId | INTEGER | FK vers Projets |
| AssignedDevId | INTEGER | FK vers Devs |
| DateCreation | TEXT | ISO 8601 |
| DateEcheance | TEXT | ISO 8601 |
| DateDerniereMaj | TEXT | ISO 8601 |
| EstimeJours | REAL | Estimation en jours |

#### Table: Utilisateurs
| Colonne | Type | Description |
|---------|------|-------------|
| Id | INTEGER PRIMARY KEY | |
| Nom | TEXT | |
| Prenom | TEXT | |
| Email | TEXT UNIQUE | |
| UsernameWindows | TEXT | Pour authentification Windows |
| RoleId | INTEGER | FK vers Roles |
| DateCreation | TEXT | |

#### Table: Roles
| Colonne | Type | Description |
|---------|------|-------------|
| Id | INTEGER PRIMARY KEY | 1=Admin, 2=Chef, 3=Dev, 4=BA |
| Nom | TEXT | |
| PeutCreerDemandes | INTEGER | 0/1 |
| PeutChiffrer | INTEGER | 0/1 |
| PeutPrioriser | INTEGER | 0/1 |
| PeutGererUtilisateurs | INTEGER | 0/1 |
| PeutVoirKPI | INTEGER | 0/1 |
| PeutGererReferentiels | INTEGER | 0/1 |
| PeutModifierTaches | INTEGER | 0/1 |
| PeutSupprimerTaches | INTEGER | 0/1 |

#### Table: AuditLogs
| Colonne | Type | Description |
|---------|------|-------------|
| Id | INTEGER PRIMARY KEY | |
| Action | TEXT | CREATE/UPDATE/DELETE/LOGIN/LOGOUT |
| UserId | INTEGER | FK vers Utilisateurs |
| Username | TEXT | Cache du nom |
| EntityType | TEXT | BacklogItem/Utilisateur/Role/Projet |
| EntityId | INTEGER | ID de l'entité modifiée |
| EntityName | TEXT | Cache du nom |
| OldValue | TEXT | JSON avant modification |
| NewValue | TEXT | JSON après modification |
| DateAction | TEXT | ISO 8601 |

#### Table: Projets
| Colonne | Type | Description |
|---------|------|-------------|
| Id | INTEGER PRIMARY KEY | |
| Nom | TEXT UNIQUE | |
| Description | TEXT | |
| ChefProjetId | INTEGER | FK vers Utilisateurs |
| DateDebut | TEXT | ISO 8601 |
| DateFin | TEXT | ISO 8601 |
| Statut | TEXT | Actif/Terminé/En pause |

#### Table: Devs
| Colonne | Type | Description |
|---------|------|-------------|
| Id | INTEGER PRIMARY KEY | |
| Nom | TEXT | |
| Prenom | TEXT | |
| Disponibilite | INTEGER | 0-100% |
| TauxJournalier | REAL | Euros/jour |

#### Table: Notifications
| Colonne | Type | Description |
|---------|------|-------------|
| Id | INTEGER PRIMARY KEY | |
| Type | TEXT | Urgent/Attention/Info/Success |
| Titre | TEXT | |
| Message | TEXT | |
| TacheId | INTEGER | FK vers BacklogItems |
| EstLue | INTEGER | 0/1 |
| DateCreation | TEXT | ISO 8601 |

### Migration automatique
`SqliteDatabase.MigrateDatabaseSchema()` exécutée au démarrage:
- Détection des colonnes manquantes
- Ajout de colonnes avec valeurs par défaut
- Mise à jour des rôles avec nouvelles permissions
- Pas de perte de données

---

## 📖 Guide d'utilisation

### Démarrage
1. **Lancer** `BacklogManager.exe`
2. **Authentification Windows** automatique (UsernameWindows)
3. **Rôle** chargé depuis la base de données
4. **Interface** adaptée selon permissions

### Gestion du backlog
1. **Créer une tâche**: Bouton "Nouvelle Tâche" (si permission)
2. **Modifier**: Éditer directement dans la DataGrid ou double-clic
3. **Assigner**: ComboBox "Dev assigné" (si permission)
4. **Prioriser**: ComboBox "Priorité" (si permission)
5. **Chiffrer**: ComboBox "Complexité" (si permission)
6. **Supprimer**: Sélectionner + bouton "Supprimer" (si permission)

### Vue Kanban
1. **Glisser** une carte de tâche
2. **Déposer** dans une autre colonne
3. **Animation** verte BNP confirme le succès
4. **Statut** sauvegardé automatiquement en base

### Administration
1. **Bouton "Admin"** visible si PeutGererUtilisateurs ou PeutGererReferentiels
2. **Onglet Utilisateurs**: CRUD utilisateurs, assignation de rôles
3. **Onglet Rôles**: Édition des 8 permissions par rôle
4. **Onglet Projets**: Gestion des projets
5. **Onglet Équipe**: Gestion des développeurs
6. **Onglet Statistiques**: Vue d'ensemble

### Audit
1. **Administration** → **Onglet Journal d'Audit**
2. **Filtrer** par utilisateur, date, action
3. **Consulter** OldValue/NewValue pour voir les changements
4. **Exporter** en CSV pour analyse externe

### Notifications
1. **Badge 🔔** dans MainWindow affiche le nombre
2. **Cliquer** pour ouvrir NotificationsWindow
3. **Filtrer** par type ou statut
4. **Marquer comme lu** ou supprimer

### Statistiques
1. **Bouton "📊 KPI"** (si PeutVoirKPI)
2. **Consulter** les cartes KPI, graphiques, tableaux
3. **Exporter PDF**: Générer HTML → Ouvrir navigateur → Ctrl+P

### Paramètres
1. **Bouton "⚙️ Paramètres"**
2. **Sauvegarder** la base de données (copie timestampée)
3. **Restaurer** depuis une sauvegarde (redémarrage auto)
4. **Exporter** données en JSON ou CSV

---

## 🚀 Axes d'amélioration

### Priorité HAUTE (Court terme)

#### 1. Performance et optimisation
- **Chargement paresseux** (Lazy loading) pour grandes bases
  - Implémenter pagination dans BacklogView (ex: 100 tâches par page)
  - Requêtes SQL avec LIMIT/OFFSET
  - Indicateur de chargement visuel
  
- **Cache en mémoire** pour données fréquemment consultées
  - Cache des projets/développeurs (rarement modifiés)
  - Invalidation intelligente du cache
  - Réduction des appels base de données

- **Requêtes asynchrones** (async/await)
  - LoadItems() asynchrone dans ViewModels
  - UI non bloquante pendant chargements
  - Progress bar pour longues opérations

#### 2. Notifications push temps réel
- **SignalR** ou WebSockets pour notifications multi-utilisateurs
  - Alerte quand tâche assignée
  - Notification de changements de statut
  - Synchronisation temps réel entre postes

#### 3. Gestion des pièces jointes
- **Ajout de fichiers** aux tâches
  - Table Attachments (FilePath, TacheId, DateUpload)
  - Stockage dans dossier dédié (ex: `Attachments/`)
  - Prévisualisation images dans EditTacheWindow
  - Limitation taille fichiers (ex: 10 MB max)

#### 4. Recherche avancée
- **Barre de recherche globale**
  - Recherche fulltext sur Titre + Description
  - Filtres combinés (Projet + Statut + Dev)
  - Historique de recherche
  - Recherche dans commentaires

### Priorité MOYENNE (Moyen terme)

#### 5. Graphiques interactifs (KPI)
- **Librairie de graphiques** (LiveCharts, OxyPlot)
  - Diagrammes en barres animés
  - Graphiques circulaires (répartition par statut)
  - Courbes d'évolution (burndown chart)
  - Export PNG/SVG

#### 6. Sprints et roadmap
- **Gestion des sprints Scrum**
  - Table Sprints (DateDebut, DateFin, Objectif)
  - Association BacklogItem → Sprint
  - Burndown chart par sprint
  - Vélocité de l'équipe

- **Roadmap visuelle**
  - Timeline multi-projets
  - Dépendances entre tâches
  - Jalons (milestones)

#### 7. Rapports automatisés
- **Génération de rapports planifiés**
  - Rapport hebdomadaire par email
  - Synthèse mensuelle pour management
  - Alertes automatiques (retards, blocages)
  - Templates personnalisables

#### 8. Gestion des commentaires enrichis
- **Commentaires avec formatage**
  - Markdown ou RTF
  - Mentions utilisateurs (@nom)
  - Pièces jointes dans commentaires
  - Historique de modifications

#### 9. API REST
- **Exposition API** pour intégrations externes
  - Endpoints: /api/backlog, /api/projets, /api/utilisateurs
  - Authentification JWT
  - Webhooks pour événements
  - Documentation Swagger

### Priorité BASSE (Long terme)

#### 10. Mode hors ligne
- **Synchronisation** avec serveur central
  - Queue de modifications locales
  - Résolution de conflits
  - Indicateur de statut réseau

#### 11. Application mobile
- **Xamarin** ou **MAUI** pour iOS/Android
  - Consultation backlog en mobilité
  - Changement de statut
  - Notifications push natives
  - Synchronisation avec version desktop

#### 12. Intelligence artificielle
- **Suggestions automatiques**
  - Estimation de complexité basée sur historique
  - Détection de tâches similaires
  - Prédiction de délais
  - Analyse de sentiment dans commentaires

#### 13. Intégration Git
- **Lien avec commits Git**
  - Références tâches dans messages de commit (ex: #TASK-123)
  - Affichage des commits liés dans EditTacheWindow
  - Statut auto-update si commit détecté

#### 14. Thèmes et personnalisation
- **Thème sombre** complet
  - ResourceDictionary séparé
  - Switch dynamique sans redémarrage
  - Préservation préférence utilisateur

- **Personnalisation colonnes**
  - Drag & drop colonnes DataGrid
  - Largeurs sauvegardées par utilisateur
  - Colonnes masquables

#### 15. Internationalisation (i18n)
- **Multi-langues**
  - Fichiers de ressources (.resx)
  - Français, Anglais, Espagnol
  - Détection locale Windows
  - Switch runtime

#### 16. Accessibilité
- **Support WCAG 2.1**
  - Navigation clavier complète
  - Lecteurs d'écran (JAWS, NVDA)
  - Contraste élevé
  - Tailles de police ajustables

#### 17. Tests automatisés
- **Tests unitaires** (xUnit, NUnit)
  - Couverture Services (> 80%)
  - Mocks pour base de données
  - Tests de permissions

- **Tests d'intégration**
  - Scénarios complets (Création → Modification → Suppression)
  - Tests de migration base de données

- **Tests UI** (Appium, WinAppDriver)
  - Tests de navigation
  - Tests de permissions UI

---

## 🐛 Bugs connus et limitations

### Bugs mineurs
1. **Kanban**: Lors de drag trop rapide, la carte peut devenir invisible (rare)
   - **Workaround**: Cliquer sur une autre carte puis revenir
   
2. **Timeline**: Barres se chevauchent si beaucoup de tâches par dev
   - **Solution envisagée**: Scroll vertical par développeur

3. **Notifications**: Timer peut créer plusieurs notifications en double si base lente
   - **Solution envisagée**: Lock sur AnalyserTaches()

### Limitations actuelles
1. **Base de données**: Pas de gestion de transactions complexes
   - SQLite limite les écritures concurrentes
   
2. **Permissions**: Pas de permissions personnalisées par utilisateur
   - Uniquement basé sur rôles
   
3. **Backup**: Pas de sauvegarde automatique planifiée
   - Utilisateur doit le faire manuellement
   
4. **Export PDF**: Nécessite navigateur pour conversion HTML → PDF
   - Pas de génération PDF native

5. **Multi-utilisateurs**: Pas de synchronisation temps réel
   - Chaque poste a sa propre base locale

---

## 📞 Support et maintenance

### Logs d'erreur
- **Emplacement**: `C:\Users\[User]\AppData\Local\BacklogManager\Logs\`
- **Format**: `error_YYYYMMDD.log`
- **Contenu**: StackTrace, Message, Timestamp

### Base de données
- **Emplacement par défaut**: `C:\Users\[User]\AppData\Local\BacklogManager\backlog.db`
- **Sauvegardes**: Dossier `Backups/` au même emplacement
- **Format**: `backlog_backup_YYYYMMDD_HHMMSS.db`

### Réinitialisation
Pour réinitialiser complètement l'application:
1. Fermer BacklogManager
2. Supprimer `%LOCALAPPDATA%\BacklogManager\`
3. Relancer l'application → Nouvelle base créée

### Contact support
- **Email**: support.backlogmanager@bnpparibas.com (fictif)
- **Intranet**: https://intranet.bnpparibas.com/tools/backlogmanager
- **Wiki**: Documentation complète et FAQ

---

## 📜 Historique des versions

### Version 1.0.0 (Actuelle - Novembre 2025)
- ✅ Gestion complète du backlog (CRUD)
- ✅ Système de permissions à 8 niveaux
- ✅ Vue Kanban avec drag & drop
- ✅ Timeline Gantt
- ✅ Administration (Utilisateurs, Rôles, Projets, Équipe)
- ✅ Audit Log complet
- ✅ Notifications automatiques
- ✅ Statistiques et KPI
- ✅ Export HTML (PDF via impression)
- ✅ Paramètres système (Backup/Restore)
- ✅ Planning Poker
- ✅ Gestion des demandes

### Prochaines versions (Roadmap)
- **v1.1.0**: Graphiques interactifs (LiveCharts)
- **v1.2.0**: Gestion des sprints et burndown chart
- **v1.3.0**: API REST et webhooks
- **v2.0.0**: Mode multi-utilisateurs avec serveur central

---

## 🏆 Crédits et remerciements

**Développement**: Équipe BacklogManager BNP Paribas  
**Framework**: Microsoft WPF, .NET Foundation  
**Base de données**: SQLite (Public Domain)  
**Branding**: BNP Paribas (#00915A - Vert signature)  

**Architecture inspirée par**:
- Clean Architecture (Robert C. Martin)
- MVVM Pattern (Microsoft)
- SOLID Principles

---

**Document mis à jour le**: 16 novembre 2025  
**Version document**: 1.0  
**Auteur**: Équipe BacklogManager
