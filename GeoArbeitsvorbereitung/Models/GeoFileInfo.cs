namespace GeoArbeitsvorbereitung.Models;

public class GeoFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public DateTime LastWriteTime { get; set; }
    public string[] Segments { get; set; } = [];

    /// <summary>Segment 2 (index 1): Zeichnungsnummer</summary>
    public string DrawingNumber => Segments.Length > 1 ? Segments[1] : string.Empty;

    /// <summary>Segment 4 (index 3): Material – used as output subfolder</summary>
    public string Material => Segments.Length > 3 ? Segments[3] : string.Empty;
}
