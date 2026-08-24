namespace PH.DbAppSettings.Storage.Dialects;

public sealed class SqliteDialect : ISqlDialect
{
    public string DialectName => "Sqlite";

    public string EscapeIdentifier(string identifier) => $"[{identifier}]";

    public string FormatTableName(string schema, string table) => EscapeIdentifier(table);

    public string GetCreateTableSql(string schema, string table)
    {
        var fullTable = FormatTableName(schema, table);
        return $"""
            CREATE TABLE IF NOT EXISTS {fullTable} (
                [Key] TEXT NOT NULL,
                [Environment] TEXT NOT NULL,
                [Value] TEXT NULL,
                [IsEncrypted] INTEGER NOT NULL DEFAULT 0,
                [UpdatedAt] TEXT NOT NULL,
                CONSTRAINT [PK_{table}] PRIMARY KEY ([Key], [Environment])
            );
            """;
    }

    public string GetSelectAllSql(string schema, string table)
    {
        var fullTable = FormatTableName(schema, table);
        return $"SELECT [Key], [Environment], [Value], [IsEncrypted], [UpdatedAt] FROM {fullTable} WHERE [Environment] = @Environment;";
    }

    public string GetSelectByKeySql(string schema, string table)
    {
        var fullTable = FormatTableName(schema, table);
        return $"SELECT [Key], [Environment], [Value], [IsEncrypted], [UpdatedAt] FROM {fullTable} WHERE [Key] = @Key AND [Environment] = @Environment;";
    }

    public string GetUpsertSql(string schema, string table)
    {
        var fullTable = FormatTableName(schema, table);
        return $"""
            INSERT INTO {fullTable} ([Key], [Environment], [Value], [IsEncrypted], [UpdatedAt])
            VALUES (@Key, @Environment, @Value, @IsEncrypted, @UpdatedAt)
            ON CONFLICT ([Key], [Environment]) DO UPDATE SET
                [Value] = excluded.[Value],
                [IsEncrypted] = excluded.[IsEncrypted],
                [UpdatedAt] = excluded.[UpdatedAt];
            """;
    }

    public string GetDeleteSql(string schema, string table)
    {
        var fullTable = FormatTableName(schema, table);
        return $"DELETE FROM {fullTable} WHERE [Key] = @Key AND [Environment] = @Environment;";
    }

    public string GetMaxUpdatedAtSql(string schema, string table)
    {
        var fullTable = FormatTableName(schema, table);
        return $"SELECT MAX([UpdatedAt]) FROM {fullTable} WHERE [Environment] = @Environment;";
    }

    public string GetIsEmptySql(string schema, string table)
    {
        var fullTable = FormatTableName(schema, table);
        return $"SELECT 1 FROM {fullTable} WHERE [Environment] = @Environment LIMIT 1;";
    }
}
