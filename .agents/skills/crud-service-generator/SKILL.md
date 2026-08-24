---
name: crud-service-generator
description: |
  Generates a complete C# CRUD service class following layered service patterns.
  Use when: (1) a user asks to create a service for an entity,
  (2) a user says "generate CRUD service for [Entity]",
  (3) adding a new domain area that needs Create/Read/Update/Delete operations,
  (4) a user asks "how do I structure a service in this project",
  (5) implementing a service layer over an existing EF Core entity.
---

# CRUD Service Generator

Generate a complete C# service class implementing **Create, Read, Update, and Delete** operations
following a layered, validation-first architecture with EF Core.

> **Before generating**:
> 1. Use TokenSave MCP tools (specifically `tokensave_search`, `tokensave_context`, or `tokensave_files`) to locate the entity class, existing service classes, DB Contexts, and validators to copy established project patterns.
> 2. If target templates, references, or source class files are large (>150 lines), use `headroom_compress` to compress the content and avoid context window clutter.
> 3. Identify the project's service base class (e.g., `ServiceBase`, `Service`), its `ValidationResultFlow` equivalent, and the `DbContext` name. See [./references/service-base-classes.md](./references/service-base-classes.md) and [./references/validation-result-flow.md](./references/validation-result-flow.md).


---

## Quick Reference: Return Types

| Operation   | Return type                           | Notes                         |
|-------------|---------------------------------------|-------------------------------|
| `GetAll`    | `Task<TDto?[]>`                       | No validation tuple           |
| `GetById`   | `Task<TDto?>`                         | Returns `null` if not found   |
| `Create`    | `Task<(Validation, TDto?)>`           | `null` item when invalid      |
| `Edit`      | `Task<(Validation, TDto?)>`           | `null` item when invalid/404  |
| `Delete`    | `Task<(Validation, bool)>`            | `false` when invalid/404      |

`Validation` = `ValidationResultFlow` (project-specific wrapper over `FluentValidation.ValidationResult`).

---

## Step 1: Define the Service Contract

Before writing the service, identify or create:

1. **Entity** — the EF Core entity class (e.g., `Product`)
2. **DTO** — the output record returned to callers (e.g., `ProductDto`)
3. **Create request** — the input record for Create (e.g., `ProductCreateRequest`)
4. **Edit validator** — the validator used for Edit AND Delete checks (e.g., `ProductDtoValidator`)
5. **Create validator** — the validator used for Create (e.g., `ProductCreateRequestValidator`)

Define the service interface:

```csharp
public interface IProductService
{
    Task<ProductDto?[]> GetAll(CancellationToken token);
    Task<ProductDto?> GetById(string id, CancellationToken token);
    Task<(ValidationResultFlow Validation, ProductDto? Item)> Create(
        ProductCreateRequest request, CancellationToken token);
    Task<(ValidationResultFlow Validation, ProductDto? Item)> Edit(
        ProductDto request, CancellationToken token);
    Task<(ValidationResultFlow Validation, bool Deleted)> Delete(
        string id, CancellationToken token);
}
```

---

## Step 2: Service Class Structure

Inherit from the project's service base class. Inject the `DbContext`, `ILogger<T>`, and
**concrete validator types** (not `IValidator<T>`).

```csharp
public class ProductService : ServiceBase, IProductService
{
    private readonly AppDbContext _ctx;
    private readonly ILogger<ProductService> _logger;
    private readonly ProductCreateRequestValidator _createValidator;
    private readonly ProductDtoValidator _editValidator;

    public ProductService(
        AppDbContext ctx,
        ILogger<ProductService> logger,
        ProductCreateRequestValidator createValidator,
        ProductDtoValidator editValidator)
    {
        _ctx = ctx;
        _logger = logger;
        _createValidator = createValidator;
        _editValidator = editValidator;
    }
```

### Static mapping method

Always use a **private static** `ToDto` method for entity→DTO mapping:

