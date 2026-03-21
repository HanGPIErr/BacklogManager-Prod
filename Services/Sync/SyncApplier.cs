using System;
using System.Data.SQLite;
using System.Text.Json;

namespace BacklogManager.Services.Sync
{
    /// <summary>
    /// Applique une SyncOperation reçue du NAS sur la DB locale (SqliteDatabase).
    ///
    /// Stratégie d'application :
    ///   - Les opérations *Upsert utilisent INSERT OR REPLACE (idempotent)
    ///   - Les opérations *Delete utilisent DELETE WHERE Id = X
    ///
    /// Stratégie de conflit :
    ///   - Last-Write-Wins basé sur TimestampUtc de l'opération
    ///   - Si la ligne locale a un LastModifiedUtc (via DateDerniereMaj) plus récent
    ///     que l'opération distante → conflit détecté, l'opération est quand même appliquée
    ///     (LWW) mais le conflit est tracé dans SyncApplied.
    ///
    /// L'application est toujours atomique (transaction SQLite).
    /// </summary>
    public class SyncApplier
    {
        private readonly LocalDatabaseFactory _localDb;
        private readonly string               _clientId;

        public SyncApplier(LocalDatabaseFactory localDb, string clientId)
        {
            _localDb  = localDb;
            _clientId = clientId;
        }

