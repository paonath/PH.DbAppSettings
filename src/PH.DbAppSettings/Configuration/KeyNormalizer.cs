namespace PH.DbAppSettings.Configuration;

/// <summary>
/// Utility per la normalizzazione bidirezionale delle chiavi di configurazione tra DB e IConfiguration.
/// </summary>
public static class KeyNormalizer
{
    /// <summary>
    /// Converte una chiave DB (usando '__' o '/') nel formato standard ':' per IConfiguration.
    /// </summary>
    public static string ToConfigurationKey(string dbKey)
    {
        ArgumentNullException.ThrowIfNull(dbKey);
        return dbKey.Replace("__", ":").Replace("/", ":");
    }

    /// <summary>
    /// Converte una chiave IConfiguration (usando ':') nel formato standard '__' per il DB.
    /// </summary>
    public static string ToDbKey(string configKey)
    {
        ArgumentNullException.ThrowIfNull(configKey);
        return configKey.Replace(":", "__");
    }
}
