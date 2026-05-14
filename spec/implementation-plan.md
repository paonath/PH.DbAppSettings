---
type: execution-plan
title: "PH.DbAppSettings — Implementation Plan"
version: "1.0.0"
status: completed
created: 2026-05-14
updated: 2026-05-14
author: "AI Developer"
project: "PH.DbAppSettings"
target: ai
task-count: 17
tags: [dotnet, efcore, configuration]
---

# PH.DbAppSettings — Implementation Plan

> **Scopo:** Questo documento è un piano di implementazione atomico destinato a un'AI. Ogni task è indipendente, verificabile e deve essere eseguito nell'ordine indicato. Non procedere al task successivo senza aver completato e verificato il precedente.

---

## Contesto e Obiettivo

Libreria .NET 10 riutilizzabile (`PH.DbAppSettings`) che:
- Legge la configurazione da `appsettings.json` **una sola volta** al bootstrap.
- Persiste tutte le coppie chiave/valore su database relazionale tramite **Entity Framework Core 10**.
- Espone la configurazione persistita come `IConfigurationProvider` nativo .NET.
- Garantisce che l'**unica** configurazione obbligatoria fuori dal DB sia la **connection string**.

---

## Stack Tecnologico

| Componente | Versione |
|---|---|
| .NET | 10.0 |
| C# | 14 |
| Entity Framework Core | 10.x |
| Target Framework | `net10.0` |
| Database supportati | SQL Server, PostgreSQL, SQLite |
| Test | xUnit + Testcontainers |

---

## Formato Chiave/Valore

La gerarchia JSON viene appiattita usando `__` (doppio underscore) come separatore, compatibile con le environment variables di .NET.

| JSON | Chiave su DB |
|---|---|
| `ConnectionStrings.Default` | `ConnectionStrings__Default` |
| `Logging.LogLevel.Default` | `Logging__LogLevel__Default` |
| `MyApp.FeatureFlags.EnableCache` | `MyApp__FeatureFlags__EnableCache` |
| Array: `AllowedHosts[0]` | `AllowedHosts__0` |

**Regola:** `chiave_db = chiave_IConfiguration.Replace(":", "__")`

---

## Modello Dati

### Tabella `AppSettings`

```sql
CREATE TABLE AppSettings (
    [Key]       NVARCHAR(512)   NOT NULL,
    Environment NVARCHAR(64)    NOT NULL DEFAULT 'Production',
    [Value]     NVARCHAR(4000)  NULL,
    IsEncrypted BIT             NOT NULL DEFAULT 0,
    CONSTRAINT PK_AppSettings PRIMARY KEY ([Key], Environment)
);
```

---

## Lista Task Atomici

### TASK-01 — Struttura della Solution

**Obiettivo:** Creare la struttura di cartelle e file di progetto.

**Azioni:**
1. Creare `PH.DbAppSettings.sln`
2. Creare `src/PH.DbAppSettings/PH.DbAppSettings.csproj` (classlib, net10.0)
3. Creare `tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj` (xUnit, net10.0)
4. Aggiungere entrambi i progetti alla solution
5. Aggiungere riferimento dal progetto test al progetto libreria

**Dipendenze NuGet (libreria):**
- `Microsoft.EntityFrameworkCore` 10.x
- `Microsoft.EntityFrameworkCore.Relational` 10.x
- `Microsoft.Extensions.Configuration.Abstractions` 10.x
- `Microsoft.Extensions.Hosting.Abstractions` 10.x
- `Microsoft.Extensions.Logging.Abstractions` 10.x

**Dipendenze NuGet (test):**
- `xunit` 2.x
- `xunit.runner.visualstudio`
- `Microsoft.NET.Test.Sdk`
- `Testcontainers` (per integration test)
- `Microsoft.EntityFrameworkCore.Sqlite` (per test in-memory)

**Verifica:** `dotnet build` senza errori.

---

### TASK-02 — Entity `AppSettingEntry`

**File:** `src/PH.DbAppSettings/Data/AppSettingEntry.cs`

**Implementare:**
```csharp
public sealed class AppSettingEntry
{
    public required string Key { get; set; }
    public string Environment { get; set; } = "Production";
    public string? Value { get; set; }
    public bool IsEncrypted { get; set; }
}
```

**Verifica:** Compilazione senza errori.

---

### TASK-03 — EF Configuration `AppSettingEntryConfiguration`

**File:** `src/PH.DbAppSettings/Data/AppSettingEntryConfiguration.cs`

**Implementare** `IEntityTypeConfiguration<AppSettingEntry>`:
- Tabella: nome configurabile (default `AppSettings`), schema configurabile (default `dbo`)
- Primary key composita: `[Key]` + `Environment`
- `Key`: max length 512, required
- `Environment`: max length 64, required
- `Value`: max length 4000, nullable
- `IsEncrypted`: required, default `false`

