---
step: "004"
title: "Storage Engine, Provider, and DI Extensions Refactoring Design"
status: "completed"
created_at: "2026-08-24T10:00:25+02:00"
---

# Storage Engine, Provider, and DI Extensions Refactoring Design

## Purpose

Design the refactored `EfCoreStorageEngine`, configuration builder extensions, options builder methods, and dependency injection lifecycles supporting polymorphic derived `AppSettingsDbContext` instances.

## Design Details

### 1. Polymorphic EfCoreStorageEngine

`EfCoreStorageEngine` maintains its contract against the base `AppSettingsDbContext`:

```csharp
public sealed class EfCoreStorageEngine : IDbAppSettingsStorageEngine
{
    private readonly Func<AppSettingsDbContext> _contextFactory;
    private readonly bool _ownsContext;

    public EfCoreStorageEngine(Func<AppSettingsDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _ownsContext = true;
    }

    public EfCoreStorageEngine(AppSettingsDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _contextFactory = () => dbContext;
        _ownsContext = false;
    }

    // Factory method for strongly-typed contexts
    public static EfCoreStorageEngine FromContext<TContext>(Func<TContext> factory)
        where TContext : AppSettingsDbContext
        => new(() => factory());
    
    // ... GetAllAsync, UpsertAsync, DeleteAsync remain identical via base AppSettings property
}
```

### 2. DbAppSettingsMutableOptions Generic Methods

Replace concrete methods with generic, provider-agnostic configuration methods:

```csharp
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
    public Func<IDbAppSettingsStorageEngine>? StorageEngineFactory { get; set; }

    /// <summary>
    /// Configures EF Core using a custom context factory delegate.
    /// </summary>
    public void UseEntityFramework<TContext>(Func<TContext> contextFactory)
        where TContext : AppSettingsDbContext
    {
        StorageEngineFactory = () => new EfCoreStorageEngine(() => contextFactory());
    }

    /// <summary>
    /// Configures EF Core using a DbContextOptionsBuilder configuration action.
    /// </summary>
    public void UseEntityFramework<TContext>(Action<DbContextOptionsBuilder<TContext>> configureDbContext)
        where TContext : AppSettingsDbContext
    {
        StorageEngineFactory = () =>
        {
            var builder = new DbContextOptionsBuilder<TContext>();
            configureDbContext(builder);
            return new EfCoreStorageEngine(() => 
                (TContext)Activator.CreateInstance(typeof(TContext), builder.Options)!);
        };
    }

    /// <summary>
    /// Configures EF Core using a DbContextOptionsBuilder action receiving the resolved ConnectionString.
    /// </summary>
    public void UseEntityFramework<TContext>(Action<DbContextOptionsBuilder<TContext>, string> configureDbContext)
        where TContext : AppSettingsDbContext
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new InvalidOperationException(
                "ConnectionString must be configured before calling UseEntityFramework with connectionString delegate.");
        }

        StorageEngineFactory = () =>
        {
            var builder = new DbContextOptionsBuilder<TContext>();
            configureDbContext(builder, ConnectionString);
            return new EfCoreStorageEngine(() => 
                (TContext)Activator.CreateInstance(typeof(TContext), builder.Options)!);
        };
    }
}
```

### 3. DbAppSettingsExtensions Generic Overloads

Provide generic extension overloads for `IConfigurationBuilder` and `IServiceCollection`:

```csharp
public static class DbAppSettingsExtensions
{
    public static IConfigurationBuilder AddDbAppSettings<TContext>(
        this IConfigurationBuilder builder,
        IConfiguration bootstrapConfig,
        Action<DbAppSettingsMutableOptions>? configure = null)
        where TContext : AppSettingsDbContext
    {
        // Extracts connection string, initializes mutable options, invokes configure lambda, adds DbAppSettingsConfigurationSource
    }

    public static IServiceCollection AddDbAppSettingsServices<TContext>(
        this IServiceCollection services,
        Action<DbAppSettingsMutableOptions> configure)
        where TContext : AppSettingsDbContext
    {
        // Registers DbAppSettingsOptions, IDbAppSettingsStorageEngine, IDbAppSettingsReader, IDbAppSettingsWriter, SeedService, ReloadBackgroundService
    }
}
```

### 4. Removal of Default SQLite Fallbacks

- `DbAppSettingsProvider.CreateDefaultEfEngine()`: Removed. If `StorageEngineFactory` is not configured, throw a clear `InvalidOperationException("A storage engine (EF Core or Dapper) must be explicitly configured.")`.
- `ReloadBackgroundService`: Removed parameterless SQLite fallback constructor. Requires `IDbAppSettingsStorageEngine` via constructor injection.

## Handoff

- Findings: Generic extension points on `DbAppSettingsMutableOptions` and `DbAppSettingsExtensions` allow host applications to configure any `TContext : AppSettingsDbContext` and any relational provider without SQLite defaults.
- Confidence: high.
- Assumptions: The host application has a constructor in `TContext` that accepts `DbContextOptions<TContext>` or `DbContextOptions`.
- Open questions: What specific questions should be posed to the user during the mandatory mid-reasoning human checkpoint?
