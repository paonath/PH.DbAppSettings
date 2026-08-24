---
name: csharp-dto-generator
description: |
  Generates C# DTO record pairs (EntityCreateDto + EntityDto) from EF Core entity classes following the project record patterns.
  Use when: (1) a user asks to generate a DTO from an entity class, (2) a user says "create DTO for [entity]",
  (3) a user adds a new entity and needs its DTO counterpart, (4) a user asks to "convert entity to DTO record".
---

# C# DTO Generator

Generate a pair of immutable `record` DTOs from an EF Core entity class following the project's established patterns.

## Output: Two Records Per Entity

Always generate exactly **two** record types per entity:

| Record | Purpose | Contains |
|---|---|---|
| `{Entity}CreateDto` | Input/creation payload | All non-virtual, non-PK properties |
| `{Entity}Dto` | Full read model | Inherits `{Entity}CreateDto` + adds `Id` |

Place both records in a single file named `{Entity}Dto.cs` inside the Models project of the current solution.

> **If no Models project or folder is identifiable**, stop and ask the user where to save the file before generating any code.

> **Namespace note**: All namespaces in this skill (`Prada.Models.Dtos`, etc.) are **examples only**.
> Always infer the correct namespace from the target project: inspect existing `.cs` files in the destination folder or the project's root namespace in the `.csproj` file, then use that namespace consistently.

---

## Step 1: Classify Entity Properties

Before scanning the entity class, use TokenSave MCP tools (specifically `tokensave_context`, `tokensave_search`, or `tokensave_files`) to locate the entity declaration and retrieve its property structure quickly.

Scan the entity class and sort each property into one of three groups:


**GROUP A — Primary Key** (goes into `{Entity}Dto` only):
- The property overriding `Id` from `Entity<TKey>`, or decorated with `[Key]`

**GROUP B — Owned scalar properties** (goes into `{Entity}CreateDto`):
- All `public` non-`virtual` properties that are NOT the primary key
- Include foreign-key scalar fields (e.g. `UserIdLockedBy`) when they represent meaningful data the caller should provide
- Preserve the exact C# type (e.g. `string?`, `DateTime`, `decimal`)

**GROUP C — Navigation / collection properties** (excluded from both DTOs):
- Any `virtual` property
- Any `ICollection<T>` or `IEnumerable<T>` property
- Any reference navigation the entity marks virtual

---

## Step 1b: Handle Enums

If any GROUP B property uses an `enum` type that is **not accessible** in the target DTO project's scope (i.e. defined inside the Dal/domain layer and not exposed publicly), generate a mirror enum in the **same file**, immediately before the two DTO records:

```csharp
/// <summary>Mirror of <c>EntityNamespace.{EnumName}</c> for use in the DTO layer.</summary>
public enum {EnumName}Dto
{
    Value1,
    Value2,
    // ... same values as the original enum
}
```

Use the mirrored `{EnumName}Dto` type in the DTO property instead of the original type.

---

## Step 2: Generate `{Entity}CreateDto`

```csharp
/// <summary>
/// DTO for creating a new {Entity} entity.
/// </summary>
public record {Entity}CreateDto
{
    // One property per GROUP B item, order matches entity declaration
    /// <summary>Gets {description}.</summary>
    public {Type} {Property} { get; init; }

    /// <summary>Initializes a new instance of the <see cref="{Entity}CreateDto"/> record.</summary>
    public {Entity}CreateDto() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="{Entity}CreateDto"/> record with specified values.
    /// </summary>
    // param per ogni proprietà GROUP B
    public {Entity}CreateDto({params})
    {
        // Assignment per ogni proprietà
    }

    /// <summary>Deconstructs the record into its component values.</summary>
    public void Deconstruct({out params}) { ... }
}
```

Rules:
- Use `init` accessors — never `set`
- **Never use a primary constructor** — always declare properties explicitly and use the two-constructor pattern
- Default values in the empty constructor: delegate to parameterized constructor via `: this(...)` with `string.Empty` for strings, `default` for value types, `null` for nullable types
- Order of parameters in constructors mirrors property declaration order in the entity
- Emit XML `<summary>` on the record and every property; add `<param>` on each constructor parameter
- **Do not add `using` statements** unless strictly required by a type not reachable from the file-scoped namespace; never add unused usings
- **Do not invent properties**: the DTO must contain exactly the entity's GROUP B properties — no more, no less

