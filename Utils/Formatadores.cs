using System.Globalization;
using System.Text.RegularExpressions;

namespace ConversorXmlNFeDanfePdf.Utils;

public static class Formatadores
{
    private static readonly CultureInfo Brazil = new("pt-BR");

    public static string OnlyDigits(string? value) => Regex.Replace(value ?? "", "\\D", "");

    public static string CnpjCpf(string? value)
    {
        var digits = OnlyDigits(value);
        return digits.Length switch
        {
            14 => Convert.ToUInt64(digits).ToString(@"00\.000\.000\/0000\-00"),
            11 => Convert.ToUInt64(digits).ToString(@"000\.000\.000\-00"),
            _ => value ?? ""
        };
    }

    public static string Cep(string? value)
    {
        var digits = OnlyDigits(value);
        return digits.Length == 8 ? Convert.ToUInt64(digits).ToString(@"00000\-000") : value ?? "";
    }

    public static string ChaveAcesso(string? value)
    {
        var digits = OnlyDigits(value);
        if (digits.Length != 44)
            return value ?? "";
        return string.Join(" ", Enumerable.Range(0, 11).Select(i => digits.Substring(i * 4, 4)));
    }

    public static string Data(DateTime? value) => value?.ToString("dd/MM/yyyy", Brazil) ?? "";
    public static string Hora(TimeSpan? value) => value?.ToString(@"hh\:mm\:ss", Brazil) ?? "";
    public static string Moeda(decimal value) => value.ToString("N2", Brazil);
    public static string Quantidade(decimal value) => value.ToString("N4", Brazil);
    public static string Percentual(decimal value) => value == 0 ? "" : value.ToString("N2", Brazil);
    public static string ArquivoSeguro(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray()).Trim();
    }
}