**Verifica:** Compilazione senza errori.

---

### TASK-04 — `AppSettingsDbContext`

**File:** `src/PH.DbAppSettings/Data/AppSettingsDbContext.cs`

**Implementare:**
```csharp
public class AppSettingsDbContext(DbContextOptions<AppSettingsDbContext> options)
    : DbContext(options)
{
    public DbSet<AppSettingEntry> AppSettings => Set<AppSettingEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AppSettingEntryConfiguration());
    }
}
```

**Verifica:** Compilazione senza errori.

---

### TASK-05 — `DbAppSettingsOptions`

**File:** `src/PH.DbAppSettings/DbAppSettingsOptions.cs`

**Implementare** la classe con le seguenti proprietà:

| Proprietà | Tipo | Default | Descrizione |
|---|---|---|---|
| `ConnectionString` | `required string` | — | Connection string al DB. OBBLIGATORIA |
| `Environment` | `string` | `"Production"` | Nome ambiente |
| `AutoMigrate` | `bool` | `true` | Applica migrazioni EF al bootstrap |
| `SeedOnEmpty` | `bool` | `true` | Seeding da appsettings.json se DB vuoto |
| `ForceReseed` | `bool` | `false` | Forza re-seeding sovrascrivendo valori esistenti |
| `ExcludeKeysFromSeed` | `IReadOnlyList<string>` | `[]` | Chiavi escluse dal seeding |
| `EncryptValues` | `bool` | `false` | Abilita cifratura valori a riposo |
| `ReloadInterval` | `TimeSpan?` | `null` | Intervallo ricarica automatica (null = disabilitato) |
| `SchemaName` | `string` | `"dbo"` | Schema DB |
| `TableName` | `string` | `"AppSettings"` | Nome tabella |

**Verifica:** Compilazione senza errori.

---

### TASK-06 — `IValueEncryptor` e `AesGcmValueEncryptor`

**File:** `src/PH.DbAppSettings/Encryption/IValueEncryptor.cs`

```csharp
public interface IValueEncryptor
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
```

**File:** `src/PH.DbAppSettings/Encryption/AesGcmValueEncryptor.cs`

**Implementare** `IValueEncryptor` usando `System.Security.Cryptography.AesGcm`:
- Chiave a 256 bit derivata da un secret passato nel costruttore
- Output in formato Base64: `nonce (12 byte) + tag (16 byte) + ciphertext`
- `Decrypt` inverte il processo

**Verifica:** Unit test in `tests/` che cifra e decifra una stringa e verifica l'uguaglianza.

---

### TASK-07 — `SeedService`

**File:** `src/PH.DbAppSettings/Services/SeedService.cs`

**Implementare** la classe con metodo `SeedAsync(IConfiguration configuration, CancellationToken ct)`:

**Algoritmo:**
1. Chiama `configuration.AsEnumerable(makePathsRelative: false)`
2. Filtra le coppie con valore `null`
3. Filtra le chiavi presenti in `DbAppSettingsOptions.ExcludeKeysFromSeed`
4. Converte ogni chiave: `key.Replace(":", "__")`
5. Per ogni chiave:
   - Se `ForceReseed = false`: esegue INSERT solo se la chiave non esiste già (upsert condizionale)
   - Se `ForceReseed = true`: esegue UPSERT sovrascrivendo il valore
6. Imposta `Environment` dal valore in `DbAppSettingsOptions`
7. Se `EncryptValues = true`, cifra il valore tramite `IValueEncryptor` prima del salvataggio e imposta `IsEncrypted = true`

**Dipendenze iniettate:** `AppSettingsDbContext`, `DbAppSettingsOptions`, `ILogger<SeedService>`, `IValueEncryptor?` (opzionale)

**Verifica:** Unit test con SQLite in-memory che verifica:
- Seeding corretto di chiavi gerarchiche
- Esclusione delle chiavi in `ExcludeKeysFromSeed`
- Idempotenza (doppio seeding non duplica le righe)
- `ForceReseed = true` sovrascrive i valori

---

### TASK-08 — `IDbAppSettingsWriter`

**File:** `src/PH.DbAppSettings/Services/IDbAppSettingsWriter.cs`

```csharp
public interface IDbAppSettingsWriter
{
    Task SetAsync(string key, string? value, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
}
```

**File:** `src/PH.DbAppSettings/Services/DbAppSettingsWriter.cs`

**Implementare** `IDbAppSettingsWriter`:
- `SetAsync`: UPSERT su `AppSettings` per la chiave e l'ambiente corrente; se `EncryptValues = true`, cifra il valore
- `DeleteAsync`: rimuove la riga corrispondente

**Verifica:** Unit test con SQLite in-memory.

---

### TASK-09 — `DbAppSettingsProvider`

