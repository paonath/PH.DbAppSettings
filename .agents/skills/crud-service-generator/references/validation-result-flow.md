# ValidationResultFlow API Reference

`ValidationResultFlow` is a project-specific wrapper over `FluentValidation.ValidationResult`.
It adds fluent mutation methods, a `NotFound` helper, and an `Init` factory for async validators.

> **Adapt this to your project**: if the project uses `ValidationResult` directly,
> replace `ValidationResultFlow` with `ValidationResult` and adjust the API calls accordingly.

---

## Factory: ValidationResultFlow.Init

Use `Init` to run a `FluentValidation` validator and wrap the result:

```csharp
// Pattern 1: async validator (most common)
var validation = await ValidationResultFlow.Init(
    nameof(Create),
    () => _createValidator.ValidateAsync(request, token));

// Pattern 2: inline async lambda (no dedicated validator)
var validation = await ValidationResultFlow.Init(
    nameof(Delete),
    () =>
    {
        var result = new FluentValidation.Results.ValidationResult();
        if (hasChildren)
            result.Errors.Add(new FluentValidation.Results.ValidationFailure(
                nameof(id), "Cannot delete: entity has dependent records."));
        return Task.FromResult(result);
    });
```

Parameters:
- `scope` — operation name, used as context label in logs (use `nameof(MethodName)`)
- `validatorFunc` — a `Func<Task<ValidationResult>>` that calls the validator

---

## Key Properties

| Property   | Type                          | Description                                   |
|------------|-------------------------------|-----------------------------------------------|
| `IsValid`  | `bool`                        | `true` when no errors                         |
| `Errors`   | `IList<ValidationFailure>`    | FluentValidation failures                     |
| `Title`    | `string?`                     | Optional human-readable title for the error   |
| `Detail`   | `IEnumerable<string>?`        | Optional detail strings                       |
| `Inner`    | `IEnumerable<ValidationResultFlow>?` | Nested validation results              |

---

## Fluent Mutation Methods

All methods **return `this`** for chaining:

```csharp
// Add an error on a named property
validation.WithError("Name", "Name is required.");

// Add an error with the attempted value (shown in API error responses)
validation.WithError("Id", "Product not found.", id);

// Add a human-readable title
validation.WithTitle("Validation failed");

// Add detail lines (e.g., for machine-readable error context)
validation.WithDetail("Operation: Create");

// Nest another validation result (e.g., from a sub-operation)
validation.WithInnerError(subValidation);
```

---

## NotFound Helpers

Use these when `FindAsync` returns `null` — they add a standard "not found" error:

```csharp
// Adds: property = "id", message = "The requested resource was not found."
validation.NotFound();

// Adds the same error plus sets the attempted value to the provided id
validation.NotFound(id);
```

Typical usage pattern:

```csharp
var entity = await _ctx.Products.FindAsync([request.Id], token);
if (entity is null)
{
    return (validation.NotFound(request.Id), null);
}
```

---

## CanDelete Convention

The edit validator (used for Update and Delete) typically exposes a `CanDelete` method that
checks business rules before deletion (e.g., no dependent records):

```csharp
/// <summary>
/// Checks whether the entity with <paramref name="id"/> can be safely deleted.
/// Returns a ValidationResult with errors if deletion is blocked.
/// </summary>
public async Task<ValidationResult> CanDelete(string id, CancellationToken token)
{
    var result = new ValidationResult();

    var hasOrderLines = await _ctx.OrderLines.AnyAsync(ol => ol.ProductId == id, token);
    if (hasOrderLines)
        result.Errors.Add(new ValidationFailure(
            nameof(id), "Cannot delete: product has associated order lines."));

    return result;
}
```

In the service:

```csharp
var validation = await ValidationResultFlow.Init(
    nameof(Delete), () => _editValidator.CanDelete(id, token));
```

If the edit validator does not expose `CanDelete`, implement the check inline in the service
using `ValidationResultFlow.Init` with a lambda (see Factory section above).

---

## Assertion Patterns in Tests

```csharp
// Assert success
Assert.True(result.Validation.IsValid);
Assert.NotNull(result.Item);

// Assert specific error
Assert.False(result.Validation.IsValid);
Assert.Null(result.Item);
Assert.Contains(result.Validation.Errors,
    e => e.ErrorMessage == MyValidator.SOME_ERROR_CONSTANT);

// Assert not found
Assert.False(result.Validation.IsValid);
Assert.Contains(result.Validation.Errors,
    e => e.PropertyName == "id");
```

Use `CheckAndWriteValidationError(result.Validation)` (or equivalent) in your test base to
dump validation errors to the test output for easier debugging.

---

## Alternative: ValidationResult Directly

If the project uses vanilla `FluentValidation.ValidationResult` (without a wrapper), adapt calls:

```csharp
// Instead of ValidationResultFlow.Init:
var validationResult = await _createValidator.ValidateAsync(request, token);
if (!validationResult.IsValid)
{
    return (validationResult, null);
}

// Instead of validation.NotFound():
validationResult.Errors.Add(
    new ValidationFailure("id", "The requested resource was not found."));
return (validationResult, null);
```
