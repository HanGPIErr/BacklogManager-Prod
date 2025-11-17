# Gestion du Temps Réel - Spécifications Techniques

## Vue d'ensemble
Ce document décrit l'implémentation complète du système de gestion du temps réel (CRA - Compte Rendu d'Activité) dans BacklogManager.

## Objectif
Permettre le suivi précis du temps réel passé par les développeurs sur chaque tâche, avec comparaison par rapport aux estimations initiales (chiffrage en heures).

## Architecture

### 1. Modèle de données

#### Entité CRA
```csharp
public class CRA
{
    public int Id { get; set; }
    public int BacklogItemId { get; set; }
    public int DevId { get; set; }
    public DateTime Date { get; set; }
    public double HeuresTravaillees { get; set; }
    public string Commentaire { get; set; }
    public DateTime DateCreation { get; set; }
}
```

#### Schema SQLite
```sql
CREATE TABLE CRA (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    BacklogItemId INTEGER NOT NULL,
    DevId INTEGER NOT NULL,
    Date TEXT NOT NULL,
    HeuresTravaillees REAL NOT NULL,
    Commentaire TEXT,
    DateCreation TEXT NOT NULL,
    FOREIGN KEY (BacklogItemId) REFERENCES BacklogItems(Id),
    FOREIGN KEY (DevId) REFERENCES Utilisateurs(Id)
);

CREATE INDEX idx_cra_backlogitem ON CRA(BacklogItemId);
CREATE INDEX idx_cra_dev ON CRA(DevId);
CREATE INDEX idx_cra_date ON CRA(Date);
```

### 2. Modifications du schéma BacklogItem

#### Suppression de la colonne Points
La colonne `Points` (complexité Fibonacci) est supprimée car elle était peu utilisée et créait de la confusion avec le chiffrage en heures.

#### Ajout de DateDebut
```sql
ALTER TABLE BacklogItems ADD COLUMN DateDebut TEXT;
```

#### Modification de Statut
Extension de l'énumération pour supporter 6 états au lieu de 4 :
- 1: A faire
- 2: En attente (nouveau)
- 3: A prioriser (nouveau)
- 4: En cours
- 5: Test
- 6: Terminé

### 3. Services

#### CRAService
Service métier pour la gestion des CRA :

**Méthodes principales :**
- `SaveCRA(CRA cra)` : Sauvegarde un CRA avec validations
- `GetCRAsByBacklogItem(int backlogItemId)` : Liste des CRA pour une tâche
- `GetCRAsByDev(int devId, DateTime? dateDebut, DateTime? dateFin)` : CRA d'un dev sur une période
- `GetTempsReelTache(int backlogItemId)` : Calcul du temps total réel
- `GetChargeParJour(int devId, DateTime date)` : Charge journalière d'un dev
- `GetEcartTache(int backlogItemId)` : Écart entre estimé et réel
- `EstEnDepassement(int backlogItemId)` : Indicateur de dépassement (>110% du chiffrage)
- `EstEnRisque(int backlogItemId)` : Indicateur de risque (>90% du chiffrage)
- `DeleteCRA(int id)` : Suppression d'un CRA

**Règles de validation :**
- Un dev ne peut pas saisir plus de 24h par jour
- Pas de saisie sur dates futures
- HeuresTravaillees > 0
- BacklogItem et Dev doivent exister

#### Modifications SqliteDatabase
Ajout des méthodes :
- `GetCRAs(int? backlogItemId, int? devId, DateTime? dateDebut, DateTime? dateFin)`
- `SaveCRA(CRA cra)`
- `DeleteCRA(int id)`

### 4. Interface utilisateur

#### Vue 1 : Saisie quotidienne (CRASaisieWindow)
**Objectif :** Permettre la saisie rapide du temps journalier

**Composants :**
- DatePicker : Sélection de la date (défaut : aujourd'hui, max : aujourd'hui)
- ComboBox Dev : Sélection du développeur (pré-rempli avec utilisateur connecté)
- ComboBox Tâche : Liste des tâches actives du dev (filtrée par statut En cours/Test)
- TextBox Heures : Saisie du temps (double, 0.5 minimum)
- Boutons rapides : 0.5h, 1h, 2h, 4h (remplissage rapide)
- TextBox Commentaire : Contexte facultatif
- Récapitulatif jour : Total des heures déjà saisies pour cette date
- Bouton Valider : Sauvegarde avec validation

