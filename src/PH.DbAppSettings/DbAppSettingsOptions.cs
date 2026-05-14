namespace PH.DbAppSettings;

public sealed class DbAppSettingsOptions
{
    /// <summary>Connection string al DB di configurazione. OBBLIGATORIA.</summary>
    public required string ConnectionString { get; init; }

    /// <summary>Nome dell'ambiente (default: ASPNETCORE_ENVIRONMENT ?? "Production").</summary>
    public string Environment { get; init; } = "Production";

    /// <summary>Applica automaticamente le migrazioni EF al bootstrap.</summary>
    public bool AutoMigrate { get; init; } = true;

    /// <summary>Esegue il seeding da appsettings.json se il DB è vuoto.</summary>
    public bool SeedOnEmpty { get; init; } = true;

    /// <summary>Forza il re-seeding sovrascrivendo i valori esistenti.</summary>
    public bool ForceReseed { get; init; } = false;

    /// <summary>Chiavi da escludere dal seeding (es. connection strings sensibili).</summary>
    public IReadOnlyList<string> ExcludeKeysFromSeed { get; init; } = [];

    /// <summary>Abilita la cifratura dei valori a riposo (richiede IValueEncryptor).</summary>
    public bool EncryptValues { get; init; } = false;

    /// <summary>Intervallo di ricarica automatica dal DB (null = disabilitato).</summary>
    public TimeSpan? ReloadInterval { get; init; } = null;

    /// <summary>Nome dello schema DB (default: "dbo" per SQL Server).</summary>
    public string SchemaName { get; init; } = "dbo";

    /// <summary>Nome della tabella (default: "AppSettings").</summary>
    public string TableName { get; init; } = "AppSettings";
}
