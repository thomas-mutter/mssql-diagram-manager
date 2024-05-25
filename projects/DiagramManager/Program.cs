using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

    public static async Task Main(string[] args)
    {
        IHost host;
        try
        {
            ConfigureLogging();
            host = BuildHost(args);
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine(ex.Message);
            Environment.ExitCode = 1;
            return;
        }

        await host.RunAsync();
    }

    public static IHost BuildHost(string[] args) => Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration(builder => builder.AddCommandLine(args, CommandLineSwitchMappings))
        .ConfigureServices((context, services) =>
        {
            Settings settings = context.Configuration.GetValidatedSettings();
            services.AddSingleton(settings);
            services.AddTransient<DiagramManager>();
            services.AddSingleton<IHostedService, TaskWorker>();
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
}
