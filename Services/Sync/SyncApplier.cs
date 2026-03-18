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
            if (_localDb.IsAlreadyApplied(op.OperationId))
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
                        ApplySimpleDelete(conn, tx, "BacklogItems", op.EntityId);
                        break;

                    case SyncOp.DemandeUpsert:
                        ApplyDemandeUpsert(conn, tx, op, ref hasConflict, ref conflictDetail);
                        break;
                    case SyncOp.DemandeDelete:
                        ApplySimpleDelete(conn, tx, "Demandes", op.EntityId);
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
                        ApplySimpleDelete(conn, tx, "Utilisateurs", op.EntityId);
                        break;

                    case SyncOp.RoleUpsert:
                        ApplyGenericJsonUpsert(conn, tx, "Roles", op);
                        break;

                    case SyncOp.EquipeUpsert:
                        ApplyGenericJsonUpsert(conn, tx, "Equipes", op);
                        break;
                    case SyncOp.EquipeDelete:
                        ApplySimpleDelete(conn, tx, "Equipes", op.EntityId);
                        break;

                    case SyncOp.ProgrammeUpsert:
                        ApplyGenericJsonUpsert(conn, tx, "Programmes", op);
                        break;
                    case SyncOp.ProgrammeDelete:
                        ApplySimpleDelete(conn, tx, "Programmes", op.EntityId);
                        break;

                    case SyncOp.CRAUpsert:
                        ApplyGenericJsonUpsert(conn, tx, "CRA", op);
                        break;
                    case SyncOp.CRADelete:
                        ApplySimpleDelete(conn, tx, "CRA", op.EntityId);
                        break;

                    case SyncOp.CommentaireUpsert:
                        ApplyGenericJsonUpsert(conn, tx, "Commentaires", op);
                        break;

                    case SyncOp.NotificationUpsert:
                        ApplyGenericJsonUpsert(conn, tx, "Notifications", op);
                        break;
                    case SyncOp.NotificationDelete:
                        ApplySimpleDelete(conn, tx, "Notifications", op.EntityId);
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
                        ApplySimpleDelete(conn, tx, "PlanningVM", op.EntityId);
                        break;

                    case SyncOp.DemandeEchangeUpsert:
                        ApplyGenericJsonUpsert(conn, tx, "DemandeEchangeVM", op);
                        break;
                    case SyncOp.DemandeEchangeDelete:
                        ApplySimpleDelete(conn, tx, "DemandeEchangeVM", op.EntityId);
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
            // Détection de conflit LWW : si la ligne locale est plus récente que l'op distante
            using (var check = conn.CreateCommand())
            {
                check.Transaction = tx;
                check.CommandText = "SELECT DateDerniereMaj FROM BacklogItems WHERE Id = @id";
                check.Parameters.AddWithValue("@id", op.EntityId);
                var localTs = check.ExecuteScalar()?.ToString();
                if (localTs != null &&
                    DateTime.TryParse(localTs, out var localDate) &&
                    localDate > op.TimestampUtc.ToLocalTime())
                {
                    conflict       = true;
                    conflictDetail = $"Conflit LWW: local={localDate:HH:mm:ss} > distant={op.TimestampUtc:HH:mm:ss}";
                    // On applique quand même (LWW = dernier op "horloge" gagne)
                    // Mais on trace le conflit
                }
            }

            // Récupérer les champs depuis le payload JSON
            var doc = JsonDocument.Parse(op.PayloadJson);
            var r   = doc.RootElement;

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    INSERT OR REPLACE INTO BacklogItems
                        (Id, Titre, Description, ProjetId, DevId, Type, Priorite, Statut,
                         Points, ChiffrageHeures, TempsReelHeures, DateFinAttendue,
                         DateDebut, DateFin, DateCreation, DateDerniereMaj, EstArchive,
                         SprintId, DemandeId, DevSupporte, TacheSupportee)
                    VALUES
                        (@Id, @Titre, @Description, @ProjetId, @DevId, @Type, @Priorite, @Statut,
                         @Points, @ChiffrageHeures, @TempsReelHeures, @DateFinAttendue,
                         @DateDebut, @DateFin, @DateCreation, @DateDerniereMaj, @EstArchive,
                         @SprintId, @DemandeId, @DevSupporte, @TacheSupportee)";

                BindJsonParam(cmd, "@Id",               r, "id");
                BindJsonParam(cmd, "@Titre",             r, "titre");
                BindJsonParam(cmd, "@Description",       r, "description");
                BindJsonParam(cmd, "@ProjetId",          r, "projetId");
                BindJsonParam(cmd, "@DevId",             r, "devId",         "devAssigneId");
                BindJsonParam(cmd, "@Type",              r, "typeDemande",   "type");
                BindJsonParam(cmd, "@Priorite",          r, "priorite");
                BindJsonParam(cmd, "@Statut",            r, "statut");
                BindJsonParam(cmd, "@Points",            r, "complexite",    "points");
                BindJsonParam(cmd, "@ChiffrageHeures",   r, "chiffrageHeures");
                BindJsonParam(cmd, "@TempsReelHeures",   r, "tempsReelHeures");
                BindJsonParam(cmd, "@DateFinAttendue",   r, "dateFinAttendue");
                BindJsonParam(cmd, "@DateDebut",         r, "dateDebut");
                BindJsonParam(cmd, "@DateFin",           r, "dateFin");
                BindJsonParam(cmd, "@DateCreation",      r, "dateCreation");
                BindJsonParam(cmd, "@DateDerniereMaj",   r, "dateDerniereMaj");
                BindJsonParam(cmd, "@EstArchive",        r, "estArchive");
                BindJsonParam(cmd, "@SprintId",          r, "sprintId");
                BindJsonParam(cmd, "@DemandeId",         r, "demandeId");
                BindJsonParam(cmd, "@DevSupporte",       r, "devSupporte");
                BindJsonParam(cmd, "@TacheSupportee",    r, "tacheSupportee");
                cmd.ExecuteNonQuery();
            }
        }

        private void ApplyDemandeUpsert(SQLiteConnection conn, SQLiteTransaction tx,
            SyncOperation op, ref bool conflict, ref string conflictDetail)
        {
            // Conflit LWW sur Demandes (pas de DateDerniereMaj explicite → on utilise DateCreation + TimestampUtc)
            // Pour les demandes on applique toujours sans détection de conflit fine
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
        /// Applique un DELETE simple sur n'importe quelle table.
        /// </summary>
        private void ApplySimpleDelete(SQLiteConnection conn, SQLiteTransaction tx, string table, int id)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = $"DELETE FROM {table} WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Applique un upsert générique en passant par INSERT OR REPLACE avec les colonnes
        /// extraites du payload JSON.
        ///
        /// LIMITATION : suppose que toutes les colonnes de la table sont présentes dans le JSON.
        /// Pour les tables simples (Equipes, Programmes, Sprints, etc.) c'est garanti car
        /// le payload est produit par JsonSerializer.Serialize(entity) dans SyncedDatabase.
        /// </summary>
        private void ApplyGenericJsonUpsert(SQLiteConnection conn, SQLiteTransaction tx, string table, SyncOperation op)
        {
            // On utilise une approche pragmatique : construire un INSERT OR REPLACE
            // en lisant les colonnes réelles de la table, puis mapper depuis le JSON.
            // Pour les tables dont on ne gère pas les champs dynamiques, on délègue à
            // la méthode "raw JSON payload" qui fait un REPLACE complet.

            var doc = JsonDocument.Parse(op.PayloadJson);
            var root = doc.RootElement;

            // Récupérer la liste des colonnes de la table
            var columns = GetTableColumns(conn, table);
            if (columns == null || columns.Length == 0)
            {
                LoggingService.Instance.LogWarning($"[SyncApplier] Table {table} non trouvée, opération ignorée.");
                return;
            }

            // Construire INSERT OR REPLACE dynamique
            var colList  = string.Join(", ", columns);
            var paramList = string.Join(", ", System.Array.ConvertAll(columns, c => "@" + c));

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = $"INSERT OR REPLACE INTO {table} ({colList}) VALUES ({paramList})";

                foreach (var col in columns)
                {
                    string jsonKey = char.ToLower(col[0]) + col.Substring(1); // PascalCase → camelCase
                    if (root.TryGetProperty(jsonKey, out var prop))
                    {
                        cmd.Parameters.AddWithValue("@" + col, GetJsonValue(prop));
                    }
                    else if (root.TryGetProperty(col, out var prop2))
                    {
                        cmd.Parameters.AddWithValue("@" + col, GetJsonValue(prop2));
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@" + col, DBNull.Value);
                    }
                }

                cmd.ExecuteNonQuery();
            }
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

        private string[] GetTableColumns(SQLiteConnection conn, string table)
        {
            try
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT name FROM pragma_table_info('{table}')";
                    var cols = new System.Collections.Generic.List<string>();
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) cols.Add(r.GetString(0));
                    return cols.ToArray();
                }
            }
            catch { return null; }
        }

        private void BindJsonParam(SQLiteCommand cmd, string paramName, JsonElement root,
            string primaryKey, string fallbackKey = null)
        {
            JsonElement prop;
            if (root.TryGetProperty(primaryKey, out prop) ||
                (!string.IsNullOrEmpty(fallbackKey) && root.TryGetProperty(fallbackKey, out prop)))
            {
                cmd.Parameters.AddWithValue(paramName, GetJsonValue(prop));
            }
            else
            {
                cmd.Parameters.AddWithValue(paramName, DBNull.Value);
            }
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
    }
}
