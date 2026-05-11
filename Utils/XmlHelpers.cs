using System.Globalization;
using System.Xml.Linq;

namespace ConversorXmlNFeDanfePdf.Utils;

public static class XmlHelpers
{
    public static XElement? FirstChild(this XElement? element, string localName)
        => element?.Elements().FirstOrDefault(x => x.Name.LocalName == localName);

    public static XElement? FirstDescendant(this XElement? element, string localName)
        => element?.Descendants().FirstOrDefault(x => x.Name.LocalName == localName);

    public static string Text(this XElement? element, string localName)
        => element.FirstChild(localName)?.Value.Trim() ?? "";

    public static string DescText(this XElement? element, string localName)
        => element.FirstDescendant(localName)?.Value.Trim() ?? "";

    public static decimal DecimalText(this XElement? element, string localName)
    {
        var text = element.Text(localName);
        return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }

    public static DateTime? DateTimeText(this XElement? element, string localName)
    {
        var text = element.Text(localName);
        if (string.IsNullOrWhiteSpace(text))
            return null;
        return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dto)
            ? dto.LocalDateTime
            : DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date) ? date : null;
    }
}
