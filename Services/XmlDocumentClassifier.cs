using System.Xml.Linq;

namespace ConversorXmlNFeDanfePdf.Services;

public enum FiscalXmlKind
{
    NFe,
    NFSe,
    CTe,
    MDFe,
    EventOnly,
    Unknown
}

public sealed record FiscalXmlClassification(FiscalXmlKind Kind, string Message)
{
    public bool CanGenerateDanfe => Kind == FiscalXmlKind.NFe;
}

public sealed class XmlDocumentClassifier
{
    public FiscalXmlClassification Classify(string xmlPath)
    {
        try
        {
            var document = XDocument.Load(xmlPath, LoadOptions.None);
            var names = document.Descendants()
                .Concat(document.Root is null ? [] : new[] { document.Root })
                .Select(x => x.Name.LocalName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (names.Contains("infNFe"))
                return new FiscalXmlClassification(FiscalXmlKind.NFe, "NF-e valida para DANFE.");

            if (names.Contains("NFSe") || names.Contains("infNFSe") || names.Contains("CompNfse") || names.Contains("ConsultarNfseServicoPrestadoResposta"))
                return new FiscalXmlClassification(FiscalXmlKind.NFSe, "XML de NFS-e detectado. DANFE nao se aplica a Nota Fiscal de Servico.");

            if (names.Contains("infCte"))
                return new FiscalXmlClassification(FiscalXmlKind.CTe, "XML de CT-e detectado. Este sistema gera DANFE apenas para NF-e.");

            if (names.Contains("infMDFe"))
                return new FiscalXmlClassification(FiscalXmlKind.MDFe, "XML de MDF-e detectado. Este sistema gera DANFE apenas para NF-e.");

            if (names.Contains("infEvento") || names.Contains("procEventoNFe"))
                return new FiscalXmlClassification(FiscalXmlKind.EventOnly, "XML de evento detectado sem a NF-e completa. Carregue o XML autorizado da NF-e/procNFe.");

            return new FiscalXmlClassification(FiscalXmlKind.Unknown, "XML fiscal nao reconhecido como NF-e/procNFe.");
        }
        catch (Exception ex)
        {
            return new FiscalXmlClassification(FiscalXmlKind.Unknown, $"XML invalido ou ilegivel: {ex.Message}");
        }
    }
}
