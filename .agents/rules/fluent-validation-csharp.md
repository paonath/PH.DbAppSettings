---
trigger: model_decision
description: C# FluentValidation Rules and Guidelines: Guidelines and constraints for generating and maintaining csharp FluentValidation validators
globs: '**/*.cs'
---

## Behavioral Directives (Design & Style Standards)

- **Always inherit from the appropriate custom base class** (e.g., `SanitizerValidator<T>`, `BaseAuthenticatedValidator<T>`, `BasedRolesValidator<T>`, `QueryRequestValidator<T>`) if the project defines them. Check existing validators first; inherit from `AbstractValidator<T>` only if no custom hierarchy exists.
- **Always declare error messages as `public static string` constants** using `SCREAMING_SNAKE_CASE` at the top of the validator class. Never inline string literals in `.WithMessage()`.
- **Always use `AnyAsync()`** for existence and Foreign Key checks in database validation rules.
- **Always call `.Trim()`** on string parameters before database comparisons.
- **Always enforce case-insensitive uniqueness checks** by trimming and using `.ToUpperInvariant()` or `.ToUpper()` on both sides of string comparison expressions inside DB queries (e.g., `p.Code.ToUpperInvariant() == code.Trim().ToUpperInvariant()`).
- **Always validate collection boundaries first** by adding `.NotNull().WithMessage(...)` and `.NotEmpty().WithMessage(...)` guards on collection properties before validating individual collection elements.
- **Always use `EmailValidationMode.AspNetCoreCompatible`** when using the built-in `.EmailAddress()` validator.
- **Always specify target property names** using `.WithName(nameof(Request.Property))` when writing cross-property rules bound to the whole object (e.g., `RuleFor(x => x)`).
- **Always register validators as Scoped** in the Dependency Injection container. Never register validators as `Singleton` to avoid scope lifetime violations (since they inject scoped services like `DbContext` or current user context). Inject them directly as concrete types unless open-generic resolution (e.g., `IValidator<T>`) is required by generic dispatching middleware.
- **Always wrap and log failures in the Service Layer**: When calling validators in service classes, log a warning containing the error messages when validation fails and wrap results in the project-established flow wrapper (e.g., a `ValidationResultFlow` or equivalent `ValidationResult` tuple return pattern).

## Procedural Guidelines (Workflows & Implementation Patterns)

### Base Class Decision Logic
When creating a validator, select the base class using the following workflow:
1. **Authentication Check**:
   - If validation depends on authentication or role checks, inherit from the project's authenticated base class (e.g., `BaseAuthenticatedValidator<T>`).
   - If specific user roles are required, inherit from `BasedRolesValidator<T>` and pass required roles to the base constructor.
2. **Query/Pagination Check**:
   - If validating search, list, or pagination parameters, inherit from or compose with `QueryRequestValidator<T>`. Validate that page indexes/sizes are positive and mutually exclusive parameters are not set together.
3. **Default / Public API**:
   - For public or anonymous endpoints, inherit from the simplest available base containing standard sanitization rules (e.g., `SanitizerValidator<T>`).

### Advanced Validation Patterns

- **Entity Caching**: When a rule must validate the same database entity multiple times (e.g., within a `RuleForEach` loop), declare a private cache dictionary (`Dictionary<TKey, TEntity?>`) and a helper method (`GetCached(id, token)`) inside the validator to prevent redundant database round-trips.
- **Cascading Dependent Rules**: Use `.Cascade(CascadeMode.Stop).DependentRules(() => { ... })` to nest rules that should only run if the parent rule (e.g., database existence check) has succeeded.
- **Consolidated Async Checks (CustomAsync)**: Use `RuleFor(...).CustomAsync(async (val, context, token) => ...)` to group multiple dependent async checks in a single database lookup. Emit errors for different properties using `context.AddFailure(...)` to optimize performance.
- **Class-Level Cascades**: Set `ClassLevelCascadeMode = CascadeMode.Stop;` at the top of the validator constructor for workflows where checking subsequent properties is redundant if previous constraints fail.
- **Validator Composition**: Reuse rules across DTOs using `Include(new OtherValidator<T>())` instead of duplicating validation logic or using deep inheritance.
- **Out-of-band Business Guards**: Expose public async methods on the validator (e.g., `Task<ValidationResult> CanDelete(...)`) for lifecycle guards (like deletion safety checks) that are invoked explicitly by services rather than as part of request DTO validation.