# xUnit Testing SKILL

A comprehensive SKILL for creating and editing high-quality xUnit tests following project patterns, conventions, and best practices for .NET with EF Core and FluentValidation.

## Quick Start

Create a basic unit test in 30 seconds:

```csharp
using Xunit;

namespace Example.Tests;

public class ArticoloTests
{
    /// <summary>
    /// Create_WithValidData_ReturnsEntity: Tests that entity creation succeeds with valid data
    /// </summary>
    [Fact]
    public void Create_WithValidData_ReturnsEntity()
    {
        // Arrange
        var id = "test-id-123";
        var name = "Test Article";
        
        // Act
        var articolo = new Articolo { Id = id, Name = name };
        
        // Assert
        Assert.NotNull(articolo);
        Assert.Equal(id, articolo.Id);
        Assert.Equal(name, articolo.Name);
    }
}
```

## 1. Overview

### When to Use This SKILL

- Creating new xUnit test files for .NET entities, services, or repositories
- Editing existing tests to follow project conventions
- Verifying test quality and structure
- Learning xUnit patterns and project-specific standards
- Testing EF Core repositories, FluentValidation validators, or domain entities

### What's Inside

- **Test patterns**: Unit, integration, validation, and repository test templates
- **Naming conventions**: Standard pattern for all test methods
- **Real DI patterns**: How to inject real implementations (no Moq)
- **Assertions**: Common xUnit assertions with examples
- **Fixtures**: Setup/teardown with IAsyncLifetime
- **Domain-specific**: Italian entities (Articolo, Versione), audit system, string-based IDs

---

## 2. Core Principles

### AAA Pattern (Arrange-Act-Assert)

Every test has three clear sections:

```csharp
[Fact]
public void Operation_Scenario_ExpectedResult()
{
    // Arrange: Set up test data and dependencies
    var sut = new SystemUnderTest();
    var input = new TestData();
    
    // Act: Execute the operation being tested
    var result = sut.MethodToTest(input);
    
    // Assert: Verify the result matches expectations
    Assert.NotNull(result);
    Assert.Equal(expected, result.Property);
}
```

### Real Implementations, No Mocks

**Project Standard**: No Moq framework. Inject real dependencies.

```csharp
// ❌ Don't do this (Moq)
var mockRepository = new Mock<IRepository>();

// ✅ Do this (real implementation)
var repository = new EfCoreRepository(dbContext);
```

### No Moq Policy

- Never use `Mock<T>` or any mocking framework
- Always inject real instances of dependencies
- Use real DbContext or in-memory databases for EF Core tests
- Create stub/fake implementations only when absolutely necessary

### One Assertion Per Concept

2-5 assertions per test, each testing one specific thing:

```csharp
[Fact]
public void Save_WithValidData_UpdatesDatabase()
{
    // Arrange
    var context = new TestDbContext();
    var articolo = new Articolo { Id = NewId.NextGuid().ToString(), Name = "Test" };
    
    // Act
    context.Articoli.Add(articolo);
    context.Author = "test@example.com"; // Audit requirement
    context.SaveChanges();
    
    // Assert
    Assert.NotEmpty(context.Articoli);
    Assert.Single(context.Articoli.Where(a => a.Id == articolo.Id));
}
```

---

## 3. xUnit Fundamentals

### [Fact] Attribute

Single test case with no parameters:

```csharp
[Fact]
public void Increment_WithOne_ReturnsTwo()
{
    var result = Calculator.Increment(1);
    Assert.Equal(2, result);
}
```

### [Theory] Attribute

Parametrized test running multiple times with different data:

```csharp
[Theory]
[InlineData(1, 2)]
[InlineData(2, 3)]
[InlineData(0, 1)]
public void Increment_WithValue_ReturnsIncrementedValue(int input, int expected)
{
    var result = Calculator.Increment(input);
    Assert.Equal(expected, result);
}
```

### [InlineData]

Provide test data inline for [Theory]:

```csharp
[Theory]
[InlineData("valid@example.com", true)]
[InlineData("invalid-email", false)]
[InlineData("", false)]
public void Validate_WithEmailAddress_ReturnsCorrectResult(string email, bool isValid)
{
    var validator = new EmailValidator();
    var result = validator.Validate(email);
    Assert.Equal(isValid, result);
}
```

### [MemberData]

Reference test data from a property or method:

```csharp
public static TheoryData<int, int> AdditionTestData => new()
{
    { 1, 1 },
    { 2, 3 },
    { 10, 20 }
};

[Theory]
[MemberData(nameof(AdditionTestData))]
public void Add_WithData_ReturnsSum(int a, int b)
{
    var result = Calculator.Add(a, b);
    Assert.Equal(a + b, result);
}
```

### Async Tests

Use `async Task` for asynchronous operations:

