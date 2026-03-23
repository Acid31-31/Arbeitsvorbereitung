using GeoArbeitsvorbereitung.Models;

namespace GeoArbeitsvorbereitung.Services;

public interface IGeoFileService
{
    /// <summary>
    /// Searches <paramref name="searchRoot"/> recursively for *.geo files
    /// where segment 2 matches <paramref name="drawingNumber"/> (case-insensitive).
    /// Returns the file with the newest LastWriteTime, or null if none found.
    /// </summary>
    GeoFileInfo? FindNewest(string searchRoot, string drawingNumber);

    /// <summary>
    /// Returns the new filename with the last quantity segment (e.g. "3x") replaced by "{newQuantity}x".
    /// </summary>
    string BuildNewFileName(GeoFileInfo source, int newQuantity);

    /// <summary>
    /// Returns the full target path: {outputRoot}\{material}\{newFileName}
    /// </summary>
    string BuildTargetPath(string outputRoot, GeoFileInfo source, string newFileName);

    /// <summary>
    /// Creates the target directory if needed and copies the source file to targetPath.
    /// Never overwrites – caller must ensure targetPath does not exist.
    /// </summary>
    void CopyFile(string sourcePath, string targetPath);
}
