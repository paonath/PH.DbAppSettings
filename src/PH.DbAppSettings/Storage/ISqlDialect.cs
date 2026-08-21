namespace PH.DbAppSettings.Storage;

/// <summary>
/// Contratto per la generazione di query SQL compatibili con specifici database server.
/// </summary>
public interface ISqlDialect
{
    string DialectName { get; }
    string EscapeIdentifier(string identifier);
    string FormatTableName(string schema, string table);
    string GetCreateTableSql(string schema, string table);
    string GetSelectAllSql(string schema, string table);
    string GetSelectByKeySql(string schema, string table);
    string GetUpsertSql(string schema, string table);
    string GetDeleteSql(string schema, string table);
    string GetMaxUpdatedAtSql(string schema, string table);
    string GetIsEmptySql(string schema, string table);
}