```csharp
[Fact]
public async Task SaveAsync_WithValidData_PersistsToDatabase()
{
    // Arrange
    var context = new TestDbContext();
    var articolo = new Articolo { Id = NewId.NextGuid().ToString(), Name = "Test" };
    
    // Act
    context.Articoli.Add(articolo);
    context.Author = "test@example.com";
    await context.SaveChangesAsync();
    
    // Assert
    var saved = await context.Articoli.FirstOrDefaultAsync(a => a.Id == articolo.Id);
    Assert.NotNull(saved);
}
```

---

## 4. Test Naming Convention

**Pattern**: `[Verb]_[Scenario]_[ExpectedResult]`

### Components

- **Verb**: Action being tested (Create, Update, Delete, Validate, Query, Save, etc.)
- **Scenario**: Specific condition or input (WithValidData, WithNullName, WithDuplicateId, etc.)
- **ExpectedResult**: What should happen (ReturnsEntity, ThrowsException, UpdatesDatabase, etc.)

### Examples

```
✅ Create_WithValidData_ReturnsEntity
✅ Update_WithNullName_ThrowsArgumentNullException
✅ Delete_WithExistingId_RemovesFromDatabase
✅ Validate_WithInvalidEmail_ReturnsFalse
✅ Query_WithActiveFilter_ReturnsOnlyActive
✅ Save_WithAuditMetadata_SetsAuthorProperty
```

### Anti-patterns

```
❌ Test1, Test2 (meaningless)
❌ CreateTest, UpdateTest (vague)
❌ TestCreateArticolo (verb at end)
❌ Create_ReturnsArticolo (missing scenario context)
```

---

## 5. Test Templates

### Unit Test Template (Entity Creation)

```csharp
public class EntityCreationTests
{
    [Fact]
    public void Create_WithRequiredProperties_ReturnsEntity() { /* Arrange, Act, Assert */ }
    [Fact]
    public void Create_WithMissingField_ThrowsException() { /* Arrange, Act, Assert */ }
}
```

See [`examples/test-unit-entity-creation.cs`](examples/test-unit-entity-creation.cs) for the full template.

### Theory Test Template (Multiple Scenarios)

```csharp
[Theory]
[InlineData(null, false)]
[InlineData("", false)]
[InlineData("valid-name", true)]
public void Create_WithVariousNames_ValidatesCorrectly(string name, bool shouldSucceed)
{
    // Arrange & Act
    var action = () => new Articolo { Id = "test-123", Name = name };
    
    // Assert
    if (shouldSucceed)
    {
        var articolo = action();
        Assert.NotNull(articolo);
    }
    else
    {
        Assert.Throws<ArgumentException>(action);
    }
}
```

### Integration Test Template (EF Core)

```csharp
public class EntityRepositoryTests
{
    [Fact]
    public async Task Add_WithValidEntity_PersistsToDatabase() { /* Arrange, Act, Assert */ }
    [Fact]
    public async Task Query_WithFilter_ReturnsMatchingEntities() { /* Arrange, Act, Assert */ }
}
```

See [`examples/test-integration-efcore.cs`](examples/test-integration-efcore.cs) for the full template.

### Validation Test Template (FluentValidation)

```csharp
public class EntityValidatorTests
{
    private readonly EntityValidator _validator = new();
    [Fact]
    public void Validate_WithValidData_ReturnsNoErrors() { /* Arrange, Act, Assert */ }
    [Fact]
    public void Validate_WithMissingField_ReturnsError() { /* Arrange, Act, Assert */ }
}
```

See [`examples/test-validation-rules.cs`](examples/test-validation-rules.cs) for the full template.

---

## 6. Dependency Injection in Tests

### Constructor Injection Pattern

Inject real dependencies through the constructor:

```csharp
public class ArticoloServiceTests
{
    private readonly PradaContext _context;
    private readonly ArticoloService _service;

    public ArticoloServiceTests()
    {
        // Arrange: Set up real dependencies
        _context = new TestPradaContext(); // In-memory test database
        _service = new ArticoloService(_context);
    }

    [Fact]
    public async Task GetArticolo_WithValidId_ReturnsArticolo()
    {
        // Act
        var result = await _service.GetArticolo("articolo-123");
        
        // Assert
        Assert.NotNull(result);
    }
}
```

### IAsyncLifetime for Setup/Teardown

Use `IAsyncLifetime` for async initialization:

```csharp
public class ArticoloRepositoryTests : IAsyncLifetime
{
    private readonly TestPradaContext _context;

    public ArticoloRepositoryTests()
    {
        _context = new TestPradaContext();
    }

    public async Task InitializeAsync()
    {
        // Async setup: create test data, migrations, etc.
        await _context.Database.EnsureCreatedAsync();
        
        var testData = new Articolo
        {
            Id = "seed-1",
            Name = "Seed Article",
            Stagione = "Winter"
        };
        
        _context.Articoli.Add(testData);
        _context.Author = "test@example.com";
        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        // Cleanup: drop tables, close connections, etc.
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task Query_ReturnsSeededData()
    {
        var articoli = await _context.Articoli.ToListAsync();
        Assert.Single(articoli);
    }
}
```

