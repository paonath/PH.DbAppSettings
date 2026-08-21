---
trigger: model_decision
description: Minimal API endpoint patterns and conventions for .NET projects
globs: '**/*.cs'
---

## Minimal API Patterns

### Project Structure

```
/Endpoints       — Route handlers organized by feature group
/Models          — DTOs, requests, responses (record types)
/Services        — Business logic interfaces (IXxxService)
/Data            — EF Core DbContext and configurations
/Validators      — FluentValidation input validators
```

### Endpoint Organization

- Place endpoint mapping in a separate `EndpointMap.cs` under `Endpoints/`.
- Place each endpoint group in a single file under an appropriate subfolder of `Endpoints/`.
- Group endpoints by domain area (e.g., `ItemEndpoints`, `UserEndpoints`, `CategoryEndpoints`).
- Use `WebApplication.CreateBuilder()` for setup.
- Use endpoint routing: `app.MapGet()`, `app.MapPost()`, `app.MapPut()`, `app.MapDelete()`.

### Response Patterns

- Return `IResult` from endpoint handlers.
- Use `TypedResults` for strongly-typed responses (`Results.Ok()`, `Results.Created()`, `Results.NotFound()`).
- Use record types for all input and output data in endpoints.
- Implement consistent error response format.

### DI Registration

```csharp
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddValidatorsFromAssemblyContaining<CreateItemValidator>();
```

### Validation and Error Handling

- Use FluentValidation for all input validation (reference `fluent-validation-csharp.md`).
- Implement global exception handling middleware.
- Return appropriate HTTP status codes.
- Include correlation IDs for tracing.

### Documentation (OpenAPI)

- Use `.WithName()` for operation IDs.
- Use `.WithTags()` for grouping.
- Use `.WithSummary()` and `.WithDescription()` for documentation.
- Use `.Produces<T>()` to document response types.

### Authorization

- Use `.RequireAuthorization()` on endpoints.
- Implement role-based and policy-based authorization.
- Secure endpoints by default; explicitly allow anonymous access.
- High-privilege roles (e.g., system admin) may bypass certain ACL checks — define bypass rules per project.

### Code Conventions

- File-scoped namespaces with correct namespace.
- Global usings defined in `GlobalUsings.cs`.
- Nullable reference types always enabled.
- Async patterns for all I/O operations.