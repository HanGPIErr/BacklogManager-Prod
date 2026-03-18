using System;

namespace BacklogManager.Services.Sync
{
    /// <summary>
    /// Représente une opération de modification enregistrée dans le journal local
    /// puis publiée sur le NAS pour propagation aux autres clients.
    ///
    /// Format de fichier NAS : binaire Newtonsoft / System.Text.Json compact.
    /// Chaque SyncOperation est immutable une fois créée (append-only log).
    /// L'idempotence est garantie par OperationId (GUID unique par opération).
    /// </summary>
    public class SyncOperation
    {
        // ── Identité ────────────────────────────────────────────────────
        /// <summary>Identifiant unique de l'opération. Sert de clé d'idempotence.</summary>
        public string OperationId      { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Numéro de séquence LOCAL (LSN). Monotonicaly increasing par client.</summary>
        public long   LocalSequence    { get; set; }

        // ── Source ──────────────────────────────────────────────────────
        /// <summary>Identifiant du client qui a produit cette opération (nom machine + utilisateur).</summary>
        public string OriginClientId   { get; set; }

        /// <summary>Nom de l'utilisateur ayant déclenché l'opération.</summary>
        public string OriginUsername   { get; set; }

        /// <summary>Timestamp UTC de la modification côté client source.</summary>
        public DateTime TimestampUtc   { get; set; } = DateTime.UtcNow;

        // ── Opération ────────────────────────────────────────────────────
        /// <summary>Type d'opération. Voir constantes SyncOp.*</summary>
        public string OperationType    { get; set; }

        /// <summary>Nom de la table SQLite cible.</summary>
        public string TableName        { get; set; }

        /// <summary>ID de la ligne affectée (0 pour INSERT dont l'ID est dans Payload).</summary>
        public int    EntityId         { get; set; }

        /// <summary>
        /// Payload JSON complet de l'entité après modification.
        /// Pour les deletes : contient uniquement {"Id": X, "IsDeleted": true}.
        /// </summary>
        public string PayloadJson      { get; set; }

        // ── Durabilité ───────────────────────────────────────────────────
        /// <summary>True si cette opération a été publiée sur le NAS.</summary>
        public bool   IsPublished      { get; set; }

        /// <summary>Timestamp UTC de publication effective sur le NAS.</summary>
        public DateTime? PublishedAtUtc { get; set; }

        /// <summary>True si cette opération a été appliquée sur la DB locale.</summary>
        public bool   IsApplied        { get; set; }

        /// <summary>Date d'application locale (pour tracé).</summary>
        public DateTime? AppliedAtUtc  { get; set; }

        // ── Conflits ─────────────────────────────────────────────────────
        /// <summary>True si un conflit a été détecté lors de l'application.</summary>
        public bool   HasConflict      { get; set; }

        /// <summary>Description du conflit.</summary>
        public string ConflictDetail   { get; set; }

        // ── Helpers ──────────────────────────────────────────────────────
        /// <summary>
        /// Nom du fichier sur le NAS.
        /// Format: {TimestampUtc:yyyyMMddHHmmssffff}_{OriginClientId}_{OperationId}.syncop
        /// Tri naturel par timestamp garantit l'ordre de replay.
        /// </summary>
        public string NasFileName =>
            $"{TimestampUtc:yyyyMMddHHmmssffff}_{SanitizeForFilename(OriginClientId)}_{OperationId}{NasLayout.OpExtension}";

        private static string SanitizeForFilename(string s)
        {
            if (string.IsNullOrEmpty(s)) return "unknown";
            foreach (var c in System.IO.Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            return s.Length > 32 ? s.Substring(0, 32) : s;
        }
    }
}
