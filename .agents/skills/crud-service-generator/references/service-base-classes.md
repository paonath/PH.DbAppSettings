# Service Base Classes Reference

This file illustrates a common base class hierarchy for C# CRUD services.
Adapt class names, namespaces, and injected types to match your project's actual implementation.

> **How to use**: If your project already defines a `ServiceBase` or `Service` class,
> read its source first and inherit from it directly. Use this file as a structural reference only.

---

## ServiceBase (root abstract class)

**Role**: Defines the common contract for all services. Typically implements
`IService`, `IDisposable`, and/or `IAsyncDisposable`. Carries a scoped lifecycle identifier
(current user or tenant) that the audit system uses.

```csharp
/// <summary>
/// Abstract base for all domain services.
/// </summary>
public abstract class ServiceBase : IDisposable, IAsyncDisposable
{
    /// <summary>The identifier of the user executing the operation (for audit trails).</summary>
    protected string? Identifier { get; }

    protected ServiceBase(string? identifier)
    {
        Identifier = identifier;
    }

    public virtual void Dispose() { }
    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

---

## DataService (scoped request context)

Some projects wrap the `DbContext` and the current-user identity in a single injectable
`DataService` object, so every service receives both in one constructor parameter.

```csharp
/// <summary>
/// Provides the scoped DbContext and current-user identity for a service.
/// </summary>
public class DataService
{
    /// <summary>The EF Core database context for this request.</summary>
    public AppDbContext Context { get; }

    /// <summary>The authenticated user's identifier (e.g., email or sub claim).</summary>
    public string? Identifier { get; }

    public DataService(AppDbContext context, ICurrentUser currentUser)
    {
        Context = context;
        Identifier = currentUser.Email;
    }
}
```

When a project uses `DataService`, the service constructor looks like this:

```csharp
public ProductService(
    DataService dataService,
    ILogger<ProductService> logger,
    ProductCreateRequestValidator createValidator,
    ProductDtoValidator editValidator)
    : base(dataService.Identifier)           // passes identity to ServiceBase
{
    _ctx     = dataService.Context;          // extract DbContext from the wrapper
    _logger  = logger.BeginScope(nameof(ProductService)) as ILogger<ProductService> ?? logger;
    _createValidator = createValidator;
    _editValidator   = editValidator;
}
```

> If your project injects `AppDbContext` directly (without `DataService`), the base call becomes
> `base(currentUser.Identifier)` or is omitted if ServiceBase has a parameterless constructor.

---

## Audit System Integration

If your project auto-audits EF Core changes (inserts/updates/deletes), set the author on the
context **before** calling `SaveChangesAsync`:

```csharp
// Set this once per operation, before SaveChangesAsync:
_ctx.Author = Identifier;           // or however your audit system reads the user

await _ctx.Products.AddAsync(entity, token);
await _ctx.SaveChangesAsync(token);
```

Apply `[SkipAudit]` on entity properties that should not be tracked:

```csharp
[SkipAudit]
public string? InternalNotes { get; set; }
```

---

## IEntity\<TKey\> (common entity base)

Most entities implement a typed `IEntity<TKey>` interface that exposes the primary key:

```csharp
public interface IEntity<TKey>
{
    TKey Id { get; set; }
}
```

Entity example:

```csharp
public class Product : IEntity<string>
{
    public string  Id          { get; set; } = NewId.NextGuid();
    public string  Name        { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string  CategoryId  { get; set; } = string.Empty;
    public bool?   IsDeleted   { get; set; }   // soft-delete flag
}
```

---

## DbSet Convention

Access `DbSet<T>` directly on the context. Use a descriptive name matching the entity's plural:

```csharp
// In AppDbContext:
public DbSet<Product> Products { get; set; } = null!;
```

In the service, access via `_ctx.Products` (or `_ctx.Set<Product>()` when the set name is unknown).
