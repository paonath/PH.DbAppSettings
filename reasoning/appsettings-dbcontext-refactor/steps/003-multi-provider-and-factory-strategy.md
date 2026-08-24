---
step: "003"
title: "Multi-Provider Strategy, Connection String Inheritance, and Design-Time Factory Lifecycle"
status: "completed"
created_at: "2026-08-24T10:00:10+02:00"
---

# Multi-Provider Strategy, Connection String Inheritance, and Design-Time Factory Lifecycle

## Purpose

Analyze relational provider extensibility (Npgsql, SQL Server, Pomelo MySQL, SQLite), dynamic connection string inheritance, and the role of design-time migrations when `AppSettingsDbContext` is an abstract class.

## Multi-Provider Relational Support

### 1. Provider Decoupling in Core Assembly

- `PH.DbAppSettings` targets `net10.0` and references `Microsoft.EntityFrameworkCore` and `Microsoft.EntityFrameworkCore.Relational`.
- Concrete relational drivers (such as `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.SqlServer`, `Pomelo.EntityFrameworkCore.MySql`, `Microsoft.EntityFrameworkCore.Sqlite`) are provided by the host application.
- This avoids bloated dependencies in the core package and allows the host application to use any version and dialect.

### 2. Provider Configuration Flow

The host application configures its preferred provider via standard `DbContextOptionsBuilder` lambda:

```csharp
// Example with PostgreSQL / Npgsql:
builder.Configuration.AddDbAppSettings<AppDbContext>(bootstrapConfig, options =>
{
    options.UseEntityFramework<AppDbContext>((builder, connStr) => 
        builder.UseNpgsql(connStr));
});

// Example with SQL Server:
builder.Configuration.AddDbAppSettings<AppDbContext>(bootstrapConfig, options =>
{
    options.UseEntityFramework<AppDbContext>((builder, connStr) => 
        builder.UseSqlServer(connStr));
});

// Example with Pomelo MySQL:
builder.Configuration.AddDbAppSettings<AppDbContext>(bootstrapConfig, options =>
{
    options.UseEntityFramework<AppDbContext>((builder, connStr) => 
        builder.UseMySql(connStr, ServerVersion.AutoDetect(connStr)));
});
```

### 3. Connection String Inheritance

- Connection strings can be read from standard configuration keys:
  - `"ConnectionStrings:DefaultConnection"`
  - `"DbAppSettings:ConnectionString"`
  - Environment variable `"DbAppSettings__ConnectionString"` or `"ConnectionStrings__DefaultConnection"`
- The connection string is resolved once during bootstrap and passed into the provider configuration delegate, ensuring the application database credentials and settings are shared.

## Design-Time Migrations and Factory Strategy

### 1. The Migration Paradigm with Abstract Base Context

- When `AppSettingsDbContext` is abstract, it is not an independent deployable database schema.
- Instead, the host application's derived `AppDbContext` is the entity framework context that owns the database schema.
- When the host runs `dotnet ef migrations add AddAppSettings` in their project:
  - EF Core scans `AppDbContext`.
  - EF Core finds `DbSet<AppSettingEntry> AppSettings` and invokes `base.OnModelCreating(modelBuilder)`.
  - EF Core generates DDL migrations tailored to the host's provider (e.g. `npgsql`, `sqlserver`, `mysql`, `sqlite`).

### 2. Elimination of Hardcoded SQLite Factory in Core Library

- `AppSettingsDbContextFactory` in `src/PH.DbAppSettings/Data/` implemented `IDesignTimeDbContextFactory<AppSettingsDbContext>` with hardcoded SQLite (`"Data Source=designtime.db"`).
- Because `AppSettingsDbContext` is now abstract, `IDesignTimeDbContextFactory<AppSettingsDbContext>` is invalid at runtime and design time (EF Core cannot instantiate an abstract class).
- The obsolete `AppSettingsDbContextFactory` must be removed from `PH.DbAppSettings` (along with the obsolete migration snapshot files tied to the old concrete context).
- If needed, an extensible base factory or test-specific factory can be provided for unit testing in `tests/PH.DbAppSettings.Tests/`.

## Handoff

- Findings: Abstracting `AppSettingsDbContext` moves migration ownership to the host application's `AppDbContext`, eliminating the need for hardcoded SQLite design-time factories in the core library and enabling native support for PostgreSQL, SQL Server, MySQL, and SQLite.
- Confidence: high.
- Assumptions: The host application manages migrations for its own database or uses `EnsureCreatedAsync()`.
- Open questions: How should `DbAppSettingsExtensions` and `EfCoreStorageEngine` be refactored to support generic and non-generic context injection?
