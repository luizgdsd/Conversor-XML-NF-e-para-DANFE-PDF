namespace ConversorXmlNFeDanfePdf.Models;

public sealed class Transporte
{
    public string RazaoSocial { get; set; } = "";
    public string ModalidadeFrete { get; set; } = "";
    public string CodigoAntt { get; set; } = "";
    public string Placa { get; set; } = "";
    public string UfPlaca { get; set; } = "";
    public string Documento { get; set; } = "";
    public string Endereco { get; set; } = "";
    public string Municipio { get; set; } = "";
    public string Uf { get; set; } = "";
    public string InscricaoEstadual { get; set; } = "";
    public decimal Quantidade { get; set; }
    public string Especie { get; set; } = "";
    public string Marca { get; set; } = "";
    public string Numeracao { get; set; } = "";
    public decimal PesoBruto { get; set; }
    public decimal PesoLiquido { get; set; }
}
