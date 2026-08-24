using PH.DbAppSettings.Cli;
using PH.DbAppSettings.Cli.Models;

namespace PH.DbAppSettings.Tests;

public class AppSettingsJsonAnalyzerTests
{
    [Fact]
    public void AnalyzeJson_FlattensNestedJson_Correctly()
    {
        // Arrange
        var json = """
            {
              "Logging": {
                "LogLevel": {
                  "Default": "Information",
                  "Microsoft.AspNetCore": "Warning"
                }
              },
              "AllowedHosts": "*",
              "Features": {
                "MaxRetries": 5,
                "EnableCache": true,
                "Tags": ["Alpha", "Beta"]
              }
            }
            """;

        // Act
        var result = AppSettingsJsonAnalyzer.Analyze(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(7, result.TotalKeys);

        var defaultLogLevel = result.Items.FirstOrDefault(i => i.RawKey == "Logging:LogLevel:Default");
        Assert.NotNull(defaultLogLevel);
        Assert.Equal("Logging__LogLevel__Default", defaultLogLevel.DbKey);
        Assert.Equal("Information", defaultLogLevel.Value);

        var maxRetries = result.Items.FirstOrDefault(i => i.RawKey == "Features:MaxRetries");
        Assert.NotNull(maxRetries);
        Assert.Equal("5", maxRetries.Value);

        var enableCache = result.Items.FirstOrDefault(i => i.RawKey == "Features:EnableCache");
        Assert.NotNull(enableCache);
        Assert.Equal("true", enableCache.Value);

        var tag0 = result.Items.FirstOrDefault(i => i.RawKey == "Features:Tags:0");
        Assert.NotNull(tag0);
        Assert.Equal("Features__Tags__0", tag0.DbKey);
        Assert.Equal("Alpha", tag0.Value);
    }

    [Fact]
    public void AnalyzeJson_DetectsSensitiveKeys()
    {
        // Arrange
        var json = """
            {
              "ConnectionStrings": {
                "DefaultConnection": "Server=localhost;Database=mydb;"
              },
              "Security": {
                "ApiSecret": "super-secret-key-123",
                "UserPassword": "password123",
                "JwtToken": "xyz789"
              },
              "Public": {
                "AppName": "MyApp"
              }
            }
            """;

        // Act
        var result = AppSettingsJsonAnalyzer.Analyze(json);

        // Assert
        Assert.Equal(5, result.TotalKeys);
        Assert.Equal(4, result.SensitiveKeysCount);

        var appName = result.Items.First(i => i.RawKey == "Public:AppName");
        Assert.False(appName.IsSensitive);

        var secret = result.Items.First(i => i.RawKey == "Security:ApiSecret");
        Assert.True(secret.IsSensitive);

        var connStr = result.Items.First(i => i.RawKey == "ConnectionStrings:DefaultConnection");
        Assert.True(connStr.IsSensitive);
    }

    [Fact]
    public void AnalyzeJson_EmptyJson_ReturnsZeroKeys()
    {
        // Arrange
        var json = "{}";

        // Act
        var result = AppSettingsJsonAnalyzer.Analyze(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalKeys);
        Assert.Empty(result.Items);
    }
}
