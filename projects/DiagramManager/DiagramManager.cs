using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Mutter.Tools.SqlServer.DiagramManager;

public class DiagramManager(Settings settings, ILogger<DiagramManager> log)
{
    private static readonly (string Artefact, string Type, string FileName)[] DiagramArtefacts =
    [
        ("[dbo].[sysdiagrams]", "U", "DiagramSupport/sysdiagrams.sql"),
        ("[dbo].[sp_alterdiagram]", "P", "DiagramSupport/sp_alterdiagram.sql"),
        ("[dbo].[sp_creatediagram]", "P", "DiagramSupport/sp_creatediagram.sql"),
        ("[dbo].[sp_dropdiagram]", "P", "DiagramSupport/sp_dropdiagram.sql"),
        ("[dbo].[sp_helpdiagramdefinition]", "P", "DiagramSupport/sp_helpdiagramdefinition.sql"),
        ("[dbo].[sp_helpdiagrams]", "P", "DiagramSupport/sp_helpdiagrams.sql"),
        ("[dbo].[sp_renamediagram]", "P", "DiagramSupport/sp_renamediagram.sql"),
        ("[dbo].[sp_upgraddiagrams]", "P", "DiagramSupport/sp_upgraddiagrams.sql")
    ];

    private async Task<(string Name, string FileName, bool Exists)> GetDiagramArtefactExistanceAsync(
        SqlConnection db, string name, string type, string fileName)
    {
        const string stmt = "select count(1) from sys.objects where object_id = OBJECT_ID(@name) and type = @type";
        SqlCommand cmd = new(stmt, db);
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@type", type);
        object? result = await cmd.ExecuteScalarAsync();
        return (name, fileName, result != DBNull.Value && Convert.ToInt32(result) > 0);
    }

    private async Task ExportDiagramsAsync(SqlConnection db, string folder, string? diagramName)
    {
        bool allDiagrams = string.IsNullOrEmpty(diagramName);

        DirectoryInfo directoryInfo = new(folder);
        if (!directoryInfo.Exists)
        {
            directoryInfo.Create();
        }

        string stmt = "select name, definition from dbo.sysdiagrams";
        if (!allDiagrams)
        {
            stmt += " where name = @name";
        }

        SqlCommand cmd = new(stmt, db);
        if (!allDiagrams)
        {
            cmd.Parameters.AddWithValue("@name", diagramName);
        }

        await using SqlDataReader reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            string name = reader.GetString(0);
            byte[] definition = reader.GetFieldValue<byte[]>(1);
            string fileName = Path.Combine(folder, name + ".diagram");
            await File.WriteAllBytesAsync(fileName, definition);

            log.LogInformation("Exported diagram {Name} to {FileName}", name, fileName);
        }
    }

    private async Task ImportDiagramsAsync(SqlConnection db, string folder, string? diagramName)
    {
        bool allDiagrams = string.IsNullOrEmpty(diagramName);
        if (!Directory.Exists(folder))
        {
            log.LogError("Folder {Folder} does not exist", folder);
            return;
        }

        string deleteStmt = "delete from dbo.sysdiagrams where name = @name";
        SqlCommand deleteCommand = new(deleteStmt, db);
        SqlParameter deleteNameParameter = deleteCommand.Parameters.Add("@name", System.Data.SqlDbType.NVarChar);

        string insertStmt = "insert into dbo.sysdiagrams (name, principal_id, version, definition) values (@name, 1, 1, @definition)";
        SqlCommand insertCommand = new(insertStmt, db);
        SqlParameter nameParameter = insertCommand.Parameters.Add("@name", System.Data.SqlDbType.NVarChar);
        SqlParameter dataParameter = insertCommand.Parameters.Add("@definition", System.Data.SqlDbType.VarBinary);

        string[] diagramFiles = allDiagrams ? Directory.GetFiles(folder, "*.diagram") : [Path.Combine(folder, diagramName + ".diagram")];
        foreach (string fileName in diagramFiles)
        {
            FileInfo fi = new(fileName);
            string name = fi.Name.Replace(".diagram", string.Empty);
            byte[] data = await File.ReadAllBytesAsync(fileName);
            log.LogInformation("Installing diagram {Name} with {Bytes} bytes", name, data.Length);

            deleteNameParameter.Value = name;
            await deleteCommand.ExecuteNonQueryAsync();

            nameParameter.Value = name;
            dataParameter.Value = data;
            await insertCommand.ExecuteNonQueryAsync();
        }
    }

    private async Task EnsureDiagramArtefactsArePresentAsync(ILogger<DiagramManager> log, SqlConnection db)
    {
        log.LogInformation("Ensure diagram artefacts are present");

        IEnumerable<Task<(string Name, string FileName, bool Exists)>> checkTasks =
            DiagramArtefacts.Select(a => GetDiagramArtefactExistanceAsync(db, a.Artefact, a.Type, a.FileName));
        await Task.WhenAll(checkTasks);
        (string Name, string FileName, bool Exists)[] diagramArtefactCheckResult = checkTasks.Select(t => t.Result).ToArray();

        foreach ((string name, string fileName, bool exists) in diagramArtefactCheckResult)
        {
            if (exists)
            {
                log.LogInformation("{Name} is already there", name);
            }
            else
            {
                log.LogInformation("Installing {Name} from {FileName}", name, fileName);

                string stmt = await File.ReadAllTextAsync(fileName);
                SqlCommand cmd = new(stmt, db);
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }

    public async Task ImportAsync(string folder, string? diagramName)
    {
        log.LogInformation("Connecting to database {ConnectionString}", settings.ConnectionString);
        SqlConnection db = new(settings.ConnectionString);
        await db.OpenAsync();

        await EnsureDiagramArtefactsArePresentAsync(log, db);

        if (string.IsNullOrEmpty(diagramName))
        {
            log.LogInformation("Importing *.diagram from folder {Folder}", folder);
        }
        else
        {
            log.LogInformation("Importing {DiagramName}.diagram from folder {Folder}", diagramName, folder);
        }

        await ImportDiagramsAsync(db, folder, diagramName);
        await db.CloseAsync();
    }

    public async Task ExportAsync(string folder, string? diagramName)
    {
        log.LogInformation("Connecting to database {ConnectionString}", settings.ConnectionString);
        SqlConnection db = new SqlConnection(settings.ConnectionString);
        await db.OpenAsync();

        (string N, string F, bool Exists) sysdiagramArtefact = await GetDiagramArtefactExistanceAsync(db, "[dbo].[sysdiagrams]", "U", string.Empty);
        if (!sysdiagramArtefact.Exists)
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

        await ExportDiagramsAsync(db, folder, diagramName);
        await db.CloseAsync();
    }
}
