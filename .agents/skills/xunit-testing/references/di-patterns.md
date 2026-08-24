# Dependency Injection Testing Patterns

Real dependency injection patterns for xUnit tests in .NET projects (NO Moq).

## Version Policy

- Use ALWAYS the latest stable xUnit version available at implementation time.
- Official xUnit website: https://xunit.net/

## Scope

- This guide is generic and reusable across projects.
- Replace sample context/service/repository names with your project types.

---

## Table of Contents

1. [Core Principles](#core-principles)
2. [Service Registration](#service-registration)
3. [IClassFixture Pattern](#iclassfixture-pattern)
4. [IAsyncLifetime with DI](#iasynclifetime-with-di)
5. [Collection Fixtures](#collection-fixtures)
6. [Common Scenarios](#common-scenarios)
7. [Anti-Patterns](#anti-patterns)

---

## Core Principles

Use **real dependency injection** (NO Mocking frameworks like Moq).

### Why Real DI?

✅ Tests reflect real application behavior  
✅ Catches integration issues early  
✅ Simpler, more maintainable tests  
✅ Faster execution than mocked services  
✅ Natural refactoring when dependencies change  

### Real vs Mock Example

```csharp
// ❌ DON'T: Using Moq
var mockRepository = new Mock<IArticoloRepository>();
mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
    .ReturnsAsync(new Articolo());
var service = new ArticoloService(mockRepository.Object);
```

```csharp
// ✅ DO: Using real DI with in-memory database
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseInMemoryDatabase("test-db")
    .Options;
var context = new AppDbContext(options);
var repository = new ArticoloRepository(context);
var service = new ArticoloService(repository);
```

---

## Service Registration

### Setting Up ServiceCollection for Tests

Create a fixture that builds a real service provider:

```csharp
public class ServiceCollectionFixture : IAsyncDisposable
{
    private readonly IServiceProvider _serviceProvider;
    public IServiceScope Scope { get; }
    
    public ServiceCollectionFixture()
    {
        var services = new ServiceCollection();
        
        // Add DbContext with in-memory database
        var dbName = $"test-{Guid.NewGuid()}";
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName),
            ServiceLifetime.Transient
        );
        
        // Add repositories
        services.AddScoped<IArticoloRepository, ArticoloRepository>();
        services.AddScoped<IVersioneRepository, VersioneRepository>();
        
        // Add services
        services.AddScoped<IArticoloService, ArticoloService>();
        services.AddScoped<IVersioneService, VersioneService>();
        
        // Add other dependencies
        services.AddLogging();
        
        _serviceProvider = services.BuildServiceProvider();
        Scope = _serviceProvider.CreateScope();
    }
    
    public T GetService<T>() where T : notnull
    {
        return Scope.ServiceProvider.GetRequiredService<T>();
    }
    
    public async ValueTask DisposeAsync()
    {
        await Scope.DisposeAsync();
        _serviceProvider.Dispose();
    }
}
```

---

## IClassFixture Pattern

Use `IClassFixture<T>` to inject dependencies into test class:

### Simple Fixture

```csharp
public class DatabaseFixture : IAsyncLifetime
{
    private AppDbContext _context;
    public AppDbContext Context => _context;
    
    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"test-{Guid.NewGuid()}")
            .Options;
            
        _context = new AppDbContext(options);
        await _context.Database.EnsureCreatedAsync();
    }
    
    public async Task DisposeAsync()
    {
        if (_context != null)
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.DisposeAsync();
        }
    }
}
```

### Test Class Using Fixture

```csharp
public class ArticoloRepositoryTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly ArticoloRepository _repository;
    
    // Fixture injected into constructor
    public ArticoloRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _repository = new ArticoloRepository(_fixture.Context);
    }
    
    [Fact]
    public async Task Create_WithValidArticolo_SavesSuccessfully()
    {
        // Arrange
        var articolo = new Articolo 
        { 
            Id = NewId.NextGuid().ToString(),
            Name = "Test Article",
            Stagione = "Winter"
        };
        
        // Act
        var result = await _repository.CreateAsync(articolo);
        
        // Assert
        Assert.NotNull(result);
    }
    
    [Fact]
    public async Task GetById_WithExistingArticolo_ReturnsEntity()
    {
        // Arrange
        var articolo = new Articolo 
        { 
            Id = NewId.NextGuid().ToString(),
            Name = "Existing",
            Stagione = "Summer"
        };
        
        _fixture.Context.Articoli.Add(articolo);
        _fixture.Context.Author = "test@example.com";
        await _fixture.Context.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetByIdAsync(articolo.Id);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(articolo.Id, result.Id);
    }
}
```

### With ServiceCollection

```csharp
public class ServiceFixture : IAsyncLifetime
{
    private IServiceProvider _serviceProvider;
    public IServiceScope Scope { get; private set; }
    
    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
        
        // Setup database
        var dbName = $"test-{Guid.NewGuid()}";
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName),
            ServiceLifetime.Transient
        );
        
        // Setup repositories
        services.AddScoped<IArticoloRepository, ArticoloRepository>();
        services.AddScoped<IVersioneRepository, VersioneRepository>();
        
        // Setup services
        services.AddScoped<IArticoloService, ArticoloService>();
        
        _serviceProvider = services.BuildServiceProvider();
        Scope = _serviceProvider.CreateScope();
        
        // Initialize database
        var context = Scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();
    }
    
    public async Task DisposeAsync()
    {
        if (Scope != null)
        {
            var context = Scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.EnsureDeletedAsync();
            await Scope.DisposeAsync();
        }
        _serviceProvider?.Dispose();
    }
    
    public T GetService<T>() where T : notnull
    {
        return Scope.ServiceProvider.GetRequiredService<T>();
    }
}
```

### Tests Using ServiceCollection Fixture

```csharp
public class ArticoloServiceTests : IClassFixture<ServiceFixture>
{
    private readonly ServiceFixture _fixture;
    private readonly IArticoloService _service;
    private readonly AppDbContext _context;
    
    public ArticoloServiceTests(ServiceFixture fixture)
    {
        _fixture = fixture;
        _service = fixture.GetService<IArticoloService>();
        _context = fixture.GetService<AppDbContext>();
    }
    
    [Fact]
    public async Task CreateArticolo_WithValidData_ReturnsNewArticolo()
    {
        // Arrange
        _context.Author = "test@example.com";
        var articolo = new Articolo 
        { 
            Id = NewId.NextGuid().ToString(),
            Name = "New Article",
            Stagione = "Winter"
        };
        
        // Act
        var result = await _service.CreateArticoloAsync(articolo);
        
        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(default, result.Id);
    }
}
```

---

## IAsyncLifetime with DI

Combine `IAsyncLifetime` with dependency injection for test setup:

```csharp
public class ArticoloIntegrationTests : IAsyncLifetime
{
    private AppDbContext _context;
    private IArticoloRepository _repository;
    private IArticoloService _service;
    
    // Initialize async dependencies before each test
    public async Task InitializeAsync()
    {
        // Setup database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"test-{Guid.NewGuid()}")
            .Options;
            
        _context = new AppDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        
        // Initialize repository (depends on context)
        _repository = new ArticoloRepository(_context);
        
        // Initialize service (depends on repository)
        _service = new ArticoloService(_repository);
    }
    
    // Cleanup after each test
    public async Task DisposeAsync()
    {
        if (_context != null)
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.DisposeAsync();
        }
    }
    
    [Fact]
    public async Task Create_PersistsToDatabase_AndRetrievable()
    {
        // Arrange
        var articolo = new Articolo 
        { 
            Id = NewId.NextGuid().ToString(),
            Name = "Test Article",
            Stagione = "Winter"
        };
        
        // Act - Create through service
        var created = await _service.CreateArticoloAsync(articolo);
        
        // Act - Retrieve through repository
        var retrieved = await _repository.GetByIdAsync(created.Id);
        
        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(created.Name, retrieved.Name);
        Assert.Equal(created.Stagione, retrieved.Stagione);
    }
}
```

---

## Collection Fixtures

Share fixture state across multiple test classes:

### Define Collection Fixture

```csharp
// Define the collection
[CollectionDefinition("Database Collection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    // This class has no code, just defines the collection
}
```

### Use in Multiple Test Classes

```csharp
[Collection("Database Collection")]
public class ArticoloRepositoryTests
{
    private readonly DatabaseFixture _fixture;
    
    public ArticoloRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task GetAll_ReturnsAllArticoli()
    {
        // Uses shared _fixture
        var all = await new ArticoloRepository(_fixture.Context)
            .GetAllAsync();
            
        Assert.NotEmpty(all);
    }
}

[Collection("Database Collection")]
public class ArticoloServiceTests
{
    private readonly DatabaseFixture _fixture;
    
    public ArticoloServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task Create_WithValidData_SavesToSharedDatabase()
    {
        // Uses same _fixture as ArticoloRepositoryTests
        var service = new ArticoloService(
            new ArticoloRepository(_fixture.Context)
        );
        
        // ...
    }
}
```

---

## Common Scenarios

### Scenario 1: Testing with Seeded Data

```csharp
public class ArticoloRepositoryWithDataTests : IAsyncLifetime
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
        
        // Seed data
        await SeedTestDataAsync();
        
        _repository = new ArticoloRepository(_context);
    }
    
    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }
    
    private async Task SeedTestDataAsync()
    {
        _context.Articoli.AddRange(
            new Articolo 
            { 
                Id = "winter-1", 
                Name = "Winter Coat", 
                Stagione = "Winter" 
            },
            new Articolo 
            { 
                Id = "summer-1", 
                Name = "Summer Dress", 
                Stagione = "Summer" 
            },
            new Articolo 
            { 
                Id = "spring-1", 
                Name = "Spring Jacket", 
                Stagione = "Spring" 
            }
        );
        
        _context.Author = "seed@example.com";
        await _context.SaveChangesAsync();
    }
    
    [Fact]
    public async Task GetByStagione_WithWinterFilter_ReturnsOnlyWinter()
    {
        // Act
        var results = await _repository.GetByStagioneAsync("Winter");
        
        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, a => Assert.Equal("Winter", a.Stagione));
    }
}
```

### Scenario 2: Testing with Multiple Services

```csharp
public class ArticoloWorkflowTests : IAsyncLifetime
{
    private AppDbContext _context;
    private IArticoloRepository _articoloRepository;
    private IVersioneRepository _versioneRepository;
    private IArticoloService _articoloService;
    private IVersioneService _versioneService;
    
    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"test-{Guid.NewGuid()}")
            .Options;
            
        _context = new AppDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        
        // Initialize all repositories
        _articoloRepository = new ArticoloRepository(_context);
        _versioneRepository = new VersioneRepository(_context);
        
        // Initialize all services
        _articoloService = new ArticoloService(_articoloRepository);
        _versioneService = new VersioneService(
            _versioneRepository, 
            _articoloRepository
        );
    }
    
    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }
    
    [Fact]
    public async Task CreateArticoloWithVersione_Works()
    {
        // Arrange
        _context.Author = "test@example.com";
        var articolo = new Articolo { /* ... */ };
        var versione = new Versione { /* ... */ };
        
        // Act
        var createdArticolo = await _articoloService.CreateArticoloAsync(articolo);
        versione.ArticoloId = createdArticolo.Id;
        var createdVersione = await _versioneService.CreateVersioneAsync(versione);
        
        // Assert
        Assert.NotNull(createdArticolo);
        Assert.NotNull(createdVersione);
        Assert.Equal(createdArticolo.Id, createdVersione.ArticoloId);
    }
}
```

### Scenario 3: Testing Exception Scenarios

```csharp
public class ArticoloValidationTests
{
    private readonly ArticoloValidator _validator;
    
    public ArticoloValidationTests()
    {
        _validator = new ArticoloValidator();
    }
    
    [Fact]
    public void Validate_WithNullName_ThrowsValidationException()
    {
        // Arrange
        var articolo = new Articolo 
        { 
            Id = "1",
            Name = null!,
            Stagione = "Winter"
        };
        
        // Act
        var result = _validator.TestValidate(articolo);
        
        // Assert
        result.ShouldHaveValidationErrorFor(a => a.Name);
    }
}
```

---

## Anti-Patterns

### ❌ Using Moq/Mocking Frameworks

```csharp
// DON'T: Inject mocks
var mockRepository = new Mock<IArticoloRepository>();
mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
    .ReturnsAsync(new Articolo { Id = "1" });

var service = new ArticoloService(mockRepository.Object);
var result = await service.GetArticoloAsync("1");
```

**Better**: Use real repository with in-memory database
```csharp
// DO: Use real dependencies
var context = new AppDbContext(
    new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase("test-db")
        .Options
);
var repository = new ArticoloRepository(context);
var service = new ArticoloService(repository);
```

### ❌ Creating New Instances Without Cleanup

```csharp
// DON'T: No cleanup
[Fact]
public void Test1() { var context = new AppDbContext(/**/); /* test */ }

[Fact]
public void Test2() { var context = new AppDbContext(/**/); /* test */ }
// Database not cleaned between tests
```

**Better**: Use IAsyncLifetime for cleanup
```csharp
// DO: Proper cleanup
public class Tests : IAsyncLifetime
{
    private AppDbContext _context;
    
    public async Task InitializeAsync()
    {
        _context = new AppDbContext(/**/);
        await _context.Database.EnsureCreatedAsync();
    }
    
    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }
}
```

### ❌ Creating Global Static Fixtures

```csharp
// DON'T: Static state shared across tests
public static class GlobalFixture
{
    public static AppDbContext Context { get; set; } = new(/**/);
}

[Fact]
public void Test1() { /* modifies GlobalFixture.Context */ }

[Fact]
public void Test2() { /* sees changes from Test1 */ } // ❌ Test coupling
```

**Better**: Unique fixture per test
```csharp
// DO: Each test gets clean instance
public class Tests : IAsyncLifetime
{
    private AppDbContext _context;
    
    public async Task InitializeAsync()
    {
        // Each test gets unique database name
        var dbName = $"test-{Guid.NewGuid()}";
        _context = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options
        );
    }
}
```

---

## References

[xUnit Fixtures](https://xunit.net/docs/fixtures)  
[xUnit Dependency Injection](https://xunit.net/docs/getting-started/netcore#console-runner)  
[Service Collection](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.servicecollection)  
[EF Core In-Memory Provider](https://docs.microsoft.com/en-us/ef/core/providers/in-memory/)
