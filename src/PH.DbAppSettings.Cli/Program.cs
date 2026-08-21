using PH.DbAppSettings.Cli.Commands;
using Spectre.Console.Cli;

namespace PH.DbAppSettings.Cli;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var app = new CommandApp();

        app.Configure(config =>
        {
            config.SetApplicationName("dbappsettings");

            config.AddCommand<AnalyzeCommand>("analyze")
                .WithDescription("Analyzes a local appsettings.json file, showing flattened keys, types, and sensitive fields.");

            config.AddCommand<ImportCommand>("import")
                .WithDescription("Imports flattened settings from an appsettings.json file directly into a database table.");

            config.AddCommand<ExportCommand>("export")
                .WithDescription("Exports configuration settings from a database table into a structured appsettings.json file.");
        });

        return await app.RunAsync(args);
    }
}
