# xUnit Assertions Reference Guide

Common xUnit assertions with examples and use cases.

## Version Policy

- Use ALWAYS the latest stable xUnit version available at implementation time.
- Official xUnit website: https://xunit.net/

## Scope

- This guide is generic and reusable across projects.
- Replace sample domain names with your own types.

---

## Null & Empty Assertions

### Assert.Null(value)
Verify that a value is null.

```csharp
var result = GetOptionalValue();
Assert.Null(result);
```

### Assert.NotNull(value)
Verify that a value is not null.

```csharp
var entity = new Entity { Id = "test" };
Assert.NotNull(entity);
```

### Assert.Empty(collection)
Verify that a collection has no items.

```csharp
var entities = await context.Entities.ToListAsync();
Assert.Empty(entities);
```

### Assert.NotEmpty(collection)
Verify that a collection has at least one item.

```csharp
var entities = await context.Entities.ToListAsync();
Assert.NotEmpty(entities);
```

### Assert.Single(collection)
Verify that a collection has exactly one item.

```csharp
var articoli = await context.Articoli.Where(a => a.Id == "123").ToListAsync();
Assert.Single(articoli);
```

### Assert.Single(collection, predicate)
Verify exactly one item matches the condition.

```csharp
var result = Assert.Single(articoli, a => a.Stagione == "Winter");
Assert.Equal("Winter Coat", result.Name);
```

---

## Equality Assertions

### Assert.Equal(expected, actual)
Verify two values are equal.

```csharp
var articolo = new Articolo { Name = "Test" };
Assert.Equal("Test", articolo.Name);

// With custom comparer
Assert.Equal(expected, actual, StringComparer.OrdinalIgnoreCase);
```

### Assert.NotEqual(expected, actual)
Verify two values are not equal.

```csharp
Assert.NotEqual(originalName, updatedName);
```

### Assert.Same(expected, actual)
Verify two references point to the same object.

```csharp
var obj1 = container.GetService<IService>();
var obj2 = container.GetService<IService>();
Assert.Same(obj1, obj2); // For singletons
```

### Assert.NotSame(expected, actual)
Verify two references point to different objects.

```csharp
var obj1 = new Articolo { Id = "1" };
var obj2 = new Articolo { Id = "1" };
Assert.NotSame(obj1, obj2); // Different instances
```

---

## Type Assertions

### Assert.IsType<T>(object)
Verify exact type match (not derived types).

```csharp
var result = GetValue();
Assert.IsType<Articolo>(result);
```

### Assert.IsAssignableFrom<T>(object)
Verify type is compatible (includes derived types).

```csharp
var result = GetEntity();
Assert.IsAssignableFrom<Entity>(result); // Works for Entity and derived types
```

---

## Collection Assertions

### Assert.Contains(item, collection)
Verify item is in collection.

```csharp
var articoli = new List<Articolo> { /* ... */ };
Assert.Contains(targetArticolo, articoli);
```

### Assert.Contains(item, collection, comparer)
Verify item with custom comparison.

```csharp
Assert.Contains(targetId, articoli, (id, a) => a.Id == id);
```

### Assert.DoesNotContain(item, collection)
Verify item is not in collection.

```csharp
var articoli = await context.Articoli.ToListAsync();
Assert.DoesNotContain(deletedArticolo, articoli);
```

### Assert.All(collection, action)
Verify all items satisfy a condition.

```csharp
var winterArticoli = await context.Articoli
    .Where(a => a.Stagione == "Winter")
    .ToListAsync();

Assert.All(winterArticoli, a => Assert.Equal("Winter", a.Stagione));
```

---

## String Assertions

### Assert.StartsWith(expectedStart, actualString)
Verify string starts with prefix.

```csharp
var name = articolo.Name;
Assert.StartsWith("Premium", name);
```

### Assert.EndsWith(expectedEnd, actualString)
Verify string ends with suffix.

```csharp
Assert.EndsWith("2026", articolo.Stagione);
```

### Assert.Contains(expectedSubstring, actualString)
Verify substring exists.

```csharp
Assert.Contains("Winter", articolo.Name);
```

