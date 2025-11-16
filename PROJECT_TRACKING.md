# Plan d'implémentation des permissions et administration

## 1. Permissions définies dans Role.cs
✅ PeutCreerDemandes
✅ PeutChiffrer
✅ PeutPrioriser
✅ PeutGererUtilisateurs
✅ PeutVoirKPI
✅ PeutGererReferentiels
✅ PeutModifierTaches (ajouté)
✅ PeutSupprimerTaches (ajouté)

## 2. Service PermissionService
✅ Services/PermissionService.cs créé
✅ Centralise toute la logique de permissions
✅ Méthodes contextuelles (PeutModifierTache, PeutSupprimerTache, PeutChangerStatut)
✅ Propriétés IsAdmin, IsChefDeProjet, IsDeveloppeur, IsBusinessAnalyst
✅ PeutAccederAdministration combiné (GererUtilisateurs || GererReferentiels)

## 3. Intégration dans MainWindow
✅ PermissionService initialisé avec currentUser et currentRole
✅ Visibilité des boutons contrôlée:
   - BtnAdmin: PeutAccederAdministration
   - BtnGererEquipe: PeutGererEquipe
   - BtnDemandes: PeutCreerDemandes
✅ Gestionnaire d'exceptions globales ajouté

## 4. Fenêtre d'administration unifiée
✅ **AdministrationWindow.xaml créée** avec TabControl à 5 onglets:
   1. 👥 Utilisateurs - GestionUtilisateursPage
   2. 🎭 Rôles - GestionRolesPage (création/modification permissions)
   3. 📊 Projets - GestionProjetsPage
   4. 🧑‍💼 Équipe - GestionEquipePage
   5. 📈 Statistiques - Vue d'ensemble (tâches, projets, utilisateurs par rôle)

✅ **Pages créées** dans Views/Pages/:
   - GestionUtilisateursPage.xaml + .xaml.cs
   - GestionRolesPage.xaml + .xaml.cs (édition des 8 permissions par rôle)
   - GestionProjetsPage.xaml + .xaml.cs
   - GestionEquipePage.xaml + .xaml.cs

✅ **Statistiques améliorées**:
   - Nombre de tâches par statut (À faire, En cours, Terminées)
   - Nombre de projets actifs
   - Nombre d'utilisateurs actifs
   - Nombre de développeurs
   - **Détail par rôle** (Admin, Chef de Projet, Business Analyst, Développeur)
   - Progression moyenne des tâches

## 5. Database et migration
✅ Colonnes PeutModifierTaches et PeutSupprimerTaches ajoutées à la table Roles
✅ Méthode MigrateDatabaseSchema() pour migration automatique
✅ UpdateRole() implémenté dans IDatabase, SqliteDatabase et JsonDatabase
✅ GetRoles() corrigé avec SELECT explicite (pas de SELECT *)
✅ Valeurs par défaut selon les rôles:
   - Admin et ChefDeProjet: PeutModifierTaches=1, PeutSupprimerTaches=1
   - Développeur: PeutModifierTaches=1, PeutSupprimerTaches=0
   - BusinessAnalyst: PeutModifierTaches=0, PeutSupprimerTaches=0

## 6. Corrections de bugs
✅ GestionProjetsWindow: Id=0 pour auto-increment (pas de calcul manuel)
✅ Erreur SQL "Index out of bounds" corrigée (ordre des colonnes)
✅ Base de données recréée avec structure complète

## 7. Tests de permissions par rôle

### Administrateur (J00001):
✅ Accès complet à l'administration
✅ Peut modifier toutes les tâches
✅ Peut supprimer toutes les tâches
✅ Peut gérer utilisateurs, rôles, équipe
✅ Voit tous les KPI

### Chef de Projet (J20001):
✅ Peut créer/modifier/supprimer tâches
✅ Peut chiffrer et prioriser
✅ Peut assigner des développeurs
✅ Voit les KPI
❌ N'a pas accès à l'administration (pas de GererUtilisateurs)

### Développeur (J04831, J30001-J30004):
✅ Peut créer des demandes
✅ Peut chiffrer (Planning Poker)
✅ Peut modifier ses propres tâches
✅ Peut changer le statut de ses tâches
❌ Ne peut pas supprimer de tâches
❌ Ne peut pas assigner de développeurs
❌ Ne peut pas prioriser

### Business Analyst (J10001, J10002):
✅ Peut créer des demandes
✅ Voit les KPI
❌ Ne peut pas chiffrer
❌ Ne peut pas modifier de tâches
❌ Ne peut pas prioriser

---

## 📋 TODO - Fonctionnalités restantes à implémenter

### 🔴 Priorité HAUTE (Fonctionnalités critiques)

#### Visibilité des boutons selon permissions dans les vues
- [x] **BacklogView.xaml**: Lier visibilité des boutons
  - [x] Bouton "Nouvelle Tâche" → PeutCreerTaches
  - [x] Bouton "Enregistrer" → PeutModifierTaches (contextuel)
  - [x] Bouton "Nouveau Projet" → PeutGererReferentiels
  - [x] ComboBox "Priorité" → PeutPrioriser
  - [x] ComboBox "Complexité" → PeutChiffrer
  - [x] ComboBox "Dev Assigné" → PeutAssignerDev
  
