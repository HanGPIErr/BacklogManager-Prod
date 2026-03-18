using System;

namespace BacklogManager.Services.Sync
{
    /// <summary>
    /// Représente l'index persisté sur le NAS indiquant le dernier snapshot validé
    /// et le sequence offset à partir duquel rejouer les opérations.
    /// Fichier: {NasSyncPath}/manifest.json
    /// </summary>
    public class SyncManifest
    {
        /// <summary>Version de format (pour évolutions futures).</summary>
        public int    FormatVersion        { get; set; } = 1;

        /// <summary>Nom du fichier snapshot actif (relatif au dossier snapshots/).</summary>
        public string SnapshotFileName     { get; set; }

        /// <summary>Timestamp UTC du snapshot.</summary>
        public DateTime SnapshotTimestampUtc { get; set; }

        /// <summary>
        /// Toutes les opérations avec NasFileName >= cette valeur doivent être
        /// rejouées pour aller du snapshot à l'état courant.
        /// (correspond au NasFileName du premier op APRÈS le snapshot)
        /// </summary>
        public string OpsAfterSnapshot    { get; set; }

        /// <summary>Client qui a produit ce snapshot (debug).</summary>
        public string CreatedByClientId   { get; set; }

        /// <summary>Date de création du manifest.</summary>
        public DateTime CreatedAtUtc      { get; set; } = DateTime.UtcNow;

        /// <summary>Nombre approximatif d'opérations au moment du snapshot (pour info).</summary>
        public int    OperationCountAtSnapshot { get; set; }
    }
}
