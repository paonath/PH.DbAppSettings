namespace PH.DbAppSettings.Storage.Dialects;

public sealed class SqlServerDialect : ISqlDialect
{
    public string DialectName => "SqlServer";

    public string EscapeIdentifier(string identifier) => $"[{identifier}]";

    public string FormatTableName(string schema, string table)
        => string.IsNullOrWhiteSpace(schema) ? EscapeIdentifier(table) : $"{EscapeIdentifier(schema)}.{EscapeIdentifier(table)}";

    public string GetCreateTableSql(string schema, string table)
    {
        var fullTable = FormatTableName(schema, table);
        var schemaCheck = string.IsNullOrWhiteSpace(schema) ? "dbo" : schema;
        return $"""
            IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = '{schemaCheck}' AND t.name = '{table}')
            BEGIN
                CREATE TABLE {fullTable} (
                    [Key] NVARCHAR(512) NOT NULL,
                    [Environment] NVARCHAR(64) NOT NULL,
                    [Value] NVARCHAR(MAX) NULL,
                    [IsEncrypted] BIT NOT NULL DEFAULT 0,
                    [UpdatedAt] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
                    CONSTRAINT [PK_{table}] PRIMARY KEY ([Key], [Environment])
                );
            END
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
            MERGE INTO {fullTable} AS target
            USING (SELECT @Key AS [Key], @Environment AS [Environment]) AS source
            ON (target.[Key] = source.[Key] AND target.[Environment] = source.[Environment])
            WHEN MATCHED THEN
                UPDATE SET target.[Value] = @Value, target.[IsEncrypted] = @IsEncrypted, target.[UpdatedAt] = @UpdatedAt
            WHEN NOT MATCHED THEN
                INSERT ([Key], [Environment], [Value], [IsEncrypted], [UpdatedAt])
                VALUES (@Key, @Environment, @Value, @IsEncrypted, @UpdatedAt);
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
        return $"SELECT TOP 1 1 FROM {fullTable} WHERE [Environment] = @Environment;";
    }
}
