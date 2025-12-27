using MermaidSharp.Core.Models;

namespace MermaidSharp.Diagrams.Packet;

public class PacketModel : DiagramBase
{
    public int BitsPerRow { get; set; } = 32;
    public List<PacketField> Fields { get; } = [];
}

public class PacketField
{
    public int StartBit { get; init; }
    public int EndBit { get; init; }
    public required string Label { get; init; }

    public int Width => EndBit - StartBit + 1;
}
