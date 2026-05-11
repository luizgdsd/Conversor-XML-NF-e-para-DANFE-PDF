using ConversorXmlNFeDanfePdf.Models;
using ConversorXmlNFeDanfePdf.Utils;

namespace ConversorXmlNFeDanfePdf.Services;

public sealed class NFeBatchProcessor
{
    private readonly FileScannerService _scanner = new();
    private readonly XmlNFeParser _parser = new();
    private readonly DanfePdfGenerator _pdfGenerator = new();

    public async Task<IReadOnlyList<ProcessingResult>> ProcessAsync(
        ProcessingOptions options,
        IProgress<ProcessingResult>? resultProgress = null,
        IProgress<int>? percentProgress = null,
        CancellationToken cancellationToken = default)
    {
        var xmlFiles = _scanner.FindXmlFiles(options.InputFolder, options.IncludeSubfolders);
        return await ProcessFilesAsync(xmlFiles, options, resultProgress, percentProgress, cancellationToken);
    }

    public async Task<IReadOnlyList<ProcessingResult>> ProcessFilesAsync(
        IReadOnlyList<string> xmlFiles,
        ProcessingOptions options,
        IProgress<ProcessingResult>? resultProgress = null,
        IProgress<int>? percentProgress = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ProcessingResult>();
        Directory.CreateDirectory(options.OutputFolder);

        for (var index = 0; index < xmlFiles.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var xml = xmlFiles[index];
            var result = await Task.Run(() => ProcessSingle(xml, options), cancellationToken);
            results.Add(result);
            resultProgress?.Report(result);
            percentProgress?.Report(xmlFiles.Count == 0 ? 100 : (int)Math.Round((index + 1) * 100.0 / xmlFiles.Count));
        }

        if (xmlFiles.Count == 0)
            percentProgress?.Report(100);

        return results;
    }

    private ProcessingResult ProcessSingle(string xmlPath, ProcessingOptions options)
    {
        var result = new ProcessingResult
        {
            XmlFile = xmlPath,
            Status = "Pendente"
        };

        try
        {
            WaitUntilFileIsReady(xmlPath);
            var nfe = _parser.Parse(xmlPath);
            result.Key = nfe.ChaveAcesso;
            result.Number = nfe.Numero;
            result.Issuer = nfe.Emitente.RazaoSocial;
            result.Recipient = nfe.Destinatario.RazaoSocial;

            var existedBefore = false;
            var pdfPath = ResolveOutputPath(options.OutputFolder, nfe, options.ExistingPdfAction, out existedBefore);
            if (pdfPath is null)
            {
                result.Status = "Ignorado";
                result.Message = "PDF ja existente e a configuracao atual manda ignorar.";
                return result;
            }

            _pdfGenerator.Generate(nfe, pdfPath);
            result.PdfPath = pdfPath;
            result.Status = existedBefore && options.ExistingPdfAction == ExistingPdfAction.Overwrite ? "Sobrescrito" : "Gerado";
            result.Message = "Convertido com sucesso.";
        }
        catch (Exception ex)
        {
            result.Status = "Erro";
            result.Message = ex.Message;
        }

        return result;
    }

    private static void WaitUntilFileIsReady(string path)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (stream.Length > 0)
                    return;
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            Thread.Sleep(500);
        }
    }

    private static string? ResolveOutputPath(string outputFolder, NFeData nfe, ExistingPdfAction action, out bool existedBefore)
    {
        var baseName = !string.IsNullOrWhiteSpace(nfe.ChaveAcesso)
            ? $"{nfe.ChaveAcesso}_DANFE"
            : $"NF_{nfe.Numero}_{nfe.Serie}_{nfe.Emitente.Cnpj}";
        baseName = Formatadores.ArquivoSeguro(baseName);
        var path = Path.Combine(outputFolder, baseName + ".pdf");
        existedBefore = File.Exists(path);

        if (!existedBefore)
            return path;

        return action switch
        {
            ExistingPdfAction.Ignore => null,
            ExistingPdfAction.Overwrite => path,
            ExistingPdfAction.IncrementSuffix => Increment(path),
            _ => path
        };
    }

    private static string Increment(string path)
    {
        var folder = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
        for (var i = 1; i < 10000; i++)
        {
            var candidate = Path.Combine(folder, $"{name}_{i:000}{extension}");
            if (!File.Exists(candidate))
                return candidate;
        }

        throw new IOException("Nao foi possivel gerar nome incremental para o PDF.");
    }
}
