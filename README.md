# SqlServer Diagram Manager
This tool exports and imports diagrams from SqlServer databases.

## Build & Publish
Building project DiagramManager.csproj will create a nuget package in the nugets folder, e.g. `Mutter.Tools.SqlServer.DiagramManager.<version>.nupkg`.
This package can be published to a nuget feed.

```powershell
cd projects/DiagramManager
dotnet build

cd ../../
nuget push nugets/Mutter.Tools.SqlServer.DiagramManager.<version>.nupkg
```

## Install from local path
```powershell
dotnet tool install --global --add-source ./nugets Mutter.Tools.SqlServer.DiagramManager
```

## Uninstall
```powershell
dotnet tool uninstall Mutter.Tools.SqlServer.DiagramManager
```

## Update
```powershell
dotnet tool update Mutter.Tools.SqlServer.DiagramManager
```

## Usage
Parameters:
-m, --Mode (required): Mode of the operation (export or import)
-f, --Folder (required): Folder where the diagram will be exported or imported
-n, --DiagramName (optional): Name of the diagram to export or import
-db, --ConnectionString (required): Connection string to the database

```sh
manage-sql-diagrams -m export|import -f <directory> -db <connectionString> [-n <DiagramName>]
```

## Export
Either all or a specific diagram can be exported. The tool will create a file with the diagram in the directory specified with -f parameter.

```sh
manage-sql-diagrams
  -m export
  -f c:\Temp\Diagram
  [-n <DiagramName>]
  -db "Server=(localdb)\mssqllocaldb;Database=MyDb;Integrated Security=true;TrustServerCertificate=true"
```

## Import
Either all or a specific diagram can be imported. The tool will create a file with the diagram in the directory specified with -f parameter.

```sh
manage-sql-diagrams
  -m import
  -f c:\Temp\Diagram
  [-n <DiagramName>]
  -db "Server=(localdb)\mssqllocaldb;Database=MyDb;Integrated Security=true;TrustServerCertificate=true"
```
