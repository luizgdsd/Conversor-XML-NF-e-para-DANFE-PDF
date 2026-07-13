using ConversorXmlNFeDanfePdf.Models;
using ConversorXmlNFeDanfePdf.Utils;

namespace ConversorXmlNFeDanfePdf.Services;

public sealed class NFeBatchProcessor
{
    private readonly FileScannerService _scanner = new();
    private readonly XmlNFeParser _parser = new();
    private readonly XmlNFSeParser _nfseParser = new();
    private readonly DanfePdfGenerator _pdfGenerator = new();
    private readonly NFSePdfGenerator _nfsePdfGenerator = new();
    private readonly XmlDocumentClassifier _classifier = new();

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
            var classification = _classifier.Classify(xmlPath);
            if (classification.Kind == FiscalXmlKind.NFSe)
                ProcessNFSe(xmlPath, options, result);
            else
                ProcessNFe(xmlPath, options, result);
        }
        catch (Exception ex)
        {
            result.Status = "Erro";
            result.Message = ex.Message;
        }

        return result;
    }

    private void ProcessNFe(string xmlPath, ProcessingOptions options, ProcessingResult result)
    {
        var nfe = _parser.Parse(xmlPath);
        result.Key = nfe.ChaveAcesso;
        result.Number = nfe.Numero;
        result.Issuer = nfe.Emitente.RazaoSocial;
        result.Recipient = nfe.Destinatario.RazaoSocial;

        var pdfPath = ResolveOutputPath(options.OutputFolder, nfe, options.ExistingPdfAction, out var existedBefore);
        if (pdfPath is null)
        {
            MarkIgnoredExisting(result);
            return;
        }

        _pdfGenerator.Generate(nfe, pdfPath);
        MarkGenerated(result, pdfPath, existedBefore, options.ExistingPdfAction, "NF-e convertida com sucesso.");
    }

    private void ProcessNFSe(string xmlPath, ProcessingOptions options, ProcessingResult result)
    {
        var nfse = _nfseParser.Parse(xmlPath);
        result.Key = string.IsNullOrWhiteSpace(nfse.CodigoVerificacao) ? "NFS-e" : nfse.CodigoVerificacao;
        result.Number = nfse.Numero;
        result.Issuer = nfse.Prestador.RazaoSocial;
        result.Recipient = nfse.Tomador.RazaoSocial;

        var pdfPath = ResolveOutputPath(options.OutputFolder, nfse, options.ExistingPdfAction, out var existedBefore);
        if (pdfPath is null)
        {
            MarkIgnoredExisting(result);
            return;
        }

        _nfsePdfGenerator.Generate(nfse, pdfPath);
        MarkGenerated(result, pdfPath, existedBefore, options.ExistingPdfAction, "NFS-e convertida com sucesso.");
    }

    private static void MarkIgnoredExisting(ProcessingResult result)
    {
        result.Status = "Ignorado";
        result.Message = "PDF ja existente e a configuracao atual manda ignorar.";
    }

    private static void MarkGenerated(ProcessingResult result, string pdfPath, bool existedBefore, ExistingPdfAction action, string message)
    {
        result.PdfPath = pdfPath;
        result.Status = existedBefore && action == ExistingPdfAction.Overwrite ? "Sobrescrito" : "Gerado";
        result.Message = message;
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

    private static string? ResolveOutputPath(string outputFolder, NFSeData nfse, ExistingPdfAction action, out bool existedBefore)
    {
        var document = !string.IsNullOrWhiteSpace(nfse.CodigoVerificacao)
            ? nfse.CodigoVerificacao
            : nfse.Prestador.Documento;
        var baseName = $"NFSE_{nfse.Numero}_{document}";
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
