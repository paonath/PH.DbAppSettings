namespace PH.DbAppSettings.Storage.Dialects;

public sealed class PostgreSqlDialect : ISqlDialect
{
    public string DialectName => "PostgreSql";

    public string EscapeIdentifier(string identifier) => $"\"{identifier}\"";

    public string FormatTableName(string schema, string table)
        => string.IsNullOrWhiteSpace(schema) ? EscapeIdentifier(table) : $"{EscapeIdentifier(schema)}.{EscapeIdentifier(table)}";

    public string GetCreateTableSql(string schema, string table)
    {
        var fullTable = FormatTableName(schema, table);
        return $"""
            CREATE TABLE IF NOT EXISTS {fullTable} (
                "Key" VARCHAR(512) NOT NULL,
                "Environment" VARCHAR(64) NOT NULL,
                "Value" TEXT NULL,
                "IsEncrypted" BOOLEAN NOT NULL DEFAULT FALSE,
                "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                CONSTRAINT "PK_{table}" PRIMARY KEY ("Key", "Environment")
            );
            """;
    }

    public string GetSelectAllSql(string schema, string table)
    {
        var fullTable = FormatTableName(schema, table);
        return $"SELECT \"Key\", \"Environment\", \"Value\", \"IsEncrypted\", \"UpdatedAt\" FROM {fullTable} WHERE \"Environment\" = @Environment;";
    }

    public string GetSelectByKeySql(string schema, string table)
    {
        var fullTable = FormatTableName(schema, table);
        return $"SELECT \"Key\", \"Environment\", \"Value\", \"IsEncrypted\", \"UpdatedAt\" FROM {fullTable} WHERE \"Key\" = @Key AND \"Environment\" = @Environment;";
    }

    public string GetUpsertSql(string schema, string table)
    {
        var fullTable = FormatTableName(schema, table);
        return $"""
            INSERT INTO {fullTable} ("Key", "Environment", "Value", "IsEncrypted", "UpdatedAt")
            VALUES (@Key, @Environment, @Value, @IsEncrypted, @UpdatedAt)
            ON CONFLICT ("Key", "Environment") DO UPDATE SET
                "Value" = EXCLUDED."Value",
                "IsEncrypted" = EXCLUDED."IsEncrypted",
                "UpdatedAt" = EXCLUDED."UpdatedAt";
            """;
    }

    public string GetDeleteSql(string schema, string table)
    {
        var fullTable = FormatTableName(schema, table);
        return $"DELETE FROM {fullTable} WHERE \"Key\" = @Key AND \"Environment\" = @Environment;";
    }

    public string GetMaxUpdatedAtSql(string schema, string table)
    {
        var fullTable = FormatTableName(schema, table);
        return $"SELECT MAX(\"UpdatedAt\") FROM {fullTable} WHERE \"Environment\" = @Environment;";
    }

    public string GetIsEmptySql(string schema, string table)
    {
        var fullTable = FormatTableName(schema, table);
        return $"SELECT 1 FROM {fullTable} WHERE \"Environment\" = @Environment LIMIT 1;";
    }
}
