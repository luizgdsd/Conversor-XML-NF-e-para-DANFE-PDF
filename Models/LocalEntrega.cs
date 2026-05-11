namespace ConversorXmlNFeDanfePdf.Models;

public sealed class LocalEntrega
{
    public string RazaoSocial { get; set; } = "";
    public string Documento { get; set; } = "";
    public string InscricaoEstadual { get; set; } = "";
    public AddressData Endereco { get; set; } = new();
    public bool Existe => !string.IsNullOrWhiteSpace(Documento)
        || !string.IsNullOrWhiteSpace(RazaoSocial)
        || !string.IsNullOrWhiteSpace(Endereco.LinhaEndereco);
}
