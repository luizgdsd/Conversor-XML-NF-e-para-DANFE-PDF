namespace ConversorXmlNFeDanfePdf.Models;

public sealed class UpdateInfo
{
    public Version CurrentVersion { get; set; } = new(1, 0, 0);
    public Version LatestVersion { get; set; } = new(1, 0, 0);
    public string TagName { get; set; } = "";
    public string ReleaseName { get; set; } = "";
    public string ReleaseNotes { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string AssetName { get; set; } = "";
    public bool IsUpdateAvailable => LatestVersion > CurrentVersion && !string.IsNullOrWhiteSpace(DownloadUrl);
}
