using Xunit;

namespace Example.Tests;

/// <summary>
/// Parametrized unit tests for Articolo entity validation with multiple scenarios.
/// Demonstrates [Theory] pattern with [InlineData] for testing various input combinations.
/// </summary>
public class ArticoloValidationTests
{
    /// <summary>
    /// Create_WithVariousNames_CreatesSuccessfully:
    /// Verifies that Articolo can be created with different name values, including edge cases.
    /// </summary>
    [Theory]
    [InlineData("Single Word")]
    [InlineData("Two Words Article")]
    [InlineData("Very Long Name With Multiple Words And Special Characters - Edition 2026")]
    [InlineData("A")]
    public void Create_WithVariousNames_CreatesSuccessfully(string name)
    {
        // Arrange
        var id = "articolo-theory-001";

        // Act
        var articolo = new Articolo
        {
            Id = id,
            Name = name
        };

        // Assert
        Assert.NotNull(articolo);
        Assert.Equal(name, articolo.Name);
        Assert.False(string.IsNullOrEmpty(articolo.Name));
    }

    /// <summary>
    /// Name_WithDifferentLengths_AllowsCreation:
    /// Parametrized test verifying that entity names of various lengths are accepted.
    /// Demonstrates different realistic name scenarios.
    /// </summary>
    [Theory]
    [InlineData("Winter Coat")]
    [InlineData("Summer Dress")]
    [InlineData("Spring Jacket")]
    [InlineData("Autumn Boots")]
    [InlineData("Casual T-Shirt")]
    public void Name_WithDifferentLengths_AllowsCreation(string name)
    {
        // Arrange & Act
        var articolo = new Articolo
        {
            Id = $"articolo-{name.Replace(" ", "-").ToLower()}",
            Name = name
        };

        // Assert
        Assert.NotNull(articolo);
        Assert.NotEmpty(articolo.Name);
        Assert.Contains(name, articolo.Name);
    }

    /// <summary>
    /// Stagione_WithSeasonValues_StoresCorrectly:
    /// Parametrized test checking that Italian season names are properly stored.
    /// Uses actual project season values.
    /// </summary>
    [Theory]
    [InlineData("Winter 2026")]
    [InlineData("Spring 2026")]
    [InlineData("Summer 2026")]
    [InlineData("Autumn 2026")]
    public void Stagione_WithSeasonValues_StoresCorrectly(string stagione)
    {
        // Arrange
        var articolo = new Articolo
        {
            Id = "articolo-stagione-test",
            Name = "Test Article",
            Stagione = stagione
        };

        // Act
        var retrievedStagione = articolo.Stagione;

        // Assert
        Assert.NotNull(retrievedStagione);
        Assert.Equal(stagione, retrievedStagione);
    }

    /// <summary>
    /// Id_WithDifferentStringFormats_StoresAsIs:
    /// Parametrized test verifying that various string ID formats are accepted and stored correctly.
    /// Tests with GUID format, hash format, and custom IDs.
    /// </summary>
    [Theory]
    [InlineData("guid-1234-5678-9abc-def01234")]
    [InlineData("id-hash-a1b2c3d4e5f6")]
    [InlineData("articolo-001")]
    [InlineData("SKU-WINTER-2026-001")]
    public void Id_WithDifferentStringFormats_StoresAsIs(string id)
    {
        // Arrange & Act
        var articolo = new Articolo { Id = id, Name = "Test" };

        // Assert
        Assert.NotNull(articolo.Id);
        Assert.Equal(id, articolo.Id);
    }

    /// <summary>
    /// Properties_AreIndependentAcrossInstances_DontInterfere:
    /// Parametrized test creating multiple instances and verifying they maintain independent state.
    /// </summary>
    [Theory]
    [InlineData("Instance-1", "Name-1")]
    [InlineData("Instance-2", "Name-2")]
    [InlineData("Instance-3", "Name-3")]
    public void Properties_AreIndependentAcrossInstances_DontInterfere(string id, string name)
    {
        // Arrange
        var articolo1 = new Articolo { Id = id, Name = name };
        var articolo2 = new Articolo { Id = "other-id", Name = "other-name" };

        // Act
        var id1Retrieved = articolo1.Id;
        var id2Retrieved = articolo2.Id;

        // Assert
        Assert.NotEqual(id1Retrieved, id2Retrieved);
        Assert.Equal(id, id1Retrieved);
        Assert.NotEqual(id, id2Retrieved);
    }
}
