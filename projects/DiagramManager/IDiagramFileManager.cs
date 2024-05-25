using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mutter.Tools.SqlServer.DiagramManager;

/// <summary>
/// Abstraction for file system operations for unit testing support
/// </summary>
internal interface IDiagramFileManager
{
    void EnsureFolderExists(string folder);

    bool FolderExists(string folder);

    IAsyncEnumerable<DbDiagram> GetDiagramsAsync(string folder, string? diagramName);

    Task SaveDiagramAsync(string folder, DbDiagram diagram);

    Task<string> ReadAllTextAsync(string fileName);
}