### Assert.Matches(expectedRegexPattern, actualString)
Verify string matches regex pattern.

```csharp
Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", dateString);
```

---

## Numeric Assertions

### Assert.InRange(value, low, high)
Verify value is within range (inclusive).

```csharp
var quantity = 5;
Assert.InRange(quantity, 1, 10); // 1 <= 5 <= 10
```

### Assert.NotInRange(value, low, high)
Verify value is outside range.

```csharp
Assert.NotInRange(quantity, 100, 1000);
```

---

## Boolean Assertions

### Assert.True(condition)
Verify condition is true.

```csharp
var isValid = articolo.Validate();
Assert.True(isValid);
```

### Assert.False(condition)
Verify condition is false.

```csharp
var isEmpty = await context.Articoli.CountAsync() == 0;
Assert.False(isEmpty);
```

---

## Exception Assertions

### Assert.Throws<T>(action)
Verify specific exception is thrown.

```csharp
var exception = Assert.Throws<ArgumentNullException>(() => 
{
    var articolo = new Articolo { Name = null! };
});

// Verify exception message
Assert.Contains("Name", exception.Message);
```

### Assert.ThrowsAny<T>(action)
Verify any exception of type T or derived is thrown.

```csharp
Assert.ThrowsAny<Exception>(() => MethodThatThrows());
```

### Assert.ThrowsAsync<T>(action)
Verify exception in async method.

```csharp
var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
{
    await _service.ProcessAsync(null);
});
```

---

## Advanced Assertions

### Assert.Multiple(actions)
Group multiple assertions; reports all failures, not just first.

```csharp
var articolo = new Articolo { Id = "1", Name = "Test" };

Assert.Multiple(
    () => Assert.NotNull(articolo),
    () => Assert.Equal("1", articolo.Id),
    () => Assert.Equal("Test", articolo.Name)
);
```

---

## Common Patterns

### Test with Multiple Assertions

```csharp
[Fact]
public void Create_WithValidData_ReturnsEntity()
{
    // Arrange
    var id = "test-123";
    var name = "Test Article";
    
    // Act
    var articolo = new Articolo { Id = id, Name = name };
    
    // Assert
    Assert.NotNull(articolo);              // 1. Entity created
    Assert.Equal(id, articolo.Id);         // 2. ID set correctly
    Assert.Equal(name, articolo.Name);     // 3. Name set correctly
    Assert.IsType<Articolo>(articolo);     // 4. Correct type
}
```

### Test with Exception

```csharp
[Fact]
public void Create_WithNullName_ThrowsException()
{
    var exception = Assert.Throws<ArgumentNullException>(() =>
    {
        var articolo = new Articolo { Id = "1", Name = null! };
    });
    
    Assert.NotNull(exception);
    Assert.Contains("Name", exception.ParamName);
}
```

### Test Collection Operations

```csharp
[Fact]
public async Task Query_WithFilter_ReturnsMatches()
{
    // Add items
    _context.Articoli.AddRange(
        new Articolo { Id = "1", Stagione = "Winter" },
        new Articolo { Id = "2", Stagione = "Summer" }
    );
    _context.Author = "test@example.com";
    await _context.SaveChangesAsync();
    
    // Query
    var winter = await _context.Articoli
        .Where(a => a.Stagione == "Winter")
        .ToListAsync();
    
    // Assert
    Assert.NotEmpty(winter);
    Assert.Single(winter);
    Assert.All(winter, a => Assert.Equal("Winter", a.Stagione));
}
```

---

## Assertion Guidelines

✅ **DO**:
- Use specific assertion for clarity (`Assert.Equal` not `Assert.True(a == b)`)
- Check one logical concept per assertion
- Use meaningful variables names in assertions
- Include custom messages when needed
- Group related assertions with `Assert.Multiple`

❌ **DON'T**:
- Use vague assertions like `Assert.True` for complex conditions
- Chain too many assertions without clarity
- Use assertions for test control flow (use if/else instead)
- Forget to verify exception messages when testing failures

---

## References

[xUnit Assertions Reference](https://xunit.net/docs/comparisons#assertions)