**Validation temps réel :**
- Alerte si total jour > 8h (warning)
- Erreur si total jour > 24h (blocage)
- Message si date future (blocage)

**ViewModel : CRAViewModel**
```csharp
public class CRAViewModel : ViewModelBase
{
    public ObservableCollection<Utilisateur> Devs { get; set; }
    public ObservableCollection<BacklogItem> TachesActives { get; set; }
    public DateTime DateSelectionnee { get; set; }
    public Utilisateur DevSelectionne { get; set; }
    public BacklogItem TacheSelectionnee { get; set; }
    public double Heures { get; set; }
    public string Commentaire { get; set; }
    public double TotalJour { get; private set; }
    public ICommand SaveCRACommand { get; }
    public ICommand SetHeuresCommand { get; } // Pour boutons rapides
}
```

#### Vue 2 : Historique CRA (CRAHistoriqueWindow)
**Objectif :** Consultation et gestion des CRA saisis

**Composants :**
- Filtres :
  - DatePicker Début / Fin (défaut : mois en cours)
  - ComboBox Dev (si admin : tous devs, si dev : soi-même uniquement)
  - ComboBox Projet
  - ComboBox Tâche
  - Boutons rapides : Aujourd'hui, Cette semaine, Ce mois, Tout
- DataGrid CRA :
  - Colonnes : Date, Dev, Projet, Tâche, Heures, Commentaire, Actions
  - Tri par date décroissante
  - Actions : Supprimer (avec confirmation)
- Footer :
  - Nombre de CRA
  - Total des heures

**Permissions :**
- Dev : Voit uniquement ses propres CRA
- Admin/Manager : Voit tous les CRA

**ViewModel : CRAHistoriqueViewModel**
```csharp
public class CRAHistoriqueViewModel : ViewModelBase
{
    public ObservableCollection<CRADisplay> CRAs { get; set; }
    public DateTime? DateDebut { get; set; }
    public DateTime? DateFin { get; set; }
    public Utilisateur DevFiltre { get; set; }
    public int TotalCRA => CRAs.Count;
    public double TotalHeures => CRAs.Sum(c => c.Heures);
    public ICommand LoadCRAsCommand { get; }
    public ICommand DeleteCRACommand { get; }
    public ICommand FilterTodayCommand { get; }
    public ICommand FilterWeekCommand { get; }
    public ICommand FilterMonthCommand { get; }
    public ICommand FilterAllCommand { get; }
}

public class CRADisplay
{
    public int Id { get; set; }
    public string Date { get; set; }
    public string DevNom { get; set; }
    public string ProjetNom { get; set; }
    public string TacheNom { get; set; }
    public double Heures { get; set; }
    public string Commentaire { get; set; }
}
```

#### Vue 3 : Modifications BacklogView
**Ajouts :**
- Colonne "Temps réel" après "Chiffrage" : Affiche `GetTempsReelTache()`
- Colonne "Écart" : Affiche différence avec code couleur
  - Vert : <= 100%
  - Orange : 100-110% (en risque)
  - Rouge : > 110% (dépassement)
- Suppression de la colonne "Complexité" (Points)

**Filtres additionnels :**
- Checkbox "En dépassement" : Filtre les tâches > 110%
- Checkbox "En risque" : Filtre les tâches 90-110%

#### Vue 4 : Modifications EditTacheWindow
**Suppressions :**
- ComboBox Complexité (Points)

**Ajouts :**
- DatePicker DateDebut : Date de début effective (nullable, visible uniquement si Statut >= En cours)

**Modifications :**
- Label "Chiffrage" : Clarification "Chiffrage (heures)"

#### Vue 5 : Modifications KanbanView
**Expansion de 4 à 6 colonnes :**
1. En attente (Statut = 2)
2. A prioriser (Statut = 3)
3. A faire (Statut = 1)
4. En cours (Statut = 4)
5. Test (Statut = 5)
6. Terminé (Statut = 6)