```csharp
    private static ProductDto? ToDto(Product? entity)
    {
        if (entity is null) { return null; }
        return new ProductDto(entity.Id, entity.Name, entity.Description, entity.CategoryId);
    }
```

Rules:
- Return `null` when the entity is `null`
- Map primitive properties only — no navigation properties in DTOs
- Keep it side-effect free; do not call `ToDto` on collections inside this method

---

## Step 3: Implement Read Methods

Read methods return data **directly** — no validation tuple.

### GetAll

```csharp
public async Task<ProductDto?[]> GetAll(CancellationToken token)
{
    _logger.LogTrace("{Method} started at {StartTime}", nameof(GetAll), DateTime.UtcNow);

    var entities = await _ctx.Products
        .Where(p => !p.IsDeleted.HasValue || p.IsDeleted == false)
        .ToArrayAsync(token);

    var result = entities.Select(ToDto).ToArray();
    _logger.LogDebug("{Method} returned {Count} items", nameof(GetAll), result.Length);
    return result;
}
```

### GetById

```csharp
public async Task<ProductDto?> GetById(string id, CancellationToken token)
{
    _logger.LogTrace("{Method} started for id '{Id}'", nameof(GetById), id);

    var entity = await _ctx.Products
        .AsNoTracking()                                              // read-only: skip tracking
        .Where(p => !p.IsDeleted.HasValue || p.IsDeleted == false)  // soft-delete filter
        .Where(p => p.Id == id)
        .SingleOrDefaultAsync(token);

    if (entity is null)
    {
        _logger.LogDebug("{Method}: Product '{Id}' not found", nameof(GetById), id);
    }

    return ToDto(entity);
}
```

Rules:
- Use `AsNoTracking()` for all read-only queries
- Filter soft-deleted records at the query level
- Return `null` when not found — do not throw

---

## Step 4: Implement Create

```csharp
public async Task<(ValidationResultFlow Validation, ProductDto? Item)> Create(
    ProductCreateRequest request, CancellationToken token)
{
    _logger.LogTrace("{Method} started at {StartTime} - Payload: {@Request}",
        nameof(Create), DateTime.UtcNow, request);

    // 1. Validate
    var validation = await ValidationResultFlow.Init(
        nameof(Create), () => _createValidator.ValidateAsync(request, token));

    if (!validation.IsValid)
    {
        _logger.LogDebug("{Method} validation failed: {@Errors}", nameof(Create), validation.Errors);
        return (validation, null);
    }

    // 2. Build entity — trim all string inputs
    var entity = new Product
    {
        Id          = NewId.NextGuid(),          // replace with your ID strategy
        Name        = request.Name.Trim(),
        Description = request.Description?.Trim(),
        CategoryId  = request.CategoryId.Trim(),
        IsDeleted   = false,
    };

    // 3. Persist
    await _ctx.Products.AddAsync(entity, token);
    await _ctx.SaveChangesAsync(token);

    _logger.LogDebug("{Method}: Product '{Id}' created", nameof(Create), entity.Id);
    _logger.LogTrace("{Method} completed at {EndTime}", nameof(Create), DateTime.UtcNow);
    return (validation, ToDto(entity));
}
```

Rules:
- Always `.Trim()` string inputs before writing to the database
- Set scalar defaults (e.g., `IsDeleted = false`, `Active = true`) on the entity, not in the validator
- Return `(validation, null)` immediately on failure — do not attempt to save

---

## Step 5: Implement Edit / Update