### ServiceCollection for Complex Setup

For tests requiring multiple dependencies, build a `ServiceCollection` with real registrations and resolve via `IServiceProvider`. See [DI Patterns](references/di-patterns.md) for full examples.

---

## 7. Common Assertions

See [Assertions Guide](references/assertions-guide.md) for the complete reference with examples grouped by type (null/empty, equality, collections, type checks, strings, ranges, exceptions).

---

## 8. Project-Specific Conventions

### No Moq - Real DI Only

See Section 2 “Real Implementations, No Mocks” for the full No Moq policy.

### Audit System - Set Author Before SaveChanges

Many entities go through the PradaContext audit system:

```csharp
[Fact]
public async Task Save_WithAuditMetadata_RecordsAuthor()
{
    // Arrange
    var context = new TestPradaContext();
    var articolo = new Articolo
    {
        Id = NewId.NextGuid().ToString(),
        Name = "Test"
    };
    
    // Act
    context.Articoli.Add(articolo);
    context.Author = "test@example.com";      // ← Required before SaveChanges
    context.ContextIdentifier = "TestContext"; // ← Optional scoping
    await context.SaveChangesAsync();
    
    // Assert
    // Verify audit records were created
    var auditRows = await context.AuditRow.Where(ar => ar.EntityId == articolo.Id).ToListAsync();
    Assert.NotEmpty(auditRows);
}
```

### String IDs via NewId

Most entities use `string` IDs generated by NewId:

```csharp
using IdGen;

var id = NewId.NextGuid().ToString(); // Generates sortable string ID

var articolo = new Articolo
{
    Id = id,
    Name = "Test Article"
};
```

### Required Properties (C# 11)

Entity properties marked `required` must be initialized:

```csharp
public class Articolo : Entity<string>
{
    public required string Name { get; set; }
    public required string Stagione { get; set; }
}

// In test:
var articolo = new Articolo
{
    Id = NewId.NextGuid().ToString(),
    Name = "Test",           // ← Required
    Stagione = "Winter 2026" // ← Required
};
```

### Italian Domain Language

Preserve Italian entity names and properties in test names:

```csharp
// ✅ Use Italian terms from domain
public class ArticoloTests { }
public class VersioneTests { }
public class MicroattivitaTests { }

// ✅ Map to English in comments
/// <summary>
/// Articolo (Article) tests for creation and updates
/// </summary>
```

---

## 9. Integration Testing with EF Core

### In-Memory Database

```csharp
var options = new DbContextOptionsBuilder<PradaContext>()
    .UseInMemoryDatabase(databaseName: "TestDatabase_" + Guid.NewGuid())
    .Options;

var context = new PradaContext(options);
```

### Real Test Database

```csharp
var connectionString = "Server=.;Database=TestDb;Integrated Security=true;";
var options = new DbContextOptionsBuilder<PradaContext>()
    .UseSqlServer(connectionString)
    .Options;

var context = new PradaContext(options);
```

### Transaction Rollback Pattern

```csharp
[Fact]
public async Task Save_ThenRollback_RestoresOriginalState()
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    
    // Arrange & Act
    var articolo = new Articolo { Id = NewId.NextGuid().ToString(), Name = "Test" };
    _context.Articoli.Add(articolo);
    _context.Author = "test@example.com";
    await _context.SaveChangesAsync();
    
    // Assert before rollback
    Assert.Single(await _context.Articoli.ToListAsync());
    
    // Rollback
    await transaction.RollbackAsync();
    
    // Assert after rollback (new context)
    var newContext = new TestPradaContext();
    Assert.Empty(await newContext.Articoli.ToListAsync());
}
```

---

## 10. Fixtures & Setup/Teardown

### IClassFixture for Shared Setup

Share setup across multiple tests in a class:

```csharp
public class ArticoloServiceTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public ArticoloServiceTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Service_UsesSharedDatabase()
    {
        var articoli = await _fixture.Context.Articoli.ToListAsync();
        Assert.NotEmpty(articoli);
    }
}

public class TestDatabaseFixture : IAsyncLifetime
{
    public PradaContext Context { get; private set; }

    public async Task InitializeAsync()
    {
        Context = new TestPradaContext();
        await Context.Database.EnsureCreatedAsync();
        // Seed test data
    }

    public async Task DisposeAsync()
    {
        await Context.Database.EnsureDeletedAsync();
        await Context.DisposeAsync();
    }
}
```

