# Advanced Validation Patterns

Common patterns for complex FluentValidation validators in C# projects.

---

## Pattern 1: Entity Caching

Used when a validator must check the same entity multiple times (e.g., in `RuleForEach` or
multiple rules). Avoids repeated DB round-trips.

```csharp
private readonly Dictionary<int, MyEntity?> _cache = new();

private async Task<MyEntity?> GetCached(int id, CancellationToken token)
{
    if (!_cache.ContainsKey(id))
    {
        _cache[id] = await _ctx.MyEntities.FindAsync([id], token);
    }
    return _cache[id];
}

// Usage inside a MustAsync rule:
RuleForEach(x => x.EntityIds)
    .MustAsync(async (req, id, token) =>
    {
        var entity = await GetCached(id, token);
        return entity is not null;
    })
    .WithMessage(ENTITY_NOT_FOUND);
```

---

## Pattern 2: DependentRules

Runs inner rules only when the outer rule passes. Use for cascaded business-rule checks
(e.g., only validate a property of an entity if that entity exists).

```csharp
RuleFor(x => x.Id)
    .MustAsync(async (req, id, token) =>
        await _ctx.Items.FindAsync([id], token) is not null)
    .WithMessage(ITEM_NOT_FOUND)
    .Cascade(CascadeMode.Stop)
    .DependentRules(() =>
    {
        RuleFor(x => x.Id)
            .MustAsync(async (req, id, token) =>
            {
                var item = await _ctx.Items.FindAsync([id], token);
                return item!.IsActive;
            })
            .WithMessage(ITEM_INACTIVE);
    });
```

---

## Pattern 3: CustomAsync — Multi-Field Failures

Use `CustomAsync` when one async check must emit failures on different properties.

```csharp
RuleFor(x => x.OrderId)
    .CustomAsync(async (orderId, context, token) =>
    {
        var req = context.InstanceToValidate;
        var order = await _ctx.Orders
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == orderId, token);

        if (order is null)
        {
            context.AddFailure(nameof(MyRequest.OrderId), ORDER_NOT_FOUND);
            return;
        }

        if (order.IsClosed)
            context.AddFailure(nameof(MyRequest.OrderId), ORDER_CLOSED);

        if (order.Customer.IsBlocked)
            context.AddFailure(nameof(MyRequest.CustomerId), CUSTOMER_BLOCKED);
    })
    .When(x => x.OrderId > 0);
```

---

## Pattern 4: RuleForEach with Collection Validation

```csharp
// Validate each email in a list
RuleForEach(x => x.Emails)
    .NotNull()
    .EmailAddress(EmailValidationMode.AspNetCoreCompatible);

// Validate each ID against the database
RuleForEach(x => x.UserIds)
    .MustAsync(async (req, userId, token) =>
        await _ctx.Users.AnyAsync(u => u.Id == userId, token))
    .WithMessage(USER_NOT_FOUND);
```

---

## Pattern 5: Block-Level When (When + lambda)

Group multiple rules that only apply under a condition:

```csharp
When(x => x.Type == TransferType.External, () =>
{
    RuleFor(x => x.DestinationAccountId)
        .NotEmpty()
        .WithMessage(DESTINATION_REQUIRED);

    RuleFor(x => x.DestinationAccountId)
        .MustAsync(async (req, id, token) =>
            await _ctx.Accounts.AnyAsync(a => a.Id == id, token))
        .WithMessage(DESTINATION_NOT_FOUND);
});
```

---

## Pattern 6: Validator Composition via Include

Use `Include()` to embed another validator's rules without inheritance. Useful for combining
reusable bases (e.g., pagination) with domain-specific rules.

```csharp
public class SearchProductsValidator : BaseAuthenticatedValidator<SearchProductsRequest>
{
    public SearchProductsValidator(ICurrentUser user, AppDbContext ctx)
        : base(user)
    {
        // Include shared pagination rules
        Include(new QueryRequestValidator<SearchProductsRequest, ProductDto>());

        // Domain-specific rules
        RuleFor(x => x.DateFrom)
            .LessThanOrEqualTo(x => x.DateTo)
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue)
            .WithMessage(DATE_RANGE_INVALID);
    }
}
```

---

## Pattern 7: Out-of-Band Validation (Custom Public Methods)

Validators can expose additional public async methods for business-rule checks that don't fit
the `ValidateAsync` pipeline (e.g., a `CanDelete` check before deletion).

```csharp
public class ProductValidator : AbstractValidator<CreateProductRequest>
{
    // Standard pipeline
    public ProductValidator(AppDbContext ctx) { /* rules */ }

    // Out-of-band: soft-delete eligibility check
    public async Task<ValidationResult> CanDelete(int id, CancellationToken token)
    {
        var result = new ValidationResult();

        var product = await _ctx.Products.FindAsync([id], token);
        if (product is null)
        {
            result.Errors.Add(new ValidationFailure(nameof(id), PRODUCT_NOT_FOUND));
            return result;
        }

        var hasOpenOrders = await _ctx.OrderLines
            .AnyAsync(ol => ol.ProductId == id && !ol.Order.IsClosed, token);
        if (hasOpenOrders)
            result.Errors.Add(new ValidationFailure(nameof(id), PRODUCT_HAS_OPEN_ORDERS));

        return result;
    }
}
```

---

## Pattern 8: Role-Conditional Logic Inside Rules

When privileged users (e.g., admins) bypass a restriction that applies to other roles:

```csharp
RuleFor(x => x.Status)
    .MustAsync(async (req, status, token) =>
    {
        // Privileged users bypass this check
        if (CurrentUser.Principal.IsInRole(AppRoles.Admin))
            return true;

        // All other roles: enforce business constraint
        return await _ctx.Items.AnyAsync(i =>
            i.Id == req.Id && i.AllowedStatuses.Contains(status), token);
    })
    .WithMessage(STATUS_NOT_ALLOWED);
```

---

## Pattern 9: ClassLevelCascadeMode

For validators with many rules where later rules are meaningless after an early failure,
set `ClassLevelCascadeMode = CascadeMode.Stop` at the top of the constructor:

```csharp
public class UpdateOrderValidator : AbstractValidator<UpdateOrderRequest>
{
    public UpdateOrderValidator(AppDbContext ctx)
    {
        ClassLevelCascadeMode = CascadeMode.Stop;  // ← stop on first failure

        // ... rules ...
    }
}
```

---

## Pattern 10: Case-Insensitive Uniqueness Check

Always call `.Trim()` and use `.ToUpperInvariant()` / `.ToUpper()` for case-insensitive DB
string comparisons to prevent duplicate records differing only by whitespace or casing.

```csharp
RuleFor(x => x.Code)
    .MustAsync(async (req, code, token) =>
    {
        var exists = await _ctx.Products.AnyAsync(p =>
            p.Code.ToUpperInvariant() == code.Trim().ToUpperInvariant(), token);
        return !exists;
    })
    .WithMessage(CODE_DUPLICATE);
```
