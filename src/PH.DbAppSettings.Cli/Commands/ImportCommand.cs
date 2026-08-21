using System.ComponentModel;
using PH.DbAppSettings.Cli.Services;
using PH.DbAppSettings.Encryption;
using PH.DbAppSettings.Storage;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PH.DbAppSettings.Cli.Commands;

public sealed class ImportCommandSettings : CommandSettings
{
    [Description("Path to the appsettings.json file to import.")]
    [CommandArgument(0, "<FILE>")]
    public string FilePath { get; set; } = "appsettings.json";

    [Description("Database connection string.")]
    [CommandOption("-c|--connection <CONNECTION_STRING>")]
    public string ConnectionString { get; set; } = "";

    [Description("SQL dialect (sqlserver, postgres, sqlite, mysql).")]
    [CommandOption("-d|--dialect <DIALECT>")]
    public string Dialect { get; set; } = "sqlserver";

    [Description("Target environment (e.g. Production, Development, Staging).")]
    [CommandOption("-e|--environment <ENVIRONMENT>")]
    public string Environment { get; set; } = "Production";

    [Description("Database schema name.")]
    [CommandOption("-s|--schema <SCHEMA>")]
    public string Schema { get; set; } = "";

    [Description("Database table name.")]
    [CommandOption("-t|--table <TABLE>")]
    public string Table { get; set; } = "AppSettings";

    [Description("Automatically create table schema if it does not exist.")]
    [CommandOption("-m|--auto-migrate")]
    public bool AutoMigrate { get; set; } = true;

    [Description("Encryption secret key (32 bytes base64 or string).")]
    [CommandOption("--encrypt-secret <SECRET>")]
    public string? EncryptSecret { get; set; }
}

public sealed class ImportCommand : AsyncCommand<ImportCommandSettings>
{
    private readonly AppSettingsJsonAnalyzer _analyzer;

    public ImportCommand()
    {
        _analyzer = new AppSettingsJsonAnalyzer();
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ImportCommandSettings settings)
    {
        return await ExecuteAsync(settings);
    }

    public async Task<int> ExecuteAsync(ImportCommandSettings settings)
    {
        if (!File.Exists(settings.FilePath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] File not found: {settings.FilePath}");
            return 1;
        }

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

        if (settings.AutoMigrate)
        {
            AnsiConsole.MarkupLine("[grey]Ensuring database schema is created...[/]");
            await engine.EnsureSchemaCreatedAsync();
        }

        var jsonContent = await File.ReadAllTextAsync(settings.FilePath);
        var analysis = _analyzer.Analyze(jsonContent);

        IValueEncryptor? encryptor = null;
        if (!string.IsNullOrWhiteSpace(settings.EncryptSecret))
        {
            encryptor = new AesGcmValueEncryptor(settings.EncryptSecret);
        }

        var records = new List<AppSettingRecord>();
        var now = DateTimeOffset.UtcNow;

        foreach (var item in analysis.Items)
        {
            var value = item.Value;
            var isEncrypted = false;

            if (item.IsSensitive && encryptor is not null && value is not null)
            {
                value = encryptor.Encrypt(value);
                isEncrypted = true;
            }

            records.Add(new AppSettingRecord
            {
                Key = item.DbKey,
                Environment = settings.Environment,
                Value = value,
                IsEncrypted = isEncrypted,
                UpdatedAt = now
            });
        }

        await engine.UpsertBatchAsync(records);

        AnsiConsole.MarkupLine($"[bold green]Successfully imported {records.Count} configuration entries[/] into environment '[bold cyan]{settings.Environment}[/]'.");
        return 0;
    }
}
