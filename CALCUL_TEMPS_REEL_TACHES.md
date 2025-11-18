# 📊 Calcul du Temps Réel des Tâches - BacklogManager

## 🎯 Vue d'ensemble

Le **temps réel** d'une tâche représente le **temps effectivement passé** par les développeurs, calculé à partir des **CRA (Comptes Rendus d'Activité)** saisis dans l'application.

---

## 🔢 Principe de calcul

### Formule de base

```
Temps Réel d'une Tâche = Somme de tous les CRA associés à cette tâche
```

⚠️ **IMPORTANT** : Les CRA se saisissent en **demi-journées** (0.5j ou 1j), pas en heures !
- **0.5 jour** = 1 demi-journée (matin ou après-midi)
- **1 jour** = 1 journée complète

### Exemple concret

Si une tâche "BACKLOGBUGCRA" a les CRA suivants :
- **5 novembre** : 1 jour
- **6 novembre** : 0.5 jour  
- **7 novembre** : 1 jour

**Temps réel total = 1 + 0.5 + 1 = 2.5 jours**

---

## 💾 Stockage des données

### Table `CRA` (base de données SQLite)

```sql
CREATE TABLE CRA (
    Id INTEGER PRIMARY KEY,
    UtilisateurId INTEGER,        -- Qui a travaillé
    TacheId INTEGER,              -- Sur quelle tâche
    DateSaisie TEXT,              -- Quel jour
    Heures REAL,                  -- Nombre de jours (0.5 ou 1.0)
    Commentaire TEXT,
    EstPrevisionnel INTEGER       -- 0 = réel, 1 = prévisionnel (futur)
)
```

**Clés importantes** : 
- La colonne `Heures` contient des **jours** (0.5 ou 1.0), pas des heures réelles
- Seuls les CRA avec `EstPrevisionnel = 0` sont comptés dans le temps réel

---

## 🔍 Méthode de calcul dans le code

### Fichier : `Services/BacklogService.cs`

#### Méthode `GetTempsReelTache(int tacheId)`

```csharp
public double GetTempsReelTache(int tacheId)
{
    // Récupérer tous les CRA liés à cette tâche
    var cras = _db.GetAllCRAs()
        .Where(c => c.TacheId == tacheId && !c.EstPrevisionnel)  // Uniquement CRA réels (passés)
        .ToList();
    
    // Additionner tous les jours
    return cras.Sum(c => c.Heures);  // c.Heures contient des jours (0.5 ou 1.0)
}
```

**Explication ligne par ligne :**

1. `_db.GetAllCRAs()` → Récupère tous les CRA de la base de données
2. `.Where(c => c.TacheId == tacheId)` → Filtre uniquement les CRA de cette tâche
3. `&& !c.EstPrevisionnel` → Exclut les CRA prévisionnels (futurs)
4. `.Sum(c => c.Heures)` → Additionne tous les jours (0.5j ou 1j par CRA)

---

## 📅 Différence : Temps Réel vs Prévisionnel

