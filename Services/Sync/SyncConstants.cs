using System;

namespace BacklogManager.Services.Sync
{
    /// <summary>
    /// Constantes et noms d'opérations pour le moteur de synchronisation local-first.
    /// Toute entité métier modifiée produit une SyncOperation identifiée par ces constantes.
    /// </summary>
    public static class SyncOp
    {
        // Backlog
        public const string BacklogItemUpsert  = "BacklogItem.Upsert";
        public const string BacklogItemDelete  = "BacklogItem.Delete";

        // Demandes
        public const string DemandeUpsert      = "Demande.Upsert";
        public const string DemandeDelete      = "Demande.Delete";

        // Projets
        public const string ProjetUpsert       = "Projet.Upsert";

        // Sprints
        public const string SprintUpsert       = "Sprint.Upsert";

        // Utilisateurs
        public const string UtilisateurUpsert  = "Utilisateur.Upsert";
        public const string UtilisateurDelete  = "Utilisateur.Delete";

        // Roles
        public const string RoleUpsert         = "Role.Upsert";

        // Equipes
        public const string EquipeUpsert       = "Equipe.Upsert";
        public const string EquipeDelete       = "Equipe.Delete";

        // Programmes
        public const string ProgrammeUpsert    = "Programme.Upsert";
        public const string ProgrammeDelete    = "Programme.Delete";

        // CRA
        public const string CRAUpsert          = "CRA.Upsert";
        public const string CRADelete          = "CRA.Delete";

        // Commentaires
        public const string CommentaireUpsert  = "Commentaire.Upsert";

        // Notifications
        public const string NotificationUpsert     = "Notification.Upsert";
        public const string NotificationDelete     = "Notification.Delete";
        public const string NotificationBulkUpdate = "Notification.BulkUpdate";
        public const string NotificationBulkDelete = "Notification.BulkDelete";

        // Configuration
        public const string ConfigUpsert       = "Config.Upsert";

        // Planning VM
        public const string PlanningVMUpsert   = "PlanningVM.Upsert";
        public const string PlanningVMDelete   = "PlanningVM.Delete";

        // Demandes Echange VM
        public const string DemandeEchangeUpsert = "DemandeEchange.Upsert";
        public const string DemandeEchangeDelete  = "DemandeEchange.Delete";

        // Poker
        public const string PokerSessionUpsert = "PokerSession.Upsert";
        public const string PokerVoteUpsert    = "PokerVote.Upsert";

        // Disponibilites
        public const string DisponibiliteUpsert = "Disponibilite.Upsert";
    }

    /// <summary>
    /// Noms de fichiers / sous-dossiers utilisés sur le NAS.
    /// Structure NAS :
    ///   {NasSyncPath}/
    ///     ops/             ← fichiers .syncop (un par opération publiée)
    ///     snapshots/       ← fichiers .snapshot (SQLite compacté)
    ///     leases/          ← fichiers .lease (lock temporaire pour compaction)
    ///     manifest.json    ← index du dernier snapshot validé
    /// </summary>
    public static class NasLayout
    {
        public const string OpsFolder       = "ops";
        public const string SnapshotsFolder = "snapshots";
        public const string LeasesFolder    = "leases";
        public const string ManifestFile    = "manifest.json";

        // Extension des fichiers opération
        public const string OpExtension     = ".syncop";

        // Extension des snapshots
        public const string SnapshotExtension = ".snapshot";

        // Extension des leases
        public const string LeaseExtension  = ".lease";
    }
}
