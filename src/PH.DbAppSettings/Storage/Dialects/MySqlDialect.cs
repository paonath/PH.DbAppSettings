namespace PH.DbAppSettings.Storage.Dialects;

public sealed class MySqlDialect : ISqlDialect
{
    public string DialectName => "MySql";

    public string EscapeIdentifier(string identifier) => $"`{identifier}`";

    public string FormatTableName(string schema, string table)
        => string.IsNullOrWhiteSpace(schema) ? EscapeIdentifier(table) : $"{EscapeIdentifier(schema)}.{EscapeIdentifier(table)}";

    public string GetCreateTableSql(string schema, string table)
    {
        var fullTable = FormatTableName(schema, table);
        return $"""
            CREATE TABLE IF NOT EXISTS {fullTable} (
                `Key` VARCHAR(512) NOT NULL,
                `Environment` VARCHAR(64) NOT NULL,
                `Value` LONGTEXT NULL,
                `IsEncrypted` TINYINT(1) NOT NULL DEFAULT 0,
                `UpdatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                CONSTRAINT `PK_{table}` PRIMARY KEY (`Key`, `Environment`)
            );
            """;
    }

    public string GetSelectAllSql(string schema, string table)
    {
        var fullTable = FormatTableName(schema, table);
        return $"SELECT `Key`, `Environment`, `Value`, `IsEncrypted`, `UpdatedAt` FROM {fullTable} WHERE `Environment` = @Environment;";
    }

    public string GetSelectByKeySql(string schema, string table)
    {
        var fullTable = FormatTableName(schema, table);
        return $"SELECT `Key`, `Environment`, `Value`, `IsEncrypted`, `UpdatedAt` FROM {fullTable} WHERE `Key` = @Key AND `Environment` = @Environment;";
    }

    public string GetUpsertSql(string schema, string table)
    {
        var fullTable = FormatTableName(schema, table);
        return $"""
            INSERT INTO {fullTable} (`Key`, `Environment`, `Value`, `IsEncrypted`, `UpdatedAt`)
            VALUES (@Key, @Environment, @Value, @IsEncrypted, @UpdatedAt)
            ON DUPLICATE KEY UPDATE
                `Value` = VALUES(`Value`),
                `IsEncrypted` = VALUES(`IsEncrypted`),
                `UpdatedAt` = VALUES(`UpdatedAt`);
            """;
    }

    public string GetDeleteSql(string schema, string table)
    {
        var fullTable = FormatTableName(schema, table);
        return $"DELETE FROM {fullTable} WHERE `Key` = @Key AND `Environment` = @Environment;";
    }

    public string GetMaxUpdatedAtSql(string schema, string table)
    {
        var fullTable = FormatTableName(schema, table);
        return $"SELECT MAX(`UpdatedAt`) FROM {fullTable} WHERE `Environment` = @Environment;";
    }

    public string GetIsEmptySql(string schema, string table)
    {
        var fullTable = FormatTableName(schema, table);
        return $"SELECT 1 FROM {fullTable} WHERE `Environment` = @Environment LIMIT 1;";
    }
}
