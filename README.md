# SqlServer Diagram Manager
This tool exports and imports diagrams from SqlServer databases.

## Build & Publish
```powershell
cd projects/DiagramManager
dotnet pack

nuget push nugets/Mutter.Tools.SqlServer.DiagramManager.<version>.nupkg --source https://dev.azure.com/swisslife/U_ITWorkbench/_artifacts/feed/SwissLife-U
```

This command builds a Nuget package `nugets/DiagramManager.nupkg`

## Install
```powershell
dotnet tool install Mutter.Tools.SqlServer.DiagramManager
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
-db, --ConnectionString (required): Connection string to the database
-f, --Folder (required): Folder where the diagram will be exported or imported
-m, --Mode (required): Mode of the operation (export or import)
-n, --DiagramName (optional): Name of the diagram to export or import

```sh
dotnet tool run manage-sql-diagrams -- -db <connectionString> -f <directory> -m export|import [-n <DiagramName>]
```

## Export
Either all or a specific diagram can be exported. The tool will create a file with the diagram in the directory specified with -f parameter.

```sh
dotnet tool run manage-sql-diagrams -- [-n <DiagramName>] -f c:\Temp\Diagram -m export -db "Server=(localdb)\mssqllocaldb;Database=MyDb;Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=True"
```

## Import
Either all or a specific diagram can be imported. The tool will create a file with the diagram in the directory specified with -f parameter.

```sh
dotnet tool run manage-sql-diagrams -- [-n <DiagramName>] -f c:\Temp\Diagram -m import -db "Server=(localdb)\mssqllocaldb;Database=MyDb;Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=True"
```

