namespace ConversorXmlNFeDanfePdf.Models;

public sealed class NFSeData
{
    public string XmlPath { get; set; } = "";
    public string Numero { get; set; } = "";
    public string CodigoVerificacao { get; set; } = "";
    public DateTime? DataEmissao { get; set; }
    public DateTime? Competencia { get; set; }
    public string NaturezaOperacao { get; set; } = "";
    public string RegimeEspecialTributacao { get; set; } = "";
    public string OptanteSimplesNacional { get; set; } = "";
    public string IncentivadorCultural { get; set; } = "";
    public string Status { get; set; } = "AUTORIZADA";
    public bool Cancelada { get; set; }
    public string MotivoCancelamento { get; set; } = "";
    public NFSeParty Prestador { get; set; } = new();
    public NFSeParty Tomador { get; set; } = new();
    public NFSeService Servico { get; set; } = new();
    public NFSeValues Valores { get; set; } = new();
}

public sealed class NFSeParty
{
    public string RazaoSocial { get; set; } = "";
    public string NomeFantasia { get; set; } = "";
    public string Documento { get; set; } = "";
    public string InscricaoMunicipal { get; set; } = "";
    public string InscricaoEstadual { get; set; } = "";
    public AddressData Endereco { get; set; } = new();
    public string Email { get; set; } = "";
}

public sealed class NFSeService
{
    public string Discriminacao { get; set; } = "";
    public string ItemListaServico { get; set; } = "";
    public string CodigoTributacaoMunicipio { get; set; } = "";
    public string CodigoCnae { get; set; } = "";
    public string MunicipioPrestacao { get; set; } = "";
    public string UfPrestacao { get; set; } = "";
}

public sealed class NFSeValues
{
    public decimal ValorServicos { get; set; }
    public decimal ValorDeducoes { get; set; }
    public decimal ValorPis { get; set; }
    public decimal ValorCofins { get; set; }
    public decimal ValorInss { get; set; }
    public decimal ValorIr { get; set; }
    public decimal ValorCsll { get; set; }
    public decimal OutrasRetencoes { get; set; }
    public decimal ValorIss { get; set; }
    public decimal Aliquota { get; set; }
    public decimal DescontoIncondicionado { get; set; }
    public decimal DescontoCondicionado { get; set; }
    public decimal BaseCalculo { get; set; }
    public decimal ValorLiquidoNfse { get; set; }
    public bool IssRetido { get; set; }
}