**Indicateurs visuels sur chaque carte :**
- Icône 🕐 avec temps réel si > 0
- Badge orange si en risque (90-110%)
- Badge rouge si en dépassement (>110%)

#### Ajouts MainWindow
**Nouveaux boutons dans la barre d'outils :**
- "⏱️ Saisir CRA" : Ouvre CRASaisieWindow
- "📊 Historique CRA" : Ouvre CRAHistoriqueWindow

### 5. Règles métier

#### Calculs
```
Temps réel = Somme(HeuresTravaillees) de tous les CRA de la tâche
Écart absolu = Temps réel - Chiffrage
Écart % = (Temps réel / Chiffrage) × 100
En risque = Écart % > 90% ET Écart % <= 110%
En dépassement = Écart % > 110%
```

#### Permissions
- **Dev** : 
  - Peut saisir ses propres CRA
  - Voit uniquement ses propres CRA dans l'historique
  - Ne peut pas supprimer les CRA de plus de 7 jours
- **Manager/Admin** :
  - Voit tous les CRA
  - Peut supprimer n'importe quel CRA
  - Peut saisir pour n'importe quel dev (délégation)

### 6. Points d'attention technique

#### Performance
- Index sur CRA(BacklogItemId, DevId, Date)
- Cache du temps réel dans BacklogViewModel (invalidé lors de la sauvegarde d'un CRA)
- Lazy loading des CRA dans l'historique (pagination si > 1000 entrées)

#### Intégrité des données
- Transaction pour sauvegarde CRA + mise à jour BacklogItem.DateDebut
- Cascade delete : Suppression tâche → suppression CRA associés
- Audit log : Tracer les créations/suppressions de CRA

#### UX
- Messages de confirmation avant suppression
- Toast notifications après sauvegarde
- Validation temps réel (pas de saisie silencieuse d'erreurs)

## Plan de déploiement

### Sprint 1 : Fondations (1 semaine) - ✅ TERMINÉ
- [x] Création entité CRA
- [x] Modifications schéma SQLite
- [x] CRAService avec validations
- [x] Conversion affichage heures → jours (1j = 8h)

### Sprint 2 : Interface CRA (1 semaine) - ✅ TERMINÉ
- [x] CRASaisieWindow + ViewModel (saisie en jours: 0.5j, 1j, 1.5j, 2j)
- [x] CRAHistoriqueWindow + ViewModel (affichage en jours)
- [x] Validation max 3j/jour et alertes

### Sprint 3 : Intégration vues existantes (3 jours) - ✅ TERMINÉ
- [x] Modifications BacklogView (indicateurs temps réel en jours)
- [x] Modifications EditTacheWindow (chiffrage en jours, suppression Complexité, ajout DateDebut)
- [x] Modifications KanbanView (6 colonnes au lieu de 4)
- [x] Ajout boutons MainWindow

### Sprint 4 : Rapports et statistiques (4 jours) - ⏳ À FAIRE
- [ ] Rapport hebdomadaire par dev
- [ ] Rapport mensuel par projet
- [ ] Export Excel des CRA

### Sprint 5 : Polissage (2 jours) - ⏳ À FAIRE
- [ ] Corrections bugs
- [ ] Documentation utilisateur

## Métriques de succès
- 100% des devs saisissent leur CRA quotidiennement
- 0 tâche sans temps réel après 1 semaine de Sprint 3
- Écart moyen chiffrage/réel < 20% après 1 mois d'utilisation
- Temps de saisie moyen < 2 min/jour

## Notes de migration
**Migration base existante :**
1. Sauvegarde de backlog.db
2. Suppression colonne Points : `ALTER TABLE BacklogItems DROP COLUMN Points;` (SQLite 3.35+)
3. Ajout DateDebut : `ALTER TABLE BacklogItems ADD COLUMN DateDebut TEXT;`
4. Création table CRA avec indexes
5. Validation : Vérifier que toutes les tâches sont visibles dans l'UI

**Rollback possible jusqu'au Sprint 3** (avant suppression colonne Points).
