---
trigger: model_decision
description: C# record and DTO patterns including ToDto/ToEntity conversion
globs: '**/*.cs'
---

## DTO Record Structure

All DTO records MUST follow this pattern:

- Use `record` keyword (not `class`).
- All properties use `init` accessors (never `set`).
- Include an empty constructor that delegates to the full constructor with defaults.
- Include a secondary constructor accepting all public properties.
- Include XML documentation comments for the record and all properties.
- Include a `Deconstruct` method when the record has 2+ properties.

### Naming

- Suffix DTO records with `Dto` (e.g., `UserDto`, `NodeDto`).
- Suffix request models with `Request` (e.g., `CreateNodeRequest`).
- Place each record in its own file named after the record (e.g., `UserDto.cs`).
- Organize in appropriate folders: `Dtos/`, `Requests/`, `Responses/`.

### Example

```csharp
/// <summary>
/// Represents a user data transfer object.
/// </summary>
public record UserDto
{
    /// <summary>Gets the user identifier.</summary>
    public string Id { get; init; }

    /// <summary>Gets the user name.</summary>
    public string Name { get; init; }

    public UserDto() : this(string.Empty, string.Empty) { }

    public UserDto(string id, string name)
    {
        Id = id;
        Name = name;
    }

    public void Deconstruct(out string id, out string name)
    {
        id = Id;
        name = Name;
    }
}
```

## ToDto / ToEntity Conversion Pattern

For every Entity-DTO pair, create a static class with two static methods in a dedicated file.

### Rules

- Methods are **static** (not extension methods).
- `ToDto` accepts nullable entity, returns nullable DTO.
- `ToEntity` accepts DTO and an optional existing entity to update.
- If `entity` parameter is null in `ToEntity`, create a new instance.
- **MUST NOT** modify `Id` property when updating an existing entity.
- Handle null inputs: return `null` if input is null (no `ArgumentNullException`).
- Include XML documentation on both methods.

### Example

```csharp
public static class NodeDtoExtensions
{
    public static NodeDto? ToDto(Node? entity)
    {
        if (entity == null) return null;

        return new NodeDto
        {
            Id = entity.Id,
            Name = entity.Name,
            ParentId = entity.ParentId,
        };
    }

    public static Node? ToEntity(NodeDto? dto, Node? entity = null)
    {
        if (dto == null && entity == null) return null;

        entity ??= new Node();
        entity.Name = dto?.Name ?? entity.Name;
        entity.ParentId = dto?.ParentId ?? entity.ParentId;

        return entity;
    }
}
```

## DTO Client/Server Contract (Strict Rule)

For every server DTO exposed by endpoints, a matching TypeScript DTO MUST exist 1:1 in the client. TypeScript interfaces MUST mirror the server DTO exactly (property names, enums, nullability). Source: the project's OpenAPI/Swagger spec file.