using System.ComponentModel;
using PH.DbAppSettings.Cli.Services;
using PH.DbAppSettings.Encryption;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PH.DbAppSettings.Cli.Commands;

public sealed class RewriteJsonCommandSettings : CommandSettings
{
    [Description("Path for the rewritten output JSON file.")]
    [CommandOption("-o|--output <OUTPUT_PATH>")]
    public string OutputPath { get; set; } = "appsettings.json";

    [Description("Database connection string.")]
    [CommandOption("-c|--connection <CONNECTION_STRING>")]
    public string ConnectionString { get; set; } = "";

    [Description("SQL dialect (sqlserver, postgres, sqlite, mysql).")]
    [CommandOption("-d|--dialect <DIALECT>")]
    public string Dialect { get; set; } = "sqlserver";

    [Description("Target environment to rewrite from (e.g. Production, Development, Staging).")]
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

public sealed class RewriteJsonCommand : AsyncCommand<RewriteJsonCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RewriteJsonCommandSettings settings)
    {
        return await ExecuteAsync(settings);
    }

    public async Task<int> ExecuteAsync(RewriteJsonCommandSettings settings)
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

        var decryptedEntries = new List<KeyValuePair<string, string?>>();

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

            decryptedEntries.Add(new KeyValuePair<string, string?>(entry.Key, value));
        }

        var jsonOutput = JsonTreeReconstructor.ReconstructJson(decryptedEntries);

        var outputDir = Path.GetDirectoryName(settings.OutputPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        await File.WriteAllTextAsync(settings.OutputPath, jsonOutput);
        AnsiConsole.MarkupLine($"[bold green]Successfully rewritten {entries.Count} entries[/] to [cyan]{settings.OutputPath}[/].");

        return 0;
    }
}
