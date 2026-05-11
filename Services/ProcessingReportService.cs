using System.Text;
using ConversorXmlNFeDanfePdf.Models;

namespace ConversorXmlNFeDanfePdf.Services;

public sealed class ProcessingReportService
{
    public string SaveCsv(string outputFolder, IReadOnlyList<ProcessingResult> results)
    {
        Directory.CreateDirectory(outputFolder);
        var path = Path.Combine(outputFolder, $"relatorio_processamento_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        var sb = new StringBuilder();
        sb.AppendLine("arquivo_xml;chave;numero;emitente;destinatario;status;pdf;mensagem");
        foreach (var item in results)
        {
            sb.AppendLine(string.Join(";",
                Escape(item.XmlFile),
                Escape(item.Key),
                Escape(item.Number),
                Escape(item.Issuer),
                Escape(item.Recipient),
                Escape(item.Status),
                Escape(item.PdfPath),
                Escape(item.Message)));
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        return path;
    }

    public string SaveTxtSummary(string outputFolder, IReadOnlyList<ProcessingResult> results)
    {
        Directory.CreateDirectory(outputFolder);
        var path = Path.Combine(outputFolder, $"relatorio_resumo_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        var generated = results.Count(x => x.Status == "Gerado" || x.Status == "Sobrescrito");
        var ignored = results.Count(x => x.Status == "Ignorado");
        var errors = results.Count(x => x.Status == "Erro");

        var sb = new StringBuilder();
        sb.AppendLine("Conversor XML NF-e para DANFE PDF");
        sb.AppendLine($"Data: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        sb.AppendLine($"Total de XMLs encontrados: {results.Count}");
        sb.AppendLine($"Total de PDFs gerados: {generated}");
        sb.AppendLine($"Total de arquivos ignorados: {ignored}");
        sb.AppendLine($"Total de erros: {errors}");
        sb.AppendLine();
        foreach (var item in results)
            sb.AppendLine($"{item.Status} | {item.XmlFile} | {item.PdfPath} | {item.Message}");

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        return path;
    }

    private static string Escape(string value)
    {
        value ??= "";
        return value.Contains(';') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
