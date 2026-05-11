using ConversorXmlNFeDanfePdf.UI;
using QuestPDF.Infrastructure;

namespace ConversorXmlNFeDanfePdf;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        Services.AutoUpdateService.CleanupUpdateArtifacts();
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
