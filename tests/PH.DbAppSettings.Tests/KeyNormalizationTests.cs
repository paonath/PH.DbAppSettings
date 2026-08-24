namespace PH.DbAppSettings.Tests;

public class KeyNormalizationTests
{
    private static string Normalize(string key) => key.Replace(":", "__");

    [Theory]
    [InlineData("ConnectionStrings:Default", "ConnectionStrings__Default")]
    [InlineData("Logging:LogLevel:Default", "Logging__LogLevel__Default")]
    [InlineData("MyApp:FeatureFlags:EnableCache", "MyApp__FeatureFlags__EnableCache")]
    [InlineData("AllowedHosts:0", "AllowedHosts__0")]
    [InlineData("SimpleKey", "SimpleKey")]
    [InlineData("A:B:C:D", "A__B__C__D")]
    public void Normalize_ReturnsExpectedDbKey(string input, string expected)
    {
        var result = Normalize(input);
        Assert.Equal(expected, result);
    }
}
