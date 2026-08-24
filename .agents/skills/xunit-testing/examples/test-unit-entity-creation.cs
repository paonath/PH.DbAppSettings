using Xunit;

namespace Example.Tests;

/// <summary>
/// Unit tests for Articolo entity creation and property validation.
/// Tests basic entity creation, property assignment, and required field validation.
/// </summary>
public class ArticoloCreationTests
{
    /// <summary>
    /// Create_WithAllRequiredProperties_ReturnsArticolo:
    /// Verifies that Articolo entity can be created successfully with all required properties set.
    /// </summary>
    [Fact]
    public void Create_WithAllRequiredProperties_ReturnsArticolo()
    {
        // Arrange
        var id = "articolo-unit-test-001";
        var name = "Premium Winter Coat";
        var stagione = "Winter 2026";

        // Act
        var articolo = new Articolo
        {
            Id = id,
            Name = name,
            Stagione = stagione
        };

        // Assert
        Assert.NotNull(articolo);
        Assert.Equal(id, articolo.Id);
        Assert.Equal(name, articolo.Name);
        Assert.Equal(stagione, articolo.Stagione);
    }

    /// <summary>
    /// Create_WithMinimalProperties_ReturnsArticolo:
    /// Verifies that entity creation succeeds with only essential properties.
    /// </summary>
    [Fact]
    public void Create_WithMinimalProperties_ReturnsArticolo()
    {
        // Arrange
        var id = "articolo-minimal-001";
        var name = "Basic Article";

        // Act
        var articolo = new Articolo
        {
            Id = id,
            Name = name
        };

        // Assert
        Assert.NotNull(articolo);
        Assert.Equal(id, articolo.Id);
        Assert.Equal(name, articolo.Name);
    }

    /// <summary>
    /// Properties_CanBeUpdatedAfterCreation_UpdatesSuccessfully:
    /// Verifies that entity properties can be modified after creation.
    /// </summary>
    [Fact]
    public void Properties_CanBeUpdatedAfterCreation_UpdatesSuccessfully()
    {
        // Arrange
        var articolo = new Articolo
        {
            Id = "articolo-update-001",
            Name = "Original Name"
        };
        var newName = "Updated Name";

        // Act
        articolo.Name = newName;

        // Assert
        Assert.Equal(newName, articolo.Name);
    }

    /// <summary>
    /// Id_AssignedWithNewIdString_StoresCorrectly:
    /// Verifies that string-based IDs (from NewId) are properly stored in the entity.
    /// </summary>
    [Fact]
    public void Id_AssignedWithNewIdString_StoresCorrectly()
    {
        // Arrange
        var newId = "guid-string-uuid-format-1234567890";
        var articolo = new Articolo { Id = newId, Name = "Test" };

        // Act
        var retrievedId = articolo.Id;

        // Assert
        Assert.NotNull(retrievedId);
        Assert.Equal(newId, retrievedId);
        Assert.IsType<string>(retrievedId);
    }
}
