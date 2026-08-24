using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PH.DbAppSettings.Configuration;
using PH.DbAppSettings.Storage;

namespace PH.DbAppSettings.Cli;

public static class DbAppSettingsCliRunner
{
    public const string CliPrefix = "dbappsettings";

    public static async Task<int> RunAsync(
        IServiceProvider services,
        string[] args,
        TextWriter? output = null,
        TextWriter? error = null,
        CancellationToken ct = default)
    {
        output ??= Console.Out;
        error ??= Console.Error;

        if (args.Length == 0)
        {
            PrintUsage(output);
            return 0;
        }

        var commandArgs = args.ToList();
        if (commandArgs[0].Equals(CliPrefix, StringComparison.OrdinalIgnoreCase) ||
            commandArgs[0].Equals($"--{CliPrefix}", StringComparison.OrdinalIgnoreCase))
        {
            commandArgs.RemoveAt(0);
        }

        if (commandArgs.Count == 0 || commandArgs[0] is "-h" or "--help" or "help")
        {
            PrintUsage(output);
            return 0;
        }

        var subcommand = commandArgs[0].ToLowerInvariant();
        var remainingArgs = commandArgs.Skip(1).ToArray();

        try
        {
            return subcommand switch
            {
                "analyze" => await HandleAnalyzeAsync(remainingArgs, output, error, ct),
                "import" => await HandleImportAsync(services, remainingArgs, output, error, ct),
                "ingest" => await HandleIngestAsync(services, remainingArgs, output, error, ct),
                "export" => await HandleExportAsync(services, remainingArgs, output, error, ct),
                "rewrite-json" => await HandleRewriteJsonAsync(services, remainingArgs, output, error, ct),
                _ => HandleUnknown(subcommand, error)
            };
        }
        catch (Exception ex)
        {
            await error.WriteLineAsync($"Error executing command '{subcommand}': {ex.Message}");
            return 1;
        }
    }

    private static void PrintUsage(TextWriter output)
    {
        output.WriteLine("DbAppSettings CLI Tool");
        output.WriteLine("Usage: dbappsettings <command> [options]");
        output.WriteLine();
        output.WriteLine("Commands:");
        output.WriteLine("  analyze <file> [-d|--detailed]              Analyze and audit appsettings.json");
        output.WriteLine("  import <file> [-e|--environment <env>]      Import settings into database");
        output.WriteLine("  ingest <file> [-e|--environment <env>] [-y] Ingest settings and delete source file");
        output.WriteLine("  export <output-file> [-e <env>]             Export database records to JSON file");
        output.WriteLine("  rewrite-json <output-file> [-e <env>]       Reconstruct typed JSON tree from database");
    }

    private static int HandleUnknown(string subcommand, TextWriter error)
    {
        error.WriteLine($"Unknown subcommand '{subcommand}'. Run 'dbappsettings --help' for available commands.");
        return 1;
    }

    private static async Task<int> HandleAnalyzeAsync(string[] args, TextWriter output, TextWriter error, CancellationToken ct)
    {
        if (args.Length == 0)
        {
            await error.WriteLineAsync("Error: Missing file path for analyze command. Usage: analyze <file>");
            return 1;
        }

        var filePath = args[0];
        if (!File.Exists(filePath))
        {
            await error.WriteLineAsync($"Error: File not found: {filePath}");
            return 1;
        }

        var jsonContent = await File.ReadAllTextAsync(filePath, ct);
        var result = AppSettingsJsonAnalyzer.Analyze(jsonContent);

        await output.WriteLineAsync($"--- DbAppSettings Analysis: {Path.GetFileName(filePath)} ---");
        await output.WriteLineAsync($"Total keys: {result.TotalCount}");
        await output.WriteLineAsync($"Sensitive keys detected: {result.SensitiveCount}");
        await output.WriteLineAsync();

        foreach (var item in result.Items)
        {
            var sensitiveLabel = item.IsSensitive ? "[SENSITIVE]" : "[OK]";
            var displayVal = item.IsSensitive ? "******" : (item.Value ?? "null");
            await output.WriteLineAsync($"  {sensitiveLabel,-11} {item.RawKey,-40} => {displayVal}");
        }

        return 0;
    }

