using System.Xml.Linq;
using ConversorXmlNFeDanfePdf.Models;
using ConversorXmlNFeDanfePdf.Utils;

namespace ConversorXmlNFeDanfePdf.Services;

public sealed class XmlNFeParser
{
    public NFeData Parse(string xmlPath)
    {
        var document = XDocument.Load(xmlPath, LoadOptions.PreserveWhitespace);
        var infNFe = document.Descendants().FirstOrDefault(x => x.Name.LocalName == "infNFe")
            ?? throw new InvalidOperationException("Tag infNFe nao encontrada. O XML nao parece ser uma NF-e/procNFe valida.");

        var ide = infNFe.FirstChild("ide");
        var emit = infNFe.FirstChild("emit");
        var dest = infNFe.FirstChild("dest");
        var entrega = infNFe.FirstChild("entrega");
        var total = infNFe.FirstChild("total")?.FirstChild("ICMSTot");
        var transp = infNFe.FirstChild("transp");
        var infAdic = infNFe.FirstChild("infAdic");

        var dataSaidaEntrada = ide.DateTimeText("dhSaiEnt") ?? ide.DateTimeText("dSaiEnt");
        var horaSaidaEntrada = dataSaidaEntrada?.TimeOfDay ?? ParseTime(ide.Text("hSaiEnt"));
        var chave = (infNFe.Attribute("Id")?.Value ?? "").Replace("NFe", "", StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(chave))
            chave = document.Descendants().FirstOrDefault(x => x.Name.LocalName == "chNFe")?.Value.Trim() ?? "";
        var cancellation = DetectCancellation(document);

        return new NFeData
        {
            XmlPath = xmlPath,
            ChaveAcesso = Formatadores.OnlyDigits(chave),
            Numero = ide.Text("nNF"),
            Serie = ide.Text("serie"),
            DataEmissao = ide.DateTimeText("dhEmi") ?? ide.DateTimeText("dEmi"),
            DataSaidaEntrada = dataSaidaEntrada?.Date,
            HoraSaidaEntrada = horaSaidaEntrada,
            TipoOperacao = ide.Text("tpNF") == "0" ? "ENTRADA" : "SAIDA",
            NaturezaOperacao = ide.Text("natOp"),
            ProtocoloAutorizacao = document.Descendants().FirstOrDefault(x => x.Name.LocalName == "nProt")?.Value.Trim() ?? "",
            StatusNota = cancellation.IsCanceled ? "CANCELADA" : "AUTORIZADA",
            Cancelada = cancellation.IsCanceled,
            MotivoCancelamento = cancellation.Message,
            Emitente = ParseEmitente(emit),
            Destinatario = ParseDestinatario(dest),
            LocalEntrega = ParseLocalEntrega(entrega),
            Totais = ParseTotais(total),
            Transporte = ParseTransporte(transp),
            Produtos = ParseProdutos(infNFe).ToList(),
            InformacoesComplementares = infAdic.Text("infCpl"),
            InformacoesFisco = infAdic.Text("infAdFisco")
        };
    }

    private static TimeSpan? ParseTime(string text)
        => TimeSpan.TryParse(text, out var value) ? value : null;

    private static (bool IsCanceled, string Message) DetectCancellation(XDocument document)
    {
        var cancelStats = new HashSet<string> { "101", "151", "155", "135" };
        var statusNodes = document.Descendants().Where(x => x.Name.LocalName == "cStat").Select(x => x.Value.Trim()).ToList();
        var eventTypes = document.Descendants().Where(x => x.Name.LocalName == "tpEvento").Select(x => x.Value.Trim()).ToList();
        var descriptions = document.Descendants().Where(x => x.Name.LocalName is "descEvento" or "xMotivo").Select(x => x.Value.Trim()).ToList();

        var hasCancelEvent = eventTypes.Any(x => x == "110111")
            || descriptions.Any(x => x.Contains("cancel", StringComparison.OrdinalIgnoreCase));
        var hasCancelStatus = statusNodes.Any(cancelStats.Contains);

        if (!hasCancelEvent && !hasCancelStatus)
            return (false, "");

        var protocol = document.Descendants().FirstOrDefault(x => x.Name.LocalName == "nProt")?.Value.Trim();
        var date = document.Descendants().FirstOrDefault(x => x.Name.LocalName is "dhRegEvento" or "dhRecbto")?.Value.Trim();
        var reason = descriptions.FirstOrDefault(x => x.Contains("cancel", StringComparison.OrdinalIgnoreCase))
            ?? descriptions.FirstOrDefault()
            ?? "Evento/status de cancelamento encontrado no XML.";

        var parts = new List<string> { reason };
        if (!string.IsNullOrWhiteSpace(protocol))
            parts.Add($"Protocolo: {protocol}");
        if (!string.IsNullOrWhiteSpace(date))
            parts.Add($"Data: {date}");

        return (true, string.Join(" | ", parts));
    }

    private static Emitente ParseEmitente(XElement? emit)
    {
        var ender = emit.FirstChild("enderEmit");
        return new Emitente
        {
            RazaoSocial = emit.Text("xNome"),
            Cnpj = Formatadores.CnpjCpf(emit.Text("CNPJ")),
            InscricaoEstadual = emit.Text("IE"),
            InscricaoEstadualSubstitutoTributario = emit.Text("IEST"),
            Endereco = ParseAddress(ender)
        };
    }

