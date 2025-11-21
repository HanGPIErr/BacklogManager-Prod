' =====================================
' BacklogManager - Installation VBS
' =====================================

Option Explicit

Dim objShell, objFSO, objWshShell
Dim strScriptPath, strSourcePath, strDestPath
Dim strExePath, intResult

Set objShell = CreateObject("Shell.Application")
Set objFSO = CreateObject("Scripting.FileSystemObject")
Set objWshShell = CreateObject("WScript.Shell")

' Chemin du script et destination
strScriptPath = objFSO.GetParentFolderName(WScript.ScriptFullName)
strSourcePath = strScriptPath
strDestPath = "C:\SGI_SUPPORT\APPLICATIONS\BacklogManager"
strExePath = strDestPath & "\BacklogManager.exe"

' Message de bienvenue
intResult = MsgBox("BacklogManager - Installation" & vbCrLf & vbCrLf & _
                   "L'application sera installée dans :" & vbCrLf & _
                   strDestPath & vbCrLf & vbCrLf & _
                   "Voulez-vous continuer ?", vbQuestion + vbYesNo, "Installation")

If intResult = vbNo Then
    WScript.Quit
End If

' Créer le dossier de destination (avec dossiers parents)
If Not objFSO.FolderExists(strDestPath) Then
    ' Créer tous les dossiers parents nécessaires
    CreateFolderRecursive strDestPath
End If

' Copier tous les fichiers
On Error Resume Next
CopyFolder strSourcePath, strDestPath

' Créer le dossier data
Dim strDataPath
strDataPath = strDestPath & "\data"
If Not objFSO.FolderExists(strDataPath) Then
    objFSO.CreateFolder(strDataPath)
End If

' Créer le raccourci bureau
CreateDesktopShortcut strExePath

' Message de fin
MsgBox "Installation terminée avec succès !" & vbCrLf & vbCrLf & _
       "BacklogManager va maintenant démarrer.", vbInformation, "Installation"

' Lancer l'application
objWshShell.Run """" & strExePath & """", 1, False

WScript.Quit

' =====================================
' Fonctions
' =====================================

Sub CopyFolder(strSource, strDest)
    Dim objFolder, objFile, objSubFolder
    
    ' Copier tous les fichiers du dossier courant
    Set objFolder = objFSO.GetFolder(strSource)
    
    For Each objFile In objFolder.Files
        ' Ne pas copier le script VBS lui-même ni les fichiers temporaires
        If LCase(objFSO.GetExtensionName(objFile.Name)) <> "vbs" And _
           LCase(objFile.Name) <> "install.ps1" And _
           LCase(objFile.Name) <> "uninstall.ps1" Then
            objFSO.CopyFile objFile.Path, strDest & "\", True
        End If
    Next
    
    ' Copier les sous-dossiers (x64, x86)
    For Each objSubFolder In objFolder.SubFolders
        If LCase(objSubFolder.Name) <> "release" And _
           LCase(objSubFolder.Name) <> "debug" Then
            Dim strNewDest
            strNewDest = strDest & "\" & objSubFolder.Name
            If Not objFSO.FolderExists(strNewDest) Then
                objFSO.CreateFolder(strNewDest)
            End If
            CopySubFolder objSubFolder.Path, strNewDest
        End If
    Next
End Sub

Sub CopySubFolder(strSource, strDest)
    Dim objFolder, objFile, objSubFolder
    
    Set objFolder = objFSO.GetFolder(strSource)
    
    ' Copier tous les fichiers
    For Each objFile In objFolder.Files
        objFSO.CopyFile objFile.Path, strDest & "\", True
    Next
    
    ' Copier les sous-dossiers récursivement
    For Each objSubFolder In objFolder.SubFolders
        Dim strNewDest
        strNewDest = strDest & "\" & objSubFolder.Name
        If Not objFSO.FolderExists(strNewDest) Then
            objFSO.CreateFolder(strNewDest)
        End If
        CopySubFolder objSubFolder.Path, strNewDest
    Next
End Sub

Sub CreateDesktopShortcut(strTargetPath)
    Dim strDesktop, strShortcutPath, objShortcut
    
    ' Bureau public (tous les utilisateurs)
    strDesktop = objWshShell.SpecialFolders("AllUsersDesktop")
    If strDesktop = "" Then
        ' Si pas d'accès au bureau public, utiliser le bureau de l'utilisateur
        strDesktop = objWshShell.SpecialFolders("Desktop")
    End If
    
    strShortcutPath = strDesktop & "\BacklogManager.lnk"
    
    Set objShortcut = objWshShell.CreateShortcut(strShortcutPath)
    objShortcut.TargetPath = strTargetPath
    objShortcut.WorkingDirectory = objFSO.GetParentFolderName(strTargetPath)
    objShortcut.Description = "BacklogManager - Gestion de projets Agile"
    objShortcut.IconLocation = strTargetPath & ",0"
    objShortcut.Save
End Sub

' Créer les dossiers récursivement (parents inclus)
Sub CreateFolderRecursive(strPath)
    Dim strParent
    
    If objFSO.FolderExists(strPath) Then
        Exit Sub
    End If
    
    strParent = objFSO.GetParentFolderName(strPath)
    
    If strParent <> "" And Not objFSO.FolderExists(strParent) Then
        CreateFolderRecursive strParent
    End If
    
    objFSO.CreateFolder strPath
End Sub

