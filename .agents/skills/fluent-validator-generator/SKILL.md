---
name: fluent-validator-generator
description: |
  Generates C# FluentValidation validators following best-practice layered patterns.
  Use when: (1) a user asks to create a validator for a request/DTO class,
  (2) a user says "generate validator for [Entity]Request",
  (3) adding a new service operation that needs input validation,
  (4) a user asks "how do I validate with FluentValidation in this project",
  (5) asked to add role-based or auth validation to an operation.
---

# FluentValidation Validator Generator

Generate `AbstractValidator<T>` subclasses following best-practice patterns for C# projects.  
Package: `FluentValidation` (NuGet). Docs: https://docs.fluentvalidation.net/en/latest/

> **Before generating**:
> 1. Use TokenSave MCP tools (specifically `tokensave_search`, `tokensave_context`, or `tokensave_files`) to locate the request/DTO class and check whether the project already defines custom base validator classes (e.g., `BaseAuthenticatedValidator<T>`, `BasedRolesValidator<T>`, a sanitization base) or existing validator constants.
> 2. If target class files, references, or DTO definitions are large (>150 lines), use `headroom_compress` to compress the content and optimize context window usage.
> 3. Read their source and inherit from them instead of `AbstractValidator<T>` directly. See [./references/base-classes.md](./references/base-classes.md) for an example hierarchy.


---

## Step 1: Choose the Correct Base Class

Use this decision tree:

```
Does the project define custom base validator classes?
├── YES → Read those classes and use the most appropriate one (see note above)
└── NO  → Use AbstractValidator<T> directly

Does the validator operate on a search/query/pagination DTO?
├── YES → Add pagination range checks (PageNumber ≥ 1, PageSize ∈ [1..N])
└── NO  → Standard property rules

Does validation require an authenticated user?
├── NO  → Inherit from the simplest available base (or AbstractValidator<T>)
└── YES → Does it require specific roles?
          ├── NO  → Inherit from the authentication base class
          └── YES → Inherit from the role-check base class
                    └── Pass the required role(s) to the base constructor
```

