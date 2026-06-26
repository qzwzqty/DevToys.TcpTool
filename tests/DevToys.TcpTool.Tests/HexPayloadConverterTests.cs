using DevToys.TcpTool.Core;

namespace DevToys.TcpTool.Tests;

public sealed class HexPayloadConverterTests
{
    [Fact] public void Parse_accepts_spaces_and_uppercase_hex()
    {
        byte[] bytes = HexPayloadConverter.Parse("48 65 6C 6C 6F");
        Assert.Equal([0x48, 0x65, 0x6C, 0x6C, 0x6F], bytes);
    }

    [Fact] public void Parse_accepts_lowercase_and_0x_prefixes()
    {
        byte[] bytes = HexPayloadConverter.Parse("0x48 0x65 6c");
        Assert.Equal([0x48, 0x65, 0x6C], bytes);
    }

    [Fact] public void Parse_rejects_odd_hex_digits()
    {
        var ex = Assert.Throws<FormatException>(() => HexPayloadConverter.Parse("ABC"));
        Assert.Equal("HEX input must contain an even number of digits.", ex.Message);
    }

    [Fact] public void Parse_rejects_non_hex_characters()
    {
        var ex = Assert.Throws<FormatException>(() => HexPayloadConverter.Parse("48 ZZ"));
        Assert.Equal("HEX input contains invalid character 'Z'.", ex.Message);
    }

    [Fact] public void ToHex_normalizes_to_uppercase_byte_groups()
    {
        Assert.Equal("00 0A FF", HexPayloadConverter.ToHex([0x00, 0x0A, 0xFF]));
    }

    [Fact] public void ToBinary_renders_eight_bits_per_byte()
    {
        Assert.Equal("00000000 00001010 11111111", HexPayloadConverter.ToBinary([0x00, 0x0A, 0xFF]));
    }

    [Fact] public void ToUtf8Text_decodes_utf8_bytes()
    {
        Assert.Equal("Hello", HexPayloadConverter.ToUtf8Text([0x48, 0x65, 0x6C, 0x6C, 0x6F]));
    }
}
