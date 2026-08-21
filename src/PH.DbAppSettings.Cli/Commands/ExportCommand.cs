using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using PH.DbAppSettings.Cli.Services;
using PH.DbAppSettings.Configuration;
using PH.DbAppSettings.Encryption;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PH.DbAppSettings.Cli.Commands;

public sealed class ExportCommandSettings : CommandSettings
{
    [Description("Path for the output JSON file.")]
    [CommandOption("-o|--output <OUTPUT_PATH>")]
    public string OutputPath { get; set; } = "appsettings.exported.json";

    [Description("Database connection string.")]
    [CommandOption("-c|--connection <CONNECTION_STRING>")]
    public string ConnectionString { get; set; } = "";

    [Description("SQL dialect (sqlserver, postgres, sqlite, mysql).")]
    [CommandOption("-d|--dialect <DIALECT>")]
    public string Dialect { get; set; } = "sqlserver";

    [Description("Target environment to export (e.g. Production, Development, Staging).")]
    [CommandOption("-e|--environment <ENVIRONMENT>")]
    public string Environment { get; set; } = "Production";

    [Description("Database schema name.")]
    [CommandOption("-s|--schema <SCHEMA>")]
    public string Schema { get; set; } = "";

    [Description("Database table name.")]
    [CommandOption("-t|--table <TABLE>")]
    public string Table { get; set; } = "AppSettings";

    [Description("Decryption secret key (32 bytes base64 or string).")]
    [CommandOption("--decrypt-secret <SECRET>")]
    public string? DecryptSecret { get; set; }
}

public sealed class ExportCommand : AsyncCommand<ExportCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ExportCommandSettings settings)
    {
        return await ExecuteAsync(settings);
    }

    public async Task<int> ExecuteAsync(ExportCommandSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Connection string is required. Specify with -c or --connection.");
            return 1;
        }

        var (engine, _) = StorageEngineFactory.Create(
            settings.ConnectionString,
            settings.Dialect,
            settings.Schema,
            settings.Table);

        var entries = await engine.GetAllAsync(settings.Environment);
        if (entries.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]Warning:[/] No configuration entries found for environment '{settings.Environment}'.");
        }

        IValueEncryptor? encryptor = null;
        if (!string.IsNullOrWhiteSpace(settings.DecryptSecret))
        {
            encryptor = new AesGcmValueEncryptor(settings.DecryptSecret);
        }

        var rootObject = new JsonObject();

        foreach (var entry in entries)
        {
            var value = entry.Value;
            if (entry.IsEncrypted && encryptor is not null && value is not null)
            {
                try
                {
                    value = encryptor.Decrypt(value);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Warning:[/] Failed to decrypt key '{entry.Key}': {ex.Message}");
                }
            }

            var configKey = KeyNormalizer.ToConfigurationKey(entry.Key);
            InsertIntoJsonTree(rootObject, configKey, value);
        }

        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        var jsonOutput = rootObject.ToJsonString(jsonOptions);

        var outputDir = Path.GetDirectoryName(settings.OutputPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        await File.WriteAllTextAsync(settings.OutputPath, jsonOutput);
        AnsiConsole.MarkupLine($"[bold green]Successfully exported {entries.Count} entries[/] to [cyan]{settings.OutputPath}[/].");

        return 0;
    }

    private static void InsertIntoJsonTree(JsonObject root, string path, string? value)
    {
        var segments = path.Split(':');
        var current = root;

        for (var i = 0; i < segments.Length - 1; i++)
        {
            var segment = segments[i];
            if (current.TryGetPropertyValue(segment, out var existingNode) && existingNode is JsonObject existingObj)
            {
                current = existingObj;
            }
            else
            {
                var newObj = new JsonObject();
                current[segment] = newObj;
                current = newObj;
            }
        }

        var leafSegment = segments[^1];
        current[leafSegment] = value is null ? null : JsonValue.Create(value);
    }
}