- [x] **ProjetsView.xaml**: Contrôler actions CRUD
  - [x] Bouton "Nouveau Projet" → PeutGererReferentiels
  - [x] Bouton "Modifier" → PeutModifierTaches
  - [x] Bouton "Supprimer" → PeutSupprimerTaches
  - N/A ComboBox "Priorité" → PeutPrioriser (pas de ComboBox éditable dans la vue liste)
  - N/A ComboBox "Dev assigné" → PeutAssignerDev (pas de ComboBox éditable dans la vue liste)

- [x] **EditTacheWindow.xaml**: Permissions contextuelles
  - [x] Champs éditables selon PeutModifierTache(tache)
  - [x] Bouton "Supprimer" selon PeutSupprimerTache(tache) (N/A - pas de bouton supprimer dans cette fenêtre)
  - [x] ComboBox Dev selon PeutAssignerDev

#### Intégration PermissionService dans ViewModels
- [x] **BacklogViewModel**: 
  - [x] Ajouter paramètre PermissionService au constructeur
  - [x] Ajouter propriétés Visibility (PeutCreerTachesVisibility, etc.)
  - [x] Modifier CanExecute des Commands selon permissions
  - [x] CommandManager.InvalidateRequerySuggested() dans SelectedItem
  
- [x] **ProjetsViewModel**:
  - [x] Même pattern que BacklogViewModel
  - [x] Commands.CanExecute basé sur permissions
  - [x] Propriétés Visibility ajoutées

#### Converter XAML
- [x] Vérifier si BooleanToVisibilityConverter existe déjà (dans App.xaml)
- [x] Confirmé: BooleanToVisibilityConverter existe (ligne 10 de App.xaml)

### 🟡 Priorité MOYENNE (Améliorations UX)

#### Gestion des utilisateurs
- [x] **GestionUtilisateursPage**: Fenêtre modale pour ajouter/modifier utilisateur
  - [x] Formulaire: Nom, Prénom, Email, UsernameWindows, RoleId
  - [x] Validation des champs
  - [x] Actualisation automatique de la liste
  - [x] Bouton supprimer avec confirmation

#### Gestion de l'équipe
- [x] **GestionEquipeWindow/Page**: Mode édition fonctionnel
  - [x] _devEnEdition bien défini au clic sur "Éditer"
  - [x] Bouton "Éditer" visible dans la DataGrid
  - [x] Actualisation après modification
  - [x] Confirmation avant suppression

#### Feedback utilisateur
- [x] Messages d'erreur plus détaillés (avec StackTrace)
- [x] Confirmations avant suppressions (utilisateurs, équipe, projets)
- [ ] Indicateurs de chargement pour opérations longues

### 🟢 Priorité BASSE (Fonctionnalités avancées)

#### KPI & Statistiques détaillées
- [x] Créer **StatistiquesWindow.xaml** avec graphiques
  - [x] Graphique en barres: Tâches par statut
  - [x] Graphique en barres: Charge par développeur
  - [x] Tableau: Taux de complétion par projet
  - [x] Métrique: Temps moyen par complexité
  - [x] Bouton "Exporter en PDF" (placeholder)
  - [x] Contrôle d'accès par PeutVoirKPI (Admin, Chef de Projet, Business Analyst)
  - [x] Cartes KPI rapides (Total, Terminées %, En cours, Projets actifs)

#### Paramètres système
- [x] Créer **ParametresWindow.xaml**
  - [x] Interface avec sections: Base de données, Export/Import, Sauvegarde, Affichage
  - [x] Design cohérent avec le style BNP Paribas
- [x] Implémenter fonctionnalités **ParametresWindow.xaml.cs**
  - [x] Configuration du chemin de la base de données (avec avertissement redémarrage)
  - [x] Export complet des données (JSON avec statistiques)
  - [x] Export CSV des tâches du backlog
  - [x] Import de données (placeholder avec avertissement)
  - [x] Sauvegarde complète de la base de données (.db)
  - [x] Restauration depuis une sauvegarde (avec redémarrage auto)
  - [x] Affichage de la dernière sauvegarde
  - [x] Paramètres d'affichage (thème, langue) - placeholders pour versions futures
- [x] Ajouter accès dans l'interface
  - [x] Bouton "⚙️ Paramètres" dans MainWindow
  - [x] Accessible à tous les utilisateurs
- [x] Mettre à jour BacklogManager.csproj
  - [x] Ajout de ParametresWindow.xaml et .xaml.cs

#### Audit Log
- [x] Ajouter table **AuditLog** dans la base de données
  - [x] Colonnes: Id, Action, UserId, EntityType, EntityId, OldValue, NewValue, DateAction
  - [x] Implémenté dans SqliteDatabase et JsonDatabase
  - [x] Domain/AuditLog.cs créé avec 10 propriétés
