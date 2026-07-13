using System.Xml.Linq;
using ConversorXmlNFeDanfePdf.Models;
using ConversorXmlNFeDanfePdf.Utils;

namespace ConversorXmlNFeDanfePdf.Services;

public sealed class XmlNFSeParser
{
    public NFSeData Parse(string xmlPath)
    {
        var document = XDocument.Load(xmlPath, LoadOptions.PreserveWhitespace);
        var root = document.Root ?? throw new InvalidOperationException("XML de NFS-e vazio ou invalido.");
        var infNfse = Find(root, "InfNfse", "infNFSe", "NFSe", "Nfse")
            ?? throw new InvalidOperationException("Tags principais de NFS-e nao encontradas.");

        var declaracao = Find(root, "InfDeclaracaoPrestacaoServico", "DeclaracaoPrestacaoServico");
        var servico = Find(declaracao, "Servico") ?? Find(infNfse, "Servico") ?? Find(root, "Servico");
        var valores = Find(servico, "Valores") ?? Find(infNfse, "Valores") ?? Find(root, "Valores");
        var valoresNfse = Find(infNfse, "ValoresNfse") ?? Find(root, "ValoresNfse");
        var prestadorServico = Find(infNfse, "PrestadorServico") ?? Find(root, "PrestadorServico");
        var prestadorDeclaracao = Find(declaracao, "Prestador") ?? Find(root, "Prestador");
        var tomadorServico = Find(infNfse, "TomadorServico") ?? Find(root, "TomadorServico");
        var tomadorDeclaracao = Find(declaracao, "Tomador") ?? Find(root, "Tomador");
        var cancelada = DetectCancellation(root);

        return new NFSeData
        {
            XmlPath = xmlPath,
            Numero = Text(infNfse, "Numero") ?? Text(root, "NumeroNfse") ?? Text(root, "Numero") ?? "",
            CodigoVerificacao = Text(infNfse, "CodigoVerificacao") ?? Text(root, "CodigoVerificacao") ?? "",
            DataEmissao = Date(infNfse, "DataEmissao") ?? Date(root, "DataEmissao"),
            Competencia = Date(declaracao, "Competencia") ?? Date(servico, "Competencia") ?? Date(root, "Competencia"),
            NaturezaOperacao = Text(infNfse, "NaturezaOperacao") ?? Text(declaracao, "NaturezaOperacao") ?? "",
            RegimeEspecialTributacao = Text(infNfse, "RegimeEspecialTributacao") ?? Text(declaracao, "RegimeEspecialTributacao") ?? "",
            OptanteSimplesNacional = SimNao(Text(infNfse, "OptanteSimplesNacional") ?? Text(declaracao, "OptanteSimplesNacional")),
            IncentivadorCultural = SimNao(Text(infNfse, "IncentivadorCultural") ?? Text(declaracao, "IncentivadorCultural")),
            Status = cancelada.IsCanceled ? "CANCELADA" : "AUTORIZADA",
            Cancelada = cancelada.IsCanceled,
            MotivoCancelamento = cancelada.Message,
            Prestador = ParseParty(prestadorServico, prestadorDeclaracao),
            Tomador = ParseParty(tomadorServico ?? tomadorDeclaracao, tomadorDeclaracao ?? tomadorServico),
            Servico = ParseService(servico),
            Valores = ParseValues(valores, valoresNfse, servico)
        };
    }

    private static NFSeParty ParseParty(XElement? party, XElement? fallback = null)
    {
        var endereco = Find(party, "Endereco") ?? Find(fallback, "Endereco");
        var contato = Find(party, "Contato") ?? Find(fallback, "Contato");
        string T(string localName) => Text(party, localName) ?? Text(fallback, localName) ?? "";
        string First(params string[] values) => values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "";
        return new NFSeParty
        {
            RazaoSocial = First(T("RazaoSocial"), T("Nome"), T("xNome")),
            NomeFantasia = T("NomeFantasia"),
            Documento = Formatadores.CnpjCpf(First(T("Cnpj"), T("CNPJ"), T("Cpf"), T("CPF"))),
            InscricaoMunicipal = T("InscricaoMunicipal"),
            InscricaoEstadual = T("InscricaoEstadual"),
            Email = Text(contato, "Email") ?? T("Email"),
            Endereco = new AddressData
            {
                Logradouro = Text(endereco, "Endereco") ?? Text(endereco, "Logradouro") ?? Text(endereco, "xLgr") ?? "",
                Numero = Text(endereco, "Numero") ?? Text(endereco, "NumeroEndereco") ?? Text(endereco, "nro") ?? "",
                Complemento = Text(endereco, "Complemento") ?? Text(endereco, "xCpl") ?? "",
                Bairro = Text(endereco, "Bairro") ?? Text(endereco, "xBairro") ?? "",
                Cep = Formatadores.Cep(Text(endereco, "Cep") ?? Text(endereco, "CEP") ?? ""),
                Municipio = Text(endereco, "Municipio") ?? Text(endereco, "CodigoMunicipio") ?? Text(endereco, "xMun") ?? "",
                Uf = Text(endereco, "Uf") ?? Text(endereco, "UF") ?? "",
                Telefone = Text(contato, "Telefone") ?? Text(party, "Telefone") ?? ""
            }
        };
    }

