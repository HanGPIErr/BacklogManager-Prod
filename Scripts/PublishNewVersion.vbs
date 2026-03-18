' =====================================
' ORBITT - Publication d'une nouvelle version
' =====================================

Option Explicit

Dim objShell, objFSO
Dim strScriptPath, strVersion, strChangelog, strMandatory
Dim intMandatory

Set objShell = CreateObject("WScript.Shell")
Set objFSO = CreateObject("Scripting.FileSystemObject")

' Chemin du script PowerShell
strScriptPath = objFSO.GetParentFolderName(WScript.ScriptFullName)

' Demander le numéro de version
strVersion = InputBox("Entrez le numéro de version (ex: 0.3.0):", "Nouvelle Version", "0.3.0")
If strVersion = "" Then
    WScript.Quit
End If

' Demander le changelog
strChangelog = InputBox("Entrez les modifications (changelog):", "Changelog", "Correctifs et améliorations")
If strChangelog = "" Then
    strChangelog = "Nouvelle version " & strVersion
End If

' Demander si c'est une version obligatoire
intMandatory = MsgBox("Cette version est-elle OBLIGATOIRE ?" & vbCrLf & vbCrLf & _
                      "Oui = Les utilisateurs doivent installer" & vbCrLf & _
                      "Non = Mise à jour optionnelle", _
                      vbQuestion + vbYesNo, "Version obligatoire ?")

If intMandatory = vbYes Then
    strMandatory = "$true"
Else
    strMandatory = "$false"
End If

' Confirmation
Dim strMessage
strMessage = "Publication de la version:" & vbCrLf & vbCrLf & _
             "Version: " & strVersion & vbCrLf & _
             "Changelog: " & strChangelog & vbCrLf & _
             "Obligatoire: " & IIf(strMandatory = "$true", "Oui", "Non") & vbCrLf & vbCrLf & _
             "Continuer ?"

If MsgBox(strMessage, vbQuestion + vbYesNo, "Confirmation") = vbNo Then
    WScript.Quit
End If

' Construire la commande PowerShell
Dim strCommand
strCommand = "powershell.exe -NoProfile -ExecutionPolicy Bypass -Command """ & _
             "cd '" & strScriptPath & "'; " & _
             ".\PublishRelease.ps1 -Version '" & strVersion & "' " & _
             "-Changelog '" & Replace(strChangelog, "'", "''") & "' " & _
             "-Mandatory " & strMandatory & """"

' Exécuter avec la fenêtre visible
objShell.Run strCommand, 1, True

' Message de fin
MsgBox "Publication terminée !" & vbCrLf & vbCrLf & _
       "Version " & strVersion & " publiée avec succès.", _
       vbInformation, "Terminé"

WScript.Quit

' Fonction IIf (VBScript n'a pas IIf natif)
Function IIf(condition, trueValue, falseValue)
    If condition Then
        IIf = trueValue
    Else
        IIf = falseValue
    End If
End Function
