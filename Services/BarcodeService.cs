using System.Drawing;
using System.Drawing.Imaging;

namespace ConversorXmlNFeDanfePdf.Services;

public sealed class BarcodeService
{
    private const int StartB = 104;
    private const int StartC = 105;
    private const int Stop = 106;

    private static readonly string[] Patterns =
    [
        "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312", "132212", "221213",
        "221312", "231212", "112232", "122132", "122231", "113222", "123122", "123221", "223211", "221132",
        "221231", "213212", "223112", "312131", "311222", "321122", "321221", "312212", "322112", "322211",
        "212123", "212321", "232121", "111323", "131123", "131321", "112313", "132113", "132311", "211313",
        "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121", "313121", "211331",
        "231131", "213113", "213311", "213131", "311123", "311321", "331121", "312113", "312311", "332111",
        "314111", "221411", "431111", "111224", "111422", "121124", "121421", "141122", "141221", "112214",
        "112412", "122114", "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111",
        "111242", "121142", "121241", "114212", "124112", "124211", "411212", "421112", "421211", "212141",
        "214121", "412121", "111143", "111341", "131141", "114113", "114311", "411113", "411311", "113141",
        "114131", "311141", "411131", "211412", "211214", "211232", "2331112"
    ];

    public byte[] GenerateCode128Png(string value, int height = 70, int module = 2)
    {
        var codes = Encode(value);
        var totalModules = codes.Sum(code => Patterns[code].Sum(ch => ch - '0'));
        var width = Math.Max(1, totalModules * module + 20);

        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);

        var x = 10;
        foreach (var code in codes)
        {
            var pattern = Patterns[code];
            var black = true;
            foreach (var ch in pattern)
            {
                var w = (ch - '0') * module;
                if (black)
                    graphics.FillRectangle(Brushes.Black, x, 0, w, height);
                x += w;
                black = !black;
            }
        }

        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }

    private static List<int> Encode(string value)
    {
        var digitsOnly = value.All(char.IsDigit) && value.Length % 2 == 0;
        var codes = new List<int> { digitsOnly ? StartC : StartB };

        if (digitsOnly)
        {
            for (var i = 0; i < value.Length; i += 2)
                codes.Add(int.Parse(value.Substring(i, 2)));
        }
        else
        {
            foreach (var ch in value)
            {
                var code = ch - 32;
                if (code < 0 || code > 95)
                    code = 0;
                codes.Add(code);
            }
        }

        var checksum = codes[0];
        for (var i = 1; i < codes.Count; i++)
            checksum += codes[i] * i;
        codes.Add(checksum % 103);
        codes.Add(Stop);
        return codes;
    }
}