**File:** `src/PH.DbAppSettings/Configuration/DbAppSettingsProvider.cs`

**Implementare** estendendo `ConfigurationProvider` di .NET:

**Metodi:**
- `Load()`: chiama `LoadAsync().GetAwaiter().GetResult()`
- `LoadAsync()`:
  1. Crea un `AppSettingsDbContext` con la connection string dalle opzioni
  2. Se `AutoMigrate = true`, chiama `dbContext.Database.MigrateAsync()`
  3. Se `SeedOnEmpty = true` e la tabella è vuota, chiama `SeedService.SeedAsync()`
  4. Esegue `SELECT [Key], [Value], IsEncrypted FROM AppSettings WHERE Environment = @env`
  5. Per ogni riga: se `IsEncrypted = true`, decifra il valore tramite `IValueEncryptor`
  6. Popola `Data` (dizionario `Dictionary<string, string?>` della classe base) con le coppie chiave/valore
- `Set(string key, string? value)`: chiama `IDbAppSettingsWriter.SetAsync()` e aggiorna `Data`

**Verifica:** Unit test con SQLite in-memory che verifica il caricamento corretto delle chiavi.

---

### TASK-10 — `DbAppSettingsConfigurationSource`

**File:** `src/PH.DbAppSettings/Configuration/DbAppSettingsConfigurationSource.cs`

**Implementare** `IConfigurationSource`:

```csharp
public sealed class DbAppSettingsConfigurationSource : IConfigurationSource
{
    private readonly DbAppSettingsOptions _options;
    private readonly IConfiguration? _bootstrapConfig;

    public DbAppSettingsConfigurationSource(DbAppSettingsOptions options, IConfiguration? bootstrapConfig = null)
    {
        _options = options;
        _bootstrapConfig = bootstrapConfig;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new DbAppSettingsProvider(_options, _bootstrapConfig);
}
```

**Verifica:** Compilazione senza errori.

---

### TASK-11 — Extension Methods `DbAppSettingsExtensions`

**File:** `src/PH.DbAppSettings/DbAppSettingsExtensions.cs`

**Implementare** due overload:

```csharp
public static class DbAppSettingsExtensions
{
    // Overload 1: configurazione esplicita
    public static IConfigurationBuilder AddDbAppSettings(
        this IConfigurationBuilder builder,
        Action<DbAppSettingsOptions> configure);

    // Overload 2: legge ConnectionString da bootstrapConfig
    // chiave: "DbAppSettings:ConnectionString" o env var "DbAppSettings__ConnectionString"
    public static IConfigurationBuilder AddDbAppSettings(
        this IConfigurationBuilder builder,
        IConfiguration bootstrapConfig,
        Action<DbAppSettingsOptions>? configure = null);
}
```

**Regola:** Il provider deve essere aggiunto **dopo** gli altri provider (json, env vars) in modo che i valori da DB abbiano priorità.

**Verifica:** Compilazione senza errori.

---

### TASK-12 — `ReloadBackgroundService`

**File:** `src/PH.DbAppSettings/Services/ReloadBackgroundService.cs`

**Implementare** `BackgroundService`:

**Logica:**
1. Se `ReloadInterval` è `null`, il servizio termina immediatamente
2. Altrimenti, ogni `ReloadInterval`:
   - Esegue una query per rilevare modifiche recenti (es. confronto snapshot delle chiavi/valori)
   - Se sono presenti modifiche rispetto all'ultimo caricamento:
     - Chiama `provider.Load()`
     - Chiama `provider.OnReload()` per notificare `IOptionsMonitor<T>`
     - Aggiorna il timestamp dell'ultimo caricamento

**Dipendenze:** `DbAppSettingsProvider`, `AppSettingsDbContext`, `DbAppSettingsOptions`, `ILogger<ReloadBackgroundService>`

**Verifica:** Unit test che verifica che il reload venga triggerato solo quando `UpdatedAt` cambia.

---

### TASK-13 — Registrazione nel DI Container

**File:** Aggiornare `src/PH.DbAppSettings/DbAppSettingsExtensions.cs`

**Aggiungere** un extension method su `IServiceCollection`:

```csharp
public static IServiceCollection AddDbAppSettingsServices(
    this IServiceCollection services,
    DbAppSettingsOptions options)
```

**Deve registrare:**
- `AppSettingsDbContext` con la connection string dalle opzioni
- `IDbAppSettingsWriter` → `DbAppSettingsWriter` (Scoped)
- `SeedService` (Transient)
- `ReloadBackgroundService` come `IHostedService` (se `ReloadInterval != null`)
- `IValueEncryptor` → `AesGcmValueEncryptor` (Singleton, se `EncryptValues = true`)

**Verifica:** Compilazione senza errori.

---

### TASK-14 — Migrazioni EF Core

