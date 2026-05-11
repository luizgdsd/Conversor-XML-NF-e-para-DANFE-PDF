using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using ConversorXmlNFeDanfePdf.Models;

namespace ConversorXmlNFeDanfePdf.Services;

public sealed class AutoUpdateService
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/luizgdsd/Conversor-XML-NF-e-para-DANFE-PDF/releases/latest";
    private readonly HttpClient _httpClient = new();

    public AutoUpdateService()
    {
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ConversorXmlNFeDanfePdf/1.0");
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    }

    public async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(LatestReleaseUrl, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;
        var tag = root.GetPropertyOrDefault("tag_name");
        var latest = ParseVersion(tag);
        var current = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);
        var asset = FindInstallerAsset(root);

        return new UpdateInfo
        {
            CurrentVersion = new Version(current.Major, current.Minor, current.Build < 0 ? 0 : current.Build),
            LatestVersion = latest,
            TagName = tag,
            ReleaseName = root.GetPropertyOrDefault("name"),
            ReleaseNotes = root.GetPropertyOrDefault("body"),
            DownloadUrl = asset.DownloadUrl,
            AssetName = asset.Name
        };
    }

    public async Task<string> DownloadInstallerAsync(UpdateInfo update, IProgress<int>? progress, CancellationToken cancellationToken = default)
    {
        var downloadFolder = Path.Combine(Path.GetTempPath(), "ConversorXmlNFeDanfePdf", "Updates");
        SafeDeleteDirectory(downloadFolder);
        Directory.CreateDirectory(downloadFolder);
        var fileName = string.IsNullOrWhiteSpace(update.AssetName)
            ? "Instalador_Conversor_XML_NFe_DANFE_PDF.exe"
            : update.AssetName;
        var targetPath = Path.Combine(downloadFolder, fileName);

        using var response = await _httpClient.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength;
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = File.Create(targetPath);
        var buffer = new byte[81920];
        long readTotal = 0;
        int read;

        while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            readTotal += read;
            if (total is > 0)
                progress?.Report((int)Math.Clamp(readTotal * 100 / total.Value, 0, 100));
        }

        progress?.Report(100);
        return targetPath;
    }

    public void StartInstaller(string installerPath)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = installerPath,
            Arguments = "/SILENT /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS",
            UseShellExecute = true
        });
    }

    public static void CleanupUpdateArtifacts()
    {
        var appTemp = Path.Combine(Path.GetTempPath(), "ConversorXmlNFeDanfePdf");
        var updates = Path.Combine(appTemp, "Updates");
        var unifiedWork = Path.Combine(appTemp, "UnifiedWork");

        SafeDeleteDirectory(updates);
        SafeDeleteDirectory(unifiedWork);
        DeleteOldExtractionFolders(appTemp);
    }

    private static void DeleteOldExtractionFolders(string appTemp)
    {
        if (!Directory.Exists(appTemp))
            return;

        foreach (var directory in Directory.EnumerateDirectories(appTemp))
        {
            var name = Path.GetFileName(directory);
            if (name is "Updates" or "UnifiedWork")
                continue;

            try
            {
                var info = new DirectoryInfo(directory);
                if (info.CreationTimeUtc < DateTime.UtcNow.AddHours(-6))
                    info.Delete(recursive: true);
            }
            catch
            {
            }
        }
    }

    private static void SafeDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
        }
    }

    private static (string Name, string DownloadUrl) FindInstallerAsset(JsonElement root)
    {
        if (!root.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
            return ("", "");

        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.GetPropertyOrDefault("name");
            if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!name.Contains("Instalador", StringComparison.OrdinalIgnoreCase))
                continue;

            return (name, asset.GetPropertyOrDefault("browser_download_url"));
        }

        return ("", "");
    }

    private static Version ParseVersion(string tag)
    {
        var clean = tag.Trim().TrimStart('v', 'V');
        return Version.TryParse(clean, out var version)
            ? new Version(version.Major, version.Minor, version.Build < 0 ? 0 : version.Build)
            : new Version(1, 0, 0);
    }
}

internal static class JsonElementExtensions
{
    public static string GetPropertyOrDefault(this JsonElement element, string name)
        => element.TryGetProperty(name, out var property) && property.ValueKind != JsonValueKind.Null
            ? property.GetString() ?? ""
            : "";
}
