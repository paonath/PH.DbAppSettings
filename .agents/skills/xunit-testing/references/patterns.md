# Test Patterns Reference Guide

Common testing patterns and their use cases in .NET projects.

## Version Policy

- Use ALWAYS the latest stable xUnit version available at implementation time.
- Official xUnit website: https://xunit.net/

## Scope

- This guide is generic and reusable across projects.
- Replace sample domain names with your own model names.

---

## Table of Contents

1. [Unit Test Pattern](#unit-test-pattern)
2. [Theory Test Pattern](#theory-test-pattern)
3. [Integration Test Pattern](#integration-test-pattern)
4. [Validation Test Pattern](#validation-test-pattern)
5. [Repository Pattern](#repository-pattern)
6. [Pattern Selection Guide](#pattern-selection-guide)

---

## Unit Test Pattern

**Purpose**: Test a single unit of code in isolation (entity creation, property assignment, simple calculations).

**Characteristics**:
- Uses `[Fact]` attribute
- No external dependencies (database, services)
- Fast execution
- Tests behavior, not implementation

### Structure

```csharp
[Fact]
public void [Verb]_[Scenario]_[ExpectedResult]()
{
    // Arrange: Setup test data
    var id = "test-123";
    var name = "Test Article";
    
    // Act: Execute the code being tested
    var articolo = new Articolo { Id = id, Name = name };
    
    // Assert: Verify the outcome
    Assert.NotNull(articolo);
    Assert.Equal(id, articolo.Id);
    Assert.Equal(name, articolo.Name);
}
```

### When to Use

✅ Entity creation  
✅ Property assignment  
✅ Simple calculations  
✅ Value objects  
✅ String operations  

### When NOT to Use

❌ Database operations  
❌ External API calls  
❌ Service coordination  
❌ Async operations requiring infrastructure  

### Example

```csharp
[Fact]
public void Create_WithAllRequiredProperties_ReturnsArticolo()
{
    // Arrange
    var id = NewId.NextGuid().ToString();
    var name = "Winter Coat 2024";
    var season = "Winter";
    
    // Act
    var articolo = new Articolo 
    { 
        Id = id, 
        Name = name, 
        Stagione = season 
    };
    
    // Assert
    Assert.NotNull(articolo);
    Assert.Equal(id, articolo.Id);
    Assert.Equal(name, articolo.Name);
    Assert.Equal(season, articolo.Stagione);
    Assert.IsType<Articolo>(articolo);
}
```

---

## Theory Test Pattern

**Purpose**: Test the same logic with multiple data sets to verify behavior across various inputs (parametrized tests).

**Characteristics**:
- Uses `[Theory]` attribute with `[InlineData]` or `[MemberData]`
- Multiple test cases from single test method
- Fast execution
- Great for boundary testing and edge cases

### Structure

```csharp
[Theory]
[InlineData("Winter", "Winter Coat")]
[InlineData("Summer", "Summer Dress")]
[InlineData("Spring", "Spring Jacket")]
public void [Verb]_[Scenario]_[ExpectedResult](string season, string expectedName)
{
    // Arrange: Use parameter values
    var articolo = new Articolo 
    { 
        Id = "1", 
        Stagione = season 
    };
    
    // Act
    articolo.Name = expectedName;
    
    // Assert
    Assert.Equal(expectedName, articolo.Name);
    Assert.Equal(season, articolo.Stagione);
}
```

### When to Use

✅ Boundary value testing  
✅ Multiple input scenarios  
✅ Enum/option validation  
✅ Format validation (emails, phone numbers)  
✅ Range testing  

### When NOT to Use

❌ Single scenario better served by [Fact]  
❌ Complex setup requiring fixtures  
❌ Tests needing database infrastructure  

### Example from Project

```csharp
[Theory]
[InlineData("Winter 2024")]
[InlineData("Summer 2024")]
[InlineData("Spring 2024")]
public void Create_WithVariousSeasons_CreatesSuccessfully(string season)
{
    // Act
    var articolo = new Articolo 
    { 
        Id = NewId.NextGuid().ToString(),
        Stagione = season 
    };
    
    // Assert
    Assert.NotNull(articolo);
    Assert.Equal(season, articolo.Stagione);
}
```

---

## Integration Test Pattern

**Purpose**: Test interaction between components (EF Core, database context, service coordination).

**Characteristics**:
- Uses `IAsyncLifetime` for setup/teardown
- Real database or in-memory context
- Async operations
- Tests multiple units working together

### Structure with In-Memory Database

```csharp
public class ArticoloIntegrationTests : IAsyncLifetime
{
    private AppDbContext _context;
    
    // IAsyncLifetime: Setup runs before each test
    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"test-db-{Guid.NewGuid()}")
            .Options;
            
        _context = new AppDbContext(options);
        await _context.Database.EnsureCreatedAsync();
    }
    
    // IAsyncLifetime: Cleanup after each test
    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }
    
    [Fact]
    public async Task Add_WithValidEntity_PersistsToDatabase()
    {
        // Arrange
        var articolo = new Articolo 
        { 
            Id = NewId.NextGuid().ToString(),
            Name = "Test Article",
            Stagione = "Winter"
        };
        
        // Act
        _context.Articoli.Add(articolo);
        _context.Author = "test@example.com"; // REQUIRED: Set audit author
        await _context.SaveChangesAsync();
        
        // Assert
        var saved = await _context.Articoli.FirstOrDefaultAsync(a => a.Id == articolo.Id);
        Assert.NotNull(saved);
        Assert.Equal(articolo.Name, saved.Name);
    }
}
```

### Structure with Database Fixture

```csharp
public class ArticoloRepositoryTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly ArticoloRepository _repository;
    
    public ArticoloRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _repository = new ArticoloRepository(_fixture.Context);
    }
    
    [Fact]
    public async Task Create_WithValidArticolo_SavesSuccessfully()
    {
        // Arrange
        var articolo = new Articolo { /* ... */ };
        
        // Act
        var result = await _repository.CreateAsync(articolo);
        
        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(default, result.Id);
    }
}
```

### When to Use

✅ Database operations (CRUD)  
✅ EF Core query logic  
✅ Repository patterns  
✅ Service coordination  
✅ Complex workflows  

### When NOT to Use

❌ Simple entity creation  
❌ Isolated business logic  
❌ Performance-critical tests (runs slower)  

### Practical Considerations

**Always set audit author before SaveChanges**:
```csharp
_context.Author = "test@example.com";
await _context.SaveChangesAsync();
```

**Use NewId for string IDs**:
```csharp
var id = NewId.NextGuid().ToString();
```

---

## Validation Test Pattern

**Purpose**: Test FluentValidation rules for entities and DTOs.

**Characteristics**:
- Uses `validator.TestValidate(entity)`
- Tests validation rules without database
- Fast execution
- Focuses on rule enforcement

### Structure

```csharp
public class ArticoloValidatorTests
{
    private readonly ArticoloValidator _validator;
    
    public ArticoloValidatorTests()
    {
        _validator = new ArticoloValidator();
    }
    
    [Fact]
    public void Validate_WithValidArticolo_ReturnsNoErrors()
    {
        // Arrange
        var articolo = new Articolo 
        { 
            Id = "1",
            Name = "Valid Name",
            Stagione = "Winter"
        };
        
        // Act
        var result = _validator.TestValidate(articolo);
        
        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
    
    [Fact]
    public void Validate_WithMissingName_ReturnsError()
    {
        // Arrange
        var articolo = new Articolo 
        { 
            Id = "1",
            Name = null!, // Missing required field
            Stagione = "Winter"
        };
        
        // Act
        var result = _validator.TestValidate(articolo);
        
        // Assert
        result.ShouldHaveValidationErrorFor(a => a.Name)
            .WithErrorMessage("*required*");
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WithInvalidName_ReturnsError(string invalidName)
    {
        // Arrange
        var articolo = new Articolo 
        { 
            Id = "1",
            Name = invalidName!,
            Stagione = "Winter"
        };
        
        // Act
        var result = _validator.TestValidate(articolo);
        
        // Assert
        result.ShouldHaveValidationErrorFor(a => a.Name);
    }
}
```

### When to Use

✅ Business rule validation  
✅ Input validation  
✅ Constraint testing  
✅ Error message verification  
✅ Edge case validation  

### When NOT to Use

❌ Database persistence logic  
❌ Complex workflows  
❌ Performance-dependent logic  

---

## Repository Pattern

**Purpose**: Test data access layer with repository abstraction over EF Core.

**Characteristics**:
- Tests repository interface methods
- Uses real EF Core context (no Moq)
- Async/await pattern
- Database operations

### Structure

```csharp
public class ArticoloRepositoryTests : IAsyncLifetime
{
    private AppDbContext _context;
    private ArticoloRepository _repository;
    
    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"test-{Guid.NewGuid()}")
            .Options;
            
        _context = new AppDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        _repository = new ArticoloRepository(_context);
    }
    
    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }
    
    [Fact]
    public async Task Create_WithValidArticolo_SavesSuccessfully()
    {
        // Arrange
        var articolo = new Articolo 
        { 
            Id = NewId.NextGuid().ToString(),
            Name = "New Article",
            Stagione = "Winter"
        };
        
        // Act
        var result = await _repository.CreateAsync(articolo);
        
        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(default, result.Id);
    }
    
    [Fact]
    public async Task GetById_WithExistingArticolo_ReturnsEntity()
    {
        // Arrange
        var articolo = new Articolo 
        { 
            Id = NewId.NextGuid().ToString(),
            Name = "Existing Article",
            Stagione = "Summer"
        };
        _context.Articoli.Add(articolo);
        _context.Author = "test@example.com";
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetByIdAsync(articolo.Id);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(articolo.Id, result.Id);
        Assert.Equal(articolo.Name, result.Name);
    }
    
    [Fact]
    public async Task GetById_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync("non-existent-id");
        
        // Assert
        Assert.Null(result);
    }
}
```

### When to Use

✅ Repository methods (CRUD)  
✅ Query logic with filters  
✅ Pagination logic  
✅ Complex data access patterns  
✅ Database constraints  

### When NOT to Use

❌ Entity properties  
❌ Simple value objects  
❌ UI components  

---

## Pattern Selection Guide

Use this decision tree to choose the right test pattern:

```
Is it a single unit with no dependencies?
├─ YES → [Fact] Unit Test
└─ NO → Multiple input values or scenarios?
        ├─ YES → [Theory] with [InlineData]
        └─ NO → Involves database or service coordination?
                ├─ YES → Integration Test with IAsyncLifetime
                └─ NO → Involves validation rules?
                        ├─ YES → FluentValidation Test Pattern
                        └─ NO → Involves data access/repository?
                                ├─ YES → Repository Pattern
                                └─ NO → Unit Test [Fact]
```

---

## Common Pitfalls

### ❌ Using Real Database in Unit Tests

```csharp
// BAD: Connects to production database
[Fact]
public async Task Get_ReturnsArticolo()
{
    var context = new AppDbContext("Server=prod;..."); // ❌ WRONG
    var result = await context.Articoli.FirstAsync();
    Assert.NotNull(result);
}
```

**Fix**: Use in-memory database
```csharp
// GOOD: Uses test database
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseInMemoryDatabase("test-db")
    .Options;
var context = new AppDbContext(options);
```

### ❌ Missing Audit Author

```csharp
// BAD: Audit author not set
_context.Articoli.Add(articolo);
await _context.SaveChangesAsync(); // ❌ May fail or cause audit issues
```

**Fix**: Set author before SaveChanges
```csharp
// GOOD
_context.Articoli.Add(articolo);
_context.Author = "test@example.com";
await _context.SaveChangesAsync();
```

### ❌ Mixing Multiple Concepts in One Test

```csharp
// BAD: Tests multiple things at once
[Fact]
public async Task ComplexOperation_Works()
{
    var articolo = new Articolo { /* ... */ };
    _context.Articoli.Add(articolo);
    _context.Author = "test@example.com";
    await _context.SaveChangesAsync();
    
    var updated = await _context.Articoli.FirstAsync();
    updated.Name = "Updated";
    await _context.SaveChangesAsync();
    
    Assert.Equal("Updated", updated.Name);
    // ❌ What fails? Add? Update? Query?
}
```

**Fix**: Separate into logical tests
```csharp
// GOOD: Each test one concept
[Fact]
public async Task Add_WithValidEntity_PersistsToDatabase()
{
    // Only tests adding
}

[Fact]
public async Task Update_WithExistingEntity_ModifiesSuccessfully()
{
    // Only tests updating
}
```

---

## References

[AAA Pattern](https://arrange-act-assert.com/)  
[xUnit Fixtures](https://xunit.net/docs/fixtures)  
[EF Core Testing](https://docs.microsoft.com/en-us/ef/core/testing/)  
[FluentValidation Testing](https://docs.fluentvalidation.net/latest/testing/)