**Azioni:**
1. Creare una `IDesignTimeDbContextFactory<AppSettingsDbContext>` per supportare `dotnet ef migrations add`
2. Generare la migrazione iniziale: `dotnet ef migrations add InitialCreate`
3. Verificare che il file di migrazione crei correttamente la tabella `AppSettings` con tutti i campi e l'indice univoco

**File generati:**
- `src/PH.DbAppSettings/Data/Migrations/[timestamp]_InitialCreate.cs`
- `src/PH.DbAppSettings/Data/Migrations/AppSettingsDbContextModelSnapshot.cs`

**Verifica:** `dotnet ef migrations list` mostra la migrazione. `dotnet ef database update` su SQLite locale non produce errori.

---

### TASK-15 — Test di Integrazione Bootstrap

**File:** `tests/PH.DbAppSettings.Tests/IntegrationTests/BootstrapIntegrationTests.cs`

**Implementare** i seguenti test con SQLite in-memory (o Testcontainers per SQL Server):

1. **Test_FirstBoot_SeedsFromAppSettings**: verifica che al primo avvio le chiavi da `appsettings.json` vengano salvate su DB
2. **Test_SecondBoot_DoesNotOverwrite**: verifica che al secondo avvio i valori modificati su DB non vengano sovrascritti
3. **Test_ForceReseed_Overwrites**: verifica che `ForceReseed = true` sovrascriva i valori
4. **Test_ExcludeKeys_NotSeeded**: verifica che le chiavi in `ExcludeKeysFromSeed` non vengano salvate
5. **Test_IConfiguration_ReadsFromDb**: verifica che `IConfiguration["MyApp__Setting"]` legga dal DB
6. **Test_KeyNormalization**: verifica la conversione `:` → `__` per chiavi gerarchiche e array

**Verifica:** `dotnet test` verde su tutti i test.

---

### TASK-16 — Test di Normalizzazione Chiavi

**File:** `tests/PH.DbAppSettings.Tests/KeyNormalizationTests.cs`

**Implementare** unit test che verificano:

| Input (chiave IConfiguration) | Output atteso (chiave DB) |
|---|---|
| `ConnectionStrings:Default` | `ConnectionStrings__Default` |
| `Logging:LogLevel:Default` | `Logging__LogLevel__Default` |
| `MyApp:FeatureFlags:EnableCache` | `MyApp__FeatureFlags__EnableCache` |
| `AllowedHosts:0` | `AllowedHosts__0` |

**Verifica:** `dotnet test` verde su tutti i test.

---

### TASK-17 — README e Documentazione

**File:** `README.md`

**Contenuto minimo:**

1. **Descrizione** del progetto
2. **Installazione** (NuGet package reference)
3. **Configurazione minima** in `Program.cs`:
```csharp
var bootstrapConfig = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddJsonFile("appsettings.json", optional: true)
    .Build();

builder.Configuration.AddDbAppSettings(bootstrapConfig, options =>
{
    options.AutoMigrate = true;
    options.SeedOnEmpty = true;
    options.ExcludeKeysFromSeed = ["DbAppSettings__ConnectionString"];
    options.ReloadInterval = TimeSpan.FromMinutes(5);
});
```
4. **Configurazione minima obbligatoria** (solo connection string):
```json
{
  "DbAppSettings": {
    "ConnectionString": "Server=...;Database=AppConfig;..."
  }
}
```
5. **Tabella delle opzioni** (`DbAppSettingsOptions`)
6. **Scenari d'uso** (primo avvio, aggiornamento senza rideploy, sviluppo locale)
7. **Sicurezza** (connection string solo via env var in produzione)

**Verifica:** Il README è leggibile e completo.

---

## Ordine di Esecuzione Obbligatorio

```
TASK-01 → TASK-02 → TASK-03 → TASK-04 → TASK-05 → TASK-06
    → TASK-07 → TASK-08 → TASK-09 → TASK-10 → TASK-11
    → TASK-12 → TASK-13 → TASK-14 → TASK-15 → TASK-16 → TASK-17
```

Ogni task deve compilare e superare i propri test prima di procedere al successivo.

---

## Regole Generali per l'AI

1. **Non saltare task**: ogni task è prerequisito del successivo.
2. **Non modificare l'interfaccia pubblica** senza aggiornare i test corrispondenti.
3. **La connection string NON deve mai essere salvata su DB** (paradosso del bootstrap).
4. **Il seeding è one-way** (json → DB): non sincronizzare mai DB → json.
5. **`ForceReseed = false` è il default**: proteggere i valori modificati manualmente su DB.
6. **Tutti i test devono essere verdi** prima del submit finale.
7. **Usare `__` (doppio underscore)** come separatore di gerarchia, mai `:` o `.`.
8. **Tutti i log devono usare log strutturati** con `ILogger<T>`.
