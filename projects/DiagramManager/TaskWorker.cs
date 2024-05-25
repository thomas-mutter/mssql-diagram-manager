using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mutter.Tools.SqlServer.DiagramManager;

public class TaskWorker(IHostApplicationLifetime lifetime, Settings settings, DiagramManager manager, ILogger<TaskWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ArgumentNullException.ThrowIfNull(lifetime);
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

            log.LogInformation("Diagram maintenance done");
        }
        catch (Exception ex)
        {
            log.LogInformation(ex, "Error in database digram maintenance");
        }

        lifetime.StopApplication();
    }
}
