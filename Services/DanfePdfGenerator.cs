using ConversorXmlNFeDanfePdf.Models;
using ConversorXmlNFeDanfePdf.Utils;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ConversorXmlNFeDanfePdf.Services;

public sealed class DanfePdfGenerator
{
    private readonly BarcodeService _barcodeService = new();

    public void Generate(NFeData nfe, string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(4, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontFamily("Arial Narrow").FontSize(6.4f).FontColor(Colors.Black));
                page.Content().ScaleToFit().Column(column =>
                {
                    column.Item().Element(c => ComposeReceiptStub(c, nfe));
                    column.Item().PaddingVertical(0.4f).Text(new string('-', 155)).FontSize(5);
                    column.Item().Element(c => ComposeMainHeader(c, nfe));
                    column.Item().Element(c => ComposeInvoiceStatus(c, nfe));
                    column.Item().Element(c => ComposeProtocolAndNature(c, nfe));
                    column.Item().Element(c => ComposeIssuer(c, nfe));
                    column.Item().Element(c => ComposeRecipient(c, nfe));
                    if (nfe.LocalEntrega.Existe)
                        column.Item().Element(c => ComposeDeliveryPlace(c, nfe));
                    column.Item().Element(c => ComposeTotals(c, nfe));
                    column.Item().Element(c => ComposeTransport(c, nfe));
                    column.Item().Element(c => ComposeProducts(c, nfe));
                    column.Item().Element(c => ComposeAdditionalInfo(c, nfe));
                });
                page.Footer().AlignRight().Text(text =>
                {
                    text.Span("Folha ").FontSize(6);
                    text.CurrentPageNumber().FontSize(6).Bold();
                    text.Span("/").FontSize(6);
                    text.TotalPages().FontSize(6).Bold();
                });
            });
        }).GeneratePdf(outputPath);
    }

    private static void ComposeReceiptStub(IContainer container, NFeData nfe)
    {
        container.Border(0.5f).Padding(1).Column(column =>
        {
            column.Item().Text("RECEBEMOS DE " + nfe.Emitente.RazaoSocial + " OS PRODUTOS/SERVICOS CONSTANTES DA NOTA FISCAL INDICADA ABAIXO").FontSize(5.2f);
            column.Item().Row(row =>
            {
                row.RelativeItem(2).Element(c => Field(c, "DATA DE RECEBIMENTO", ""));
                row.RelativeItem(4).Element(c => Field(c, "IDENTIFICACAO E ASSINATURA DO RECEBEDOR", ""));
                row.RelativeItem(2).Element(c => Field(c, "NF-e", $"No. {nfe.Numero}\nSerie {nfe.Serie}", true));
            });
        });
    }

    private void ComposeMainHeader(IContainer container, NFeData nfe)
    {
        container.Row(row =>
        {
            row.RelativeItem(3).Border(0.5f).Padding(1.6f).Column(column =>
            {
                column.Item().Text(nfe.Emitente.RazaoSocial).FontSize(8.8f).Bold().AlignCenter();
                column.Item().Text(nfe.Emitente.Endereco.LinhaEndereco).AlignCenter();
                column.Item().Text($"{nfe.Emitente.Endereco.Bairro} - CEP {nfe.Emitente.Endereco.Cep}").AlignCenter();
                column.Item().Text($"{nfe.Emitente.Endereco.Municipio} - {nfe.Emitente.Endereco.Uf}").AlignCenter();
                column.Item().Text($"Fone: {nfe.Emitente.Endereco.Telefone}").AlignCenter();
            });

            row.RelativeItem(2).Border(0.5f).Padding(1.6f).Column(column =>
            {
                column.Item().Text("DANFE").FontSize(14).Bold().AlignCenter();
                column.Item().Text("Documento Auxiliar da Nota Fiscal Eletronica").FontSize(6).AlignCenter();
                column.Item().PaddingTop(1).Text("0 - ENTRADA").FontSize(7).Bold().AlignCenter();
                column.Item().Text("1 - SAIDA").FontSize(7).Bold().AlignCenter();
                column.Item().Text(nfe.TipoOperacao == "ENTRADA" ? "Tipo: 0" : "Tipo: 1").FontSize(8).Bold().AlignCenter();
                column.Item().PaddingTop(1).Text($"No. {nfe.Numero}").FontSize(8.5f).Bold().AlignCenter();
                column.Item().Text($"SERIE {nfe.Serie}").FontSize(8).Bold().AlignCenter();
                column.Item().Text(text =>
                {
                    text.Span("Folha ");
                    text.CurrentPageNumber().Bold();
                    text.Span("/");
                    text.TotalPages().Bold();
                });
            });

            row.RelativeItem(4).Border(0.5f).Padding(1.6f).Column(column =>
            {
                if (!string.IsNullOrWhiteSpace(nfe.ChaveAcesso))
                    column.Item().Height(14, Unit.Millimetre).Image(_barcodeService.GenerateCode128Png(nfe.ChaveAcesso)).FitArea();
                else
                    column.Item().Height(14, Unit.Millimetre).Text("CHAVE DE ACESSO AUSENTE").AlignCenter().Bold();
                column.Item().Text("CHAVE DE ACESSO").FontSize(5).Bold().AlignCenter();
                column.Item().Text(Formatadores.ChaveAcesso(nfe.ChaveAcesso)).FontSize(7).Bold().AlignCenter();
                column.Item().PaddingTop(0.5f).Text("Consulta de autenticidade no portal nacional da NF-e").FontSize(5.2f).AlignCenter();
                column.Item().Text("www.nfe.fazenda.gov.br/portal ou no site da SEFAZ autorizadora").FontSize(5.2f).AlignCenter();
            });
        });
    }

    private static void ComposeProtocolAndNature(IContainer container, NFeData nfe)
    {
        container.Row(row =>
        {
            row.RelativeItem(5).Element(c => Field(c, "NATUREZA DA OPERACAO", nfe.NaturezaOperacao, true));
            row.RelativeItem(2).Element(c => Field(c, "PROTOCOLO DE AUTORIZACAO DE USO", nfe.ProtocoloAutorizacao, true));
        });
    }

    private static void ComposeInvoiceStatus(IContainer container, NFeData nfe)
    {
        if (!nfe.Cancelada)
        {
            container.Border(0.5f).Padding(1.2f).Text("STATUS DA NF-e: AUTORIZADA").FontSize(6.2f).SemiBold();
            return;
        }

        container.Border(1.2f)
            .BorderColor(Colors.Red.Darken1)
            .Background(Colors.Red.Lighten5)
            .Padding(2)
            .Column(column =>
            {
                column.Item().AlignCenter().Text("NF-e CANCELADA").FontSize(13).Bold().FontColor(Colors.Red.Darken2);
                if (!string.IsNullOrWhiteSpace(nfe.MotivoCancelamento))
                    column.Item().AlignCenter().Text(nfe.MotivoCancelamento).FontSize(6.2f).FontColor(Colors.Red.Darken2);
            });
    }

    private static void ComposeIssuer(IContainer container, NFeData nfe)
    {
        container.Column(column =>
        {
            column.Item().SectionTitle("EMITENTE");
            column.Item().Row(row =>
            {
                row.RelativeItem(4).Element(c => Field(c, "RAZAO SOCIAL", nfe.Emitente.RazaoSocial, true));
                row.RelativeItem(2).Element(c => Field(c, "CNPJ", nfe.Emitente.Cnpj, true));
                row.RelativeItem(2).Element(c => Field(c, "INSCRICAO ESTADUAL", nfe.Emitente.InscricaoEstadual));
                row.RelativeItem(2).Element(c => Field(c, "IE SUBST. TRIBUTARIO", nfe.Emitente.InscricaoEstadualSubstitutoTributario));
            });
            column.Item().Row(row =>
            {
                row.RelativeItem(4).Element(c => Field(c, "ENDERECO", nfe.Emitente.Endereco.LinhaEndereco));
                row.RelativeItem(2).Element(c => Field(c, "BAIRRO", nfe.Emitente.Endereco.Bairro));
                row.RelativeItem(2).Element(c => Field(c, "CEP", nfe.Emitente.Endereco.Cep));
                row.RelativeItem(2).Element(c => Field(c, "FONE", nfe.Emitente.Endereco.Telefone));
            });
            column.Item().Row(row =>
            {
                row.RelativeItem(4).Element(c => Field(c, "MUNICIPIO", nfe.Emitente.Endereco.Municipio));
                row.RelativeItem(1).Element(c => Field(c, "UF", nfe.Emitente.Endereco.Uf));
            });
        });
    }

    private static void ComposeRecipient(IContainer container, NFeData nfe)
    {
        container.Column(column =>
        {
            column.Item().SectionTitle("DESTINATARIO / REMETENTE");
            column.Item().Row(row =>
            {
                row.RelativeItem(4).Element(c => Field(c, "NOME / RAZAO SOCIAL", nfe.Destinatario.RazaoSocial, true));
                row.RelativeItem(2).Element(c => Field(c, "CNPJ / CPF", nfe.Destinatario.Documento, true));
                row.RelativeItem(2).Element(c => Field(c, "DATA DA EMISSAO", Formatadores.Data(nfe.DataEmissao)));
            });
            column.Item().Row(row =>
            {
                row.RelativeItem(4).Element(c => Field(c, "ENDERECO", nfe.Destinatario.Endereco.LinhaEndereco));
                row.RelativeItem(2).Element(c => Field(c, "BAIRRO", nfe.Destinatario.Endereco.Bairro));
                row.RelativeItem(2).Element(c => Field(c, "CEP", nfe.Destinatario.Endereco.Cep));
            });
            column.Item().Row(row =>
            {
                row.RelativeItem(3).Element(c => Field(c, "MUNICIPIO", nfe.Destinatario.Endereco.Municipio));
                row.RelativeItem(1).Element(c => Field(c, "UF", nfe.Destinatario.Endereco.Uf));
                row.RelativeItem(2).Element(c => Field(c, "INSCRICAO ESTADUAL", nfe.Destinatario.InscricaoEstadual));
                row.RelativeItem(2).Element(c => Field(c, "DATA SAIDA / ENTRADA", Formatadores.Data(nfe.DataSaidaEntrada)));
                row.RelativeItem(2).Element(c => Field(c, "HORA SAIDA / ENTRADA", Formatadores.Hora(nfe.HoraSaidaEntrada)));
            });
        });
    }

    private static void ComposeDeliveryPlace(IContainer container, NFeData nfe)
    {
        var local = nfe.LocalEntrega;
        container.Column(column =>
        {
            column.Item().SectionTitle("LOCAL DE ENTREGA");
            column.Item().Row(row =>
            {
                row.RelativeItem(4).Element(c => Field(c, "NOME / RAZAO SOCIAL", local.RazaoSocial));
                row.RelativeItem(2).Element(c => Field(c, "CNPJ / CPF", local.Documento));
                row.RelativeItem(2).Element(c => Field(c, "INSCRICAO ESTADUAL", local.InscricaoEstadual));
            });
            column.Item().Row(row =>
            {
                row.RelativeItem(4).Element(c => Field(c, "ENDERECO", local.Endereco.LinhaEndereco));
                row.RelativeItem(2).Element(c => Field(c, "BAIRRO", local.Endereco.Bairro));
                row.RelativeItem(2).Element(c => Field(c, "CEP", local.Endereco.Cep));
                row.RelativeItem(2).Element(c => Field(c, "MUNICIPIO / UF", $"{local.Endereco.Municipio} / {local.Endereco.Uf}"));
            });
        });
    }

    private static void ComposeTotals(IContainer container, NFeData nfe)
    {
        var t = nfe.Totais;
        container.Column(column =>
        {
            column.Item().SectionTitle("CALCULO DO IMPOSTO");
            column.Item().Row(row =>
            {
                row.RelativeItem().Element(c => Field(c, "BASE DE CALC. DO ICMS", Formatadores.Moeda(t.BaseCalculoIcms), true));
                row.RelativeItem().Element(c => Field(c, "VALOR DO ICMS", Formatadores.Moeda(t.ValorIcms), true));
                row.RelativeItem().Element(c => Field(c, "BASE DE CALC. ICMS S.T.", Formatadores.Moeda(t.BaseCalculoIcmsSt), true));
                row.RelativeItem().Element(c => Field(c, "VALOR DO ICMS SUBST.", Formatadores.Moeda(t.ValorIcmsSt), true));
                row.RelativeItem().Element(c => Field(c, "V. IMP. IMPORTACAO", Formatadores.Moeda(t.ValorImpostoImportacao), true));
                row.RelativeItem().Element(c => Field(c, "V. ICMS UF REMET.", Formatadores.Moeda(t.ValorIcmsUfRemetente), true));
                row.RelativeItem().Element(c => Field(c, "V. FCP UF DEST.", Formatadores.Moeda(t.ValorFcpUfDestino), true));
                row.RelativeItem().Element(c => Field(c, "VALOR DO PIS", Formatadores.Moeda(t.Pis), true));
                row.RelativeItem().Element(c => Field(c, "V. TOTAL PRODUTOS", Formatadores.Moeda(t.ValorProdutos), true));
            });
            column.Item().Row(row =>
            {
                row.RelativeItem().Element(c => Field(c, "VALOR DO FRETE", Formatadores.Moeda(t.Frete), true));
                row.RelativeItem().Element(c => Field(c, "VALOR DO SEGURO", Formatadores.Moeda(t.Seguro), true));
                row.RelativeItem().Element(c => Field(c, "DESCONTO", Formatadores.Moeda(t.Desconto), true));
                row.RelativeItem().Element(c => Field(c, "OUTRAS DESPESAS", Formatadores.Moeda(t.OutrasDespesas), true));
                row.RelativeItem().Element(c => Field(c, "VALOR TOTAL IPI", Formatadores.Moeda(t.Ipi), true));
                row.RelativeItem().Element(c => Field(c, "V. ICMS UF DEST.", Formatadores.Moeda(t.ValorIcmsUfDestino), true));
                row.RelativeItem().Element(c => Field(c, "V. TOT. TRIB.", Formatadores.Moeda(t.ValorTotalTributos), true));
                row.RelativeItem().Element(c => Field(c, "VALOR DA COFINS", Formatadores.Moeda(t.Cofins), true));
                row.RelativeItem().Element(c => Field(c, "V. TOTAL DA NOTA", Formatadores.Moeda(t.ValorTotalNota), true));
            });
        });
    }

    private static void ComposeTransport(IContainer container, NFeData nfe)
    {
        var tr = nfe.Transporte;
        container.Column(column =>
        {
            column.Item().SectionTitle("TRANSPORTADOR / VOLUMES TRANSPORTADOS");
            column.Item().Row(row =>
            {
                row.RelativeItem(4).Element(c => Field(c, "RAZAO SOCIAL", tr.RazaoSocial));
                row.RelativeItem().Element(c => Field(c, "FRETE POR CONTA", tr.ModalidadeFrete));
                row.RelativeItem().Element(c => Field(c, "CODIGO ANTT", tr.CodigoAntt));
                row.RelativeItem().Element(c => Field(c, "PLACA", tr.Placa));
                row.RelativeItem().Element(c => Field(c, "UF", tr.UfPlaca));
                row.RelativeItem(2).Element(c => Field(c, "CNPJ / CPF", tr.Documento));
            });
            column.Item().Row(row =>
            {
                row.RelativeItem(4).Element(c => Field(c, "ENDERECO", tr.Endereco));
                row.RelativeItem(2).Element(c => Field(c, "MUNICIPIO", tr.Municipio));
                row.RelativeItem().Element(c => Field(c, "UF", tr.Uf));
                row.RelativeItem(2).Element(c => Field(c, "INSCRICAO ESTADUAL", tr.InscricaoEstadual));
            });
            column.Item().Row(row =>
            {
                row.RelativeItem().Element(c => Field(c, "QUANTIDADE", Formatadores.Quantidade(tr.Quantidade)));
                row.RelativeItem().Element(c => Field(c, "ESPECIE", tr.Especie));
                row.RelativeItem().Element(c => Field(c, "MARCA", tr.Marca));
                row.RelativeItem().Element(c => Field(c, "NUMERACAO", tr.Numeracao));
                row.RelativeItem().Element(c => Field(c, "PESO BRUTO", Formatadores.Quantidade(tr.PesoBruto)));
                row.RelativeItem().Element(c => Field(c, "PESO LIQUIDO", Formatadores.Quantidade(tr.PesoLiquido)));
            });
        });
    }

    private static void ComposeProducts(IContainer container, NFeData nfe)
    {
        container.Column(column =>
        {
            column.Item().SectionTitle("DADOS DOS PRODUTOS / SERVICOS");
            column.Item().MinHeight(48, Unit.Millimetre).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(13, Unit.Millimetre);
                    columns.RelativeColumn(4);
                    columns.ConstantColumn(14, Unit.Millimetre);
                    columns.ConstantColumn(9, Unit.Millimetre);
                    columns.ConstantColumn(10, Unit.Millimetre);
                    columns.ConstantColumn(8, Unit.Millimetre);
                    columns.ConstantColumn(14, Unit.Millimetre);
                    columns.ConstantColumn(15, Unit.Millimetre);
                    columns.ConstantColumn(15, Unit.Millimetre);
                    columns.ConstantColumn(14, Unit.Millimetre);
                    columns.ConstantColumn(13, Unit.Millimetre);
                    columns.ConstantColumn(12, Unit.Millimetre);
                    columns.ConstantColumn(12, Unit.Millimetre);
                });

                table.Header(header =>
                {
                    foreach (var title in new[] { "COD.", "DESCRICAO", "NCM/SH", "CST", "CFOP", "UN", "QTDE.", "V. UNIT.", "V. TOTAL", "BC ICMS", "V. ICMS", "V. IPI", "ALIQ." })
                        header.Cell().Border(0.5f).Background(Colors.Grey.Lighten3).Padding(0.6f).AlignCenter().Text(title).FontSize(4.6f).SemiBold();
                });

                void BodyCell(string text, bool right = false)
                {
                    var cell = table.Cell().Border(0.25f).Padding(0.6f);
                    if (right)
                        cell = cell.AlignRight();
                    cell.Text(text ?? "").FontSize(4.9f);
                }

                foreach (var p in nfe.Produtos)
                {
                    BodyCell(p.Codigo);
                    BodyCell(p.Descricao);
                    BodyCell(p.NcmSh);
                    BodyCell(p.CstCsosn);
                    BodyCell(p.Cfop);
                    BodyCell(p.Unidade);
                    BodyCell(Formatadores.Quantidade(p.Quantidade), true);
                    BodyCell(Formatadores.Moeda(p.ValorUnitario), true);
                    BodyCell(Formatadores.Moeda(p.ValorTotal), true);
                    BodyCell(Formatadores.Moeda(p.BaseCalculoIcms), true);
                    BodyCell(Formatadores.Moeda(p.ValorIcms), true);
                    BodyCell(Formatadores.Moeda(p.ValorIpi), true);
                    BodyCell($"{Formatadores.Percentual(p.AliquotaIcms)}\n{Formatadores.Percentual(p.AliquotaIpi)}", true);
                }
            });
        });
    }

    private static void ComposeAdditionalInfo(IContainer container, NFeData nfe)
    {
        container.Column(column =>
        {
            column.Item().SectionTitle("DADOS ADICIONAIS");
            column.Item().Row(row =>
            {
                row.RelativeItem(3).Element(c => Field(c, "INFORMACOES COMPLEMENTARES", nfe.InformacoesComplementares));
                row.RelativeItem(2).Element(c => Field(c, "RESERVADO AO FISCO", nfe.InformacoesFisco));
            });
        });
    }

    private static void Field(IContainer container, string title, string value, bool bold = false)
    {
        container.Border(0.5f).Padding(0.9f).MinHeight(6.7f, Unit.Millimetre).Column(column =>
        {
            column.Item().Text(title.ToUpperInvariant()).FontSize(4.5f).SemiBold();
            var item = column.Item().Text(value ?? "").FontSize(6.2f);
            if (bold)
                item.Bold();
        });
    }

}

internal static class DanfeQuestExtensions
{
    public static void SectionTitle(this IContainer container, string title)
    {
        container.PaddingTop(0.7f).Border(0.5f).Background(Colors.Grey.Lighten4).Padding(0.7f).Text(title).FontSize(5.2f).Bold();
    }
}
