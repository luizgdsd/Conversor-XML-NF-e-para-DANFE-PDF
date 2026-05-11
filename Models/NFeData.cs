namespace ConversorXmlNFeDanfePdf.Models;

public sealed class NFeData
{
    public string XmlPath { get; set; } = "";
    public string ChaveAcesso { get; set; } = "";
    public string Numero { get; set; } = "";
    public string Serie { get; set; } = "";
    public DateTime? DataEmissao { get; set; }
    public DateTime? DataSaidaEntrada { get; set; }
    public TimeSpan? HoraSaidaEntrada { get; set; }
    public string TipoOperacao { get; set; } = "";
    public string NaturezaOperacao { get; set; } = "";
    public string ProtocoloAutorizacao { get; set; } = "";
    public string StatusNota { get; set; } = "AUTORIZADA";
    public bool Cancelada { get; set; }
    public string MotivoCancelamento { get; set; } = "";
    public Emitente Emitente { get; set; } = new();
    public Destinatario Destinatario { get; set; } = new();
    public LocalEntrega LocalEntrega { get; set; } = new();
    public Totais Totais { get; set; } = new();
    public Transporte Transporte { get; set; } = new();
    public List<Produto> Produtos { get; set; } = [];
    public string InformacoesComplementares { get; set; } = "";
    public string InformacoesFisco { get; set; } = "";
}
