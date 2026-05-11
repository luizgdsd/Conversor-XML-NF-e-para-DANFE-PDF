using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

namespace ConversorXmlNFeDanfePdf.Services;

public sealed class PdfMergeService
{
    public string Merge(IReadOnlyList<string> pdfFiles, string outputFolder)
    {
        Directory.CreateDirectory(outputFolder);
        var outputPath = Path.Combine(outputFolder, $"DANFEs_Unificados_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

        using var outputDocument = new PdfDocument();
        foreach (var pdfFile in pdfFiles.Where(File.Exists))
        {
            using var inputDocument = PdfReader.Open(pdfFile, PdfDocumentOpenMode.Import);
            for (var pageIndex = 0; pageIndex < inputDocument.PageCount; pageIndex++)
                outputDocument.AddPage(inputDocument.Pages[pageIndex]);
        }

        if (outputDocument.PageCount == 0)
            throw new InvalidOperationException("Nenhum PDF valido foi encontrado para unificar.");

        outputDocument.Save(outputPath);
        return outputPath;
    }
}
