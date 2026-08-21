using PH.DbAppSettings.Configuration;

namespace PH.DbAppSettings.Tests;

public class KeyNormalizerTests
{
    [Theory]
    [InlineData("Logging__LogLevel__Default", "Logging:LogLevel:Default")]
    [InlineData("ConnectionStrings__Default", "ConnectionStrings:Default")]
    [InlineData("MyApp__FeatureFlags__EnableCache", "MyApp:FeatureFlags:EnableCache")]
    [InlineData("AllowedHosts__0", "AllowedHosts:0")]
    [InlineData("SimpleKey", "SimpleKey")]
    [InlineData("A__B__C__D", "A:B:C:D")]
    [InlineData("A/B/C", "A:B:C")]
    public void ToConfigurationKey_ReturnsExpectedNormalizedKey(string dbKey, string expectedConfigKey)
    {
        // Act
        var result = KeyNormalizer.ToConfigurationKey(dbKey);

        // Assert
        Assert.Equal(expectedConfigKey, result);
    }

    [Theory]
    [InlineData("Logging:LogLevel:Default", "Logging__LogLevel__Default")]
    [InlineData("ConnectionStrings:Default", "ConnectionStrings__Default")]
    [InlineData("MyApp:FeatureFlags:EnableCache", "MyApp__FeatureFlags__EnableCache")]
    [InlineData("AllowedHosts:0", "AllowedHosts__0")]
    [InlineData("SimpleKey", "SimpleKey")]
    [InlineData("A:B:C:D", "A__B__C__D")]
    public void ToDbKey_ReturnsExpectedNormalizedKey(string configKey, string expectedDbKey)
    {
        // Act
        var result = KeyNormalizer.ToDbKey(configKey);

        // Assert
        Assert.Equal(expectedDbKey, result);
    }

    [Fact]
    public void ToConfigurationKey_ThrowsOnNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => KeyNormalizer.ToConfigurationKey(null!));
    }

    [Fact]
    public void ToDbKey_ThrowsOnNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => KeyNormalizer.ToDbKey(null!));
    }
}
