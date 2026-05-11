using System.IO.Pipes;
using System.Text;
using ConversorXmlNFeDanfePdf.UI;
using QuestPDF.Infrastructure;

namespace ConversorXmlNFeDanfePdf;

internal static class Program
{
    private const string MutexName = "Global\\GuguSolucoes.ConversorXmlNFeDanfePdf.SingleInstance";
    private const string PipeName = "GuguSolucoes.ConversorXmlNFeDanfePdf.SingleInstancePipe";

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            SignalExistingInstance();
            return;
        }

        QuestPDF.Settings.License = LicenseType.Community;
        Services.AutoUpdateService.CleanupUpdateArtifacts();
        ApplicationConfiguration.Initialize();

        using var mainForm = new MainForm();
        StartInstanceSignalServer(mainForm);
        Application.Run(mainForm);
    }

    private static void SignalExistingInstance()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(750);
            var data = Encoding.UTF8.GetBytes("show");
            client.Write(data, 0, data.Length);
        }
        catch
        {
        }
    }

    private static void StartInstanceSignalServer(MainForm mainForm)
    {
        _ = Task.Run(async () =>
        {
            while (!mainForm.IsDisposed)
            {
                try
                {
                    await using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    await server.WaitForConnectionAsync();
                    using var reader = new StreamReader(server, Encoding.UTF8);
                    _ = await reader.ReadToEndAsync();
                    if (!mainForm.IsDisposed)
                        mainForm.BeginInvoke(new Action(mainForm.RestoreFromTray));
                }
                catch
                {
                    await Task.Delay(500);
                }
            }
        });
    }
}
