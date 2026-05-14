namespace PH.DbAppSettings;

/// <summary>
/// Versione mutabile di DbAppSettingsOptions, usata come target per Action&lt;T&gt; nei metodi di configurazione.
/// </summary>
public sealed class DbAppSettingsMutableOptions
{
    public string? ConnectionString { get; set; }
    public string Environment { get; set; } = "Production";
    public bool AutoMigrate { get; set; } = true;
    public bool SeedOnEmpty { get; set; } = true;
    public bool ForceReseed { get; set; } = false;
    public IReadOnlyList<string> ExcludeKeysFromSeed { get; set; } = [];
    public bool EncryptValues { get; set; } = false;
    public TimeSpan? ReloadInterval { get; set; } = null;
    public string SchemaName { get; set; } = "dbo";
    public string TableName { get; set; } = "AppSettings";

    public DbAppSettingsOptions ToOptions()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new InvalidOperationException("DbAppSettingsMutableOptions.ConnectionString is required.");

        return new DbAppSettingsOptions
        {
            ConnectionString = ConnectionString,
            Environment = Environment,
            AutoMigrate = AutoMigrate,
            SeedOnEmpty = SeedOnEmpty,
            ForceReseed = ForceReseed,
            ExcludeKeysFromSeed = ExcludeKeysFromSeed,
            EncryptValues = EncryptValues,
            ReloadInterval = ReloadInterval,
            SchemaName = SchemaName,
            TableName = TableName
        };
    }
}