    private static Destinatario ParseDestinatario(XElement? dest)
    {
        var ender = dest.FirstChild("enderDest");
        return new Destinatario
        {
            RazaoSocial = dest.Text("xNome"),
            Documento = Formatadores.CnpjCpf(dest.Text("CNPJ") + dest.Text("CPF")),
            InscricaoEstadual = dest.Text("IE"),
            Endereco = ParseAddress(ender)
        };
    }

    private static LocalEntrega ParseLocalEntrega(XElement? entrega)
    {
        return new LocalEntrega
        {
            RazaoSocial = entrega.Text("xNome"),
            Documento = Formatadores.CnpjCpf(entrega.Text("CNPJ") + entrega.Text("CPF")),
            InscricaoEstadual = entrega.Text("IE"),
            Endereco = ParseAddress(entrega)
        };
    }

    private static AddressData ParseAddress(XElement? address)
    {
        return new AddressData
        {
            Logradouro = address.Text("xLgr"),
            Numero = address.Text("nro"),
            Complemento = address.Text("xCpl"),
            Bairro = address.Text("xBairro"),
            Cep = Formatadores.Cep(address.Text("CEP")),
            Municipio = address.Text("xMun"),
            Uf = address.Text("UF"),
            Telefone = address.Text("fone")
        };
    }

    private static Totais ParseTotais(XElement? total)
    {
        return new Totais
        {
            BaseCalculoIcms = total.DecimalText("vBC"),
            ValorIcms = total.DecimalText("vICMS"),
            BaseCalculoIcmsSt = total.DecimalText("vBCST"),
            ValorIcmsSt = total.DecimalText("vST"),
            ValorImpostoImportacao = total.DecimalText("vII"),
            ValorIcmsUfRemetente = total.DecimalText("vICMSUFRemet"),
            ValorFcpUfDestino = total.DecimalText("vFCPUFDest"),
            ValorProdutos = total.DecimalText("vProd"),
            Frete = total.DecimalText("vFrete"),
            Seguro = total.DecimalText("vSeg"),
            Desconto = total.DecimalText("vDesc"),
            OutrasDespesas = total.DecimalText("vOutro"),
            Ipi = total.DecimalText("vIPI"),
            ValorIcmsUfDestino = total.DecimalText("vICMSUFDest"),
            Pis = total.DecimalText("vPIS"),
            Cofins = total.DecimalText("vCOFINS"),
            ValorTotalNota = total.DecimalText("vNF"),
            ValorTotalTributos = total.DecimalText("vTotTrib")
        };
    }

    private static Transporte ParseTransporte(XElement? transp)
    {
        var transporta = transp.FirstChild("transporta");
        var veic = transp.FirstChild("veicTransp");
        var vol = transp.FirstChild("vol");
        return new Transporte
        {
            ModalidadeFrete = transp.Text("modFrete"),
            RazaoSocial = transporta.Text("xNome"),
            Documento = Formatadores.CnpjCpf(transporta.Text("CNPJ") + transporta.Text("CPF")),
            Endereco = transporta.Text("xEnder"),
            Municipio = transporta.Text("xMun"),
            Uf = transporta.Text("UF"),
            InscricaoEstadual = transporta.Text("IE"),
            CodigoAntt = veic.Text("RNTC"),
            Placa = veic.Text("placa"),
            UfPlaca = veic.Text("UF"),
            Quantidade = vol.DecimalText("qVol"),
            Especie = vol.Text("esp"),
            Marca = vol.Text("marca"),
            Numeracao = vol.Text("nVol"),
            PesoBruto = vol.DecimalText("pesoB"),
            PesoLiquido = vol.DecimalText("pesoL")
        };
    }

    private static IEnumerable<Produto> ParseProdutos(XElement infNFe)
    {
        foreach (var det in infNFe.Elements().Where(x => x.Name.LocalName == "det"))
        {
            var prod = det.FirstChild("prod");
            var imposto = det.FirstChild("imposto");
            var icms = imposto.FirstChild("ICMS")?.Elements().FirstOrDefault();
            var ipi = imposto.FirstChild("IPI")?.Elements().FirstOrDefault(x => x.Name.LocalName is "IPITrib" or "IPINT");

            yield return new Produto
            {
                Codigo = prod.Text("cProd"),
                Descricao = prod.Text("xProd"),
                NcmSh = prod.Text("NCM"),
                CstCsosn = icms.Text("CST") + icms.Text("CSOSN"),
                Cfop = prod.Text("CFOP"),
                Unidade = prod.Text("uCom"),
                Quantidade = prod.DecimalText("qCom"),
                ValorUnitario = prod.DecimalText("vUnCom"),
                ValorTotal = prod.DecimalText("vProd"),
                BaseCalculoIcms = icms.DecimalText("vBC"),
                ValorIcms = icms.DecimalText("vICMS"),
                ValorIpi = ipi.DecimalText("vIPI"),
                AliquotaIcms = icms.DecimalText("pICMS"),
                AliquotaIpi = ipi.DecimalText("pIPI")
            };
        }
    }
}
