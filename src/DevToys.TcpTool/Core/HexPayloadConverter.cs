using System.Globalization;
using System.Text;

namespace DevToys.TcpTool.Core;

public static class HexPayloadConverter
{
    public static byte[] Parse(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        StringBuilder digits = new(input.Length);
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (char.IsWhiteSpace(c)) continue;
            if (c == '0' && i + 1 < input.Length && (input[i + 1] == 'x' || input[i + 1] == 'X')) { i++; continue; }
            if (Uri.IsHexDigit(c)) { digits.Append(c); continue; }
            throw new FormatException($"HEX input contains invalid character '{c}'.");
        }
        if (digits.Length % 2 != 0)
            throw new FormatException("HEX input must contain an even number of digits.");
        byte[] result = new byte[digits.Length / 2];
        for (int i = 0; i < result.Length; i++)
            result[i] = byte.Parse(digits.ToString(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return result;
    }

    public static string ToHex(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty) return string.Empty;
        var sb = new StringBuilder(bytes.Length * 3 - 1);
        for (int i = 0; i < bytes.Length; i++) { if (i > 0) sb.Append(' '); sb.Append(bytes[i].ToString("X2", CultureInfo.InvariantCulture)); }
        return sb.ToString();
    }

    public static string ToBinary(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty) return string.Empty;
        var sb = new StringBuilder(bytes.Length * 9 - 1);
        for (int i = 0; i < bytes.Length; i++) { if (i > 0) sb.Append(' '); sb.Append(Convert.ToString(bytes[i], 2).PadLeft(8, '0')); }
        return sb.ToString();
    }

    public static string ToUtf8Text(ReadOnlySpan<byte> bytes) => Encoding.UTF8.GetString(bytes);

    public static byte[] FromUtf8Text(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return Encoding.UTF8.GetBytes(input);
    }
}
