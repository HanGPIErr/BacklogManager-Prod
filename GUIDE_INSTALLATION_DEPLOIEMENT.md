# Guide d'Installation et Déploiement - BacklogManager

## 📋 Table des matières
1. [Installation depuis GitHub](#installation-depuis-github)
2. [Changements à effectuer sur le nouveau PC](#changements-à-effectuer)
3. [Configuration pour environnement partagé](#configuration-environnement-partagé)
4. [Raccourci bureau automatique](#raccourci-bureau)
5. [Authentification automatique Windows](#authentification-windows)

---

## 📦 Installation depuis GitHub

### Étape 1 : Télécharger le projet

**Option A - Via Git (recommandé si Git est installé) :**
```powershell
# Ouvrir PowerShell dans le dossier Documents
cd "$env:USERPROFILE\Documents"

# Cloner le repository
git clone https://github.com/HanGPIErr/BacklogManager.git
```

**Option B - Télécharger le ZIP :**
1. Aller sur https://github.com/HanGPIErr/BacklogManager
2. Cliquer sur le bouton vert **"Code"**
3. Sélectionner **"Download ZIP"**
4. Extraire le ZIP dans `C:\Users\[VotreNom]\Documents\BacklogManager`

### Étape 2 : Vérifier les prérequis

Vérifier que **.NET Framework 4.8** est installé :
```powershell
# Dans PowerShell
Get-ChildItem 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\' | Get-ItemPropertyValue -Name Release
```
- Si le résultat est **>= 528040**, .NET 4.8 est installé ✅
- Sinon, télécharger depuis : https://dotnet.microsoft.com/download/dotnet-framework/net48

---

## 🔧 Changements à effectuer sur le nouveau PC

### 1. Chemins de base de données

**⚠️ IMPORTANT** : Le projet utilise actuellement des chemins **absolus** pour la base de données SQLite.

#### Fichier à modifier : `Services/SqliteDatabase.cs`

**Ligne à trouver (~ligne 14) :**
```csharp
private readonly string _connectionString = @"Data Source=C:\Users\HanGP\BacklogManager\backlog.db;Version=3;";
```

**Modifier selon votre installation :**

**A) Test en local (Documents) :**
```csharp
private readonly string _connectionString = $@"Data Source={Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\BacklogManager\backlog.db;Version=3;";
```

**B) Déploiement sur SharedDrive (ex: S:\) :**
```csharp
private readonly string _connectionString = @"Data Source=S:\BacklogManager\backlog.db;Version=3;";
```

**C) Chemin relatif (à côté de l'exe - recommandé pour déploiement) :**
```csharp
private readonly string _connectionString = $@"Data Source={AppDomain.CurrentDomain.BaseDirectory}backlog.db;Version=3;";
```

### 2. Images et ressources

Les images sont déjà **embedded** dans l'exe via le `.csproj`, donc **aucun changement nécessaire** ✅

---

## 🌐 Configuration Environnement Partagé

### Structure recommandée sur SharedDrive

```
S:\BacklogManager\                    (ou autre lecteur réseau)
├── BacklogManager.exe                 (Application compilée)
├── backlog.db                         (Base de données SQLite partagée)
├── System.Data.SQLite.dll             (DLL de dépendance)
├── x64\SQLite.Interop.dll            (DLL native 64-bit)
├── x86\SQLite.Interop.dll            (DLL native 32-bit)
└── (autres DLL de System.Text.Json, etc.)
```

### Compilation pour déploiement

**Dans PowerShell, à la racine du projet :**
```powershell
# Compiler en mode Release
& "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe" BacklogManager.sln /t:Rebuild /p:Configuration=Release

# Les fichiers seront dans : bin\Release\
```

### Copier vers SharedDrive

```powershell
# Exemple de copie vers S:\
Copy-Item -Path "bin\Release\*" -Destination "S:\BacklogManager\" -Recurse -Force
```

---

## 🖥️ Raccourci Bureau Automatique

### Modification à faire dans le code

#### Fichier à modifier : `App.xaml.cs`

**Ajouter cette méthode dans la classe `App` :**

```csharp
using System.IO;
using IWshRuntimeLibrary; // Ajouter référence COM "Windows Script Host Object Model"

protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);
    
    // Créer raccourci bureau au premier lancement
    CreerRaccourciDesktop();
}

private void CreerRaccourciDesktop()
{
    try
    {
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string shortcutPath = Path.Combine(desktopPath, "BacklogManager.lnk");
        
        // Ne créer que si le raccourci n'existe pas déjà
        if (!System.IO.File.Exists(shortcutPath))
        {
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string workingDirectory = Path.GetDirectoryName(exePath);
            
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = exePath;
            shortcut.WorkingDirectory = workingDirectory;
            shortcut.Description = "BacklogManager - Gestion de projets Agile";
            shortcut.IconLocation = exePath + ",0";
            shortcut.Save();
        }
    }
    catch
    {
        // Ignorer les erreurs silencieusement (permissions, etc.)
    }
}
```

**⚠️ Ajouter la référence COM :**
1. Clic droit sur le projet → **Ajouter** → **Référence**
2. Onglet **COM** → Cocher **"Windows Script Host Object Model"**
3. OK

---

## 🔐 Authentification Automatique Windows

### Modification actuelle nécessaire

#### Fichier à modifier : `Services/AuthenticationService.cs`

**Méthode à modifier (~ligne 20) :**

```csharp
public bool Login(string username, string password)
{
    // Mode AUTO : Authentification Windows automatique
    if (string.IsNullOrEmpty(username))
    {
        string windowsUsername = Environment.UserName;
        var user = _db.GetAllUtilisateurs().FirstOrDefault(u => 
            u.Username.Equals(windowsUsername, StringComparison.OrdinalIgnoreCase));
        
        if (user != null)
        {
            CurrentUser = user;
            return true;
        }
        
        // Créer automatiquement l'utilisateur s'il n'existe pas
        var newUser = new Utilisateur
        {
            Username = windowsUsername,
            Nom = windowsUsername,
            Prenom = "",
            Email = $"{windowsUsername}@company.local",
            IsAdmin = false, // Premier utilisateur = admin, autres = dev
            DateCreation = DateTime.Now
        };
        
        _db.SaveUtilisateur(newUser);
        CurrentUser = newUser;
        return true;
    }
    
    // Mode MANUEL : Authentification classique (pour admin)
    var authenticatedUser = _db.GetAllUtilisateurs().FirstOrDefault(u => 
        u.Username == username && u.MotDePasse == password);
    
    if (authenticatedUser != null)
    {
        CurrentUser = authenticatedUser;
        return true;
    }
    
    return false;
}
```

#### Fichier à modifier : `Views/LoginWindow.xaml.cs`

**Dans la méthode `Window_Loaded` (~ligne 15) :**

```csharp
private void Window_Loaded(object sender, RoutedEventArgs e)
{
    // Authentification automatique Windows
    if (_authService.Login(null, null))
    {
        // Connexion réussie avec le compte Windows
        DialogResult = true;
        Close();
    }
    // Sinon, afficher la fenêtre de login manuel
}
```

---

## 📝 Checklist de déploiement

### Sur le PC de développement (chez toi)

- [ ] Modifier `SqliteDatabase.cs` avec chemin relatif ou SharedDrive
- [ ] Ajouter méthode `CreerRaccourciDesktop()` dans `App.xaml.cs`
- [ ] Ajouter référence COM "Windows Script Host Object Model"
- [ ] Modifier `AuthenticationService.cs` pour login Windows auto
- [ ] Modifier `LoginWindow.xaml.cs` pour appel auto
- [ ] Compiler en Release
- [ ] Tester en local dans Documents
- [ ] Commit & Push sur GitHub

### Sur le nouveau PC (au bureau)

- [ ] Télécharger depuis GitHub (ZIP ou Git clone)
- [ ] Vérifier .NET Framework 4.8 installé
- [ ] Si besoin, compiler avec MSBuild
- [ ] Copier `bin\Release\*` vers `S:\BacklogManager\`
- [ ] Lancer `BacklogManager.exe` depuis SharedDrive
- [ ] Vérifier raccourci bureau créé automatiquement
- [ ] Tester login automatique avec compte Windows

---

## 🚀 Commandes rapides

### Compilation rapide
```powershell
cd "C:\Users\[VotreNom]\Documents\BacklogManager"
& "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe" BacklogManager.sln /t:Rebuild /p:Configuration=Release
```

### Déploiement vers SharedDrive
```powershell
$source = "bin\Release\*"
$destination = "S:\BacklogManager"
Copy-Item -Path $source -Destination $destination -Recurse -Force
```

### Test en local
```powershell
cd bin\Release
.\BacklogManager.exe
```

---

## ⚠️ Problèmes courants

### Erreur "Could not load file SQLite.Interop.dll"
- **Solution** : Copier les dossiers `x64\` et `x86\` avec les DLL natives

### Erreur "Database is locked"
- **Solution** : Sur SharedDrive, SQLite peut avoir des problèmes de verrouillage réseau
- **Alternative** : Utiliser JSON Database (changer dans `InitializationService.cs`)

### Raccourci bureau non créé
- **Cause** : Permissions insuffisantes
- **Solution** : Créer manuellement ou demander droits admin

### Login Windows ne fonctionne pas
- **Vérifier** : Le username Windows correspond à un utilisateur en base
- **Solution** : Créer l'utilisateur manuellement via l'interface admin

---

## 📞 Support

En cas de problème, vérifier :
1. Version .NET Framework 4.8 installée
2. Toutes les DLL présentes dans le dossier
3. Chemin de la base de données correct
4. Permissions sur le SharedDrive

**Logs d'erreur** : Ajouter un try-catch dans `App.xaml.cs` pour capturer les exceptions au démarrage.
