using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mutter.Tools.SqlServer.DiagramManager;

/// <summary>
/// Interface to database operations.
/// </summary>
internal interface ISqlServerManager
{
    Task<DbObjectState> GetDbObjectExistanceAsync(DbArtefact artefact);

    Task DeleteDiagramAsync(string name);

    Task InsertDiagramAsync(string name, byte[] data);

    IAsyncEnumerable<DbDiagram> GetDiagramsAsync(string? diagramName);

    Task InstallArtefactAsync(string stmt);
}
