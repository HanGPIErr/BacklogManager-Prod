# 🎨 Instructions pour créer l'icône Congés avec Sora

## 📋 Spécifications

**Nom du fichier** : `conges-icon.png`  
**Emplacement** : `c:\Users\HanGP\BacklogManager\Images\conges-icon.png`  
**Format** : PNG avec transparence (canal alpha)  
**Résolution** : 128x128 pixels minimum (recommandé: 256x256 pour qualité optimale)  
**Utilisation** : Grande icône dans le calendrier CRA (affichée en 48x48px) pour identifier les jours de congés

---

## 🎯 Prompt pour Sora

```
Create a vibrant, eye-catching flat design icon representing vacation/holidays (congés) for business software. The icon should feature a prominent, stylized palm tree with a bright sun in warm tropical colors (turquoise blue #00BCD4, sunny yellow/orange #FFB800, tropical green #4CAF50). Style: bold, minimalist flat design, no shadows, transparent background, very high contrast and visibility, optimized to be clearly recognizable when displayed large (48x48 pixels) in a calendar cell. The palm tree should be the dominant element, simple but distinctive.
```

---

## 🎨 Détails du design attendu

### Couleurs principales
- **Bleu turquoise** : #00BCD4 (pour le palmier/eau)
- **Orange/Jaune** : #FFB800 (pour le soleil)
- **Vert tropical** : #4CAF50 (feuilles de palmier optionnel)

### Style
- Design plat (flat design)
- Pas d'ombres portées
- Contours nets et simples
- Fond transparent
- Contraste élevé pour lisibilité

### Composition
- **Palmier stylisé** : élément dominant, bien visible
- Soleil en arrière-plan ou coin supérieur
- Centré dans le carré 128x128px
- Marges de 10-15px sur chaque côté
- Design **bold** pour être très visible même de loin

---

## 📦 Après génération avec Sora

1. Sauvegarder l'image générée sous le nom `conges-icon.png`
2. Vérifier la transparence du fond (canal alpha)
3. Redimensionner si nécessaire à 64x64px ou 128x128px
4. Placer le fichier dans : `c:\Users\HanGP\BacklogManager\Images\`
5. Recompiler le projet

---

## ✅ Vérification

L'icône apparaîtra automatiquement dans le calendrier CRA sur les jours où un développeur a saisi des heures sur une tâche de type "Congés".

**Emplacement dans l'interface** : 
- Vue CRA Calendrier
- Grille des jours du mois
- Affichée à côté de l'icône 🎊 (jours fériés) et du badge vert (heures saisies)

---

## 🔄 Alternatives temporaires

En attendant la génération Sora, le système utilise actuellement l'emoji 🌴.
Une fois l'icône PNG créée, elle remplacera automatiquement l'emoji pour un rendu plus professionnel.
