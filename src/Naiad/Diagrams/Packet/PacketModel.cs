namespace MermaidSharp.Diagrams.Packet;

public class PacketModel : DiagramBase
{
    public int BitsPerRow { get; set; } = 32;
    public List<PacketField> Fields { get; } = [];
}