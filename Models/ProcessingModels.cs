namespace ConversorXmlNFeDanfePdf.Models;

public enum ExistingPdfAction
{
    Ignore,
    Overwrite,
    IncrementSuffix
}

public sealed class ProcessingOptions
{
    public string InputFolder { get; set; } = "";
    public string OutputFolder { get; set; } = "";
    public bool IncludeSubfolders { get; set; }
    public bool GenerateUnifiedPdf { get; set; }
    public ExistingPdfAction ExistingPdfAction { get; set; } = ExistingPdfAction.IncrementSuffix;
}

public sealed class ProcessingResult
{
    public string XmlFile { get; set; } = "";
    public string Key { get; set; } = "";
    public string Number { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string Recipient { get; set; } = "";
    public string Status { get; set; } = "";
    public string Message { get; set; } = "";
    public string PdfPath { get; set; } = "";
}
