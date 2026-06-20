using Naiad.Diagrams.Packet;

public class PacketParserTests
{
    [Test]
    public async Task ParsesSingleBitField()
    {
        // Mermaid allows a single-bit field "N: label" alongside the "start-end: label" range form.
        const string input =
            """
            packet-beta
            0-3: "Version"
            4: "Flag"
            5-15: "Rest"
            """;

        var result = new PacketParser().Parse(input);

        await Assert.That(result.Success).IsTrue();

        var flag = result.Value.Fields[1];
        await Assert.That(flag.StartBit).IsEqualTo(4);
        await Assert.That(flag.EndBit).IsEqualTo(4);
        await Assert.That(flag.Width).IsEqualTo(1);
    }

    [Test]
    public async Task ResolvesRelativeWidthFields()
    {
        // "+bits" continues from where the previous field ended: +8 occupies 0-7, then +16 occupies 8-23.
        const string input =
            """
            packet-beta
            +8: "byte"
            +16: "word"
            """;

        var result = new PacketParser().Parse(input);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Fields[0].StartBit).IsEqualTo(0);
        await Assert.That(result.Value.Fields[0].EndBit).IsEqualTo(7);
        await Assert.That(result.Value.Fields[1].StartBit).IsEqualTo(8);
        await Assert.That(result.Value.Fields[1].EndBit).IsEqualTo(23);
    }

    [Test]
    public async Task RejectsNonContiguousField()
    {
        // A gap (18 follows 16, skipping 17) is invalid, matching mermaid.js.
        var message = CaptureError(
            """
            packet-beta
            0-16: "test"
            18-20: "error"
            """);

        await Assert.That(message)
            .IsEqualTo("Packet block 18 - 20 is not contiguous. It should start from 17.");
    }

    [Test]
    public async Task RejectsReversedRange()
    {
        var message = CaptureError(
            """
            packet-beta
            25-20: "error"
            """);

        await Assert.That(message)
            .IsEqualTo("Packet block 25 - 20 is invalid. End must be greater than start.");
    }

    [Test]
    public async Task RejectsZeroBitField()
    {
        var message = CaptureError(
            """
            packet-beta
            +0: "error"
            """);

        await Assert.That(message)
            .IsEqualTo("Packet block 0 is invalid. Cannot have a zero bit field.");
    }

    // Returns the message of the MermaidParseException thrown while parsing, or "" if none was thrown.
    static string CaptureError(string input)
    {
        try
        {
            new PacketParser().Parse(input);
        }
        catch (MermaidParseException exception)
        {
            return exception.Message;
        }

        return "";
    }
}
