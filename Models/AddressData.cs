namespace ConversorXmlNFeDanfePdf.Models;

public sealed class AddressData
{
    public string Logradouro { get; set; } = "";
    public string Numero { get; set; } = "";
    public string Complemento { get; set; } = "";
    public string Bairro { get; set; } = "";
    public string Cep { get; set; } = "";
    public string Municipio { get; set; } = "";
    public string Uf { get; set; } = "";
    public string Telefone { get; set; } = "";

    public string LinhaEndereco
    {
        get
        {
            var partes = new[] { Logradouro, Numero, Complemento }.Where(x => !string.IsNullOrWhiteSpace(x));
            return string.Join(", ", partes);
        }
    }
}
