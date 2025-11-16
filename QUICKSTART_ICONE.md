# 🚀 QUICK START - Icône BacklogManager

## ⚡ En 3 étapes rapides

### 1️⃣ Générer l'image (DALL-E, Midjourney, etc.)

**Copier-coller ce prompt :**
```
A modern, professional app icon for a backlog management software. 
Design features a minimalist kanban board with 3 vertical columns in BNP Paribas green (#00915A). 
Include small task cards represented as rectangles floating between columns.
Clean, flat design, professional corporate style, high contrast.
Square format, no text, suitable for Windows icon sizes 16x16 to 256x256.
```

**Télécharger** : PNG 1024x1024, fond transparent

---

### 2️⃣ Convertir en .ico

**Site recommandé** : https://convertico.com/

1. Upload votre PNG
2. Cocher TOUTES les tailles (16, 32, 48, 64, 128, 256)
3. Convert → Download

---

### 3️⃣ Placer et compiler

```powershell
# 1. Placer le fichier
# Copier backlogmanager.ico vers:
# C:\Users\HanGP\BacklogManager\Images\backlogmanager.ico

# 2. Compiler
cd C:\Users\HanGP\BacklogManager
Get-Process BacklogManager -ErrorAction SilentlyContinue | Stop-Process -Force
$msbuild = "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
& $msbuild BacklogManager.sln /t:Build /p:Configuration=Debug

# 3. Lancer
cd bin\Debug
.\BacklogManager.exe
```

---

## ✅ L'icône devrait apparaître sur :
- ✅ Le fichier .exe dans l'explorateur
- ✅ La barre des tâches Windows
- ✅ Les titres des fenêtres (LoginWindow + MainWindow)

---

## 🔧 Si l'icône ne s'affiche pas

**Vider le cache Windows :**
```powershell
Stop-Process -Name explorer -Force
Remove-Item "$env:LOCALAPPDATA\IconCache.db" -ErrorAction SilentlyContinue
Start-Process explorer.exe
```

---

## 📖 Guide détaillé

Pour plus d'informations, consultez :
- `GUIDE_ICONE_COMPLETE.md` - Guide complet pas à pas
- `Images/ICON_INSTRUCTIONS.md` - Instructions détaillées

---

**Temps estimé** : 5-10 minutes  
**Difficulté** : ⭐ Facile
