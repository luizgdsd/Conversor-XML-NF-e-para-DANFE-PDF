namespace ConversorXmlNFeDanfePdf.Models;

public sealed class Produto
{
    public string Codigo { get; set; } = "";
    public string Descricao { get; set; } = "";
    public string NcmSh { get; set; } = "";
    public string CstCsosn { get; set; } = "";
    public string Cfop { get; set; } = "";
    public string Unidade { get; set; } = "";
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal ValorTotal { get; set; }
    public decimal BaseCalculoIcms { get; set; }
    public decimal ValorIcms { get; set; }
    public decimal ValorIpi { get; set; }
    public decimal AliquotaIcms { get; set; }
    public decimal AliquotaIpi { get; set; }
}
