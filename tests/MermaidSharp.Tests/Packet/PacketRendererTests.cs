public class PacketRendererTests
{
    [Test]
    public Task Render_SimplePacket()
    {
        const string input =
            """
            packet-beta
            0-15: "Source Port"
            16-31: "Destination Port"
            """;

        return SvgVerify. Verify(input);
    }

    [Test]
    public Task Render_TCPHeader()
    {
        const string input =
            """
            packet-beta
            0-15: "Source Port"
            16-31: "Destination Port"
            32-63: "Sequence Number"
            64-95: "Acknowledgment Number"
            96-99: "Data Offset"
            100-102: "Reserved"
            103-103: "NS"
            104-104: "CWR"
            105-105: "ECE"
            106-106: "URG"
            107-107: "ACK"
            108-108: "PSH"
            109-109: "RST"
            110-110: "SYN"
            111-111: "FIN"
            112-127: "Window Size"
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task Render_IPv4Header()
    {
        const string input =
            """
            packet-beta
            0-3: "Version"
            4-7: "IHL"
            8-13: "DSCP"
            14-15: "ECN"
            16-31: "Total Length"
            32-47: "Identification"
            48-50: "Flags"
            51-63: "Fragment Offset"
            64-71: "TTL"
            72-79: "Protocol"
            80-95: "Header Checksum"
            96-127: "Source IP Address"
            128-159: "Destination IP Address"
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task Render_SingleRow()
    {
        const string input =
            """
            packet-beta
            0-7: "Byte 1"
            8-15: "Byte 2"
            16-23: "Byte 3"
            24-31: "Byte 4"
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task Render_WideFields()
    {
        const string input =
            """
            packet-beta
            0-31: "First Word"
            32-63: "Second Word"
            64-95: "Third Word"
            """;

        return SvgVerify.Verify(input);
    }
}