### ICollectionFixture for Multiple Classes

Apply `[CollectionDefinition]` + `ICollectionFixture<TFixture>` to share a fixture across multiple test classes decorated with `[Collection("...")]`. See [DI Patterns](references/di-patterns.md) for the full pattern.

---

## 11. Anti-Patterns - What NOT to Do

### ❌ Never Use Moq

```csharp
// WRONG
var mockRepository = new Mock<IRepository>();
mockRepository.Setup(r => r.Get(It.IsAny<string>())).ReturnsAsync(new Articolo());
```

### ❌ Don't Mix Multiple Concerns

```csharp
// WRONG - Tests two things at once
[Fact]
public async Task CreateAndSave_WithValidData_PersistsAndReturnsArticolo()
{
    // Tests both entity creation AND database persistence
}

// RIGHT - Test one thing
[Fact]
public void Create_WithValidData_ReturnsArticolo() { }

[Fact]
public async Task Save_WithValidEntity_PersistsToDatabase() { }
```

### ❌ Don't Use Magic Strings

```csharp
// WRONG
var articolo = new Articolo { Id = "12345", Name = "Article" };

// RIGHT - Use constants or meaningful test data
const string TestId = "test-articolo-001";
var articolo = new Articolo { Id = TestId, Name = "Premium Winter Coat" };
```

### ❌ Don't Test Implementation Details

```csharp
// WRONG - Testing internal method
[Fact]
public void PrivateMethod_WithData_ReturnsExpected() { }

// RIGHT - Test public behavior
[Fact]
public void PublicMethod_WithData_ReturnsExpected() { }
```

### ❌ Don't Forget XML Documentation

```csharp
// WRONG
[Fact]
public void Test() { }

// RIGHT
/// <summary>
/// Create_WithValidData_ReturnsArticolo: Verifies entity creation succeeds with valid input
/// </summary>
[Fact]
public void Create_WithValidData_ReturnsArticolo() { }
```

---

## 12. Decision Tree: Which Test Type?

```
Are you testing an entity or DTO property?
├─ YES → Unit Test ([Fact] or [Theory])
│
Are you testing database operations (Save, Query, Delete)?
├─ YES → Integration Test (with DbContext, IAsyncLifetime)
│
Are you testing FluentValidation rules?
├─ YES → Validation Test (with validator.TestValidate())
│
Are you testing a repository pattern?
├─ YES → Integration Test (with real Context)
│
Are you testing a service with multiple dependencies?
├─ YES → Integration Test (with ServiceCollection)
│
Otherwise → Unit Test
```

---

## 13. Validation Checklist for Tests

Before committing a test, verify:

- [ ] Test name follows `[Verb]_[Scenario]_[ExpectedResult]` pattern
- [ ] Test has XML documentation comment
- [ ] Test uses [Fact] or [Theory] (no unnamed tests)
- [ ] No Moq or mock frameworks used
- [ ] Real dependencies are injected
- [ ] AAA pattern is clear (Arrange, Act, Assert)
- [ ] 2-5 assertions per test
- [ ] Test is deterministic (no flaky/random failures)
- [ ] Test has no dependencies on other tests
- [ ] If testing database: Author property is set before SaveChanges
- [ ] String IDs generated with NewId.NextGuid().ToString()
- [ ] All required properties initialized

---

## 14. Common Errors & Solutions

### Error: "No parameterless constructor"

**Problem**: TestDbContext or fixture constructor needs dependencies

**Solution**: `new DbContextOptionsBuilder<T>().UseInMemoryDatabase("TestDb_" + Guid.NewGuid()).Options;`

### Error: "Moq not available"

**Problem**: Project doesn't allow Moq

**Solution**: `var service = new ArticoloService(new EfCoreArticoloRepository(_context));`

### Error: "Author not set" in audit

**Problem**: SaveChanges called without setting Author

**Solution**: Set `context.Author = "test@example.com"; context.ContextIdentifier = "TestName";` before `SaveChanges()`.

### Error: "Test is flaky/random"

**Problem**: Test depends on other tests or uses DateTime.Now

**Solution**:
- Never depend on test execution order
- Use fixed test data, not current time
- Each test must be independent and repeatable

---

## References

- [xUnit Official Docs](https://xunit.net/)
- [xUnit Best Practices](https://xunit.net/docs/getting-started)
- [AAA Pattern](https://arrange-act-assert.com/)
- [EF Core Testing](https://learn.microsoft.com/en-us/ef/core/testing/)
- [FluentValidation Testing](https://docs.fluentvalidation.net/latest/testing)

## See Also

- [Naming Conventions Reference](references/naming-conventions.md)
- [Assertions Guide](references/assertions-guide.md)
- [Test Patterns](references/patterns.md)
- [Dependency Injection Patterns](references/di-patterns.md)
