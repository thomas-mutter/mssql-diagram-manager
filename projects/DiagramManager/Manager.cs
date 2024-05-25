using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Mutter.Tools.SqlServer.DiagramManager;

internal class Manager(IDiagramFileManager diagramFileManager, ISqlServerManager dbManager, ILogger<Manager> log)
{
    private static readonly DbArtefact DiagramTable = new("[dbo].[sysdiagrams]", "U", "sql/sysdiagrams.sql");

    internal async Task EnsureDiagramArtefactsArePresentAsync(ISqlServerManager dbManager, ILogger<Manager> log)
    {
        log.LogInformation("Ensure diagram artefacts are present");

        DbObjectState diagramTableState = await dbManager.GetDbObjectExistanceAsync(DiagramTable);

        if (diagramTableState.Exists)
        {
            log.LogInformation("{Name} is already there", diagramTableState.Name);
        }
        else
        {
            log.LogInformation("Installing {Name} from {FileName}", diagramTableState.Name, diagramTableState.FileName);

            string stmt = await diagramFileManager.ReadAllTextAsync(diagramTableState.FileName);
            await dbManager.InstallArtefactAsync(stmt);
        }
    }

    public async Task ImportAsync(string folder, string? diagramName)
    {
        if (!diagramFileManager.FolderExists(folder))
        {
            log.LogError("Folder {Folder} does not exist", folder);
            return;
        }

        await EnsureDiagramArtefactsArePresentAsync(dbManager, log);

        if (string.IsNullOrEmpty(diagramName))
        {
            log.LogInformation("Importing *.diagram from folder {Folder}", folder);
        }
        else
        {
            log.LogInformation("Importing {DiagramName}.diagram from folder {Folder}", diagramName, folder);
        }

        await foreach (DbDiagram diagram in diagramFileManager.GetDiagramsAsync(folder, diagramName))
        {
            log.LogInformation("Installing diagram {Name} with {Bytes} bytes", diagram.Name, diagram.Definition.Length);

            await dbManager.DeleteDiagramAsync(diagram.Name);
            await dbManager.InsertDiagramAsync(diagram.Name, diagram.Definition);
        }
    }

    public async Task ExportAsync(string folder, string? diagramName)
    {
        diagramFileManager.EnsureFolderExists(folder);

        DbObjectState diagramTableState = await dbManager.GetDbObjectExistanceAsync(DiagramTable);
        if (!diagramTableState.Exists)
        {
            log.LogError("Diagram support does not exist in the database!");
            return;
        }

        if (string.IsNullOrEmpty(diagramName))
        {
            log.LogInformation("Exporting *.diagram to folder {Folder}", folder);
        }
        else
        {
            log.LogInformation("Exporting {DiagramName}.diagram to folder {Folder}", diagramName, folder);
        }

        int count = 0;
        await foreach (DbDiagram diagram in dbManager.GetDiagramsAsync(diagramName))
        {
            await diagramFileManager.SaveDiagramAsync(folder, diagram);
            log.LogInformation("Exported {Name} to {Folder}", diagram.Name, folder);
            count++;
        }

        if (!string.IsNullOrEmpty(diagramName) && count == 0)
        {
            log.LogError("Diagram {DiagramName} not found in database", diagramName);
        }
        else if (count < 1)
        {
            log.LogWarning("No diagrams found in database");
        }
        else
        {
            log.LogInformation("Exported {Count} diagrams", count);
        }
    }
}