- [x] Créer **AuditLogWindow.xaml**
  - [x] DataGrid avec historique des actions (coloré selon type)
  - [x] Filtres: Par utilisateur, date (début/fin), type d'action
  - [x] Export CSV avec échappement des caractères spéciaux
- [x] Créer **AuditLogService.cs**
  - [x] Méthodes: LogCreate, LogUpdate, LogDelete, LogLogin, LogLogout
  - [x] Service centralisé avec context utilisateur actuel
- [x] Ajouter accès dans l'interface
  - [x] Onglet "📜 Journal d'Audit" dans AdministrationWindow
  - [x] Bouton pour ouvrir AuditLogWindow
  - [x] Gestionnaire d'événements dans AdministrationWindow.xaml.cs
- [x] Mettre à jour BacklogManager.csproj
  - [x] Ajout de AuditLog.cs, AuditLogService.cs
  - [x] Ajout de AuditLogWindow.xaml et .xaml.cs
- [x] Implémenter journalisation automatique:
  - [x] Hook dans SaveBacklogItem (capture avant/après dans BacklogService)
  - [x] Hook dans AddOrUpdateUtilisateur (EditUtilisateurWindow avec audit)
  - [x] Hook dans DeleteUtilisateur (GestionUtilisateursPage avec audit)
  - [x] Hook dans AuthenticationService.Login/Logout (LogLogin appelé)
  - [x] AuditLogService passé depuis AuthenticationService → MainWindow → AdministrationWindow → Pages
  - [x] Hook dans UpdateRole (GestionRolesPage avec capture avant/après des 8 permissions)
  - [x] Hook dans SaveProjet et DeleteProjet (BacklogService avec détails complets)

#### Améliorations diverses
- [x] **Timeline**: Ajout de légendes pour les couleurs
  - [x] Légende des statuts (À faire, En cours, En test, Terminé)
  - [x] Légende des priorités (Urgente, Haute, Normale)
  - [x] Légende des alertes (Retard critique, Échéance proche, Dans les temps)
  - [x] Design cohérent avec branding BNP Paribas (vert #00915A)
- [x] **Kanban**: Drag & drop entre colonnes
  - [x] Événements PreviewMouseLeftButtonDown, PreviewMouseMove sur les cartes
  - [x] AllowDrop=True sur les 4 colonnes (À faire, En cours, En test, Terminé)
  - [x] Effets visuels BNP lors du drag (bordure verte #00915A, opacité)
  - [x] DragEnter/DragLeave avec changement de couleur de fond
  - [x] Animation de succès lors du drop (flash vert BNP)
  - [x] Mise à jour automatique du statut de la tâche
- [x] **Notifications**: Système d'alertes pour les tâches urgentes
  - [x] NotificationService.cs créé avec analyse automatique
  - [x] Types de notifications: Urgent (retard), Attention (échéance < 2j), Info (non assignée), Success (terminée)
  - [x] NotificationsWindow.xaml avec design BNP Paribas élégant
  - [x] Filtres par type et statut (lues/non lues)
  - [x] Badge de notification dans MainWindow avec compteur
  - [x] Timer automatique (analyse toutes les 5 minutes)
  - [x] Icônes émoji et codes couleur par type
  - [x] Actions: Marquer comme lu, Supprimer, Actualiser

---

## 📊 Progression globale

**Administration & Permissions**: ██████████ 100%
- ✅ Structure de base (PermissionService, AdministrationWindow)
- ✅ Gestion des rôles et permissions
- ✅ Migration base de données
- ✅ Visibilité des boutons dans BacklogView et ProjetsView
- ✅ Intégration complète dans BacklogViewModel et ProjetsViewModel
- ✅ EditTacheWindow permissions contextuelles
- ✅ Audit Log complet (table, service, UI, filtres, export CSV)
- ✅ ParametresWindow (export/import, backup/restore, configuration)

**Fonctionnalités critiques**: ██████████ 95%
- ✅ CRUD Projets, Équipe, Rôles
- ✅ Statistiques de base
- ✅ Permissions appliquées dans BacklogView et ProjetsView
- ✅ Permissions contextuelles dans EditTacheWindow
- ✅ Audit Log complet avec filtres et export
- ✅ Paramètres système avec backup/restore
- ❌ KPI avancés graphiques

**Expérience utilisateur**: ██████████ 100%
- ✅ Interface cohérente (style BNP)
- ✅ Messages de confirmation
- ✅ Messages d'erreur détaillés
- ✅ Gestion utilisateurs complète (ajout/modif/suppression)
- ✅ Validation des formulaires
- ✅ Audit Log avec filtres par utilisateur/date/action
- ✅ Export CSV des logs d'audit
- ✅ Backup/Restore base de données
- ✅ Timeline avec légende des couleurs complète
- ✅ Kanban avec drag & drop fluide et effets visuels BNP
- ✅ Système de notifications temps réel avec badge et filtres
- ✅ Feedback temps réel (drag & drop, animations, badges)
