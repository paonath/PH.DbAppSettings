using Xunit;

namespace Example.Tests;

/// <summary>
/// Integration tests for ArticoloRepository pattern with real EF Core operations.
/// Tests CRUD operations, filtering, and complex queries on the repository layer.
/// </summary>
public class ArticoloRepositoryPatternTests
{
    private readonly AppDbContext _context;
    private readonly ArticoloRepository _repository;

    /// <summary>
    /// Initialize repository with in-memory test database.
    /// </summary>
    public ArticoloRepositoryPatternTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_" + Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new ArticoloRepository(_context);
    }

    /// <summary>
    /// Create_WithValidArticolo_SavesSuccessfully:
    /// Verifies that the repository correctly saves a new Articolo entity.
    /// </summary>
    [Fact]
    public async Task Create_WithValidArticolo_SavesSuccessfully()
    {
        // Arrange
        var newArticolo = new Articolo
        {
            Id = "repo-test-001",
            Name = "Premium Cashmere Sweater",
            Stagione = "Winter 2026"
        };

        // Act
        await _repository.CreateAsync(newArticolo);

        // Assert
        var saved = await _context.Articoli
            .FirstOrDefaultAsync(a => a.Id == "repo-test-001");

        Assert.NotNull(saved);
        Assert.Equal(newArticolo.Name, saved.Name);
    }

    /// <summary>
    /// GetById_WithExistingArticolo_ReturnsArticolo:
    /// Verifies that the repository retrieves an entity by ID correctly.
    /// </summary>
    [Fact]
    public async Task GetById_WithExistingArticolo_ReturnsArticolo()
    {
        // Arrange
        var articolo = new Articolo
        {
            Id = "repo-get-001",
            Name = "Test Article",
            Stagione = "Summer 2026"
        };
        _context.Articoli.Add(articolo);
        _context.Author = "repo-test@example.com";
        await _context.SaveChangesAsync();

        // Act
        var retrieved = await _repository.GetByIdAsync("repo-get-001");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(articolo.Name, retrieved.Name);
    }

    /// <summary>
    /// GetById_WithNonExistentId_ReturnsNull:
    /// Verifies that querying a non-existent ID returns null.
    /// </summary>
    [Fact]
    public async Task GetById_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync("non-existent-id");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// UpdateName_WithExistingArticolo_ModifiesSuccessfully:
    /// Verifies that the repository correctly updates entity properties.
    /// </summary>
    [Fact]
    public async Task UpdateName_WithExistingArticolo_ModifiesSuccessfully()
    {
        // Arrange
        var articolo = new Articolo
        {
            Id = "repo-update-001",
            Name = "Original Name",
            Stagione = "Spring 2026"
        };
        _context.Articoli.Add(articolo);
        _context.Author = "repo-test@example.com";
        await _context.SaveChangesAsync();

        var newName = "Updated Premium Article";

        // Act
        await _repository.UpdateNameAsync("repo-update-001", newName);

        // Assert
        var updated = await _repository.GetByIdAsync("repo-update-001");
        Assert.NotNull(updated);
        Assert.Equal(newName, updated.Name);
    }

    /// <summary>
    /// Delete_WithExistingArticolo_RemovesSuccessfully:
    /// Verifies that the repository correctly deletes an entity.
    /// </summary>
    [Fact]
    public async Task Delete_WithExistingArticolo_RemovesSuccessfully()
    {
        // Arrange
        var articolo = new Articolo
        {
            Id = "repo-delete-001",
            Name = "To Be Deleted",
            Stagione = "Autumn 2026"
        };
        _context.Articoli.Add(articolo);
        _context.Author = "repo-test@example.com";
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync("repo-delete-001");

        // Assert
        var result = await _repository.GetByIdAsync("repo-delete-001");
        Assert.Null(result);
    }

    /// <summary>
    /// GetByStagione_WithExistingFilter_ReturnsFilteredArticoli:
    /// Verifies that the repository correctly filters entities by season.
    /// </summary>
    [Fact]
    public async Task GetByStagione_WithExistingFilter_ReturnsFilteredArticoli()
    {
        // Arrange
        _context.Articoli.AddRange(
            new Articolo { Id = "season-1", Name = "Winter Item", Stagione = "Winter 2026" },
            new Articolo { Id = "season-2", Name = "Summer Item", Stagione = "Summer 2026" },
            new Articolo { Id = "season-3", Name = "Another Winter", Stagione = "Winter 2026" }
        );
        _context.Author = "repo-test@example.com";
        await _context.SaveChangesAsync();

        // Act
        var winterArticoli = await _repository.GetByStagioneAsync("Winter 2026");

        // Assert
        Assert.NotEmpty(winterArticoli);
        Assert.Equal(2, winterArticoli.Count);
        Assert.All(winterArticoli, a => Assert.Equal("Winter 2026", a.Stagione));
    }

    /// <summary>
    /// GetAll_ReturnsAllArticoli:
    /// Verifies that the repository returns all entities without filtering.
    /// </summary>
    [Fact]
    public async Task GetAll_ReturnsAllArticoli()
    {
        // Arrange
        _context.Articoli.AddRange(
            new Articolo { Id = "all-1", Name = "Article 1", Stagione = "Winter 2026" },
            new Articolo { Id = "all-2", Name = "Article 2", Stagione = "Spring 2026" }
        );
        _context.Author = "repo-test@example.com";
        await _context.SaveChangesAsync();

        // Act
        var all = await _repository.GetAllAsync();

        // Assert
        Assert.NotEmpty(all);
        Assert.True(all.Count >= 2);
    }
}
