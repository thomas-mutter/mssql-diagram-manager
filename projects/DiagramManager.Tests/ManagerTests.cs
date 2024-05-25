using Moq;
using Xunit.Abstractions;

namespace Mutter.Tools.SqlServer.DiagramManager.Tests;

public class ManagerTests
{
    public ManagerTests(ITestOutputHelper testOutput) => XunitLogger<Manager>.Register(testOutput);

    private static async IAsyncEnumerable<DbDiagram> GetMockDiagramsAsync(int count)
    {
        int i = 0;
        while (i < count)
        {
            await Task.Delay(1);
            yield return new DbDiagram($"Diagram-{i}", [1, 2, 3]);
            i++;
        }
    }

    [Fact]
    public async Task Import_WhenFolderIsMissing_LogError()
    {
        // Setup
        Mock<IDiagramFileManager> diagramFileManagerMock = new();
        diagramFileManagerMock.Setup(x => x.FolderExists(It.IsAny<string>())).Returns(false);

        Mock<ISqlServerManager> dbManagerMock = new();
        XunitLogger<Manager> log = new();

        Manager manager = new(diagramFileManagerMock.Object, dbManagerMock.Object, log);

        // Act
        await manager.ImportAsync("testfolder", null);

        // Assert
        dbManagerMock.Verify(x => x.GetDbObjectExistanceAsync(It.IsAny<DbArtefact>()), Times.Never());
        diagramFileManagerMock.Verify(x => x.GetDiagramsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
    }

    [Fact]
    public async Task Import_WhenTableIsMissing_WillInstall()
    {
        // Setup
        Mock<IDiagramFileManager> diagramFileManagerMock = new();
        diagramFileManagerMock.Setup(x => x.FolderExists(It.IsAny<string>())).Returns(true);
        diagramFileManagerMock.Setup(x => x.GetDiagramsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(GetMockDiagramsAsync(0));
        diagramFileManagerMock.Setup(x => x.ReadAllTextAsync("sql/sysdiagrams.sql")).ReturnsAsync("create table statement");

        Mock<ISqlServerManager> dbManagerMock = new();
        dbManagerMock.Setup(db => db.GetDbObjectExistanceAsync(It.IsAny<DbArtefact>()))
            .ReturnsAsync(new DbObjectState("sysdiagrams", "sql/sysdiagrams.sql", false));

        XunitLogger<Manager> log = new();

        Manager manager = new(diagramFileManagerMock.Object, dbManagerMock.Object, log);

        // Act
        await manager.ImportAsync("testfolder", null);

        // Assert
        dbManagerMock.Verify(x => x.InstallArtefactAsync("create table statement"), Times.Once());
    }

    [Fact]
    public async Task Import_WhenTableIsPresent_WillNotInstall()
    {
        // Setup
        Mock<IDiagramFileManager> diagramFileManagerMock = new();
        diagramFileManagerMock.Setup(x => x.FolderExists(It.IsAny<string>())).Returns(true);
        diagramFileManagerMock.Setup(x => x.GetDiagramsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(GetMockDiagramsAsync(0));

        Mock<ISqlServerManager> dbManagerMock = new();
        dbManagerMock.Setup(db => db.GetDbObjectExistanceAsync(It.IsAny<DbArtefact>()))
            .ReturnsAsync(new DbObjectState("sysdiagrams", "sql/sysdiagrams.sql", true));

        XunitLogger<Manager> log = new();

        Manager manager = new(diagramFileManagerMock.Object, dbManagerMock.Object, log);

        // Act
        await manager.ImportAsync("testfolder", null);

        // Assert
        dbManagerMock.Verify(x => x.InstallArtefactAsync(It.IsAny<string>()), Times.Never());
    }

    [Fact]
    public async Task Import_WhenDiagramsArePresent_WillInstall()
    {
        // Setup
        Mock<IDiagramFileManager> diagramFileManagerMock = new();
        diagramFileManagerMock.Setup(x => x.FolderExists(It.IsAny<string>())).Returns(true);
        diagramFileManagerMock.Setup(x => x.GetDiagramsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(GetMockDiagramsAsync(3));

        Mock<ISqlServerManager> dbManagerMock = new();
        dbManagerMock.Setup(db => db.GetDbObjectExistanceAsync(It.IsAny<DbArtefact>()))
            .ReturnsAsync(new DbObjectState("sysdiagrams", "sql/sysdiagrams.sql", true));

        XunitLogger<Manager> log = new();

        Manager manager = new(diagramFileManagerMock.Object, dbManagerMock.Object, log);

        // Act
        await manager.ImportAsync("testfolder", null);

        // Assert
        dbManagerMock.Verify(x => x.DeleteDiagramAsync(It.IsAny<string>()), Times.Exactly(3));
        dbManagerMock.Verify(x => x.InsertDiagramAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Exactly(3));
    }

    [Fact]
    public async Task Export_WhenDiagramTableIsMissing_DoNothing()
    {
        // Setup
        Mock<IDiagramFileManager> diagramFileManagerMock = new();
        diagramFileManagerMock.Setup(x => x.FolderExists(It.IsAny<string>())).Returns(true);

        Mock<ISqlServerManager> dbManagerMock = new();
        dbManagerMock.Setup(db => db.GetDbObjectExistanceAsync(It.IsAny<DbArtefact>()))
            .ReturnsAsync(new DbObjectState("sysdiagrams", "sql/sysdiagrams.sql", false));

        XunitLogger<Manager> log = new();

        Manager manager = new(diagramFileManagerMock.Object, dbManagerMock.Object, log);

        // Act
        await manager.ExportAsync("testfolder", null);

        // Assert
        dbManagerMock.Verify(x => x.GetDbObjectExistanceAsync(It.IsAny<DbArtefact>()), Times.Once());
        dbManagerMock.Verify(x => x.GetDiagramsAsync(It.IsAny<string>()), Times.Never());
    }

    [Fact]
    public async Task Export_ExportDiagramsToFolder()
    {
        // Setup
        Mock<IDiagramFileManager> diagramFileManagerMock = new();
        diagramFileManagerMock.Setup(x => x.FolderExists(It.IsAny<string>())).Returns(true);

        Mock<ISqlServerManager> dbManagerMock = new();
        dbManagerMock.Setup(db => db.GetDbObjectExistanceAsync(It.IsAny<DbArtefact>()))
            .ReturnsAsync(new DbObjectState("sysdiagrams", "sql/sysdiagrams.sql", true));
        dbManagerMock.Setup(db => db.GetDiagramsAsync(It.IsAny<string>()))
            .Returns(GetMockDiagramsAsync(3));

        XunitLogger<Manager> log = new();

        Manager manager = new(diagramFileManagerMock.Object, dbManagerMock.Object, log);

        // Act
        await manager.ExportAsync("testfolder", null);

        // Assert
        diagramFileManagerMock.Verify(x => x.EnsureFolderExists("testfolder"), Times.Once());
        diagramFileManagerMock.Verify(x => x.SaveDiagramAsync(It.IsAny<string>(), It.IsAny<DbDiagram>()), Times.Exactly(3));
    }
}
