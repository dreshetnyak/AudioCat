using System.Text;

namespace AudioCat;

internal static class Encodings
{
    public static Encoding Iso8859 { get; } = Encoding.GetEncoding("ISO-8859-1");
    public static Encoding Win1251 { get; } = Encoding.GetEncoding("Windows-1251");
}