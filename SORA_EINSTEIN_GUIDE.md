# 🎨 Images Caramel & Flopy Guide - Instructions Sora

## 👥 Personnages

- **🐱 Caramel** : Le chat orange, sage et patient, toujours prêt à expliquer
- **🐰 Flopy** : Le lapin mignon, curieux et enthousiaste, qui apprend

## 📝 Prompts Sora - 3 États Émotionnels

### 1️⃣ État Normal (Neutre/Calme)

**Nom du fichier** : `caramel-flopy-normal.png`

```
Create an adorable illustration featuring two cute characters side by side: a fluffy orange tabby cat named Caramel (on the left) with warm amber eyes and a calm, wise expression, and a soft white bunny named Flopy (on the right) with floppy ears and curious pink eyes. Both characters should be shown from chest up, sitting together in a friendly, approachable pose. Caramel has one paw slightly raised in a gentle explaining gesture. The art style should be modern, clean digital illustration with soft lines, kawaii-inspired but professional. Transparent background (PNG). Soft warm lighting. Both characters should look welcoming and ready to help, with neutral, friendly expressions.
```

### 2️⃣ État Content (Heureux/Encourageant)

**Nom du fichier** : `caramel-flopy-happy.png`

```
Create an adorable illustration featuring two cute characters side by side: a fluffy orange tabby cat named Caramel (on the left) with warm amber eyes, showing a big happy smile and cheerful expression, and a soft white bunny named Flopy (on the right) with floppy ears, eyes sparkling with joy and excitement. Both characters should be shown from chest up, looking absolutely delighted. Caramel is giving a thumbs up with his paw, and Flopy's ears are perked up happily. The art style should be modern, clean digital illustration with soft lines, kawaii-inspired but professional. Transparent background (PNG). Bright, warm lighting with a subtle glow effect. Both characters radiating positive energy and celebration.
```

### 3️⃣ État Mécontent (Grognon/Menaçant mais Rigolo)

**Nom du fichier** : `caramel-flopy-grumpy.png`

```
Create an adorable but funny illustration featuring two cute characters side by side: a fluffy orange tabby cat named Caramel (on the left) with narrowed amber eyes, showing a comically grumpy/stern expression with slightly puffed cheeks and one eyebrow raised, paws crossed looking mock-serious and "threateningly cute" - like he's pretending to be tough but remains absolutely adorable, and a soft white bunny named Flopy (on the right) with floppy ears slightly drooped, looking worried and apologetic with big innocent eyes, still completely adorable and sweet but concerned by Caramel's mood. Both characters should be shown from chest up. Caramel is the grumpy one (but still cute and funny, not scary), while Flopy stays gentle and endearing, maybe with one paw slightly raised in a "sorry" gesture. The art style should be modern, clean digital illustration with soft lines, kawaii-inspired but professional. Transparent background (PNG). Slightly cooler lighting with a subtle dramatic shadow effect on Caramel only. The overall vibe should be humorous - Caramel looks "angry but too cute to take seriously" while Flopy remains the sweet, innocent companion.
```

## 📋 Spécifications techniques

- **Format** : PNG avec fond transparent
- **Dimensions recommandées** : 300x300px (minimum 250x250px)
- **Emplacement** : `Images/` (dans le dossier du projet)
  - `caramel-flopy-normal.png`
  - `caramel-flopy-happy.png`
  - `caramel-flopy-grumpy.png`
- **Style** : Illustration digitale moderne, kawaii professionnel, clean
- **Composition** : Les deux personnages côte à côte, Caramel à gauche, Flopy à droite
- **Cohérence** : Les 3 images doivent avoir la même composition, seules les expressions changent
- **Couleurs** : 
  - Caramel : Orange chaud, nuances ambrées
  - Flopy : Blanc/crème doux, oreilles roses
  - Éclairage adapté à l'émotion

## 🎯 Utilisation dans l'application

Les images seront affichées dans la fenêtre du guide selon le contexte :
- **Normal** : Question générale, état par défaut
- **Happy** : Réponse positive, succès, félicitations
- **Grumpy** : Avertissement, erreur à éviter, "attention !"

### Affichage

- Taille d'affichage : 120x120px dans la sidebar gauche
- Changement dynamique selon le ton de la réponse
- Animation de transition douce entre les états

## 📍 Intégration

Une fois les images générées avec Sora :

1. Télécharger les 3 images en PNG avec transparence
2. Les renommer selon la convention :
   - `caramel-flopy-normal.png`
   - `caramel-flopy-happy.png`
   - `caramel-flopy-grumpy.png`
3. Les placer dans le dossier `Images/` du projet
4. Ajouter les références dans `BacklogManager.csproj` :
   ```xml
   <Resource Include="Images\caramel-flopy-normal.png" />
   <Resource Include="Images\caramel-flopy-happy.png" />
   <Resource Include="Images\caramel-flopy-grumpy.png" />
   ```
5. Mettre à jour le code-behind pour gérer les 3 états d'image

## 🎨 Logique d'affichage des états

### État Normal (par défaut)
- Questions générales
- Navigation dans le guide
- Explications neutres

### État Happy
- Réponses avec "✅", "🎉", "Bravo", "Excellent"
- Confirmations de succès
- Félicitations, encouragements

### État Grumpy
- Réponses avec "⚠️", "Attention", "Important"
- Avertissements
- Points à éviter, erreurs communes

## ✅ Alternative si Sora non disponible

1. **Générateurs d'IA alternatifs** :
   - DALL-E 3
   - Midjourney
   - Stable Diffusion

2. **Illustrations libres de droits** :
   - Sites : Freepik, Vecteezy, Flaticon
   - Mots-clés : "cute cat and bunny illustration PNG transparent"
   - Licence : Libre de droits commerciaux

3. **Commission d'artiste** :
   - Fiverr, DeviantArt
   - Fournir les descriptions et spécifications ci-dessus
   - Demander les 3 états émotionnels cohérents
