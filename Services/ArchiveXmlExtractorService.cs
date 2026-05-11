using SharpCompress.Common;
using SharpCompress.Archives;
using SharpCompress.Readers;

namespace ConversorXmlNFeDanfePdf.Services;

public sealed record ExtractedXmlFiles(string SourcePath, string TempFolder, IReadOnlyList<string> XmlFiles);

public sealed class ArchiveXmlExtractorService
{
    private static readonly HashSet<string> ArchiveExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".zip", ".rar", ".7z", ".tar", ".gz", ".tgz", ".bz2", ".tbz2", ".xz", ".txz"
    };

    public bool IsSupportedArchive(string path)
        => ArchiveExtensions.Contains(Path.GetExtension(path));

    public ExtractedXmlFiles ExtractXmlFiles(string archivePath)
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), "ConversorXmlNFeDanfePdf", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempFolder);
        var extracted = new List<string>();

        using var archive = ArchiveFactory.OpenArchive(archivePath, new ReaderOptions());
        foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
        {
            var key = entry.Key ?? "";
            if (!key.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                continue;

            var fileName = Path.GetFileName(key);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = $"xml_{extracted.Count + 1:000}.xml";

            var targetPath = GetUniquePath(Path.Combine(tempFolder, fileName));
            entry.WriteToFile(targetPath, new ExtractionOptions { Overwrite = true });
            extracted.Add(targetPath);
        }

        return new ExtractedXmlFiles(archivePath, tempFolder, extracted);
    }

    private static string GetUniquePath(string path)
    {
        if (!File.Exists(path))
            return path;

        var folder = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
        for (var i = 1; i < 10000; i++)
        {
            var candidate = Path.Combine(folder, $"{name}_{i:000}{extension}");
            if (!File.Exists(candidate))
                return candidate;
        }

        throw new IOException("Nao foi possivel criar um nome unico para o XML extraido.");
    }
}
