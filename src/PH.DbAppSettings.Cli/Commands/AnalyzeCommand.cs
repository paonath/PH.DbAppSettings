using System.ComponentModel;
using PH.DbAppSettings.Cli.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PH.DbAppSettings.Cli.Commands;

public sealed class AnalyzeCommandSettings : CommandSettings
{
    [Description("Path to the appsettings.json file to analyze.")]
    [CommandArgument(0, "<FILE>")]
    public string FilePath { get; set; } = "appsettings.json";

    [Description("Show all flattened keys in a detailed table.")]
    [CommandOption("-d|--detailed")]
    public bool Detailed { get; set; }
}

public sealed class AnalyzeCommand : AsyncCommand<AnalyzeCommandSettings>
{
    private readonly AppSettingsJsonAnalyzer _analyzer;

    public AnalyzeCommand()
    {
        _analyzer = new AppSettingsJsonAnalyzer();
    }

    public override async Task<int> ExecuteAsync(CommandContext context, AnalyzeCommandSettings settings)
    {
        return await ExecuteAsync(settings);
    }

    public async Task<int> ExecuteAsync(AnalyzeCommandSettings settings)
    {
        if (!File.Exists(settings.FilePath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] File not found: {settings.FilePath}");
            return 1;
        }

        var jsonContent = await File.ReadAllTextAsync(settings.FilePath);
        var result = _analyzer.Analyze(jsonContent);

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("[bold]Configuration Key[/]");
        table.AddColumn("[bold]Database Key[/]");
        table.AddColumn("[bold]Type[/]");
        table.AddColumn("[bold]Value[/]");
        table.AddColumn("[bold]Sensitive?[/]");

        foreach (var item in result.Items)
        {
            var sensitiveMarkup = item.IsSensitive ? "[red]YES (Candidate for encryption)[/]" : "[green]No[/]";
            var displayValue = item.IsSensitive ? "******" : (item.Value ?? "[grey]null[/]");
            table.AddRow(
                Markup.Escape(item.RawKey),
                Markup.Escape(item.DbKey),
                item.ValueType,
                Markup.Escape(displayValue),
                sensitiveMarkup);
        }

        AnsiConsole.Write(new Rule($"[bold blue]DbAppSettings Analysis: {Path.GetFileName(settings.FilePath)}[/]"));
        AnsiConsole.MarkupLine($"Total flattened keys: [bold green]{result.TotalKeys}[/]");
        AnsiConsole.MarkupLine($"Sensitive keys detected: [bold red]{result.SensitiveKeysCount}[/]");
        AnsiConsole.WriteLine();

        if (settings.Detailed || result.TotalKeys <= 30)
        {
            AnsiConsole.Write(table);
        }

        return 0;
    }
}
