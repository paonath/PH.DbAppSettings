---
step: "002"
title: "Abstract AppSettingsDbContext Architecture and Host Entity Model Integration"
status: "completed"
created_at: "2026-08-24T09:59:58+02:00"
---

# Abstract AppSettingsDbContext Architecture and Host Entity Model Integration

## Purpose

Design the abstract `AppSettingsDbContext` hierarchy, constructor patterns, and entity mapping lifecycle.
Ensure seamless inheritance by host application `DbContext` instances while maintaining full compatibility with EF Core dependency injection and model building.

## Architectural Design

### 1. Abstract DbContext Class Hierarchy

Following the standard EF Core pattern used in Microsoft libraries (e.g. `IdentityDbContext`):

```csharp
namespace PH.DbAppSettings.Data;

/// <summary>
/// Abstract base DbContext providing configuration storage entities and mapping.
/// Inherit this class in your application's DbContext to co-locate AppSettings in the application database.
/// </summary>
public abstract class AppSettingsDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance with non-generic DbContextOptions.
    /// </summary>
    protected AppSettingsDbContext(DbContextOptions options) : base(options)
    {
    }

    /// <summary>
    /// Parameterless constructor for design-time tooling or derived custom factories.
    /// </summary>
    protected AppSettingsDbContext() : base()
    {
    }

    /// <summary>
    /// DbSet for application settings entries stored in the database.
    /// </summary>
    public DbSet<AppSettingEntry> AppSettings => Set<AppSettingEntry>();

    /// <summary>
    /// Applies the AppSettingEntry entity configuration.
    /// Derived classes must call base.OnModelCreating(modelBuilder).
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new AppSettingEntryConfiguration());
    }
}

/// <summary>
/// Generic abstract base DbContext for strongly typed DbContextOptions.
/// </summary>
/// <typeparam name="TContext">The derived DbContext type.</typeparam>
public abstract class AppSettingsDbContext<TContext> : AppSettingsDbContext where TContext : DbContext
{
    protected AppSettingsDbContext(DbContextOptions<TContext> options) : base(options)
    {
    }

    protected AppSettingsDbContext() : base()
    {
    }
}
```

### 2. Host Application DbContext Integration Example

Host applications simply inherit `AppSettingsDbContext` or `AppSettingsDbContext<AppDbContext>`:

```csharp
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) 
    : AppSettingsDbContext<AppDbContext>(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Configures AppSettingEntry
        
        // Host application entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
```

### 3. Entity Mapping and Custom Table/Schema Support

- `AppSettingEntryConfiguration` implements `IEntityTypeConfiguration<AppSettingEntry>`.
- Supports parameterized table name and schema name (defaulting to `"AppSettings"` and `"dbo"` or provider default).
- Model configuration helper extension `modelBuilder.ApplyDbAppSettings(tableName, schemaName)` to allow host applications to configure schema/table dynamically if overridden.

### 4. Benefits of this Architecture

1. **Zero Database Proliferation**: `AppSettings` is a standard table within the host application's database.
2. **Unified Transaction & Connection Pool**: DbContext operations and configuration access share the same ADO.NET connection pool and infrastructure.
3. **Transparent EF Core DI**: Standard `services.AddDbContext<AppDbContext>(opts => ...)` registers the derived context seamlessly in .NET DI.

## Handoff

- Findings: Abstract non-generic `AppSettingsDbContext` and generic `AppSettingsDbContext<TContext>` provide clean inheritance for any host `DbContext` with zero friction in EF Core constructor injection.
- Confidence: high.
- Assumptions: The host application has a single primary `DbContext` (or a dedicated configuration `DbContext`) that inherits `AppSettingsDbContext`.
- Open questions: How does design-time tooling and multi-provider resolution work during migrations and bootstrap?
