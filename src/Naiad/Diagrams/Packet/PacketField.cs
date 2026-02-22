namespace MermaidSharp.Diagrams.Packet;

public class PacketField
{
    public int StartBit { get; init; }
    public int EndBit { get; init; }
    public required string Label { get; init; }

    public int Width => EndBit - StartBit + 1;
}