        /// <summary>
        /// Tente d'appliquer une opération distante sur la DB locale.
        /// Idempotent : si déjà appliquée, retourne silencieusement.
        /// </summary>
        public void Apply(SQLiteConnection conn, SQLiteTransaction tx, SyncOperation op)
        {
            // Idempotence : ne pas rejouer une opération déjà appliquée
            // Vérification inline (même connexion/tx) pour éviter BUSY et incohérences
            if (IsAlreadyAppliedInline(conn, tx, op.OperationId))
                return;

            // Ne pas rejouer nos propres opérations — PullSince les filtre déjà,
            // mais on garde ce double-check de sécurité.
            // NOTE : on utilise MarkAppliedInline (même conn/tx) pour éviter le SQLite BUSY.
            if (op.OriginClientId == _clientId)
            {
                _localDb.MarkAppliedInline(conn, tx, op);
                return;
            }

            bool hasConflict       = false;
            string conflictDetail  = null;

            try
            {
                switch (op.OperationType)
                {
                    case SyncOp.BacklogItemUpsert:
                        ApplyBacklogItemUpsert(conn, tx, op, ref hasConflict, ref conflictDetail);
                        break;
                    case SyncOp.BacklogItemDelete:
                        ApplySimpleDelete(conn, tx, "BacklogItems", op.EntityId, op.OriginClientId);
                        break;

                    case SyncOp.DemandeUpsert:
                        ApplyDemandeUpsert(conn, tx, op, ref hasConflict, ref conflictDetail);
                        break;
                    case SyncOp.DemandeDelete:
                        ApplySimpleDelete(conn, tx, "Demandes", op.EntityId, op.OriginClientId);
                        break;

                    case SyncOp.ProjetUpsert:
                        ApplyGenericJsonUpsert(conn, tx, "Projets", op);
                        break;

                    case SyncOp.SprintUpsert:
                        ApplyGenericJsonUpsert(conn, tx, "Sprints", op);
                        break;

                    case SyncOp.UtilisateurUpsert:
                        ApplyGenericJsonUpsert(conn, tx, "Utilisateurs", op);
                        break;
                    case SyncOp.UtilisateurDelete:
                        ApplySimpleDelete(conn, tx, "Utilisateurs", op.EntityId, op.OriginClientId);
                        break;

                    case SyncOp.RoleUpsert:
                        ApplyGenericJsonUpsert(conn, tx, "Roles", op);
                        break;

                    case SyncOp.EquipeUpsert:
                        ApplyGenericJsonUpsert(conn, tx, "Equipes", op);
                        break;
                    case SyncOp.EquipeDelete:
                        ApplySimpleDelete(conn, tx, "Equipes", op.EntityId, op.OriginClientId);
                        break;

                    case SyncOp.ProgrammeUpsert:
                        ApplyGenericJsonUpsert(conn, tx, "Programmes", op);
                        break;
                    case SyncOp.ProgrammeDelete:
                        ApplySimpleDelete(conn, tx, "Programmes", op.EntityId, op.OriginClientId);
                        break;

                    case SyncOp.CRAUpsert:
                        ApplyGenericJsonUpsert(conn, tx, "CRA", op);
                        break;
                    case SyncOp.CRADelete:
                        ApplySimpleDelete(conn, tx, "CRA", op.EntityId, op.OriginClientId);
                        break;

                    case SyncOp.CommentaireUpsert:
                        ApplyGenericJsonUpsert(conn, tx, "Commentaires", op);
                        break;

                    case SyncOp.NotificationUpsert:
                        ApplyGenericJsonUpsert(conn, tx, "Notifications", op);
                        break;
                    case SyncOp.NotificationDelete:
                        ApplySimpleDelete(conn, tx, "Notifications", op.EntityId, op.OriginClientId);
                        break;
                    case SyncOp.NotificationBulkUpdate:
                        ApplyNotificationBulkUpdate(conn, tx, op);
                        break;
                    case SyncOp.NotificationBulkDelete:
                        ApplyNotificationBulkDelete(conn, tx, op);
                        break;

                    case SyncOp.ConfigUpsert:
                        ApplyConfigUpsert(conn, tx, op);
                        break;

                    case SyncOp.PlanningVMUpsert:
                        ApplyGenericJsonUpsert(conn, tx, "PlanningVM", op);
                        break;
                    case SyncOp.PlanningVMDelete:
                        ApplySimpleDelete(conn, tx, "PlanningVM", op.EntityId, op.OriginClientId);
                        break;

                    case SyncOp.DemandeEchangeUpsert:
                        ApplyGenericJsonUpsert(conn, tx, "DemandeEchangeVM", op);
                        break;
                    case SyncOp.DemandeEchangeDelete:
                        ApplySimpleDelete(conn, tx, "DemandeEchangeVM", op.EntityId, op.OriginClientId);
                        break;

                    case SyncOp.PokerSessionUpsert:
                        ApplyGenericJsonUpsert(conn, tx, "PokerSessions", op);
                        break;
                    case SyncOp.PokerVoteUpsert:
                        ApplyGenericJsonUpsert(conn, tx, "PokerVotes", op);
                        break;

                    case SyncOp.DisponibiliteUpsert:
                        ApplyGenericJsonUpsert(conn, tx, "Disponibilites", op);
                        break;

                    case SyncOp.DevUpsert:
                        ApplyGenericJsonUpsert(conn, tx, "Devs", op);
                        break;

                    default:
                        LoggingService.Instance.LogWarning($"[SyncApplier] Type d'opération inconnu ignoré : {op.OperationType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                hasConflict   = true;
                conflictDetail = ex.Message;
                LoggingService.Instance.LogWarning(
                    $"[SyncApplier] Erreur application {op.OperationType} #{op.EntityId} : {ex.Message}");
            }

            // Toujours dans la même connexion/transaction que le apply → pas de BUSY
            _localDb.MarkAppliedInline(conn, tx, op, hasConflict, conflictDetail);
        }

        // ─── Appliers spécialisés ────────────────────────────────────────

        private void ApplyBacklogItemUpsert(SQLiteConnection conn, SQLiteTransaction tx,
            SyncOperation op, ref bool conflict, ref string conflictDetail)
        {
            // Vérifier si cette entité a un mapping local connu
            int? mappedLocalId = GetMappedLocalId(conn, tx, "BacklogItems", op.EntityId, op.OriginClientId ?? "");

            // Détection de conflit LWW uniquement si l'entité est déjà connue
            if (mappedLocalId.HasValue)
            {
                int localId = mappedLocalId.Value;
                using (var check = conn.CreateCommand())
                {
                    check.Transaction = tx;
                    check.CommandText = "SELECT DateDerniereMaj FROM BacklogItems WHERE Id = @id";
                    check.Parameters.AddWithValue("@id", localId);
                    var localTs = check.ExecuteScalar()?.ToString();
                    if (localTs != null &&
                        DateTime.TryParse(localTs, out var localDate) &&
                        localDate > op.TimestampUtc.ToLocalTime())
                    {
                        conflict       = true;
                        conflictDetail = $"Conflit LWW: local={localDate:HH:mm:ss} > distant={op.TimestampUtc:HH:mm:ss} — op distante ignorée";
                        return;
                    }
                }
            }

            // Déléguer à ApplyGenericJsonUpsert qui gère les collisions d'Id
            ApplyGenericJsonUpsert(conn, tx, "BacklogItems", op);
        }

        private void ApplyDemandeUpsert(SQLiteConnection conn, SQLiteTransaction tx,
            SyncOperation op, ref bool conflict, ref string conflictDetail)
        {
            // LWW : vérifier si une opération locale plus récente existe pour cette Demande.
            // Résoudre l'Id local d'abord (en cas de remapping suite à une collision).
            int? mappedLocalId = GetMappedLocalId(conn, tx, "Demandes", op.EntityId, op.OriginClientId ?? "");
            int localId = mappedLocalId ?? op.EntityId;

            using (var check = conn.CreateCommand())
            {
                check.Transaction = tx;
                check.CommandText = @"SELECT MAX(TimestampUtc) FROM SyncJournal
                    WHERE TableName = 'Demandes' AND EntityId = @id";
                check.Parameters.AddWithValue("@id", localId);
                var localTs = check.ExecuteScalar()?.ToString();
                if (localTs != null &&
                    DateTime.TryParse(localTs, null, System.Globalization.DateTimeStyles.RoundtripKind, out var localDate) &&
                    localDate > op.TimestampUtc)
                {
                    conflict       = true;
                    conflictDetail = $"Conflit LWW Demande: local={localDate:HH:mm:ss} > distant={op.TimestampUtc:HH:mm:ss} — op distante ignorée";
                    return;
                }
            }

            ApplyGenericJsonUpsert(conn, tx, "Demandes", op);
        }

        /// <summary>
        /// Applique une opération de configuration (clé/valeur).
        /// </summary>
        private void ApplyConfigUpsert(SQLiteConnection conn, SQLiteTransaction tx, SyncOperation op)
        {
            var doc = JsonDocument.Parse(op.PayloadJson);
            var r   = doc.RootElement;

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    INSERT OR REPLACE INTO Configuration (Key, Value, DateModification)
                    VALUES (@Key, @Value, @DateModification)";
                cmd.Parameters.AddWithValue("@Key",              GetStr(r, "key"));
                cmd.Parameters.AddWithValue("@Value",            GetStr(r, "value"));
                cmd.Parameters.AddWithValue("@DateModification", op.TimestampUtc.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Applique un DELETE en résolvant l'Id local via le mapping d'origine.
        /// </summary>
        private void ApplySimpleDelete(SQLiteConnection conn, SQLiteTransaction tx, string table, int remoteId, string originClientId)
        {
            // Résoudre l'Id local (peut différer du remoteId en cas de collision)
            int? mappedLocalId = GetMappedLocalId(conn, tx, table, remoteId, originClientId ?? "");
            int localId = mappedLocalId ?? remoteId;

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = $"DELETE FROM [{table}] WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", localId);
                cmd.ExecuteNonQuery();
            }

            // Nettoyer le mapping devenu obsolète
            if (mappedLocalId.HasValue)
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"DELETE FROM SyncEntityOrigin 
                        WHERE TableName = @tbl AND RemoteEntityId = @rid AND OriginClientId = @client";
                    cmd.Parameters.AddWithValue("@tbl", table);
                    cmd.Parameters.AddWithValue("@rid", remoteId);
                    cmd.Parameters.AddWithValue("@client", originClientId ?? "");
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Applique un upsert générique avec gestion des collisions d'IDs via mapping.
        ///
        /// Logique :
        ///   1. On cherche si un mapping (Table, RemoteId, Client) → LocalId existe déjà
        ///      → Si oui : mise à jour de l'entité au LocalId (même si ≠ RemoteId)
        ///   2. Sinon, on vérifie si une ligne existe déjà localement avec le RemoteId
        ///      → Si oui : collision d'Id → INSERT avec nouvel auto-Id + enregistrement du mapping
        ///      → Si non  : INSERT OR REPLACE classique + enregistrement mapping (RemoteId = LocalId)
        /// </summary>
        private void ApplyGenericJsonUpsert(SQLiteConnection conn, SQLiteTransaction tx, string table, SyncOperation op)
        {
            var doc = JsonDocument.Parse(op.PayloadJson);
            var root = doc.RootElement;

            // Récupérer la liste des colonnes de la table
            var columns = GetTableColumns(conn, tx, table);
            if (columns == null || columns.Length == 0)
            {
                LoggingService.Instance.LogWarning($"[SyncApplier] Table {table} non trouvée, opération ignorée.");
                return;
            }

            // Extraire l'Id distant depuis le JSON
            int remoteId = 0;
            if (root.TryGetProperty("id", out var idProp) || root.TryGetProperty("Id", out idProp))
            {
                if (idProp.ValueKind == JsonValueKind.Number)
                    idProp.TryGetInt32(out remoteId);
            }

            if (remoteId <= 0)
            {
                // Pas d'Id valide → INSERT OR REPLACE classique
                ApplyGenericJsonUpsertReplace(conn, tx, table, columns, root);
                return;
            }

            string originClient = op.OriginClientId ?? "";

            // ── Étape 1 : vérifier si un mapping existe déjà ──────────────
            int? mappedLocalId = GetMappedLocalId(conn, tx, table, remoteId, originClient);

            if (mappedLocalId.HasValue)
            {
                int localId = mappedLocalId.Value;
                if (localId == remoteId)
                {
                    // Pas de remapping — REPLACE classique
                    ApplyGenericJsonUpsertReplace(conn, tx, table, columns, root);
                }
                else
                {
                    // Entité remappée → UPDATE au localId en excluant la colonne Id
                    ApplyUpdateAtLocalId(conn, tx, table, columns, root, localId);
                }
                return;
            }

            // ── Étape 2 : pas de mapping → vérifier collision locale ──────
            bool localRowExists = false;
            using (var chk = conn.CreateCommand())
            {
                chk.Transaction = tx;
                chk.CommandText = $"SELECT COUNT(*) FROM [{table}] WHERE Id = @id";
                chk.Parameters.AddWithValue("@id", remoteId);
                localRowExists = Convert.ToInt64(chk.ExecuteScalar()) > 0;
            }

            if (!localRowExists)
            {
                // Aucune collision → INSERT OR REPLACE normal
                ApplyGenericJsonUpsertReplace(conn, tx, table, columns, root);
                RecordEntityOrigin(conn, tx, table, remoteId, originClient, remoteId);
                return;
            }

            // ── Étape 3 : collision d'Id → INSERT avec nouvel auto-Id ─────
            var colsWithoutId = System.Array.FindAll(columns, c => !c.Equals("Id", StringComparison.OrdinalIgnoreCase));
            var colListNoId   = string.Join(", ", colsWithoutId);
            var paramListNoId = string.Join(", ", System.Array.ConvertAll(colsWithoutId, c => "@" + c));

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = $"INSERT INTO [{table}] ({colListNoId}) VALUES ({paramListNoId}); SELECT last_insert_rowid();";

                foreach (var col in colsWithoutId)
                    BindColumnFromJson(cmd, col, root);

                try
                {
                    long newId = Convert.ToInt64(cmd.ExecuteScalar());
                    RecordEntityOrigin(conn, tx, table, remoteId, originClient, (int)newId);
                    LoggingService.Instance.LogInfo(
                        $"[SyncApplier] Collision Id={remoteId} dans {table}, remappé vers Id={newId} (client={originClient}).");
                }
                catch (Exception ex)
                {
                    // INSERT échoué (ex: UNIQUE constraint) — ne PAS fallback REPLACE (écraserait la donnée locale)
                    LoggingService.Instance.LogWarning(
                        $"[SyncApplier] Collision INSERT échoué pour {table} RemoteId={remoteId}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Recherche un mapping d'Id existant pour une entité distante.
        /// Retourne le LocalEntityId si trouvé, null sinon.
        /// </summary>
        private int? GetMappedLocalId(SQLiteConnection conn, SQLiteTransaction tx, string table, int remoteId, string originClient)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"SELECT LocalEntityId FROM SyncEntityOrigin 
                    WHERE TableName = @tbl AND RemoteEntityId = @rid AND OriginClientId = @client";
                cmd.Parameters.AddWithValue("@tbl", table);
                cmd.Parameters.AddWithValue("@rid", remoteId);
                cmd.Parameters.AddWithValue("@client", originClient);
                var result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                    return Convert.ToInt32(result);
                return null;
            }
        }

        /// <summary>
        /// Enregistre (ou met à jour) le mapping d'une entité distante → Id local.
        /// </summary>
        private void RecordEntityOrigin(SQLiteConnection conn, SQLiteTransaction tx,
            string table, int remoteId, string originClient, int localId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO SyncEntityOrigin 
                    (TableName, RemoteEntityId, OriginClientId, LocalEntityId) 
                    VALUES (@tbl, @rid, @client, @lid)";
                cmd.Parameters.AddWithValue("@tbl", table);
                cmd.Parameters.AddWithValue("@rid", remoteId);
                cmd.Parameters.AddWithValue("@client", originClient ?? "");
                cmd.Parameters.AddWithValue("@lid", localId);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// UPDATE d'une entité dont l'Id local diffère de l'Id distant (entité remappée).
        /// </summary>
        private void ApplyUpdateAtLocalId(SQLiteConnection conn, SQLiteTransaction tx,
            string table, string[] columns, JsonElement root, int localId)
        {
            var colsWithoutId = System.Array.FindAll(columns, c => !c.Equals("Id", StringComparison.OrdinalIgnoreCase));
            var setParts   = System.Array.ConvertAll(colsWithoutId, c => $"[{c}] = @{c}");
            var setClause  = string.Join(", ", setParts);

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = $"UPDATE [{table}] SET {setClause} WHERE Id = @_localId";

                foreach (var col in colsWithoutId)
                    BindColumnFromJson(cmd, col, root);

                cmd.Parameters.AddWithValue("@_localId", localId);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Exécute un INSERT OR REPLACE classique (cas normal sans conflit d'Id).
        /// </summary>
        private void ApplyGenericJsonUpsertReplace(SQLiteConnection conn, SQLiteTransaction tx, string table, string[] columns, JsonElement root)
        {
            var colList  = string.Join(", ", System.Array.ConvertAll(columns, c => $"[{c}]"));
            var paramList = string.Join(", ", System.Array.ConvertAll(columns, c => "@" + c));

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = $"INSERT OR REPLACE INTO [{table}] ({colList}) VALUES ({paramList})";

                foreach (var col in columns)
                    BindColumnFromJson(cmd, col, root);

                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Lie un paramètre SQL à partir d'une colonne SQLite et d'un JsonElement.
        /// Gère la correspondance casse (camelCase JSON ↔ PascalCase SQL).
        /// </summary>
        private void BindColumnFromJson(SQLiteCommand cmd, string col, JsonElement root)
        {
            string jsonKey = char.ToLower(col[0]) + col.Substring(1);
            if (root.TryGetProperty(jsonKey, out var prop))
                cmd.Parameters.AddWithValue("@" + col, GetJsonValue(prop));
            else if (root.TryGetProperty(col, out var prop2))
                cmd.Parameters.AddWithValue("@" + col, GetJsonValue(prop2));
            else
                cmd.Parameters.AddWithValue("@" + col, DBNull.Value);
        }

        // ─── Notifications bulk ──────────────────────────────────────

        private void ApplyNotificationBulkUpdate(SQLiteConnection conn, SQLiteTransaction tx, SyncOperation op)
        {
            var doc = JsonDocument.Parse(op.PayloadJson);
            var root = doc.RootElement;

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;

                if (op.EntityId > 0)
                {
                    // Mise à jour d'une seule notification (par Id)
                    cmd.CommandText = "UPDATE Notifications SET EstLue = 1 WHERE Id = @id";
                    cmd.Parameters.AddWithValue("@id", op.EntityId);
                }
                else
                {
                    // Bulk update : toutes les notifications
                    cmd.CommandText = "UPDATE Notifications SET EstLue = 1";
                }

                cmd.ExecuteNonQuery();
            }
        }

        private void ApplyNotificationBulkDelete(SQLiteConnection conn, SQLiteTransaction tx, SyncOperation op)
        {
            var doc = JsonDocument.Parse(op.PayloadJson);
            var root = doc.RootElement;
            string filter = GetStr(root, "filter");

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;

                if (filter == "EstLue=1")
                {
                    cmd.CommandText = "DELETE FROM Notifications WHERE EstLue = 1";
                }
                else if (filter != null && filter.StartsWith("DemandeEchangeVMId="))
                {
                    // Suppression ciblée par DemandeEchangeVMId
                    int demandeId;
                    if (int.TryParse(filter.Substring("DemandeEchangeVMId=".Length), out demandeId))
                    {
                        cmd.CommandText = "DELETE FROM Notifications WHERE DemandeEchangeVMId = @did";
                        cmd.Parameters.AddWithValue("@did", demandeId);
                    }
                    else
                    {
                        return; // filtre invalide, ignorer silencieusement
                    }
                }
                else
                {
                    // filter == "all" ou autre → supprimer toutes
                    cmd.CommandText = "DELETE FROM Notifications";
                }

                cmd.ExecuteNonQuery();
            }
        }

        // ─── Helpers ─────────────────────────────────────────────────────

        private string[] GetTableColumns(SQLiteConnection conn, SQLiteTransaction tx, string table)
        {
            try
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = $"SELECT name FROM pragma_table_info('{table.Replace("'", "''")}')";
                    var cols = new System.Collections.Generic.List<string>();
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) cols.Add(r.GetString(0));
                    return cols.ToArray();
                }
            }
            catch { return null; }
        }

        private object GetJsonValue(JsonElement el)
        {
            switch (el.ValueKind)
            {
                case JsonValueKind.Null:      return DBNull.Value;
                case JsonValueKind.True:      return 1;
                case JsonValueKind.False:     return 0;
                case JsonValueKind.Number:
                    if (el.TryGetInt64(out long l))   return l;
                    if (el.TryGetDouble(out double d)) return d;
                    return el.GetRawText();
                case JsonValueKind.String:    return el.GetString();
                default:                      return el.GetRawText();
            }
        }

        private string GetStr(JsonElement root, string key)
        {
            return root.TryGetProperty(key, out var p) ? p.GetString() : null;
        }

        private bool IsAlreadyAppliedInline(SQLiteConnection conn, SQLiteTransaction tx, string operationId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "SELECT COUNT(*) FROM SyncApplied WHERE OperationId = @id";
                cmd.Parameters.AddWithValue("@id", operationId);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }
    }
}