    private static NFSeService ParseService(XElement? servico)
        => new()
        {
            Discriminacao = Text(servico, "Discriminacao") ?? "",
            ItemListaServico = Text(servico, "ItemListaServico") ?? "",
            CodigoTributacaoMunicipio = Text(servico, "CodigoTributacaoMunicipio") ?? "",
            CodigoCnae = Text(servico, "CodigoCnae") ?? "",
            MunicipioPrestacao = Text(servico, "MunicipioIncidencia") ?? Text(servico, "CodigoMunicipio") ?? "",
            UfPrestacao = Text(servico, "UfPrestacao") ?? Text(servico, "UF") ?? ""
        };

    private static NFSeValues ParseValues(XElement? valores, XElement? valoresNfse, XElement? servico)
        => new()
        {
            ValorServicos = Decimal(valores, valoresNfse, "ValorServicos"),
            ValorDeducoes = Decimal(valores, valoresNfse, "ValorDeducoes"),
            ValorPis = Decimal(valores, valoresNfse, "ValorPis"),
            ValorCofins = Decimal(valores, valoresNfse, "ValorCofins"),
            ValorInss = Decimal(valores, valoresNfse, "ValorInss"),
            ValorIr = Decimal(valores, valoresNfse, "ValorIr"),
            ValorCsll = Decimal(valores, valoresNfse, "ValorCsll"),
            OutrasRetencoes = Decimal(valores, valoresNfse, "OutrasRetencoes"),
            ValorIss = Decimal(valores, valoresNfse, "ValorIss"),
            Aliquota = Decimal(valores, valoresNfse, "Aliquota"),
            DescontoIncondicionado = Decimal(valores, valoresNfse, "DescontoIncondicionado"),
            DescontoCondicionado = Decimal(valores, valoresNfse, "DescontoCondicionado"),
            BaseCalculo = Decimal(valores, valoresNfse, "BaseCalculo"),
            ValorLiquidoNfse = Decimal(valores, valoresNfse, "ValorLiquidoNfse"),
            IssRetido = (Text(servico, "IssRetido") ?? Text(valores, "IssRetido") ?? "") is "1" or "true" or "True" or "S" or "s"
        };

    private static (bool IsCanceled, string Message) DetectCancellation(XElement root)
    {
        var localNames = root.Descendants().Select(x => x.Name.LocalName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var texts = root.Descendants().Select(x => x.Value.Trim()).Where(x => x.Length > 0).ToList();
        var hasCancelTag = localNames.Any(x => x.Contains("Cancel", StringComparison.OrdinalIgnoreCase));
        var hasCancelText = texts.Any(x => x.Contains("cancel", StringComparison.OrdinalIgnoreCase));
        if (!hasCancelTag && !hasCancelText)
            return (false, "");

        var reason = texts.FirstOrDefault(x => x.Contains("cancel", StringComparison.OrdinalIgnoreCase))
            ?? "Evento/status de cancelamento encontrado no XML.";
        return (true, reason);
    }

    private static XElement? Find(XElement? source, params string[] localNames)
        => source?.DescendantsAndSelf().FirstOrDefault(x => localNames.Contains(x.Name.LocalName, StringComparer.OrdinalIgnoreCase));

    private static string? Text(XElement? source, string localName)
        => source?.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase))?.Value.Trim();

    private static DateTime? Date(XElement? source, string localName)
    {
        var text = Text(source, localName);
        return DateTimeOffset.TryParse(text, out var dto)
            ? dto.LocalDateTime
            : DateTime.TryParse(text, out var date) ? date : null;
    }

    private static decimal Decimal(XElement? source, string localName)
    {
        var text = Text(source, localName);
        return decimal.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
    }

    private static decimal Decimal(XElement? primary, XElement? secondary, string localName)
    {
        var value = Decimal(primary, localName);
        return value != 0 ? value : Decimal(secondary, localName);
    }

    private static string SimNao(string? value)
        => value switch
        {
            "1" => "SIM",
            "2" => "NAO",
            "true" or "True" or "S" or "s" => "SIM",
            "false" or "False" or "N" or "n" => "NAO",
            _ => value ?? ""
        };
}
