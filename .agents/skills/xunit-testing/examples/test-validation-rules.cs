using Xunit;
using FluentValidation.TestHelper;

namespace Example.Tests;

/// <summary>
/// Tests for ArticoloValidator using FluentValidation testing utilities.
/// Demonstrates testing validation rules, error messages, and complex validation logic.
/// </summary>
public class ArticoloValidatorTests
{
    private readonly ArticoloValidator _validator = new();

    /// <summary>
    /// Validate_WithValidArticolo_ReturnsNoErrors:
    /// Verifies that a properly formed Articolo passes all validation rules.
    /// </summary>
    [Fact]
    public void Validate_WithValidArticolo_ReturnsNoErrors()
    {
        // Arrange
        var validArticolo = new Articolo
        {
            Id = "valid-articolo-001",
            Name = "Premium Winter Coat",
            Stagione = "Winter 2026"
        };

        // Act
        var result = _validator.TestValidate(validArticolo);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>
    /// Validate_WithMissingName_ReturnsNameRequiredError:
    /// Verifies that validation fails when required Name property is null or empty.
    /// </summary>
    [Fact]
    public void Validate_WithMissingName_ReturnsNameRequiredError()
    {
        // Arrange
        var invalidArticolo = new Articolo
        {
            Id = "invalid-1",
            Name = null!, // Required field missing
            Stagione = "Winter 2026"
        };

        // Act
        var result = _validator.TestValidate(invalidArticolo);

        // Assert
        result.ShouldHaveValidationErrorFor(a => a.Name);
    }

    /// <summary>
    /// Validate_WithEmptyName_ReturnsValidationError:
    /// Verifies that empty string for Name triggers validation error.
    /// </summary>
    [Fact]
    public void Validate_WithEmptyName_ReturnsValidationError()
    {
        // Arrange
        var invalidArticolo = new Articolo
        {
            Id = "invalid-2",
            Name = string.Empty,
            Stagione = "Summer 2026"
        };

        // Act
        var result = _validator.TestValidate(invalidArticolo);

        // Assert
        result.ShouldHaveValidationErrorFor(a => a.Name);
    }

    /// <summary>
    /// Validate_WithMissingId_ReturnsIdRequiredError:
    /// Verifies that validation fails when required Id property is not set.
    /// </summary>
    [Fact]
    public void Validate_WithMissingId_ReturnsIdRequiredError()
    {
        // Arrange
        var invalidArticolo = new Articolo
        {
            Id = null!, // Required field missing
            Name = "Valid Name",
            Stagione = "Spring 2026"
        };

        // Act
        var result = _validator.TestValidate(invalidArticolo);

        // Assert
        result.ShouldHaveValidationErrorFor(a => a.Id);
    }

    /// <summary>
    /// Validate_WithNameExceedingMaxLength_ReturnsMaxLengthError:
    /// Verifies that excessively long names trigger validation error.
    /// </summary>
    [Fact]
    public void Validate_WithNameExceedingMaxLength_ReturnsMaxLengthError()
    {
        // Arrange
        var tooLongName = new string('A', 1000); // Name exceeds maximum allowed length
        var invalidArticolo = new Articolo
        {
            Id = "invalid-3",
            Name = tooLongName,
            Stagione = "Autumn 2026"
        };

        // Act
        var result = _validator.TestValidate(invalidArticolo);

        // Assert
        result.ShouldHaveValidationErrorFor(a => a.Name);
    }

    /// <summary>
    /// Validate_WithSpecialCharactersInName_AllowsCreation:
    /// Verifies that names with special characters are accepted by the validator.
    /// </summary>
    [Theory]
    [InlineData("Jacket & Coat")]
    [InlineData("Winter's Finest")]
    [InlineData("Premium (Luxury) Edition")]
    [InlineData("Size: XL - Color: Blue")]
    public void Validate_WithSpecialCharactersInName_AllowsCreation(string nameWithSpecialChars)
    {
        // Arrange
        var articolo = new Articolo
        {
            Id = "special-chars-test",
            Name = nameWithSpecialChars,
            Stagione = "Winter 2026"
        };

        // Act
        var result = _validator.TestValidate(articolo);

        // Assert
        result.ShouldNotHaveValidationErrorFor(a => a.Name);
    }

    /// <summary>
    /// Validate_WithInvalidStagioneFormat_ReturnsValidationError:
    /// Verifies that improperly formatted season values fail validation.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithInvalidStagioneValue_ReturnsValidationError(string invalidStagione)
    {
        // Arrange
        var articolo = new Articolo
        {
            Id = "invalid-stagione",
            Name = "Test Article",
            Stagione = invalidStagione!
        };

        // Act
        var result = _validator.TestValidate(articolo);

        // Assert
        result.ShouldHaveValidationErrorFor(a => a.Stagione);
    }

    /// <summary>
    /// Validate_WithWhitespaceOnlyName_ReturnsValidationError:
    /// Verifies that whitespace-only names fail validation.
    /// </summary>
    [Fact]
    public void Validate_WithWhitespaceOnlyName_ReturnsValidationError()
    {
        // Arrange
        var articolo = new Articolo
        {
            Id = "whitespace-test",
            Name = "   ", // Only whitespace
            Stagione = "Winter 2026"
        };

        // Act
        var result = _validator.TestValidate(articolo);

        // Assert
        result.ShouldHaveValidationErrorFor(a => a.Name);
    }

    /// <summary>
    /// Validate_MultipleErrors_ReturnsAllValidationErrors:
    /// Verifies that when multiple properties are invalid, all errors are reported.
    /// </summary>
    [Fact]
    public void Validate_MultipleErrors_ReturnsAllValidationErrors()
    {
        // Arrange
        var completelyInvalidArticolo = new Articolo
        {
            Id = null!, // Invalid
            Name = null!, // Invalid
            Stagione = "" // Invalid
        };

        // Act
        var result = _validator.TestValidate(completelyInvalidArticolo);

        // Assert
        Assert.True(result.IsValid == false);
        Assert.True(result.Errors.Count >= 3);
    }
}
