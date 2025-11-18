# 🎯 Validation des CRA Prévisionnels - Plan d'Implémentation

## 📋 Problématique

### Scénario réel

**Situation initiale :**
- Le dev planifie ses CRA pour tout le mois (ex: 1-30 novembre)
- Il crée des CRA prévisionnels (`EstPrevisionnel = true`) pour les jours futurs

**Problème en cours de mois :**
- Le dev prend des **congés imprévus** (5-7 novembre)
- Une **tâche urgente** arrive (8-9 novembre)
- Il aide sur un **support client** (10 novembre)
- Il prend une **tâche RUN** (11-12 novembre)

**Résultat :**
- Les CRA prévisionnels du 5-12 novembre ne correspondent **plus à la réalité**
- Le dev ne va pas sur l'appli tous les jours pour ajuster
- Au 18 novembre, les CRA du 5-12 sont **toujours prévisionnels** mais devraient être **validés/modifiés**

---

## 🎯 Solution proposée

### Concept : Validation journalière des CRA

**Principe :**
1. Les jours **passés** avec CRA prévisionnel restent **"à valider"**
2. Le dev doit **confirmer ou modifier** ces CRA pour qu'ils deviennent **réels**
3. Visual clair : **couleur orange** pour "à valider", **bouton "Valider la journée"**

### Workflow

```
┌─────────────────────────────────────────────────────┐
│  CRA Prévisionnel créé                              │
│  (18 nov : planifié 1j sur Tâche A pour le 25 nov) │
└─────────────────────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────┐
│  Le 25 novembre arrive                              │
│  ➡️ CRA passe en statut "À VALIDER"                │
│  ➡️ Couleur ORANGE dans le calendrier              │
└─────────────────────────────────────────────────────┘
                       │
                       ▼
        ┌──────────────┴──────────────┐
        │                             │
        ▼                             ▼
┌─────────────────┐       ┌─────────────────────┐
│  Dev CONFIRME   │       │  Dev MODIFIE        │
│  "OK c'est bon" │       │  "Non j'ai fait     │
│  ➡️ CRA validé  │       │   autre chose"      │
│  EstPrevisionnel│       │  ➡️ Modif + validé  │
│  = false        │       │  EstPrevisionnel    │
└─────────────────┘       │  = false            │
                          └─────────────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │  CRA devient RÉEL             │
                    │  ➡️ Compte dans temps réel    │
                    │  ➡️ Couleur VERTE (passé)     │
                    └───────────────────────────────┘
```

---

## 🗄️ Modifications de la base de données

### Nouvelle colonne dans la table CRA

```sql
ALTER TABLE CRA ADD COLUMN EstValide INTEGER DEFAULT 0;
```

**Nouvelle logique :**
- `EstPrevisionnel = true` → CRA créé pour le futur
- `EstPrevisionnel = true` + `EstValide = false` → CRA passé **non validé** (ORANGE)
- `EstPrevisionnel = false` + `EstValide = true` → CRA **validé/réel** (VERT)

### États possibles

