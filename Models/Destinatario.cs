namespace ConversorXmlNFeDanfePdf.Models;

public sealed class Destinatario
{
    public string RazaoSocial { get; set; } = "";
    public string Documento { get; set; } = "";
    public string InscricaoEstadual { get; set; } = "";
    public AddressData Endereco { get; set; } = new();
}
