' ========================================================
' BacklogManager - Lanceur avec mapping automatique
' ========================================================

Set WshShell = CreateObject("WScript.Shell")
Set FSO = CreateObject("Scripting.FileSystemObject")

' Chemin du script
ScriptPath = FSO.GetParentFolderName(WScript.ScriptFullName)
PowerShellScript = ScriptPath & "\Start-BacklogManager.ps1"

' === CONFIGURATION RESEAU ===
' Décommenter et modifier la ligne ci-dessous pour activer le mapping automatique
' NetworkPath = "\\serveur\partage\Data"
NetworkPath = ""
' =============================

' Construire la commande PowerShell
If NetworkPath <> "" Then
    Command = "powershell.exe -ExecutionPolicy Bypass -File """ & PowerShellScript & """ -NetworkPath """ & NetworkPath & """"
Else
    Command = "powershell.exe -ExecutionPolicy Bypass -File """ & PowerShellScript & """"
End If

' Lancer en mode caché (sans fenêtre PowerShell)
WshShell.Run Command, 0, False
