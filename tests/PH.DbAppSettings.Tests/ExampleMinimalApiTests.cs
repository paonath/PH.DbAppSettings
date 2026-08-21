using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PH.DbAppSettings.Example.MinimalApi.Models;
using PH.DbAppSettings.Services;

namespace PH.DbAppSettings.Tests;

public class ExampleMinimalApiTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _appDataDir;
    private readonly string _dbPath;
    private readonly string _jsonPath;

    public ExampleMinimalApiTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "PH.DbAppSettings.ExampleTests", Guid.NewGuid().ToString("N"));
        _appDataDir = Path.Combine(_tempDir, "App_Data");
        Directory.CreateDirectory(_appDataDir);

        _dbPath = Path.Combine(_appDataDir, "appsettings.db");
        _jsonPath = Path.Combine(_tempDir, "appsettings.json");

        File.WriteAllText(_jsonPath, """
            {
              "Logging": {
                "LogLevel": {
                  "Default": "Information"
                }
              },
              "DbAppSettings": {
                "ConnectionString": "Data Source=" + _dbPath
              },
              "Application": {
                "Title": "Inventory & Order Management API",
                "Version": "1.2.0",
                "Environment": "Production",
                "EnableSwagger": true,
                "Pagination": {
                  "DefaultPageSize": 25,
                  "MaxPageSize": 100
                },
                "Security": {
                  "JwtIssuer": "https://auth.company.com",
                  "JwtAudience": "inventory-api",
                  "TokenExpirationMinutes": 60,
                  "RequireHttps": true
                },
                "Email": {
                  "SenderName": "Inventory Notification Service",
                  "SenderEmail": "no-reply@company.com",
                  "Smtp": {
                    "Host": "smtp.mailgun.org",
                    "Port": 587,
                    "UseSsl": true,
                    "Username": "postmaster@company.com"
                  }
                },
                "Features": {
                  "EnableCache": true,
                  "CacheDurationSeconds": 300,
                  "MaintenanceMode": false,
                  "AllowedOrigins": [
                    "https://app.company.com",
                    "https://admin.company.com"
                  ]
                }
              }
            }
            """.Replace("\"Data Source=\" + _dbPath", $"\"Data Source={_dbPath}\""));
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }

    private ServiceProvider BuildServiceProvider(out IConfiguration configuration)
    {
        var bootstrapConfig = new ConfigurationBuilder()
            .AddJsonFile(_jsonPath)
            .Build();

        var builder = new ConfigurationBuilder()
            .AddJsonFile(_jsonPath);

        builder.AddDbAppSettings(bootstrapConfig, options =>
        {
            options.UseEntityFrameworkSqlite($"Data Source={_dbPath}");
            options.AutoMigrate = true;
            options.SeedOnEmpty = true;
            options.ReloadInterval = TimeSpan.FromSeconds(5);
        });

        configuration = builder.Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging();

        services.AddDbAppSettingsServices(options =>
        {
            options.UseEntityFrameworkSqlite($"Data Source={_dbPath}");
            options.ReloadInterval = TimeSpan.FromSeconds(5);
        });

        // Register typed options bindings
        services.Configure<ApplicationOptions>(configuration.GetSection("Application"));
        services.Configure<EmailOptions>(configuration.GetSection("Application:Email"));
        services.Configure<FeatureOptions>(configuration.GetSection("Application:Features"));
        services.Configure<SecurityOptions>(configuration.GetSection("Application:Security"));

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task MinimalApi_SeedsAndBinds_FullApplicationOptions()
    {
        // Arrange & Act
        using var sp = BuildServiceProvider(out _);
        var options = sp.GetRequiredService<IOptions<ApplicationOptions>>().Value;

        // Assert
        Assert.NotNull(options);
        Assert.Equal("Inventory & Order Management API", options.Title);
        Assert.Equal("1.2.0", options.Version);
        Assert.Equal("Production", options.Environment);
        Assert.True(options.EnableSwagger);

        // Nested Pagination
        Assert.Equal(25, options.Pagination.DefaultPageSize);
        Assert.Equal(100, options.Pagination.MaxPageSize);

        // Nested Security
        Assert.Equal("https://auth.company.com", options.Security.JwtIssuer);
        Assert.Equal("inventory-api", options.Security.JwtAudience);
        Assert.Equal(60, options.Security.TokenExpirationMinutes);
        Assert.True(options.Security.RequireHttps);

        // Nested Email & Smtp
        Assert.Equal("Inventory Notification Service", options.Email.SenderName);
        Assert.Equal("no-reply@company.com", options.Email.SenderEmail);
        Assert.Equal("smtp.mailgun.org", options.Email.Smtp.Host);
        Assert.Equal(587, options.Email.Smtp.Port);
        Assert.True(options.Email.Smtp.UseSsl);

        // Nested Features & Array
        Assert.True(options.Features.EnableCache);
        Assert.Equal(300, options.Features.CacheDurationSeconds);
        Assert.False(options.Features.MaintenanceMode);
        Assert.Equal(2, options.Features.AllowedOrigins.Count);
        Assert.Equal("https://app.company.com", options.Features.AllowedOrigins[0]);
        Assert.Equal("https://admin.company.com", options.Features.AllowedOrigins[1]);

        // Verify that database storage contains ONLY the JSON keys and ZERO environment variables
        using var scope = sp.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<Storage.IDbAppSettingsStorageEngine>();
        var dbEntries = await storage.GetAllAsync("Production");
        Assert.DoesNotContain(dbEntries, e => e.Key.Equals("PATH", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(dbEntries, e => e.Key.Equals("USER", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(dbEntries, e => e.Key.Equals("HOME", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(dbEntries, e => e.Key.StartsWith("DbAppSettings", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MinimalApi_SubSectionBindings_WorkIndependently()
    {
        // Arrange & Act
        using var sp = BuildServiceProvider(out _);
        var emailOptions = sp.GetRequiredService<IOptions<EmailOptions>>().Value;
        var featureOptions = sp.GetRequiredService<IOptions<FeatureOptions>>().Value;
        var securityOptions = sp.GetRequiredService<IOptions<SecurityOptions>>().Value;

        // Assert
        Assert.Equal("no-reply@company.com", emailOptions.SenderEmail);
        Assert.Equal("smtp.mailgun.org", emailOptions.Smtp.Host);

        Assert.True(featureOptions.EnableCache);
        Assert.Equal(300, featureOptions.CacheDurationSeconds);

        Assert.Equal("inventory-api", securityOptions.JwtAudience);
    }

    [Fact]
    public async Task MinimalApi_WriterMutatesSetting_AndPersistsToDatabase()
    {
        // Arrange
        using var sp = BuildServiceProvider(out var config);
        using var scope = sp.CreateScope();
        var writer = scope.ServiceProvider.GetRequiredService<IDbAppSettingsWriter>();

        // Act
        await writer.SetAsync("Application:Features:EnableCache", "false");

        // Assert
        // Verify in DB directly via writer/storage
        using var scope2 = sp.CreateScope();
        var storageEngine = scope2.ServiceProvider.GetRequiredService<Storage.IDbAppSettingsStorageEngine>();
        var entry = await storageEngine.GetByKeyAsync("Application__Features__EnableCache", "Production");

        Assert.NotNull(entry);
        Assert.Equal("false", entry.Value);
    }
}
