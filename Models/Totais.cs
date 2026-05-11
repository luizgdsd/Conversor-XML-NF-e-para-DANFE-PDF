namespace ConversorXmlNFeDanfePdf.Models;

public sealed class Totais
{
    public decimal BaseCalculoIcms { get; set; }
    public decimal ValorIcms { get; set; }
    public decimal BaseCalculoIcmsSt { get; set; }
    public decimal ValorIcmsSt { get; set; }
    public decimal ValorImpostoImportacao { get; set; }
    public decimal ValorIcmsUfRemetente { get; set; }
    public decimal ValorFcpUfDestino { get; set; }
    public decimal ValorProdutos { get; set; }
    public decimal Frete { get; set; }
    public decimal Seguro { get; set; }
    public decimal Desconto { get; set; }
    public decimal OutrasDespesas { get; set; }
    public decimal Ipi { get; set; }
    public decimal ValorIcmsUfDestino { get; set; }
    public decimal Pis { get; set; }
    public decimal Cofins { get; set; }
    public decimal ValorTotalNota { get; set; }
    public decimal ValorTotalTributos { get; set; }
}
