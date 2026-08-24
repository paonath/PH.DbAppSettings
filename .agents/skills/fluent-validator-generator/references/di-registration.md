# DI Registration Reference

Validators must be registered in your application's DI container.
The exact location (e.g., `ServiceRegistration.cs`, `Program.cs`, a dedicated
`ValidatorExtensions.cs`) depends on your project's conventions.

All validators must be **`Scoped`** — never `Singleton` — because they typically
inject a `DbContext` and/or a scoped auth service.

---

## Default Pattern — Concrete Type Registration

**Always prefer registering and injecting the concrete validator type.**  
Validators and the services that consume them live in the same project, so interface
indirection via `IValidator<T>` is unnecessary overhead.

```csharp
// Register each validator as its concrete type
services.AddScoped<CreateProductValidator>();
services.AddScoped<UpdateProductValidator>();
services.AddScoped<DeleteProductValidator>();
```

Service constructor:
```csharp
public ProductService(
    CreateProductValidator createValidator,
    UpdateProductValidator updateValidator,
    AppDbContext ctx)
{ ... }
```

---

## Optional Pattern — IValidator\<T\> Interface

Only use `IValidator<T>` registration when the project has infrastructure that dispatches
validators generically (e.g., a MediatR pipeline behaviour, a middleware, or a framework
that resolves validators by open-generic interface).

```csharp
// Register the concrete type AND expose it via the interface
services.AddScoped<CreateOrderValidator>();
services.AddScoped<IValidator<CreateOrderRequest>>(sp =>
    sp.GetRequiredService<CreateOrderValidator>());
```

Service constructor (the infrastructure layer uses `IValidator<T>`; the service uses concrete):
```csharp
public OrderService(CreateOrderValidator validator) { ... }
```

---

## Auto-Discovery Alternative

If the project uses FluentValidation's built-in assembly scanning, add the validator to the
scanned assembly instead of manual registration:

```csharp
// Scans the assembly and registers all AbstractValidator<T> subclasses as Scoped
services.AddValidatorsFromAssemblyContaining<CreateOrderValidator>();
```

Prefer manual registration when validators need constructor parameters that require explicit
DI resolution (e.g., scoped auth services resolved from the HTTP context).

---

## Important: Never Use Singleton

Validators typically inject a `DbContext` (scoped) and/or an auth/user service (scoped).
Registering as `Singleton` causes scope lifetime violations at runtime.

```csharp
// ✅ Correct
services.AddScoped<MyValidator>();

// ❌ Wrong — will cause runtime DI errors
services.AddSingleton<MyValidator>();
```
