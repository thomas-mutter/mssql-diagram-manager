using System;

namespace Mutter.Tools.SqlServer.DiagramManager;

public sealed class Settings
{
    public required string ConnectionString { get; set; }

    public string? DiagramName { get; set; }

    public required string Folder { get; set; }

    public required string Mode { get; set; }

    public bool AllDiagrams => string.IsNullOrWhiteSpace(DiagramName);

    public bool Import => string.Equals(Mode, "import", StringComparison.OrdinalIgnoreCase);

    public bool Export => string.Equals(Mode, "export", StringComparison.OrdinalIgnoreCase);
}
