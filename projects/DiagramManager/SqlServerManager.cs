using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mutter.Tools.SqlServer.DiagramManager;

internal class SqlServerManager : ISqlServerManager, IDisposable
{
    private SqlConnection Db { get; }

    private SqlParameter DeleteDiagramNameParameter { get; }

    private SqlCommand DeleteDiagramCommand { get; }

    private SqlParameter InsertNameParameter { get; }

    private SqlParameter InsertDataParameter { get; }

    private SqlCommand InsertDiagramCommand { get; }

    public SqlServerManager(Settings settings, ILogger<SqlServerManager> log)
    {
        Db = new SqlConnection(settings.ConnectionString);
        log.LogInformation("Connecting to database {ConnectionString}", settings.ConnectionString);

        const string deleteStmt = "delete from dbo.sysdiagrams where name = @name";
        DeleteDiagramCommand = new(deleteStmt, Db);
        DeleteDiagramNameParameter = DeleteDiagramCommand.Parameters.Add("@name", System.Data.SqlDbType.NVarChar);

        string insertStmt = "insert into dbo.sysdiagrams (name, principal_id, version, definition) values (@name, 1, 1, @definition)";
        InsertDiagramCommand = new(insertStmt, Db);
        InsertNameParameter = InsertDiagramCommand.Parameters.Add("@name", System.Data.SqlDbType.NVarChar);
        InsertDataParameter = InsertDiagramCommand.Parameters.Add("@definition", System.Data.SqlDbType.VarBinary);
    }

    public async Task<DbObjectState> GetDbObjectExistanceAsync(DbArtefact artefact)
    {
        await EnsureConnectionOpenAsync();

        const string stmt = "select count(1) from sys.objects where object_id = OBJECT_ID(@name) and type = @type";
        SqlCommand cmd = new(stmt, Db);
        cmd.Parameters.AddWithValue("@name", artefact.Name);
        cmd.Parameters.AddWithValue("@type", artefact.Type);

        object? result = await cmd.ExecuteScalarAsync();
        return new DbObjectState(artefact.Name, artefact.FileName, result != DBNull.Value && Convert.ToInt32(result) > 0);
    }

    public async Task DeleteDiagramAsync(string name)
    {
        await EnsureConnectionOpenAsync();
        DeleteDiagramNameParameter.Value = name;
        await DeleteDiagramCommand.ExecuteNonQueryAsync();
    }

    public async Task InsertDiagramAsync(string name, byte[] data)
    {
        await EnsureConnectionOpenAsync();
        InsertNameParameter.Value = name;
        InsertDataParameter.Value = data;
        await InsertDiagramCommand.ExecuteNonQueryAsync();
    }

    public async IAsyncEnumerable<DbDiagram> GetDiagramsAsync(string? diagramName)
    {
        await EnsureConnectionOpenAsync();
        bool allDiagrams = string.IsNullOrEmpty(diagramName);

        string stmt = "select name, definition from dbo.sysdiagrams";
        if (!allDiagrams)
        {
            stmt += " where name = @name";
        }

        SqlCommand cmd = new(stmt, Db);
        if (!allDiagrams)
        {
            cmd.Parameters.AddWithValue("@name", diagramName);
        }

        await using SqlDataReader reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            string name = reader.GetString(0);
            byte[] definition = reader.GetFieldValue<byte[]>(1);
            yield return new DbDiagram(name, definition);
        }
    }

    public async Task InstallArtefactAsync(string stmt)
    {
        await EnsureConnectionOpenAsync();
        SqlCommand cmd = new(stmt, Db);
        await cmd.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        Db?.Dispose();
    }

    private async Task EnsureConnectionOpenAsync()
    {
        if (Db.State != System.Data.ConnectionState.Open)
        {
            await Db.OpenAsync();
        }
    }
}
