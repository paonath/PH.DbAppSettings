using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PH.DbAppSettings;
using PH.DbAppSettings.Example.MinimalApi.Data;
using PH.DbAppSettings.Example.MinimalApi.Models;
using PH.DbAppSettings.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Ensure App_Data directory exists
var appDataPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
if (!Directory.Exists(appDataPath))
{
    Directory.CreateDirectory(appDataPath);
}

var dbPath = Path.Combine(appDataPath, "appsettings.db");
var connectionString = $"Data Source={dbPath}";

// 1. Bootstrap configuration
var bootstrapConfig = new ConfigurationBuilder()
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

// 2. Add DbAppSettings configuration provider with Entity Framework Core and AppDbContext
builder.Configuration.AddDbAppSettings<AppDbContext>(bootstrapConfig, options =>
{
    options.UseEntityFramework<AppDbContext>(b => b.UseSqlite(connectionString));
    options.AutoMigrate = true;
    options.SeedOnEmpty = true;
    options.ReloadInterval = TimeSpan.FromSeconds(5);
});

// 3. Register DbAppSettings DI services
builder.Services.AddDbContext<AppDbContext>(opts => opts.UseSqlite(connectionString));

builder.Services.AddDbAppSettingsServices<AppDbContext>(options =>
{
    options.UseEntityFramework<AppDbContext>(b => b.UseSqlite(connectionString));
    options.ReloadInterval = TimeSpan.FromSeconds(5);
});

// 4. Strongly typed Options binding for root & nested sections
builder.Services.Configure<ApplicationOptions>(builder.Configuration.GetSection("Application"));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Application:Email"));
builder.Services.Configure<FeatureOptions>(builder.Configuration.GetSection("Application:Features"));
builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection("Application:Security"));

// OpenAPI / Documentation
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment() || true)
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Home / Info endpoint
app.MapGet("/", () => Results.Ok(new
{
    Application = "PH.DbAppSettings Minimal API Example",
    Description = "Demonstrating dynamic SQLite configuration with Entity Framework Core and typed Options binding.",
    Documentation = "/scalar/v1",
    Endpoints = new[]
    {
        "GET /api/options",
        "GET /api/options/email",
        "GET /api/options/features",
        "GET /api/options/security",
        "GET /api/settings",
        "POST /api/settings",
        "DELETE /api/settings/{key}"
    }
}))
.WithName("GetHomeInfo")
.WithSummary("API Information and Endpoints list");

// GET /api/options: Full strongly typed ApplicationOptions
app.MapGet("/api/options", (IOptionsSnapshot<ApplicationOptions> options) => Results.Ok(options.Value))
    .WithName("GetApplicationOptions")
    .WithSummary("Retrieves the complete ApplicationOptions hierarchy bound from database");

// GET /api/options/email: Strongly typed EmailOptions
app.MapGet("/api/options/email", (IOptionsSnapshot<EmailOptions> options) => Results.Ok(options.Value))
    .WithName("GetEmailOptions")
    .WithSummary("Retrieves EmailOptions (including SMTP settings)");

// GET /api/options/features: Strongly typed FeatureOptions
app.MapGet("/api/options/features", (IOptionsSnapshot<FeatureOptions> options) => Results.Ok(options.Value))
    .WithName("GetFeatureOptions")
    .WithSummary("Retrieves FeatureOptions (including flags and allowed origins array)");

// GET /api/options/security: Strongly typed SecurityOptions
app.MapGet("/api/options/security", (IOptionsSnapshot<SecurityOptions> options) => Results.Ok(options.Value))
    .WithName("GetSecurityOptions")
    .WithSummary("Retrieves SecurityOptions (JWT issuer, audience, expiration)");

// GET /api/settings: Flattened key/value dump from IConfiguration
app.MapGet("/api/settings", (IConfiguration config) =>
{
    var settings = config.AsEnumerable()
        .Where(kvp => kvp.Value is not null)
        .OrderBy(kvp => kvp.Key)
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

    return Results.Ok(settings);
})
.WithName("GetAllSettings")
.WithSummary("Retrieves all flattened configuration keys loaded in IConfiguration");

// POST /api/settings: Mutate a setting in the database at runtime
app.MapPost("/api/settings", async ([FromBody] SetSettingRequest request, IDbAppSettingsWriter writer) =>
{
    if (string.IsNullOrWhiteSpace(request.Key))
    {
        return Results.BadRequest(new ApiResponse<string>(false, "Key cannot be empty.", null));
    }

    await writer.SetAsync(request.Key, request.Value);
    return Results.Ok(new ApiResponse<SetSettingRequest>(true, $"Setting '{request.Key}' successfully updated.", request));
})
.WithName("UpdateSetting")
.WithSummary("Upserts a setting in the database and triggers options reload");

// DELETE /api/settings/{key}: Remove a setting from the database at runtime
app.MapDelete("/api/settings/{key}", async (string key, IDbAppSettingsWriter writer) =>
{
    await writer.DeleteAsync(key);
    return Results.Ok(new ApiResponse<string>(true, $"Setting '{key}' removed from database.", key));
})
.WithName("DeleteSetting")
.WithSummary("Deletes a setting from the database");

app.Run();

// Make the implicit Program class public so test projects can reference it
public partial class Program { }
