using ConversorXmlNFeDanfePdf.Models;
using ConversorXmlNFeDanfePdf.Utils;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ConversorXmlNFeDanfePdf.Services;

public sealed class NFSePdfGenerator
{
    public void Generate(NFSeData nfse, string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(8, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(8).FontColor(Colors.Black));
                page.Content().Column(column =>
                {
                    column.Spacing(3);
                    column.Item().Element(c => ComposeHeader(c, nfse));
                    column.Item().Element(c => ComposeStatus(c, nfse));
                    column.Item().Element(c => ComposeParty(c, "PRESTADOR DE SERVICOS", nfse.Prestador));
                    column.Item().Element(c => ComposeParty(c, "TOMADOR DE SERVICOS", nfse.Tomador));
                    column.Item().Element(c => ComposeService(c, nfse));
                    column.Item().Element(c => ComposeValues(c, nfse));
                    column.Item().Element(c => ComposeAdditional(c, nfse));
                });
                page.Footer().AlignRight().Text(text =>
                {
                    text.Span("Folha ");
                    text.CurrentPageNumber().Bold();
                    text.Span("/");
                    text.TotalPages().Bold();
                });
            });
        }).GeneratePdf(outputPath);
    }

    private static void ComposeHeader(IContainer container, NFSeData nfse)
    {
        container.Border(0.8f).Padding(6).Row(row =>
        {
            row.RelativeItem(3).Column(column =>
            {
                column.Item().Text("NFS-e").FontSize(24).Bold();
                column.Item().Text("Nota Fiscal de Servico Eletronica").FontSize(11).SemiBold();
                column.Item().Text("Documento auxiliar gerado a partir do XML informado.").FontSize(7);
            });
            row.RelativeItem(2).Column(column =>
            {
                column.Item().Element(c => Field(c, "NUMERO", nfse.Numero, true));
                column.Item().Element(c => Field(c, "CODIGO DE VERIFICACAO", nfse.CodigoVerificacao, true));
            });
            row.RelativeItem(2).Column(column =>
            {
                column.Item().Element(c => Field(c, "DATA DE EMISSAO", Formatadores.Data(nfse.DataEmissao), true));
                column.Item().Element(c => Field(c, "COMPETENCIA", Formatadores.Data(nfse.Competencia)));
            });
        });
    }

    private static void ComposeStatus(IContainer container, NFSeData nfse)
    {
        if (!nfse.Cancelada)
        {
            container.Border(0.5f).Padding(3).Text("STATUS DA NFS-e: AUTORIZADA").SemiBold();
            return;
        }

        container.Border(1.2f)
            .BorderColor(Colors.Red.Darken1)
            .Background(Colors.Red.Lighten5)
            .Padding(5)
            .Column(column =>
            {
                column.Item().AlignCenter().Text("NFS-e CANCELADA").FontSize(16).Bold().FontColor(Colors.Red.Darken2);
                if (!string.IsNullOrWhiteSpace(nfse.MotivoCancelamento))
                    column.Item().AlignCenter().Text(nfse.MotivoCancelamento).FontColor(Colors.Red.Darken2);
            });
    }

    private static void ComposeParty(IContainer container, string title, NFSeParty party)
    {
        container.Column(column =>
        {
            column.Item().NFSeSectionTitle(title);
            column.Item().Row(row =>
            {
                row.RelativeItem(4).Element(c => Field(c, "RAZAO SOCIAL", party.RazaoSocial, true));
                row.RelativeItem(2).Element(c => Field(c, "CNPJ / CPF", party.Documento, true));
                row.RelativeItem(2).Element(c => Field(c, "INSCRICAO MUNICIPAL", party.InscricaoMunicipal));
            });
            column.Item().Row(row =>
            {
                row.RelativeItem(4).Element(c => Field(c, "ENDERECO", party.Endereco.LinhaEndereco));
                row.RelativeItem(2).Element(c => Field(c, "BAIRRO", party.Endereco.Bairro));
                row.RelativeItem(2).Element(c => Field(c, "CEP", party.Endereco.Cep));
            });
            column.Item().Row(row =>
            {
                row.RelativeItem(3).Element(c => Field(c, "MUNICIPIO", party.Endereco.Municipio));
                row.RelativeItem().Element(c => Field(c, "UF", party.Endereco.Uf));
                row.RelativeItem(2).Element(c => Field(c, "TELEFONE", party.Endereco.Telefone));
                row.RelativeItem(3).Element(c => Field(c, "EMAIL", party.Email));
            });
        });
    }

    private static void ComposeService(IContainer container, NFSeData nfse)
    {
        var service = nfse.Servico;
        container.Column(column =>
        {
            column.Item().NFSeSectionTitle("DADOS DO SERVICO");
            column.Item().Row(row =>
            {
                row.RelativeItem(2).Element(c => Field(c, "ITEM LISTA SERVICO", service.ItemListaServico));
                row.RelativeItem(2).Element(c => Field(c, "CODIGO TRIBUTACAO", service.CodigoTributacaoMunicipio));
                row.RelativeItem(2).Element(c => Field(c, "CNAE", service.CodigoCnae));
                row.RelativeItem(2).Element(c => Field(c, "MUNICIPIO PRESTACAO", service.MunicipioPrestacao));
                row.RelativeItem().Element(c => Field(c, "UF", service.UfPrestacao));
            });
            column.Item().Border(0.5f).Padding(4).Column(inner =>
            {
                inner.Item().Text("DISCRIMINACAO DOS SERVICOS").FontSize(6).Bold();
                inner.Item().Text(string.IsNullOrWhiteSpace(service.Discriminacao) ? "-" : service.Discriminacao).FontSize(8);
            });
        });
    }

    private static void ComposeValues(IContainer container, NFSeData nfse)
    {
        var v = nfse.Valores;
        container.Column(column =>
        {
            column.Item().NFSeSectionTitle("VALORES E TRIBUTOS");
            column.Item().Row(row =>
            {
                row.RelativeItem().Element(c => Field(c, "VALOR SERVICOS", Formatadores.Moeda(v.ValorServicos), true));
                row.RelativeItem().Element(c => Field(c, "DEDUCOES", Formatadores.Moeda(v.ValorDeducoes)));
                row.RelativeItem().Element(c => Field(c, "BASE CALCULO", Formatadores.Moeda(v.BaseCalculo), true));
                row.RelativeItem().Element(c => Field(c, "ALIQUOTA", Formatadores.Percentual(v.Aliquota)));
                row.RelativeItem().Element(c => Field(c, "ISS", Formatadores.Moeda(v.ValorIss), true));
            });
            column.Item().Row(row =>
            {
                row.RelativeItem().Element(c => Field(c, "PIS", Formatadores.Moeda(v.ValorPis)));
                row.RelativeItem().Element(c => Field(c, "COFINS", Formatadores.Moeda(v.ValorCofins)));
                row.RelativeItem().Element(c => Field(c, "INSS", Formatadores.Moeda(v.ValorInss)));
                row.RelativeItem().Element(c => Field(c, "IR", Formatadores.Moeda(v.ValorIr)));
                row.RelativeItem().Element(c => Field(c, "CSLL", Formatadores.Moeda(v.ValorCsll)));
            });
            column.Item().Row(row =>
            {
                row.RelativeItem().Element(c => Field(c, "OUTRAS RETENCOES", Formatadores.Moeda(v.OutrasRetencoes)));
                row.RelativeItem().Element(c => Field(c, "DESC. INCOND.", Formatadores.Moeda(v.DescontoIncondicionado)));
                row.RelativeItem().Element(c => Field(c, "DESC. COND.", Formatadores.Moeda(v.DescontoCondicionado)));
                row.RelativeItem().Element(c => Field(c, "ISS RETIDO", v.IssRetido ? "SIM" : "NAO"));
                row.RelativeItem().Element(c => Field(c, "VALOR LIQUIDO", Formatadores.Moeda(v.ValorLiquidoNfse), true));
            });
        });
    }

    private static void ComposeAdditional(IContainer container, NFSeData nfse)
    {
        container.Column(column =>
        {
            column.Item().NFSeSectionTitle("INFORMACOES FISCAIS");
            column.Item().Row(row =>
            {
                row.RelativeItem().Element(c => Field(c, "NATUREZA OPERACAO", nfse.NaturezaOperacao));
                row.RelativeItem().Element(c => Field(c, "REGIME ESPECIAL", nfse.RegimeEspecialTributacao));
                row.RelativeItem().Element(c => Field(c, "SIMPLES NACIONAL", nfse.OptanteSimplesNacional));
                row.RelativeItem().Element(c => Field(c, "INCENTIVADOR CULTURAL", nfse.IncentivadorCultural));
            });
        });
    }

    private static void Field(IContainer container, string title, string value, bool bold = false)
    {
        container.Border(0.5f).MinHeight(10, Unit.Millimetre).Padding(2).Column(column =>
        {
            column.Item().Text(title).FontSize(5.7f).Bold();
            var text = column.Item().Text(string.IsNullOrWhiteSpace(value) ? "-" : value).FontSize(8);
            if (bold)
                text.Bold();
        });
    }
}

internal static class NFSePdfQuestExtensions
{
    public static void NFSeSectionTitle(this IContainer container, string text)
        => container.Background(Colors.Grey.Lighten2).Border(0.5f).Padding(2).Text(text).FontSize(7).Bold();
}