**Typical hierarchy** (adapt to the project's actual base classes):
```
AbstractValidator<T>                    ← FluentValidation built-in
  SanitizerValidator<T>                 ← project root; applies XSS sanitisation
    QueryRequestValidator<T>            ← validates pagination parameters
      TenantQueryRequestValidator<T>    ← + tenant existence check
    BaseAuthenticatedValidator<T>       ← requires authenticated principal
      BasedRolesValidator<T>            ← + role membership check
```

---

## Step 2: Define Error Message Constants

Declare all error messages as `public static string` constants (`SCREAMING_SNAKE_CASE`) at the top of the class. Never inline string literals in `.WithMessage()`. See the project's FluentValidation coding standards or existing validators for the established pattern.

---

## Step 3: Write Validation Rules

### 3a. Required string + max length (most common pattern)

```csharp
RuleFor(x => x.CategoryId)
    .NotEmpty()
    .WithMessage(CATEGORY_REQUIRED)
    .MaximumLength(100)
    .WithMessage(CATEGORY_MAX_LEN);
```

### 3b. DB existence check (async, EF Core)

Always use `AnyAsync` for FK / existence checks. Always call `.Trim()` on string values before comparison.

```csharp
RuleFor(x => x.CategoryId)
    .MustAsync(async (request, id, token) =>
        await _ctx.Categories.AnyAsync(c => c.Id == id.Trim(), token))
    .WithMessage(CATEGORY_NOT_FOUND);
```

### 3c. Optional FK (nullable, skip check when null)

```csharp
RuleFor(x => x.ManagerId)
    .MustAsync(async (req, id, token) =>
    {
        if (!id.HasValue) { return true; }
        return await _ctx.Users.AnyAsync(u => u.Id == id.Value, token);
    })
    .WithMessage(MANAGER_NOT_FOUND);
```

### 3d. Uniqueness (cross-field, rule on whole object)

Use `RuleFor(x => x)` for cross-property uniqueness. Use `.WithName()` to bind the error
to the correct property in the validation response.

```csharp
RuleFor(x => x)
    .MustAsync(async (request, token) =>
    {
        var exists = await _ctx.Products.AnyAsync(p =>
            p.CategoryId == request.CategoryId.Trim() &&
            p.Name != null &&
            p.Name.ToUpper() == request.Name.ToUpper().Trim(), token);
        return !exists;
    })
    .WithName(nameof(CreateProductRequest.Name))
    .WithMessage(NAME_UNIQUE);
```

### 3e. Conditional rules

```csharp
// Rule-level condition — only applies when predicate is true
RuleFor(x => x.Description)
    .MaximumLength(500)
    .WithMessage(DESCRIPTION_MAX_LEN)
    .When(x => !string.IsNullOrEmpty(x.Description));

// Block-level condition — multiple rules under the same guard
When(x => x.Type == ItemType.Physical, () =>
{
    RuleFor(x => x.Weight).GreaterThan(0).WithMessage(WEIGHT_REQUIRED);
    RuleFor(x => x.WarehouseId).NotEmpty().WithMessage(WAREHOUSE_REQUIRED);
});
```

### 3f. Collection validation

```csharp
// Null + empty guards on the collection itself
RuleFor(x => x.Tags)
    .NotNull().WithMessage(TAGS_REQUIRED)
    .NotEmpty().WithMessage(TAGS_NOT_EMPTY);

// Structural constraint on elements
RuleFor(x => x.UserIds)
    .Must(ids => ids == null || ids.All(id => id > 0))
    .WithMessage(USER_ID_POSITIVE);

// Per-element rules
RuleForEach(x => x.Emails)
    .NotNull()
    .EmailAddress(EmailValidationMode.AspNetCoreCompatible);
```

### 3g. Dependent rules (cascade: inner rules only run when outer passes)

```csharp
RuleFor(x => x.Id)
    .MustAsync(async (req, id, token) =>
        await _ctx.Orders.FindAsync([id], token) is not null)
    .WithMessage(ORDER_NOT_FOUND)
    .Cascade(CascadeMode.Stop)
    .DependentRules(() =>
    {
        RuleFor(x => x.Id)
            .MustAsync(async (req, id, token) =>
            {
                var order = await _ctx.Orders.FindAsync([id], token);
                return order!.Status != OrderStatus.Cancelled;
            })
            .WithMessage(ORDER_CANCELLED);
    });
```

### 3h. Custom async (multi-property failures from one block)

Use `CustomAsync` when a single async check needs to emit failures on different properties.

```csharp
RuleFor(x => x.Quantity)
    .CustomAsync(async (qty, context, token) =>
    {
        var item = await _ctx.Inventory
            .FirstOrDefaultAsync(i => i.Id == context.InstanceToValidate.ItemId, token);

        if (item is null)
            context.AddFailure(nameof(OrderLineRequest.ItemId), ITEM_NOT_FOUND);
        else if (item.Stock < qty)
            context.AddFailure(nameof(OrderLineRequest.Quantity), INSUFFICIENT_STOCK);
    })
    .When(x => x.Quantity > 0);
```

### 3i. Entity caching (avoid repeated DB round-trips in the same validator)

```csharp
private readonly Dictionary<int, MyEntity?> _cache = new();

private async Task<MyEntity?> GetEntity(int id, CancellationToken token)
{
    if (!_cache.ContainsKey(id))
    {
        _cache[id] = await _ctx.MyEntities.FindAsync([id], token);
    }
    return _cache[id];
}
```

### 3j. Class-level cascade (stop all rules on first failure)

Use for complex operation validators where later rules are meaningless after an early failure:

```csharp
ClassLevelCascadeMode = CascadeMode.Stop;
```

### 3k. Validator composition (Include)

Reuse another validator's rules without inheritance:

```csharp
Include(sp.GetRequiredService<AddressValidator>());
```

---

## Step 4: Full Concrete Validator Template

Replace `<Domain>`, `<Entity>`, `<RequestType>`, and `<AppDbContext>` with project-specific names.  
Replace `BasedRolesValidator` / constructor signature with the project's actual base class.

```csharp
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace YourApp.Validators.<Domain>
{
    /// <summary>
    /// Validates a <see cref="<RequestType>"/> before creating or updating a <Entity>.
    /// </summary>
    public class <Entity>Validator : AbstractValidator<<RequestType>>
    {
        private readonly <AppDbContext> _ctx;

        // ── Error message constants ──────────────────────────────────────────
        public static string NAME_REQUIRED    = "Name is required.";
        public static string NAME_MAX_LEN     = "Name must be at most 100 characters long.";
        public static string PARENT_REQUIRED  = "ParentId is required.";
        public static string PARENT_NOT_FOUND = "Parent not found.";
        public static string NAME_UNIQUE      = "An item with this name already exists in the parent.";

        /// <summary>
        /// Initializes a new instance of the <see cref="<Entity>Validator"/> class.
        /// </summary>
        /// <param name="ctx">The application database context.</param>
        public <Entity>Validator(<AppDbContext> ctx)
        {
            _ctx = ctx;

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(NAME_REQUIRED)
                .MaximumLength(100).WithMessage(NAME_MAX_LEN);

            RuleFor(x => x.ParentId)
                .NotEmpty().WithMessage(PARENT_REQUIRED)
                .Cascade(CascadeMode.Stop)
                .MustAsync(async (id, token) =>
                    await _ctx.Parents.AnyAsync(p => p.Id == id.Trim(), token))
                .WithMessage(PARENT_NOT_FOUND);

            RuleFor(x => x)
                .MustAsync(async (req, token) =>
                {
                    var exists = await _ctx.Entities.AnyAsync(e =>
                        e.ParentId == req.ParentId.Trim() &&
                        e.Name != null &&
                        e.Name.ToUpper() == req.Name!.ToUpper().Trim(), token);
                    return !exists;
                })
                .WithName(nameof(<RequestType>.Name))
                .WithMessage(NAME_UNIQUE);
        }
    }
}
```

**Adapt the constructor** when the project uses a base class requiring authentication or roles:

```csharp
// Example: project-specific base requiring an authenticated user + role array
public <Entity>Validator(ICurrentUser currentUser, <AppDbContext> ctx)
    : base(currentUser, [AppRole.Admin])
{
    _ctx = ctx;
    // ... rules ...
}
```

---

## Step 5: Register in DI

Register following the project's established DI Registration patterns (check `AGENTS.md` or existing service registrations) — validators as `Scoped` concrete types. See also [`references/di-registration.md`](./references/di-registration.md).

---

## Step 6: Use in the Service Layer

Inject the validator and wrap the result in a `ValidationResult` (or a project-specific flow
wrapper). Always log a warning when validation fails.

```csharp
public async Task<(bool IsValid, IEnumerable<string> Errors, ProductDto? Item)> CreateAsync(
    CreateProductRequest request, CancellationToken token)
{
    _logger.LogTrace("{Method} started - Request: {@Request}", nameof(CreateAsync), request);

    var result = await _validator.ValidateAsync(request, token);

    if (!result.IsValid)
    {
        var errors = result.Errors.Select(e => e.ErrorMessage);
        _logger.LogWarning("{Method} validation failed: {Errors}", nameof(CreateAsync), errors);
        return (false, errors, null);
    }

    // ... business logic ...

    _logger.LogTrace("{Method} completed", nameof(CreateAsync));
    return (true, [], dto);
}
```

> If the project defines a `ValidationResultFlow` wrapper (or equivalent), use that instead
> of returning raw `ValidationResult`. Follow the project's established service-return pattern.

---

## References

- [base-classes.md](./references/base-classes.md) — example hierarchy of project-specific base classes
- [validation-patterns.md](./references/validation-patterns.md) — advanced patterns: entity caching, DependentRules, CustomAsync, RuleForEach
- [di-registration.md](./references/di-registration.md) — DI registration decision table and examples
- Official built-in validators: https://docs.fluentvalidation.net/en/latest/built-in-validators.html
- Official DI docs: https://docs.fluentvalidation.net/en/latest/di.html