    private static async Task<int> HandleImportAsync(IServiceProvider services, string[] args, TextWriter output, TextWriter error, CancellationToken ct)
    {
        if (args.Length == 0)
        {
            await error.WriteLineAsync("Error: Missing file path for import command. Usage: import <file> [-e <environment>]");
            return 1;
        }

        var filePath = args[0];
        if (!File.Exists(filePath))
        {
            await error.WriteLineAsync($"Error: File not found: {filePath}");
            return 1;
        }

        var environment = GetEnvironmentArgument(args) ?? "Production";
        var storage = services.GetService<IDbAppSettingsStorageEngine>();
        if (storage is null)
        {
            await error.WriteLineAsync("Error: IDbAppSettingsStorageEngine is not registered in service provider. Please configure AddDbAppSettings in your host application.");
            return 1;
        }

        await output.WriteLineAsync($"Importing '{filePath}' into database for environment '{environment}'...");
        await storage.EnsureSchemaCreatedAsync(ct);

        var jsonContent = await File.ReadAllTextAsync(filePath, ct);
        var analysis = AppSettingsJsonAnalyzer.Analyze(jsonContent);

        var now = DateTimeOffset.UtcNow;
        var records = analysis.Items.Select(item => new AppSettingRecord
        {
            Key = item.DbKey,
            Environment = environment,
            Value = item.Value,
            IsEncrypted = false,
            UpdatedAt = now
        }).ToList();

        await storage.UpsertBatchAsync(records, ct);
        await output.WriteLineAsync($"Successfully imported {records.Count} settings into database.");
        return 0;
    }

    private static async Task<int> HandleIngestAsync(IServiceProvider services, string[] args, TextWriter output, TextWriter error, CancellationToken ct)
    {
        if (args.Length == 0)
        {
            await error.WriteLineAsync("Error: Missing file path for ingest command. Usage: ingest <file> [-e <environment>] [-y]");
            return 1;
        }

        var filePath = args[0];
        var autoConfirm = args.Contains("-y") || args.Contains("--yes");

        var importCode = await HandleImportAsync(services, args, output, error, ct);
        if (importCode != 0)
        {
            return importCode;
        }

        if (autoConfirm)
        {
            File.Delete(filePath);
            await output.WriteLineAsync($"Deleted source file: {filePath}");
        }
        else
        {
            await output.WriteLineAsync($"Note: Source file '{filePath}' preserved. Pass -y to automatically delete on ingest.");
        }

        return 0;
    }

    private static async Task<int> HandleExportAsync(IServiceProvider services, string[] args, TextWriter output, TextWriter error, CancellationToken ct)
    {
        if (args.Length == 0)
        {
            await error.WriteLineAsync("Error: Missing output file path. Usage: export <output-file> [-e <environment>]");
            return 1;
        }

        var outputFile = args[0];
        var environment = GetEnvironmentArgument(args) ?? "Production";

        var storage = services.GetService<IDbAppSettingsStorageEngine>();
        if (storage is null)
        {
            await error.WriteLineAsync("Error: IDbAppSettingsStorageEngine is not registered in service provider.");
            return 1;
        }

        var records = await storage.GetAllAsync(environment, ct);
        var exportData = records.Select(r => new
        {
            r.Key,
            r.Environment,
            r.Value,
            r.IsEncrypted,
            r.UpdatedAt
        }).ToList();

        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(outputFile, json, ct);

        await output.WriteLineAsync($"Successfully exported {records.Count} settings to '{outputFile}'.");
        return 0;
    }

    private static async Task<int> HandleRewriteJsonAsync(IServiceProvider services, string[] args, TextWriter output, TextWriter error, CancellationToken ct)
    {
        if (args.Length == 0)
        {
            await error.WriteLineAsync("Error: Missing output file path. Usage: rewrite-json <output-file> [-e <environment>]");
            return 1;
        }

        var outputFile = args[0];
        var environment = GetEnvironmentArgument(args) ?? "Production";

        var storage = services.GetService<IDbAppSettingsStorageEngine>();
        if (storage is null)
        {
            await error.WriteLineAsync("Error: IDbAppSettingsStorageEngine is not registered in service provider.");
            return 1;
        }

        var records = await storage.GetAllAsync(environment, ct);
        var reconstructedJson = JsonTreeReconstructor.Reconstruct(records);

        await File.WriteAllTextAsync(outputFile, reconstructedJson, ct);
        await output.WriteLineAsync($"Successfully reconstructed typed JSON hierarchy in '{outputFile}'.");
        return 0;
    }

    private static string? GetEnvironmentArgument(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] is "-e" or "--environment" or "-env")
            {
                return args[i + 1];
            }
        }
        return null;
    }
}
