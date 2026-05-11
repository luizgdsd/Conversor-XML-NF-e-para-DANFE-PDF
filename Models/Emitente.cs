namespace ConversorXmlNFeDanfePdf.Models;

public sealed class Emitente
{
    public string RazaoSocial { get; set; } = "";
    public string Cnpj { get; set; } = "";
    public string InscricaoEstadual { get; set; } = "";
    public string InscricaoEstadualSubstitutoTributario { get; set; } = "";
    public AddressData Endereco { get; set; } = new();
}
