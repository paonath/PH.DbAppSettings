using Xunit;

namespace Example.Tests;

/// <summary>
/// Integration tests for Articolo repository operations using EF Core.
/// Tests database persistence, querying, and lifecycle operations.
/// Demonstrates IAsyncLifetime for async setup/teardown.
/// </summary>
public class ArticoloRepositoryTests : IAsyncLifetime
{
    private AppDbContext _context = null!;

    /// <summary>
    /// Initialize async resources: in-memory database and test data seeding.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Create in-memory test database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_" + Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        // Seed initial test data
        var seedArticoli = new[]
        {
            new Articolo { Id = "seed-001", Name = "Winter Coat", Stagione = "Winter 2026" },
            new Articolo { Id = "seed-002", Name = "Summer Dress", Stagione = "Summer 2026" }
        };

        _context.Articoli.AddRange(seedArticoli);
        _context.Author = "integration-test@example.com";
        _context.ContextIdentifier = "ArticoloRepositoryTests.InitializeAsync";
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Cleanup: delete test database and dispose context.
    /// </summary>
    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    /// <summary>
    /// Add_WithValidEntity_PersistsToDatabase:
    /// Verifies that new Articolo entities are correctly saved to the database.
    /// </summary>
    [Fact]
    public async Task Add_WithValidEntity_PersistsToDatabase()
    {
        // Arrange
        var newArticolo = new Articolo
        {
            Id = "new-articolo-001",
            Name = "Premium Blazer",
            Stagione = "Spring 2026"
        };

        // Act
        _context.Articoli.Add(newArticolo);
        _context.Author = "integration-test@example.com";
        await _context.SaveChangesAsync();

        // Assert
        var savedArticolo = await _context.Articoli
            .FirstOrDefaultAsync(a => a.Id == newArticolo.Id);

        Assert.NotNull(savedArticolo);
        Assert.Equal(newArticolo.Name, savedArticolo.Name);
        Assert.Equal(newArticolo.Stagione, savedArticolo.Stagione);
    }

    /// <summary>
    /// Query_WithFilter_ReturnsOnlyMatchingEntities:
    /// Verifies that filtering with Where() correctly returns matching entities.
    /// </summary>
    [Fact]
    public async Task Query_WithFilter_ReturnsOnlyMatchingEntities()
    {
        // Act
        var winterArticoli = await _context.Articoli
            .Where(a => a.Stagione == "Winter 2026")
            .ToListAsync();

        // Assert
        Assert.NotEmpty(winterArticoli);
        Assert.Single(winterArticoli);
        Assert.Equal("Winter Coat", winterArticoli.First().Name);
    }

    /// <summary>
    /// Update_WithModifiedEntity_PersistsChanges:
    /// Verifies that updating an entity's properties and calling SaveChanges() persists the changes.
    /// </summary>
    [Fact]
    public async Task Update_WithModifiedEntity_PersistsChanges()
    {
        // Arrange
        var articolo = await _context.Articoli.FirstAsync(a => a.Id == "seed-001");
        var originalName = articolo.Name;
        var newName = "Updated Winter Coat - Premium";

        // Act
        articolo.Name = newName;
        _context.Author = "integration-test@example.com";
        await _context.SaveChangesAsync();

        // Assert
        var updatedArticolo = await _context.Articoli
            .FirstOrDefaultAsync(a => a.Id == "seed-001");

        Assert.NotNull(updatedArticolo);
        Assert.Equal(newName, updatedArticolo.Name);
        Assert.NotEqual(originalName, updatedArticolo.Name);
    }

    /// <summary>
    /// Delete_WithExistingEntity_RemovesFromDatabase:
    /// Verifies that removing an entity and calling SaveChanges() deletes it from the database.
    /// </summary>
    [Fact]
    public async Task Delete_WithExistingEntity_RemovesFromDatabase()
    {
        // Arrange
        var articoloToDelete = await _context.Articoli.FirstAsync(a => a.Id == "seed-002");

        // Act
        _context.Articoli.Remove(articoloToDelete);
        _context.Author = "integration-test@example.com";
        await _context.SaveChangesAsync();

        // Assert
        var deletedArticolo = await _context.Articoli
            .FirstOrDefaultAsync(a => a.Id == "seed-002");

        Assert.Null(deletedArticolo);
    }

    /// <summary>
    /// GetAll_ReturnsAllEntities:
    /// Verifies that querying all entities returns the complete collection.
    /// </summary>
    [Fact]
    public async Task GetAll_ReturnsAllEntities()
    {
        // Act
        var allArticoli = await _context.Articoli.ToListAsync();

        // Assert
        Assert.NotEmpty(allArticoli);
        Assert.Equal(2, allArticoli.Count);
    }

    /// <summary>
    /// FindById_WithExistingId_ReturnsEntity:
    /// Verifies that querying by ID returns the correct entity.
    /// </summary>
    [Fact]
    public async Task FindById_WithExistingId_ReturnsEntity()
    {
        // Act
        var articolo = await _context.Articoli
            .FirstOrDefaultAsync(a => a.Id == "seed-001");

        // Assert
        Assert.NotNull(articolo);
        Assert.Equal("Winter Coat", articolo.Name);
    }
}
