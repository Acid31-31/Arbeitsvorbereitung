using System.IO;
using System.Text.RegularExpressions;
using GeoArbeitsvorbereitung.Models;

namespace GeoArbeitsvorbereitung.Services;

public class GeoFileService : IGeoFileService
{
    private static readonly Regex QuantityPattern = new(@"^(\d+)x$", RegexOptions.IgnoreCase);

    public GeoFileInfo? FindNewest(string searchRoot, string drawingNumber)
    {
        if (!Directory.Exists(searchRoot))
            throw new DirectoryNotFoundException($"Suchordner nicht gefunden: {searchRoot}");

        GeoFileInfo? best = null;

        foreach (var filePath in Directory.EnumerateFiles(searchRoot, "*.geo", SearchOption.AllDirectories))
        {
            GeoFileInfo? info;
            try
            {
                info = ParseFile(filePath);
            }
            catch (UnauthorizedAccessException) { continue; }
            catch (IOException) { continue; }

            if (info == null) continue;
            if (!string.Equals(info.DrawingNumber, drawingNumber, StringComparison.OrdinalIgnoreCase)) continue;

            if (best == null || info.LastWriteTime > best.LastWriteTime)
                best = info;
        }

        return best;
    }

    public string BuildNewFileName(GeoFileInfo source, int newQuantity)
    {
        var nameWithoutExt = Path.GetFileNameWithoutExtension(source.FileName);
        var segments = nameWithoutExt.Split('_');

        // Replace the last segment that matches the Nx quantity pattern
        for (int i = segments.Length - 1; i >= 0; i--)
        {
            if (QuantityPattern.IsMatch(segments[i]))
            {
                segments[i] = $"{newQuantity}x";
                break;
            }
        }

        return string.Join("_", segments) + ".geo";
    }

    public string BuildTargetPath(string outputRoot, GeoFileInfo source, string newFileName)
    {
        var material = string.IsNullOrWhiteSpace(source.Material) ? "_Sonstige" : source.Material;
        return Path.Combine(outputRoot, material, newFileName);
    }

    public void CopyFile(string sourcePath, string targetPath)
    {
        var dir = Path.GetDirectoryName(targetPath)
                  ?? throw new InvalidOperationException($"Ungültiger Zielpfad: {targetPath}");
        Directory.CreateDirectory(dir);
        File.Copy(sourcePath, targetPath, overwrite: false);
    }

    private static GeoFileInfo? ParseFile(string fullPath)
    {
        var fileName = Path.GetFileName(fullPath);
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var segments = nameWithoutExt.Split('_');

        // Need at least segment 1 (customer) and segment 2 (drawing number)
        if (segments.Length < 2) return null;

        var fi = new FileInfo(fullPath);
        return new GeoFileInfo
        {
            FileName = fileName,
            FullPath = fullPath,
            LastWriteTime = fi.LastWriteTime,
            Segments = segments,
        };
    }
}
