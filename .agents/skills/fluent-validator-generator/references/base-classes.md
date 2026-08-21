# Base Validator Classes Reference

This file illustrates a common layered base-class hierarchy for FluentValidation in C# projects.
Adapt class names, namespaces, and DI types to match your project's actual implementation.

> **How to use**: If your project already defines base validator classes, read their source files
> and use these patterns as a reference — not as a template to copy verbatim.

---

## SanitizerValidator\<T\> (example: XSS root base)

**Role**: Root base class for ALL validators. Applies XSS sanitisation (e.g., `WithNoScripts()`)
on the whole object. Provides two reusable error message constants.

```csharp
public class SanitizerValidator<T> : AbstractValidator<T>
{
    public static string NOT_FOUND => "Not found.";
    public static string FORBIDDEN => "Forbidden / Not allowed.";

    public SanitizerValidator()
    {
        // Optional: apply a global XSS guard via an extension library
        // e.g., PH.FluentValidationExtensions provides WithNoScripts()
        RuleFor(x => x).WithNoScripts();
    }
}
```

`WithNoScripts()` rejects any string property containing HTML/script injection patterns.
If not available in your project, omit this rule or replace with your own sanitisation.

---

## BaseAuthenticatedValidator\<T\>

**Role**: Requires that the current user is authenticated before any domain rules run.  
**Base**: `SanitizerValidator<T>`

```csharp
public abstract class BaseAuthenticatedValidator<T> : SanitizerValidator<T>
{
    // Inject your project's current-user service (e.g., ICurrentUser, IUserContext, ClaimsPrincipal)
    protected readonly ICurrentUser CurrentUser;

    protected BaseAuthenticatedValidator(ICurrentUser currentUser)
    {
        CurrentUser = currentUser;

        RuleFor(x => x).Cascade(CascadeMode.Stop)
            .Must(_ => CurrentUser.Principal != null)
            .WithMessage("No authenticated principal found.")
            .Must(_ => CurrentUser.Principal?.Identity?.IsAuthenticated == true)
            .WithMessage("Principal is not authenticated.");
    }
}
```

`ICurrentUser` (or equivalent) is DI-injected as a **scoped** service.  
It exposes the current user's principal with `.Roles`, `.Identity.IsAuthenticated`, etc.

---

## BasedRolesValidator\<T\>

**Role**: Extends authentication check with a role membership check.  
**Base**: `BaseAuthenticatedValidator<T>`

```csharp
public abstract class BasedRolesValidator<T> : BaseAuthenticatedValidator<T>
{
    // Use your project's role type — typically an enum, string constants, or a dedicated class
    protected readonly string[] RequiredRoles;

    protected BasedRolesValidator(ICurrentUser currentUser, string[] requiredRoles)
        : base(currentUser)
    {
        RequiredRoles = requiredRoles;

        RuleFor(x => x).Cascade(CascadeMode.Stop)
            .Must(_ =>
            {
                if (CurrentUser.Principal is null) return false;
                var userRoles = CurrentUser.Principal.Roles;
                return RequiredRoles.Any(r => userRoles.Contains(r));
            })
            .WithMessage(
                $"User does not have any of the required roles: {string.Join(", ", RequiredRoles)}");
    }
}
```

**Adapt**: Replace `string[] requiredRoles` with your project's role type (e.g., an enum array).
Replace `CurrentUser.Principal.Roles` with the expression that returns the user's role list.

---

## QueryRequestValidator\<T\>

**Role**: Validates standard pagination parameters on search/list request DTOs.  
**Base**: `SanitizerValidator<T>`

```csharp
public abstract class QueryRequestValidator<T> : SanitizerValidator<T>
    where T : QueryRequest   // QueryRequest defines Page, PageSize, Paged, PerformOnlyCount, etc.
{
    protected QueryRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).When(x => x.Paged)
            .WithMessage("Page must be greater than zero.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).When(x => x.Paged)
            .WithMessage("PageSize must be greater than zero.");

        RuleFor(x => x)
            .Must(x => !(x.Paged && x.PerformOnlyCount))
            .WithMessage("Cannot use Paged and PerformOnlyCount together.");
    }
}

// Thin concrete subclass (no-op) — extend when adding domain-specific query rules
public class QueryRequestValidator<T, TResult> : QueryRequestValidator<T>
    where T : QueryRequest
{ }
```

---

## TenantQueryRequestValidator\<T\> (optional)

**Role**: Extends query validation with an async tenant existence check.  
**Base**: `QueryRequestValidator<T>`

```csharp
public abstract class TenantQueryRequestValidator<T> : QueryRequestValidator<T>
    where T : TenantQueryRequest   // TenantQueryRequest adds a TenantId property
{
    protected readonly AppDbContext Ctx;

    protected TenantQueryRequestValidator(AppDbContext ctx)
    {
        Ctx = ctx;

        RuleFor(x => x.TenantId)
            .MustAsync(async (tenantId, token) =>
                await Ctx.Tenants.AnyAsync(t => t.Id == tenantId, token))
            .WithMessage("Tenant not found.");
    }
}
```

---

## ValidationResult Wrapper (optional project pattern)

Some projects wrap `FluentValidation.ValidationResult` in a fluent helper class
(often named `ValidationResultFlow` or similar). This is **not part of FluentValidation itself**;
it is a project-level convention.

If your project defines such a wrapper, its typical API looks like:

```csharp
// Factory — wrap a FluentValidation call
var result = await ValidationResultWrapper.Init("scopeName",
    () => _validator.ValidateAsync(request, token));

// Fluent builder methods
result.WithError("propertyName", "error message");  // add a validation failure
result.WithDetail("human-readable context");         // attach optional context
result.NotFound();                                   // shorthand: add a NOT FOUND error

// State properties
bool result.IsValid
IList<ValidationFailure> result.Errors
```

### Typical service-method return pattern when using a wrapper:

```csharp
public async Task<(ValidationResult Validation, TDto? Item)> CreateAsync(
    TRequest request, CancellationToken token)
{
    var result = await _validator.ValidateAsync(request, token);

    if (!result.IsValid)
    {
        _logger.LogWarning("{Method} failed: {Errors}", nameof(CreateAsync),
            result.Errors.Select(e => e.ErrorMessage));
        return (result, null);
    }

    // ... business logic ...
    return (result, dto);
}
```