```csharp
public async Task<(ValidationResultFlow Validation, ProductDto? Item)> Edit(
    ProductDto request, CancellationToken token)
{
    _logger.LogTrace("{Method} started at {StartTime} - Payload: {@Request}",
        nameof(Edit), DateTime.UtcNow, request);

    // 1. Validate
    var validation = await ValidationResultFlow.Init(
        nameof(Edit), () => _editValidator.ValidateAsync(request, token));

    if (!validation.IsValid)
    {
        return (validation, null);
    }

    // 2. Load entity
    var entity = await _ctx.Products.FindAsync([request.Id], token);
    if (entity is null)
    {
        return (validation.NotFound(request.Id), null);
    }

    // 3. Mutate — trim all string inputs
    entity.Name        = request.Name.Trim();
    entity.Description = request.Description?.Trim();
    entity.CategoryId  = request.CategoryId.Trim();

    // 4. Persist
    _ctx.Products.Update(entity);
    await _ctx.SaveChangesAsync(token);

    _logger.LogDebug("{Method}: Product '{Id}' updated", nameof(Edit), entity.Id);
    _logger.LogTrace("{Method} completed at {EndTime}", nameof(Edit), DateTime.UtcNow);
    return (validation, ToDto(entity));
}
```

Rules:
- Use `FindAsync([id], token)` (array syntax) for primary-key lookup when writing
- Call `validation.NotFound(id)` when `FindAsync` returns `null`
- Mutate the *tracked* entity in-place; then call `Update()` before `SaveChangesAsync()`
- Do **not** re-create the entity; only mutate the fields allowed by the edit operation

---

## Step 6: Implement Delete (Soft Delete)

```csharp
public async Task<(ValidationResultFlow Validation, bool Deleted)> Delete(
    string id, CancellationToken token)
{
    _logger.LogTrace("{Method} started at {StartTime} for id '{Id}'",
        nameof(Delete), DateTime.UtcNow, id);

    // 1. Check deletion eligibility (e.g., no dependencies)
    //    The edit validator exposes a CanDelete(id, token) method — adapt if not available.
    var validation = await ValidationResultFlow.Init(
        nameof(Delete), () => _editValidator.CanDelete(id, token));

    if (!validation.IsValid)
    {
        return (validation, false);
    }

    // 2. Load entity
    var entity = await _ctx.Products.FindAsync([id], token);
    if (entity is null)
    {
        return (validation.NotFound(id), false);
    }

    // 3. Soft delete — NEVER use Remove(); set the IsDeleted flag
    entity.IsDeleted = true;
    _ctx.Products.Update(entity);
    await _ctx.SaveChangesAsync(token);

    _logger.LogDebug("{Method}: Product '{Id}' soft-deleted", nameof(Delete), id);
    _logger.LogTrace("{Method} completed at {EndTime}", nameof(Delete), DateTime.UtcNow);
    return (validation, true);
}
```

Rules:
- **Never** call `_ctx.Products.Remove(entity)` — always soft-delete via an `IsDeleted` flag
- The `CanDelete` check runs **before** loading the entity (early exit on business-rule failure)
- If the project's validator does not expose `CanDelete`, implement an inline validation lambda:

```csharp
var validation = await ValidationResultFlow.Init(nameof(Delete), () =>
{
    // custom inline check: e.g., verify no child records reference this product
    var hasChildren = _ctx.OrderLines.Any(ol => ol.ProductId == id);
    var result = new FluentValidation.Results.ValidationResult();
    if (hasChildren)
        result.Errors.Add(new FluentValidation.Results.ValidationFailure(
            nameof(id), "Cannot delete: product has associated order lines."));
    return Task.FromResult(result);
});
```

---

## Step 7: Register in DI

Register following the project's established DI Registration patterns (check `AGENTS.md` or existing service registrations) — validators as `Scoped` concrete types, service via interface.

---

## Full Example

See [./examples/ProductService.cs](./examples/ProductService.cs) for a complete, compilable example
using `Product` / `ProductDto` / `ProductCreateRequest`.

---

## References

- [./references/service-base-classes.md](./references/service-base-classes.md) — Base class structure and `DataService` pattern
- [./references/validation-result-flow.md](./references/validation-result-flow.md) — `ValidationResultFlow` API reference

Related skills:
- `fluent-validator-generator` — how to write `ProductCreateRequestValidator` and `ProductDtoValidator`
- `csharp-dto-generator` — how to write `ProductDto` and `ProductCreateRequest` records
