using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Mutter.Tools.SqlServer.DiagramManager;

internal class DiagramFileManager : IDiagramFileManager
{
    private const string Suffix = ".diagram";

    public void EnsureFolderExists(string folder)
    {
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
    }

    public bool FolderExists(string folder) => Directory.Exists(folder);

    public async IAsyncEnumerable<DbDiagram> GetDiagramsAsync(string folder, string? diagramName)
    {
        bool allDiagrams = string.IsNullOrEmpty(diagramName);
        string[] diagramFiles = allDiagrams ? Directory.GetFiles(folder, "*" + Suffix) : [Path.Combine(folder, diagramName + Suffix)];
        foreach (string fileName in diagramFiles)
        {
            FileInfo fi = new(fileName);
            string name = fi.Name.Replace(Suffix, string.Empty);
            byte[] data = await File.ReadAllBytesAsync(fileName);
            yield return new DbDiagram(name, data);
        }
    }

    public async Task SaveDiagramAsync(string folder, DbDiagram diagram)
    {
        string fileName = Path.Combine(folder, diagram.Name + Suffix);
        await File.WriteAllBytesAsync(fileName, diagram.Definition);
    }

    public async Task<string> ReadAllTextAsync(string fileName) => await File.ReadAllTextAsync(fileName);
}
