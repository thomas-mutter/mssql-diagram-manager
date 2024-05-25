using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Mutter.Tools.SqlServer.DiagramManager;

public static class Program
{
    public static IDictionary<string, string>? CommandLineSwitchMappings => new Dictionary<string, string>
    {
        ["-db"] = "ConnectionString",
        ["-f"] = "Folder",
        ["-m"] = "Mode",
        ["-n"] = "DiagramName"
    };

    public static string Usage = """

        manage-sql-diagrams -m export|import -f <directory> -db <connectionString> [-n <DiagramName>]
        
        Parameters:
        -m,  --Mode             (required): Mode of the operation (export or import)
        -f,  --Folder           (required): Folder where the diagram will be exported or imported
        -n,  --DiagramName      (optional): Name of the diagram to export or import
        -db, --ConnectionString (required): Connection string to the database

        """;

    public static async Task Main(string[] args)
    {
        if (args.Length > 0 && args[0].Equals("-h", StringComparison.InvariantCultureIgnoreCase))
        {
            Console.Out.WriteLine(Usage);
            Environment.ExitCode = 0;
            return;
        }

        IHost host;
        try
        {
            ConfigureLogging();
            host = BuildHost(args);
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine(ex.Message);

            Console.Out.WriteLine();
            Console.Out.WriteLine(Usage);

            Environment.ExitCode = 1;
            return;
        }
        
        Settings settings = host.Services.GetRequiredService<Settings>();
        Manager manager = host.Services.GetRequiredService<Manager>();
        ILogger<Manager> log = host.Services.GetRequiredService<ILogger<Manager>>();
        await DoJobAsync(settings, manager, log);
    }

    public static IHost BuildHost(string[] args) => Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration(builder => builder.AddCommandLine(args, CommandLineSwitchMappings))
        .ConfigureServices((context, services) =>
        {
            Settings settings = context.Configuration.GetValidatedSettings();
            services.AddSingleton(settings);
            services.AddTransient<IDiagramFileManager, DiagramFileManager>();
            services.AddTransient<ISqlServerManager, SqlServerManager>();
            services.AddTransient<Manager>();
        })
        .UseSerilog()
        .Build();

    private static Settings GetValidatedSettings(this IConfiguration configuration)
    {
        Settings result = configuration.Get<Settings>() ?? throw new InvalidOperationException("Settings cannot be bind to configuration");

        if (string.IsNullOrWhiteSpace(result.ConnectionString))
        {
            throw new InvalidOperationException("ConnectionString is not configured, please specify database hostname using -db|--ConnectionString");
        }

        if (string.IsNullOrWhiteSpace(result.Folder))
        {
            throw new InvalidOperationException("Path is not configured, please specify the path where your diagramms are store using -p|--Path");
        }

        if (!result.Import && !result.Export)
        {
            throw new InvalidOperationException("Mode is not configured, please specify mode using -m|--Mode (import|export)");
        }

        return result;
    }

    private static void ConfigureLogging()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    private static async Task<int> DoJobAsync(Settings settings, Manager manager, ILogger<Manager> log)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(log);

        try
        {
            if (settings.Import)
            {
                log.LogInformation("Importing diagrams");
                await manager.ImportAsync(settings.Folder, settings.DiagramName);
            }

            if (settings.Export)
            {
                log.LogInformation("Exporting diagrams");
                await manager.ExportAsync(settings.Folder, settings.DiagramName);
            }

            return 0;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error importing or exporting diagrams");
            return 1;
        }
    }
}