---

## Step 3: Generate `{Entity}Dto`

```csharp
/// <summary>
/// DTO for the {Entity} entity.
/// </summary>
public record {Entity}Dto : {Entity}CreateDto
{
    /// <summary>
    /// Gets the primary key of the {Entity} entity.
    /// </summary>
    /// <remarks>This is the primary key.</remarks>
    public {KeyType} Id { get; init; }

    /// <summary>Initializes a new instance of the <see cref="{Entity}Dto"/> record.</summary>
    public {Entity}Dto() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="{Entity}Dto"/> record with all properties.
    /// </summary>
    public {Entity}Dto({KeyType} id, {GROUP B params}) : base({GROUP B args})
    {
        Id = id;
    }

    /// <summary>Deconstructs the record into its component values.</summary>
    public void Deconstruct(out {KeyType} id, {GROUP B out params})
    {
        id = Id;
        // delegate GROUP B to CreateDto deconstructor or repeat assignments
    }
}
```

Rules:
- `Id` is always the first parameter in the full constructor and in `Deconstruct`
- The `<remarks>This is the primary key.</remarks>` tag is mandatory on the `Id` property
- The `: base()` call in the empty constructor is explicit (not implicit)

---

## Step 4: File Header & Namespace

Always use a **file-scoped namespace** declaration. The namespace must match the target project — inspect existing `.cs` files in the destination folder or the `<RootNamespace>` / `<AssemblyName>` in the `.csproj` to determine the correct value.

```csharp
// Replace <Project.Namespace> with the actual namespace of the target project.
// Example only: namespace Prada.Models.Dtos;
namespace <Project.Namespace>;

// {Entity}Dto record first (inherits second), then {Entity}CreateDto record below
```

Always declare `{Entity}Dto` first in the file, followed by `{Entity}CreateDto`. Use file-scoped namespace.

---

## Property Type Mapping Reference

| Entity type | DTO type |
|---|---|
| `required string` | `string` |
| `string?` | `string?` |
| `required DateTime` | `DateTime` |
| `DateTime?` | `DateTime?` |
| `required int` / `long` | `int` / `long` |
| `decimal?` | `decimal?` |
| `bool` | `bool` |

Preserve all nullability annotations exactly. Do not lose `?`.

---

## Empty Constructor Default Values

When the parameterized constructor is chained from the empty constructor, provide these defaults:

| C# type | Default |
|---|---|
| `string` | `string.Empty` |
| `string?` | `null` |
| `DateTime` | `default` |
| `int` / `long` | `0` |
| `decimal` | `0m` |
| `bool` | `false` |

---

## Checklist Before Emitting Output

- [ ] Both records placed in the Models project of the solution, file named `{Entity}Dto.cs`
- [ ] Namespace: inferred from the target project (existing files or `.csproj` root namespace) — **not** copied literally from this skill's examples
- [ ] `{Entity}Dto` declared first, `{Entity}CreateDto` second in the same file
- [ ] No `virtual`, `ICollection`, or navigation properties in any DTO
- [ ] `Id` property has `<remarks>This is the primary key.</remarks>`
- [ ] Both records have empty constructor chaining to parameterized constructor
- [ ] Both records have `Deconstruct` method with parameters matching all properties
- [ ] All properties use `init` — never `set`
- [ ] XML `<summary>` on every property, record, and constructor
- [ ] `<param>` XML tags present on parameterized constructor parameters
- [ ] All public non-virtual scalar properties from the entity are included in `{Entity}CreateDto` and `{Entity}Dto` (except PK which is only in `{Entity}Dto`)
- [ ] No invented properties: DTO contains exactly the entity's GROUP B properties
- [ ] No primary constructors used
- [ ] No unnecessary `using` statements added
- [ ] Enum types not visible in the DTO project scope are mirrored as `{EnumName}Dto` in the same file

---

## Example

See [examples/articolo-dto-example.md](./examples/articolo-dto-example.md) for a complete walkthrough using the `Articolo` entity.
