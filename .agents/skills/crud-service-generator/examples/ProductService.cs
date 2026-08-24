// ─── Generic CRUD Service Example ────────────────────────────────────────────
//
// Substitute the following placeholders with your project's actual types:
//
//   AppDbContext              → your EF Core DbContext
//   ServiceBase               → your project's service base class
//   ValidationResultFlow      → your project's validation result wrapper
//   NewId.NextGuid()          → your ID generation strategy (Guid.NewGuid(), etc.)
//
// This file uses a "Product" domain as a concrete illustration.
// ─────────────────────────────────────────────────────────────────────────────

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace YourApp.Services;

// ── Entity (EF Core) ─────────────────────────────────────────────────────────
// Defined in your DAL project. Shown here for reference only.
// public class Product
// {
//     public string Id          { get; set; } = string.Empty;
//     public string Name        { get; set; } = string.Empty;
//     public string? Description { get; set; }
//     public string CategoryId  { get; set; } = string.Empty;
//     public bool?  IsDeleted   { get; set; }
// }

// ── DTOs (Models/DTO project or equivalent) ──────────────────────────────────
// Defined in your Models/DTO project. Shown here for reference only.
// public record ProductDto(string Id, string Name, string? Description, string CategoryId);
// public record ProductCreateRequest(string Name, string? Description, string CategoryId);

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Service interface exposing CRUD operations for <see cref="Product"/> entities.
/// </summary>
public interface IProductService
{
    /// <summary>Returns all non-deleted products.</summary>
    Task<ProductDto?[]> GetAll(CancellationToken token);

    /// <summary>Returns the product with <paramref name="id"/>, or <see langword="null"/> if not found.</summary>
    Task<ProductDto?> GetById(string id, CancellationToken token);

    /// <summary>Creates a new product. Returns a validation result and the created DTO.</summary>
    Task<(ValidationResultFlow Validation, ProductDto? Item)> Create(
        ProductCreateRequest request, CancellationToken token);

    /// <summary>Updates an existing product. Returns a validation result and the updated DTO.</summary>
    Task<(ValidationResultFlow Validation, ProductDto? Item)> Edit(
        ProductDto request, CancellationToken token);

    /// <summary>Soft-deletes a product. Returns a validation result and a success flag.</summary>
    Task<(ValidationResultFlow Validation, bool Deleted)> Delete(
        string id, CancellationToken token);
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Implements <see cref="IProductService"/> using EF Core and FluentValidation.
/// </summary>
public class ProductService : ServiceBase, IProductService
{
    private readonly AppDbContext _ctx;
    private readonly ILogger<ProductService> _logger;

    // Inject concrete validator types — NOT IValidator<T>
    private readonly ProductCreateRequestValidator _createValidator;
    private readonly ProductDtoValidator _editValidator;

    /// <summary>
    /// Initialises a new instance of <see cref="ProductService"/>.
    /// </summary>
    /// <param name="ctx">The application database context.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="createValidator">Validator for create requests.</param>
    /// <param name="editValidator">Validator for edit requests and deletion eligibility.</param>
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

    // ── Mapping ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Maps an entity to its DTO. Returns <see langword="null"/> when the entity is <see langword="null"/>.
    /// </summary>
    private static ProductDto? ToDto(Product? entity)
    {
        if (entity is null) { return null; }
        return new ProductDto(entity.Id, entity.Name, entity.Description, entity.CategoryId);
    }

    // ── Read ─────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public async Task<ProductDto?> GetById(string id, CancellationToken token)
    {
        _logger.LogTrace("{Method} started for id '{Id}'", nameof(GetById), id);

        var entity = await _ctx.Products
            .AsNoTracking()                                              // read-only: no change tracking
            .Where(p => !p.IsDeleted.HasValue || p.IsDeleted == false)  // exclude soft-deleted
            .Where(p => p.Id == id)
            .SingleOrDefaultAsync(token);

        if (entity is null)
        {
            _logger.LogDebug("{Method}: Product '{Id}' not found", nameof(GetById), id);
        }

        return ToDto(entity);
    }

    // ── Write ─────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<(ValidationResultFlow Validation, ProductDto? Item)> Create(
        ProductCreateRequest request, CancellationToken token)
    {
        _logger.LogTrace("{Method} started at {StartTime} - Payload: {@Request}",
            nameof(Create), DateTime.UtcNow, request);

        // 1. Validate the incoming request
        var validation = await ValidationResultFlow.Init(
            nameof(Create), () => _createValidator.ValidateAsync(request, token));

        if (!validation.IsValid)
        {
            _logger.LogDebug("{Method} validation failed: {@Errors}", nameof(Create), validation.Errors);
            return (validation, null);
        }

        // 2. Build the entity — always trim string values before persisting
        var entity = new Product
        {
            Id          = NewId.NextGuid(),          // ← replace with your ID strategy
            Name        = request.Name.Trim(),
            Description = request.Description?.Trim(),
            CategoryId  = request.CategoryId.Trim(),
            IsDeleted   = false,
        };

        // 3. If the project uses an audit system, set the author here:
        //    e.g., _ctx.Author = _currentUser.Email;

        await _ctx.Products.AddAsync(entity, token);
        await _ctx.SaveChangesAsync(token);

        _logger.LogDebug("{Method}: Product '{Id}' created", nameof(Create), entity.Id);
        _logger.LogTrace("{Method} completed at {EndTime}", nameof(Create), DateTime.UtcNow);
        return (validation, ToDto(entity));
    }

    /// <inheritdoc/>
    public async Task<(ValidationResultFlow Validation, ProductDto? Item)> Edit(
        ProductDto request, CancellationToken token)
    {
        _logger.LogTrace("{Method} started at {StartTime} - Payload: {@Request}",
            nameof(Edit), DateTime.UtcNow, request);

        // 1. Validate the DTO (includes ID existence check inside the validator)
        var validation = await ValidationResultFlow.Init(
            nameof(Edit), () => _editValidator.ValidateAsync(request, token));

        if (!validation.IsValid)
        {
            return (validation, null);
        }

        // 2. Load the tracked entity for mutation
        var entity = await _ctx.Products.FindAsync([request.Id], token);
        if (entity is null)
        {
            // entity was deleted between validation and load — treat as NotFound
            return (validation.NotFound(request.Id), null);
        }

        // 3. Mutate in-place — trim all string inputs
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

    /// <inheritdoc/>
    public async Task<(ValidationResultFlow Validation, bool Deleted)> Delete(
        string id, CancellationToken token)
    {
        _logger.LogTrace("{Method} started at {StartTime} for id '{Id}'",
            nameof(Delete), DateTime.UtcNow, id);

        // 1. Check deletion eligibility (e.g., no dependent records)
        //    The edit validator exposes a CanDelete(id, token) method for this purpose.
        //    If not available, use an inline lambda (see SKILL.md Step 6).
        var validation = await ValidationResultFlow.Init(
            nameof(Delete), () => _editValidator.CanDelete(id, token));

        if (!validation.IsValid)
        {
            return (validation, false);
        }

        // 2. Load the entity
        var entity = await _ctx.Products.FindAsync([id], token);
        if (entity is null)
        {
            return (validation.NotFound(id), false);
        }

        // 3. Soft delete — NEVER call Remove(); set the flag and update
        entity.IsDeleted = true;
        _ctx.Products.Update(entity);
        await _ctx.SaveChangesAsync(token);

        _logger.LogDebug("{Method}: Product '{Id}' soft-deleted", nameof(Delete), id);
        _logger.LogTrace("{Method} completed at {EndTime}", nameof(Delete), DateTime.UtcNow);
        return (validation, true);
    }
}