### CRA Réels (EstPrevisionnel = false)
- **Dates passées** (< aujourd'hui)
- Saisis manuellement par le développeur
- **Comptent dans le temps réel**
- Exemple : CRA du 5 novembre saisi le 5 novembre

### CRA Prévisionnels (EstPrevisionnel = true)
- **Dates futures** (> aujourd'hui)
- Créés automatiquement par l'allocation intelligente
- **NE comptent PAS dans le temps réel**
- Exemple : CRA du 25 novembre créé le 18 novembre

**Pourquoi cette distinction ?**
- Le temps réel = travail **déjà effectué**
- Les prévisionnels = travail **planifié** mais pas encore fait

---

## 🎨 Affichage visuel

### Dans l'interface Backlog

```
┌─────────────────────────────────────────┐
│ Tâche: BACKLOGBUGCRA                   │
│                                         │
│ 📊 Temps Réel: 2.5j / 5j estimées      │
│ ━━━━━━━━━━░░░░░░░░░░ 50%              │
└─────────────────────────────────────────┘
```

- **Barre verte** : Progression du temps réel
- **Temps réel / Chiffrage** : Comparaison avec l'estimation initiale (en jours)

### Dans le calendrier CRA

Chaque jour affiche le total en jours :

```
┌──────┐
│  5   │  ← Numéro du jour
│ test │  ← Nom de la tâche
│ 1.0j │  ← Jours saisis (0.5j ou 1j)
└──────┘
```

---

## 🧮 Calculs dérivés

### 1. Reste à Faire (RAF)

```csharp
public double GetResteAFaire(BacklogItem tache)
{
    double tempsReel = GetTempsReelTache(tache.Id);  // En jours
    double estimation = tache.ChiffrageJours;         // En jours
    
    return Math.Max(0, estimation - tempsReel);
}
```

**Exemple :**
- Estimation : 10 jours
- Temps réel : 4.5 jours
- RAF = 10 - 4.5 = **5.5 jours restants**

### 2. Pourcentage d'avancement

```csharp
public double GetPourcentageAvancement(BacklogItem tache)
{
    double tempsReel = GetTempsReelTache(tache.Id);  // En jours
    double estimation = tache.ChiffrageJours;         // En jours
    
    if (estimation == 0) return 0;
    
    return Math.Min(100, (tempsReel / estimation) * 100);
}
```

**Exemple :**
- Temps réel : 4.5 jours
- Estimation : 10 jours
- Avancement = (4.5 / 10) × 100 = **45%**

### 3. Surcharge (dépassement)

```csharp
public bool EstEnSurcharge(BacklogItem tache)
{
    double tempsReel = GetTempsReelTache(tache.Id);  // En jours
    double estimation = tache.ChiffrageJours;         // En jours
    
    return tempsReel > estimation; // Si dépassement
}
```

**Indicateur visuel :**
- ✅ Vert si < 100%
- ⚠️ Orange si > 100%
- 🔴 Rouge si > 150%

---

## 📈 Utilisation dans les statistiques

### Fichier : `ViewModels/StatistiquesViewModel.cs`

#### Total heures travaillées sur un projet

```csharp
var tachesProjet = _backlogService.GetTaches().Where(t => t.ProjetId == projetId);
double totalHeuresReelles = 0;

foreach (var tache in tachesProjet)
{
    totalHeuresReelles += _backlogService.GetTempsReelTache(tache.Id);
}
```

#### Vélocité d'une équipe (jours/semaine)

```csharp
var crasEquipe = _db.GetAllCRAs()
    .Where(c => c.DateSaisie >= debutSemaine && c.DateSaisie <= finSemaine)
    .Where(c => !c.EstPrevisionnel);

double velocite = crasEquipe.Sum(c => c.Heures);  // Total en jours
```

---

## 🔄 Mise à jour en temps réel

### Déclencheurs de recalcul

Le temps réel est recalculé automatiquement quand :

1. **Ajout d'un CRA** → `SaisirCRA()` dans `CRACalendrierViewModel`
2. **Modification d'un CRA** → `ModifierCRA()`
3. **Suppression d'un CRA** → `SupprimerCRA()`
4. **Passage du prévisionnel au réel** → Quand la date devient passée

### Mécanisme de notification

```csharp
private void SaisirCRA()
{
    // ... Enregistrement du CRA ...
    
    // Recalculer le temps réel
    double nouveauTempsReel = _backlogService.GetTempsReelTache(tacheId);
    
    // Notifier l'interface pour mise à jour visuelle
    OnPropertyChanged(nameof(TacheSelectionnee));
    ChargerBacklog(); // Recharge la liste des tâches
}
```

---

## 🎯 Cas particuliers

### 1. Plusieurs devs sur une tâche

Le temps réel = **somme des jours de TOUS les devs**

**Exemple :**
- Dev A : 1j le 5 nov
- Dev B : 0.5j le 5 nov
- **Total jour = 1.5j** (pas de moyenne, somme directe)

### 2. CRA sur tâches spéciales

Les **tâches spéciales** (congés, formation, etc.) ne sont **pas comptées** dans le temps réel des tâches normales.

```csharp
// Les tâches spéciales ont un TypeTache != "DEVELOPPEMENT"
var crasRéels = _db.GetAllCRAs()
    .Where(c => c.TacheId == tacheId)
    .Where(c => !c.EstPrevisionnel)
    .Where(c => c.Tache.TypeTache == "DEVELOPPEMENT"); // Uniquement dev
```

### 3. CRA passé en réel automatiquement

Quand la date devient passée :
- `EstPrevisionnel` reste `true` dans la base
- Mais devient comptabilisé comme réel si le dev **confirme** le CRA

**Workflow :**
1. 18 nov : Création CRA prévisionnel pour le 25 nov (`EstPrevisionnel = true`)
2. 25 nov : Le dev valide/modifie → `EstPrevisionnel` passe à `false`
3. 26 nov : Maintenant comptabilisé dans le temps réel

---

## 📊 Exemple complet

### Tâche : "Développer API REST"

#### Données

- **Estimation** : 5 jours
- **Date début** : 3 novembre
- **Date fin attendue** : 10 novembre

#### CRA enregistrés

| Date       | Dev   | Jours | Prévisionnel |
|------------|-------|-------|--------------|
| 03/11/2025 | Alice | 1.0j  | ❌ Non       |
| 04/11/2025 | Alice | 1.0j  | ❌ Non       |
| 05/11/2025 | Bob   | 0.5j  | ❌ Non       |
| 06/11/2025 | Alice | 1.0j  | ❌ Non       |
| 07/11/2025 | Bob   | 0.5j  | ❌ Non       |
| 08/11/2025 | Alice | 1.0j  | ❌ Non       |
| 25/11/2025 | Alice | 1.0j  | ✅ Oui       |
| 26/11/2025 | Alice | 1.0j  | ✅ Oui       |

#### Calculs

**Temps réel (uniquement CRA réels) :**
```
1.0 + 1.0 + 0.5 + 1.0 + 0.5 + 1.0 = 5 jours
```

**Pourcentage d'avancement :**
```
(5 / 5) × 100 = 100%
```

**Reste à faire :**
```
5 - 5 = 0 jour
```

**Les 2j prévisionnels (25-26 nov) ne comptent PAS** car `EstPrevisionnel = true`

---

## 🛠️ API pour récupérer les données

### Méthodes disponibles dans `BacklogService`

```csharp
// Temps réel d'une seule tâche
double GetTempsReelTache(int tacheId)

// Liste des CRA d'une tâche
List<CRA> GetCRAsByTache(int tacheId)

// CRA d'un dev sur une période
List<CRA> GetCRAsByDevEtPeriode(int devId, DateTime debut, DateTime fin)

// Total heures d'un projet
double GetTotalHeuresProjet(int projetId)

// Vélocité hebdomadaire
double GetVelociteSemaine(DateTime debutSemaine)
```

### Exemple d'utilisation

```csharp
// Obtenir le temps réel d'une tâche
var tache = _backlogService.GetTacheById(123);
double tempsReel = _backlogService.GetTempsReelTache(123);

Console.WriteLine($"Tâche: {tache.Titre}");
Console.WriteLine($"Estimation: {tache.ChiffrageJours} jours");
Console.WriteLine($"Temps réel: {tempsReel} jours");
Console.WriteLine($"RAF: {tache.ChiffrageJours - tempsReel}j");
```

---

## 📝 Résumé pour présentation

**Question : "Comment est calculé le temps réel ?"**

**Réponse courte :**
> Le temps réel d'une tâche est la somme de tous les jours saisis dans les CRA (Comptes Rendus d'Activité) par les développeurs, en excluant les CRA prévisionnels (futurs). Les CRA se saisissent en demi-journées (0.5j) ou journées complètes (1j).

**Réponse technique :**
> 1. Récupération de tous les CRA liés à la tâche (`TacheId`)
> 2. Filtrage des CRA réels uniquement (`EstPrevisionnel = false`)
> 3. Sommation des jours (`SUM(Heures)`) - colonne nommée "Heures" mais contient des jours
> 4. Le résultat est comparé au chiffrage initial pour calculer l'avancement

**Points clés :**
- ✅ Basé sur les **CRA réels** (dates passées)
- ✅ Saisie en **demi-journées** (0.5j ou 1j uniquement)
- ✅ Additionne les jours de **tous les devs**
- ✅ Mis à jour **en temps réel** à chaque saisie
- ✅ Exclu les **CRA prévisionnels** (planifiés mais pas faits)
- ✅ Permet de calculer **avancement**, **RAF**, et **dépassements**

---

## 🎓 Avantages du système

1. **Traçabilité** : Historique complet du temps passé par jour
2. **Multi-dev** : Supporte plusieurs personnes sur une tâche
3. **Prédictif** : Distinction clair/prévisionnel
4. **Analytique** : Base pour statistiques et vélocité
5. **Visuel** : Indicateurs d'avancement en temps réel

---

## 🔗 Fichiers concernés

- `Services/BacklogService.cs` → Calcul du temps réel
- `Services/CRAService.cs` → Gestion des CRA
- `ViewModels/CRACalendrierViewModel.cs` → Saisie des CRA
- `ViewModels/BacklogViewModel.cs` → Affichage temps réel
- `ViewModels/StatistiquesViewModel.cs` → Analyses et rapports
- `Domain/CRA.cs` → Modèle de données

---

## 📞 Questions fréquentes

### Q1 : Le temps réel peut-il dépasser l'estimation ?
**Oui** ! C'est un indicateur de **dépassement** ou **sous-estimation** initiale.

### Q2 : Les CRA futurs comptent-ils ?
**Non**, seuls les CRA réels (dates passées, `EstPrevisionnel = false`) comptent.

### Q3 : Que se passe-t-il si on modifie un CRA ?
Le temps réel est **recalculé immédiatement** et l'interface se met à jour.

### Q4 : Peut-on avoir plusieurs CRA le même jour ?
**Oui** si plusieurs devs travaillent, **non** si même dev (le dernier CRA écrase).

### Q6 : Pourquoi en jours et pas en heures ?
Pour simplifier la saisie : **1 demi-journée = 0.5j** ou **1 journée = 1j**. Pas besoin de compter les heures précises.

### Q5 : Comment voir le détail jour par jour ?
Dans **"Suivi CRA"** (admin) ou **"Saisir CRA"** → calendrier avec heures par jour.
