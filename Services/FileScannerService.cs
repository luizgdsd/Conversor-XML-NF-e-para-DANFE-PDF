namespace ConversorXmlNFeDanfePdf.Services;

public sealed class FileScannerService
{
    public IReadOnlyList<string> FindXmlFiles(string folder, bool includeSubfolders)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            return [];

        var option = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.EnumerateFiles(folder, "*.xml", option)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