| État | EstPrevisionnel | EstValide | Signification | Couleur | Compte dans temps réel |
|------|----------------|-----------|---------------|---------|------------------------|
| 1 | `true` | `false` | **Futur** - Planifié, date pas encore arrivée | Orange clair (#FFE082) | ❌ Non |
| 2 | `true` | `false` | **À valider** - Date passée, pas encore confirmé | Orange vif (#FF9800) | ❌ Non |
| 3 | `false` | `true` | **Validé/Réel** - Confirmé par le dev | Vert (#E8F5E9) | ✅ Oui |
| 4 | `false` | `false` | **Saisi manuellement** (legacy) | Vert (#E8F5E9) | ✅ Oui |

**Note :** État 4 pour compatibilité avec CRA existants (créés avant cette fonctionnalité)

---

## 💻 Modifications du code

### 1. Modèle `Domain/CRA.cs`

**Ajouter la propriété :**

```csharp
public class CRA
{
    public int Id { get; set; }
    public int UtilisateurId { get; set; }
    public int TacheId { get; set; }
    public DateTime DateSaisie { get; set; }
    public double Heures { get; set; }
    public string Commentaire { get; set; }
    public bool EstPrevisionnel { get; set; }
    
    // ⭐ NOUVEAU
    public bool EstValide { get; set; }
    
    // ⭐ PROPRIÉTÉ CALCULÉE : CRA à valider ?
    public bool EstAValider => EstPrevisionnel && DateSaisie.Date < DateTime.Now.Date && !EstValide;
}
```

### 2. Service `Services/CRAService.cs`

**Nouvelle méthode : Valider un CRA**

```csharp
/// <summary>
/// Valide un CRA prévisionnel (le passe en réel)
/// </summary>
public void ValiderCRA(int craId)
{
    var cra = _db.GetCRAById(craId);
    if (cra == null) return;
    
    // Passe le CRA en validé
    cra.EstPrevisionnel = false;
    cra.EstValide = true;
    
    _db.UpdateCRA(cra);
}

/// <summary>
/// Valide tous les CRA d'une journée pour un utilisateur
/// </summary>
public void ValiderJournee(int utilisateurId, DateTime date)
{
    var cras = _db.GetAllCRAs()
        .Where(c => c.UtilisateurId == utilisateurId)
        .Where(c => c.DateSaisie.Date == date.Date)
        .Where(c => c.EstPrevisionnel) // Seulement les prévisionnels
        .ToList();
    
    foreach (var cra in cras)
    {
        cra.EstPrevisionnel = false;
        cra.EstValide = true;
        _db.UpdateCRA(cra);
    }
}

/// <summary>
/// Récupère les jours avec CRA à valider pour un dev
/// </summary>
public List<DateTime> GetJoursAValider(int utilisateurId)
{
    return _db.GetAllCRAs()
        .Where(c => c.UtilisateurId == utilisateurId)
        .Where(c => c.EstPrevisionnel)
        .Where(c => c.DateSaisie.Date < DateTime.Now.Date)
        .Where(c => !c.EstValide)
        .Select(c => c.DateSaisie.Date)
        .Distinct()
        .OrderBy(d => d)
        .ToList();
}
```

### 3. ViewModel `ViewModels/CRACalendrierViewModel.cs`

**Ajouter les propriétés dans `JourCalendrierViewModel` :**

```csharp
public class JourCalendrierViewModel : INotifyPropertyChanged
{
    // ... propriétés existantes ...
    
    // ⭐ NOUVEAU : Indicateurs de validation
    public bool ADesCRAsAValider { get; set; }  // Orange vif
    public int NombreCRAsAValider { get; set; }
    
    // Couleur dynamique selon l'état
    public string CouleurFond 
    { 
        get 
        {
            if (ADesCRAsAValider) return "#FF9800";  // Orange vif - À VALIDER
            if (EstDansFutur) return "#FFE082";       // Orange clair - Futur
            if (EstDansPasse) return "#E8F5E9";       // Vert - Passé validé
            if (EstAujourdhui) return "#C8E6C9";      // Vert moyen - Aujourd'hui
            return "White";
        }
    }
}
```

**Nouvelle commande de validation :**

```csharp
public ICommand ValiderJourneeCommand { get; private set; }

// Dans le constructeur
ValiderJourneeCommand = new RelayCommand<JourCalendrierViewModel>(ValiderJournee);

private void ValiderJournee(JourCalendrierViewModel jour)
{
    if (jour == null || !jour.ADesCRAsAValider) return;
    
    // Demander confirmation
    var result = MessageBox.Show(
        $"Valider tous les CRA prévisionnels du {jour.Date:dd/MM/yyyy} ?\n\n" +
        $"Cela confirmera que vous avez bien travaillé sur les tâches planifiées.\n" +
        $"Les CRA seront comptabilisés dans le temps réel.",
        "Valider la journée",
        MessageBoxButton.YesNo,
        MessageBoxImage.Question);
    
    if (result == MessageBoxResult.Yes)
    {
        _craService.ValiderJournee(UtilisateurConnecte.Id, jour.Date);
        
        // Recharger le calendrier
        ChargerCalendrier();
        
        MessageBox.Show(
            $"✅ Journée du {jour.Date:dd/MM/yyyy} validée !",
            "Validation réussie",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
```

**Modifier `ChargerCalendrier()` pour détecter les CRA à valider :**

```csharp
private void ChargerCalendrier()
{
    // ... code existant ...
    
    var aujourdhui = DateTime.Now.Date;
    
    // Pour chaque jour du calendrier
    foreach (var date in joursDuMois)
    {
        // Récupérer les CRA de ce jour
        var crasDuJour = _craService.GetCRAsByDevEtDate(UtilisateurConnecte.Id, date);
        
        // Compter les CRA à valider (prévisionnels + date passée + non validés)
        var crasAValider = crasDuJour
            .Where(c => c.EstPrevisionnel && date < aujourdhui && !c.EstValide)
            .ToList();
        
        var jourVM = new JourCalendrierViewModel
        {
            Date = date,
            // ... autres propriétés ...
            ADesCRAsAValider = crasAValider.Any(),
            NombreCRAsAValider = crasAValider.Count,
            // ...
        };
        
        JoursCalendrier.Add(jourVM);
    }
}
```

### 4. Vue XAML `Views/CRACalendrierView.xaml`

**Ajouter le bouton de validation dans le template de jour :**

```xaml
<Border x:Name="PART_Border" Background="White" CornerRadius="6" 
        Padding="{TemplateBinding Padding}" BorderBrush="#E0E0E0" BorderThickness="1">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/> <!-- ⭐ NOUVEAU : ligne pour bouton -->
        </Grid.RowDefinitions>
        
        <!-- Numéro du jour (existant) -->
        <TextBlock Grid.Row="0" Text="{Binding Jour}" ... />
        
        <!-- Contenu du jour : tâches, etc. (existant) -->
        <Grid Grid.Row="1">
            <!-- ... contenu existant ... -->
        </Grid>
        
        <!-- ⭐ NOUVEAU : Bouton de validation si CRA à valider -->
        <Button Grid.Row="2" 
                Command="{Binding DataContext.ValiderJourneeCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                CommandParameter="{Binding}"
                Background="#FF9800" 
                Foreground="White"
                Padding="5,3"
                Margin="2"
                BorderThickness="0"
                CornerRadius="3"
                Cursor="Hand"
                ToolTip="Valider cette journée"
                Visibility="{Binding ADesCRAsAValider, Converter={StaticResource BooleanToVisibilityConverter}}">
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
                <TextBlock Text="✓" FontSize="12" FontWeight="Bold" Margin="0,0,3,0"/>
                <TextBlock Text="Valider" FontSize="10"/>
            </StackPanel>
        </Button>
    </Grid>
</Border>

<!-- ⭐ NOUVEAU : DataTrigger pour couleur orange si à valider -->
<ControlTemplate.Triggers>
    <!-- À VALIDER (priorité haute) -->
    <DataTrigger Binding="{Binding ADesCRAsAValider}" Value="True">
        <Setter TargetName="PART_Border" Property="Background" Value="#FF9800"/>
    </DataTrigger>
    
    <!-- Couleurs existantes (passé, futur, etc.) -->
    <DataTrigger Binding="{Binding EstDansPasse}" Value="True">
        <Setter TargetName="PART_Border" Property="Background" Value="#E8F5E9"/>
    </DataTrigger>
    <!-- ... autres triggers ... -->
</ControlTemplate.Triggers>
```

---

## 🎨 Indicateur visuel dans le calendrier

### Légende à ajouter

```
┌─────────────────────────────────────────────┐
│ 📅 NOVEMBRE 2025            [Aujourd'hui]  │
├─────────────────────────────────────────────┤
│                                             │
│  🟧 Orange vif = CRA à valider (passé)     │
│  🟨 Orange clair = CRA prévisionnel (futur)│
│  🟩 Vert = CRA validé (réel)               │
│                                             │
└─────────────────────────────────────────────┘
```

### Exemple de calendrier avec validation

```
┌──────┬──────┬──────┬──────┬──────┬──────┬──────┐
│  LUN │  MAR │  MER │  JEU │  VEN │  SAM │  DIM │
├──────┼──────┼──────┼──────┼──────┼──────┼──────┤
│   3  │   4  │   5  │   6  │   7  │   8  │   9  │
│ VERT │ VERT │ORANGE│ORANGE│ORANGE│ VERT │      │
│      │      │  ⚠️  │  ⚠️  │  ⚠️  │      │      │
│      │      │[✓Vld]│[✓Vld]│[✓Vld]│      │      │
├──────┼──────┼──────┼──────┼──────┼──────┼──────┤
│  10  │  11  │  12  │  13  │  14  │  15  │  16  │
│ VERT │ORANGE│ORANGE│ORANGE│ORANGE│ORANGE│ORANGE│
│      │  ⚠️  │  ⚠️  │  ⚠️  │  ⚠️  │  ⚠️  │  ⚠️  │
│      │[✓Vld]│[✓Vld]│[✓Vld]│[✓Vld]│[✓Vld]│[✓Vld]│
├──────┼──────┼──────┼──────┼──────┼──────┼──────┤
│  17  │  18  │  19  │  20  │  21  │  22  │  23  │
│ORANGE│VERT  │ORG_CL│ORG_CL│ORG_CL│ORG_CL│ORG_CL│
│  ⚠️  │AUJRD │      │      │      │      │      │
│[✓Vld]│      │      │      │      │      │      │
└──────┴──────┴──────┴──────┴──────┴──────┴──────┘

Légende:
VERT = Validé (compte dans temps réel)
ORANGE = À valider ! (ne compte PAS encore)
ORG_CL = Orange clair (futur prévisionnel)
```

---

## 🔄 Workflow utilisateur

### 1. Planification initiale (1er novembre)

Le dev planifie ses tâches pour tout le mois :
- Crée des CRA prévisionnels du 1 au 30 novembre
- Tous en `EstPrevisionnel = true`, `EstValide = false`
- Calendrier : **Orange clair** pour les jours futurs

### 2. Validation quotidienne

**Option A : Validation simple (conforme au plan)**

Chaque matin ou soir, le dev :
1. Ouvre l'application
2. Voit les **jours en orange vif** (à valider)
3. Clique sur **"✓ Valider"** pour chaque jour
4. Les jours passent en **vert** → comptent dans le temps réel

**Option B : Modification avant validation**

Si le plan a changé :
1. Clique sur le jour **orange**
2. Voit les CRA prévisionnels listés
3. **Modifie** : change la tâche, les heures, ou supprime
4. **Valide** : bouton "✓ Valider la journée"
5. Le jour passe en **vert**

### 3. Validation en masse (une fois par semaine)

Le dev peut valider plusieurs jours d'un coup :
1. Nouveau bouton : **"Valider la semaine dernière"**
2. Valide tous les jours de lundi à vendredi
3. Si certains jours ont été modifiés, il les repère (gardent l'orange)

---

## 🛡️ Gestion des cas particuliers

### Cas 1 : Congés imprévus

**Scénario :** Le dev prend congé le 5-7 novembre (non planifié)

**Solution :**
1. Le 5 nov reste **orange** (CRA prévisionnel pas validé)
2. Le dev ouvre l'appli le 8 nov
3. Voit les 5-7 en **orange**
4. Clique sur chaque jour → **"Supprimer le CRA"**
5. Optionnel : Crée un CRA "Congé" (tâche spéciale)

### Cas 2 : Tâche urgente

**Scénario :** Une tâche urgente arrive le 8 novembre

**Solution :**
1. Le dev a un CRA prévisionnel sur "Tâche A" le 8 nov
2. Le 8 nov, il travaille sur "Tâche Urgente" à la place
3. Le 9 nov, il ouvre l'appli :
   - Clique sur le jour 8 (orange)
   - **Modifie** le CRA : change "Tâche A" → "Tâche Urgente"
   - Clique **"✓ Valider la journée"**
4. Le jour passe en vert, avec la bonne tâche

### Cas 3 : Support client / RUN

**Scénario :** Le dev aide sur un support client + fait du RUN

**Solution :**
1. Le jour a un CRA prévisionnel "Tâche Dev"
2. Il peut **ajouter** d'autres CRA sur le même jour :
   - 0.5j "Tâche Dev"
   - 0.5j "Support Client"
3. Puis valide la journée
4. Les 2 CRA sont comptabilisés

### Cas 4 : Oubli de validation

**Scénario :** Le dev oublie de valider pendant 2 semaines

**Solution :**
1. Badge de notification : **"12 jours à valider"**
2. Liste déroulante : "Journées à valider"
3. Bouton : **"Tout valider d'un coup"** (si conforme)
4. Ou validation manuelle jour par jour si modifications

---

## 📊 Impact sur le calcul du temps réel

### Avant (problème)

```csharp
public double GetTempsReelTache(int tacheId)
{
    var cras = _db.GetAllCRAs()
        .Where(c => c.TacheId == tacheId && !c.EstPrevisionnel)  // ❌ Oublie les prévisionnels passés
        .ToList();
    
    return cras.Sum(c => c.Heures);
}
```

**Problème :** Les CRA prévisionnels (même passés) ne comptent jamais

### Après (solution)

```csharp
public double GetTempsReelTache(int tacheId)
{
    var cras = _db.GetAllCRAs()
        .Where(c => c.TacheId == tacheId)
        .Where(c => !c.EstPrevisionnel || c.EstValide)  // ✅ Compte si validé OU si réel
        .ToList();
    
    return cras.Sum(c => c.Heures);
}
```

**Solution :** Les CRA validés comptent, même si `EstPrevisionnel = true` au départ

**Meilleure solution (plus claire) :**

```csharp
public double GetTempsReelTache(int tacheId)
{
    var cras = _db.GetAllCRAs()
        .Where(c => c.TacheId == tacheId)
        .Where(c => c.EstValide || (!c.EstPrevisionnel && c.DateSaisie.Date < DateTime.Now.Date))  
        // ✅ Compte si : Validé OU (Réel manuel + passé)
        .ToList();
    
    return cras.Sum(c => c.Heures);
}
```

---

## 🎯 Fonctionnalités supplémentaires

### 1. Badge de notification

Afficher le nombre de jours à valider :

```xaml
<Button Content="Saisir CRA" ...>
    <Button.Badge>
        <TextBlock Text="{Binding NombreJoursAValider}" 
                   Background="Red" 
                   Foreground="White"
                   FontSize="10"
                   Padding="4,2"
                   CornerRadius="8"/>
    </Button.Badge>
</Button>
```

### 2. Vue "Journées à valider"

Nouvelle section dans l'interface :

```
┌────────────────────────────────────────────┐
│ ⚠️ JOURNÉES À VALIDER (12 jours)          │
├────────────────────────────────────────────┤
│ ☐ 5 novembre   • Tâche A (1j)      [✓]   │
│ ☐ 6 novembre   • Tâche A (1j)      [✓]   │
│ ☐ 7 novembre   • Tâche A (1j)      [✓]   │
│ ☐ 8 novembre   • Tâche B (0.5j)    [✓]   │
│                • Support (0.5j)     [✓]   │
│ ...                                        │
│                                            │
│ [Tout valider] [Valider la sélection]     │
└────────────────────────────────────────────┘
```

### 3. Rappel automatique

Notification au lancement de l'appli :

```
┌─────────────────────────────────────┐
│ 📢 Rappel                           │
│                                     │
│ Vous avez 12 journées à valider.   │
│                                     │
│ [Valider maintenant] [Plus tard]   │
└─────────────────────────────────────┘
```

---

## 📝 TODO : Étapes d'implémentation

### Phase 1 : Base de données (1h)

- [ ] Ajouter colonne `EstValide` à la table CRA
- [ ] Script de migration pour les CRA existants (`EstValide = true` si `EstPrevisionnel = false`)
- [ ] Tester la migration sur base de dev

### Phase 2 : Modèle et services (2h)

- [ ] Ajouter propriété `EstValide` dans `Domain/CRA.cs`
- [ ] Ajouter propriété calculée `EstAValider`
- [ ] Implémenter `ValiderCRA()` dans `CRAService.cs`
- [ ] Implémenter `ValiderJournee()` dans `CRAService.cs`
- [ ] Implémenter `GetJoursAValider()` dans `CRAService.cs`
- [ ] Modifier `GetTempsReelTache()` dans `BacklogService.cs`

### Phase 3 : ViewModel (3h)

- [ ] Ajouter `ADesCRAsAValider` dans `JourCalendrierViewModel`
- [ ] Ajouter `NombreCRAsAValider` dans `JourCalendrierViewModel`
- [ ] Ajouter `CouleurFond` calculée
- [ ] Créer `ValiderJourneeCommand`
- [ ] Implémenter méthode `ValiderJournee()`
- [ ] Modifier `ChargerCalendrier()` pour détecter CRA à valider
- [ ] Ajouter propriété `NombreJoursAValider` (badge)

### Phase 4 : Vue XAML (2h)

- [ ] Ajouter bouton "✓ Valider" dans template de jour
- [ ] Ajouter DataTrigger pour couleur orange (#FF9800)
- [ ] Ajouter légende des couleurs
- [ ] Ajouter badge notification (nombre de jours)
- [ ] Tester responsive du bouton

### Phase 5 : Fonctionnalités avancées (4h)

- [ ] Vue "Journées à valider" (liste déroulante)
- [ ] Bouton "Tout valider d'un coup"
- [ ] Validation en masse (semaine/mois)
- [ ] Rappel au lancement de l'appli
- [ ] Statistiques : "X jours validés / Y jours travaillés"

### Phase 6 : Tests (2h)

- [ ] Test : Créer CRA prévisionnel → attendre que date passe → vérifier orange
- [ ] Test : Valider CRA → vérifier passage au vert
- [ ] Test : Modifier CRA avant validation
- [ ] Test : Temps réel inclut bien les CRA validés
- [ ] Test : Validation en masse
- [ ] Test : Badge notification
- [ ] Test : Migration base de données existante

### Phase 7 : Documentation (1h)

- [ ] Mettre à jour `CALCUL_TEMPS_REEL_TACHES.md`
- [ ] Ajouter section "Validation des CRA" dans guide utilisateur
- [ ] Screenshots de l'interface avec validation
- [ ] FAQ : "Pourquoi je dois valider mes CRA ?"

---

## ⚡ Estimation totale

**Temps de développement : ~15 heures**

- Phase 1 : 1h
- Phase 2 : 2h
- Phase 3 : 3h
- Phase 4 : 2h
- Phase 5 : 4h (optionnel)
- Phase 6 : 2h
- Phase 7 : 1h

**Impact utilisateur :** Minime, améliore la précision des temps réels

---

## 🎯 Bénéfices

### Pour le dev

✅ **Flexibilité** : Planifie à l'avance sans se bloquer
✅ **Réactivité** : Peut modifier facilement si changement de plan
✅ **Simplicité** : Validation en 1 clic si conforme
✅ **Visibilité** : Voit immédiatement les jours en retard (orange)

### Pour le manager

✅ **Précision** : Temps réel reflète la réalité (pas les prévisions)
✅ **Traçabilité** : Sait quels CRA sont validés vs planifiés
✅ **Indicateurs** : "% de jours validés" = engagement du dev
✅ **Alerte** : Détecte rapidement les devs qui ne valident pas

### Pour le projet

✅ **Fiabilité** : Statistiques basées sur du réel validé
✅ **Prédictibilité** : Vélocité calculée sur temps validé
✅ **Ajustements** : Repère écarts plan/réel rapidement

---

## 🚀 Déploiement progressif

### Étape 1 : Version minimale (Phases 1-4)

- Validation simple (1 bouton par jour)
- Couleur orange pour "à valider"
- Modification du calcul temps réel

**Déploiement : 1 semaine de dev + tests**

### Étape 2 : Version complète (Phase 5)

- Vue "Journées à valider"
- Validation en masse
- Notifications

**Déploiement : +1 semaine après stabilisation v1**

### Étape 3 : Optimisations (optionnel)

- Validation automatique si pas de changement après X jours
- Suggestions : "Vous avez fait X comme la semaine dernière ?"
- Export : "Jours validés vs non validés"

---

## 📞 Questions / Réponses

### Q : Pourquoi ne pas valider automatiquement ?

**R :** Parce que le plan change souvent (urgences, congés, support). La validation manuelle force le dev à vérifier que le CRA correspond à la réalité.

### Q : Que se passe-t-il si j'oublie de valider ?

**R :** Les CRA restent **orange** et **ne comptent PAS** dans le temps réel. Un badge rouge te rappelle le nombre de jours à valider.

### Q : Je peux valider plusieurs jours d'un coup ?

**R :** Oui ! Bouton "Tout valider" si tout est conforme, ou sélection multiple.

### Q : Les CRA validés comptent dans les statistiques ?

**R :** Oui, dès qu'un CRA est validé (`EstValide = true`), il compte dans le temps réel de la tâche.

### Q : Je peux modifier un CRA déjà validé ?

**R :** Oui, tu peux le modifier. Il reste validé sauf si tu le repasses en "prévisionnel" explicitement.

---

## 🎓 Résumé pour présentation

**Problème :**
> Les devs planifient leurs CRA à l'avance, mais la réalité change (urgences, congés). Les CRA prévisionnels faussent les statistiques de temps réel.

**Solution :**
> Système de validation quotidienne : les CRA prévisionnels passés deviennent **orange** (à valider). Le dev doit les confirmer ou modifier. Seuls les CRA validés comptent dans le temps réel.

**Workflow :**
> 1. Planification → CRA orange clair (futur)
> 2. Date arrive → CRA orange vif (à valider)
> 3. Validation → CRA vert (compte dans temps réel)

**Bénéfices :**
> ✅ Flexibilité de planification
> ✅ Précision du temps réel
> ✅ Traçabilité du travail effectué
> ✅ Détection rapide des écarts plan/réel

---

## 📁 Fichiers concernés

**À modifier :**
- `Domain/CRA.cs`
- `Services/CRAService.cs`
- `Services/BacklogService.cs`
- `ViewModels/CRACalendrierViewModel.cs`
- `Views/CRACalendrierView.xaml`
- Base de données SQLite (migration)

**À créer :**
- `Services/ValidationService.cs` (optionnel, pour logique métier)
- `Views/JourneesAValiderView.xaml` (optionnel, vue dédiée)

**À documenter :**
- `CALCUL_TEMPS_REEL_TACHES.md` (mise à jour)
- Guide utilisateur (nouvelle section)

---

**🎯 Prêt à implémenter dès que tu veux !**
