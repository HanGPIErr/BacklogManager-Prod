using System;
using System.IO;
using System.Text;

namespace BacklogManager.Services.Sync
{
    /// <summary>
    /// Gère les leases (verrous temporaires) sur le NAS pour élire le client
    /// responsable de la compaction.
    ///
    /// Mécanisme :
    ///   1. Le client tente d'acquérir un lease en écrivant un fichier
    ///      {leasesPath}/compaction.lease (via write-then-check).
    ///   2. Si le fichier existe déjà et n'est pas expiré → rejet.
    ///   3. Si le fichier existe mais est expiré (stale) → suppression + nouvelle tentative.
    ///   4. Le lease a une durée de vie de {LeaseTtlSeconds} secondes.
    ///   5. Le client renouvelle le lease pendant la compaction.
    ///   6. Libération explicite à la fin.
    ///
    /// Fichier lease : JSON { "ClientId": "...", "ExpiresAtUtc": "...", "PID": 1234 }
    /// </summary>
    public class LeaseManager
    {
        private const int DefaultLeaseTtlSeconds = 120;  // 2 minutes max pour une compaction
        private const string CompactionLeaseName  = "compaction";

        private readonly string _leasesPath;
        private readonly string _clientId;
        private readonly int    _ttlSeconds;

        private DateTime? _leaseExpiresAt;

        public LeaseManager(string leasesPath, string clientId, int leaseTtlSeconds = DefaultLeaseTtlSeconds)
        {
            _leasesPath = leasesPath;
            _clientId   = clientId;
            _ttlSeconds = leaseTtlSeconds;
        }

        /// <summary>
        /// Tente d'acquérir le lease de compaction.
        /// </summary>
        /// <returns>True si le lease a été acquis par ce client.</returns>
        public bool TryAcquireCompactionLease()
        {
            try
            {
                string leasePath = Path.Combine(_leasesPath, CompactionLeaseName + NasLayout.LeaseExtension);
                string tmpPath   = leasePath + ".tmp." + _clientId;

                // Examiner le lease existant
                if (File.Exists(leasePath))
                {
                    var existing = ReadLease(leasePath);
                    if (existing != null && DateTime.UtcNow < existing.ExpiresAtUtc)
                    {
                        // Lease valide détenu par un autre client
                        LoggingService.Instance.LogInfo(
                            $"[LeaseManager] Lease compaction détenu par {existing.ClientId}, expire {existing.ExpiresAtUtc:HH:mm:ss}");
                        return false;
                    }

                    // Lease expiré (stale) → on peut le prendre
                    try { File.Delete(leasePath); } catch { }
                }

                // Écrire notre lease
                var lease = new LeaseEntry
                {
                    ClientId     = _clientId,
                    ExpiresAtUtc = DateTime.UtcNow.AddSeconds(_ttlSeconds),
                    PID          = System.Diagnostics.Process.GetCurrentProcess().Id
                };

                string json = System.Text.Json.JsonSerializer.Serialize(lease);
                File.WriteAllText(tmpPath, json, Encoding.UTF8);
                File.Move(tmpPath, leasePath);  // atomique sur NTFS

                // Double-check : relire pour confirmer qu'on a bien notre entrée
                // (race condition très improbable sur NAS d'entreprise, mais on vérifie)
                System.Threading.Thread.Sleep(50); // petit délai pour laisser le NAS flush
                var readBack = ReadLease(leasePath);
                if (readBack == null || readBack.ClientId != _clientId)
                {
                    LoggingService.Instance.LogWarning("[LeaseManager] Race condition détectée sur le lease.");
                    return false;
                }

                _leaseExpiresAt = lease.ExpiresAtUtc;
                LoggingService.Instance.LogInfo($"[LeaseManager] Lease compaction acquis jusqu'à {lease.ExpiresAtUtc:HH:mm:ss}");
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning($"[LeaseManager] Impossible d'acquérir le lease : {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Renouvelle le lease actif (appelé pendant une longue opération de compaction).
        /// </summary>
        public void RenewLease()
        {
            try
            {
                string leasePath = Path.Combine(_leasesPath, CompactionLeaseName + NasLayout.LeaseExtension);
                if (!File.Exists(leasePath)) return;

                var existing = ReadLease(leasePath);
                if (existing == null || existing.ClientId != _clientId) return;

                existing.ExpiresAtUtc = DateTime.UtcNow.AddSeconds(_ttlSeconds);
                string json = System.Text.Json.JsonSerializer.Serialize(existing);
                File.WriteAllText(leasePath, json, Encoding.UTF8);
                _leaseExpiresAt = existing.ExpiresAtUtc;
            }
            catch { /* non-fatal */ }
        }

        /// <summary>
        /// Libère explicitement le lease.
        /// </summary>
        public void ReleaseLease()
        {
            try
            {
                string leasePath = Path.Combine(_leasesPath, CompactionLeaseName + NasLayout.LeaseExtension);
                if (!File.Exists(leasePath)) return;

                var existing = ReadLease(leasePath);
                if (existing?.ClientId == _clientId)
                {
                    File.Delete(leasePath);
                    LoggingService.Instance.LogInfo("[LeaseManager] Lease compaction libéré.");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning($"[LeaseManager] Erreur libération lease : {ex.Message}");
            }
            finally
            {
                _leaseExpiresAt = null;
            }
        }

        /// <summary>
        /// Indique si notre lease est encore valide (non expiré).
        /// </summary>
        public bool IsLeaseValid() =>
            _leaseExpiresAt.HasValue && DateTime.UtcNow < _leaseExpiresAt.Value;

        // ─── Helpers ─────────────────────────────────────────────────────

        private LeaseEntry ReadLease(string path)
        {
            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                return System.Text.Json.JsonSerializer.Deserialize<LeaseEntry>(json);
            }
            catch { return null; }
        }

        private class LeaseEntry
        {
            public string   ClientId     { get; set; }
            public DateTime ExpiresAtUtc { get; set; }
            public int      PID          { get; set; }
        }
    }
}
