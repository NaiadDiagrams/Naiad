using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace MermaidSharp.Diagrams.Packet;

public class PacketParser : IDiagramParser<PacketModel>
{
    public DiagramType DiagramType => DiagramType.Packet;

    // Quoted label
    static readonly Parser<char, string> QuotedLabel =
        Char('"').Then(Token(c => c != '"').ManyString()).Before(Char('"'));

    // Unquoted label (rest of line)
    static readonly Parser<char, string> UnquotedLabel =
        Token(c => c != '\r' && c != '\n').AtLeastOnceString()
            .Select(s => s.Trim());

    // Label (quoted or unquoted)
    static readonly Parser<char, string> Label =
        QuotedLabel.Or(UnquotedLabel);

    // Field: start-end: "label" or start-end: label
    static readonly Parser<char, PacketField> FieldParser =
        from _ in CommonParsers.InlineWhitespace
        from start in Digit.AtLeastOnceString().Select(int.Parse)
        from __ in Char('-')
        from end in Digit.AtLeastOnceString().Select(int.Parse)
        from ___ in Char(':')
        from ____ in CommonParsers.InlineWhitespace
        from label in Label
        from _____ in CommonParsers.LineEnd
        select new PacketField
        {
            StartBit = start,
            EndBit = end,
            Label = label
        };

    // Skip line (comments, empty lines)
    static readonly Parser<char, Unit> SkipLine =
        Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
            .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

    // Content item
    static Parser<char, PacketField?> ContentItem =>
        OneOf(
            Try(FieldParser.Select(f => (PacketField?)f)),
            SkipLine.ThenReturn((PacketField?)null)
        );

    public Parser<char, PacketModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from __ in OneOf(CIString("packet-beta"), CIString("packet"))
        from ___ in CommonParsers.InlineWhitespace
        from ____ in CommonParsers.LineEnd
        from result in ContentItem.ManyThen(End)
        select BuildModel(result.Item1.Where(f => f != null).Cast<PacketField>().ToList());

    static PacketModel BuildModel(List<PacketField> fields)
    {
        var model = new PacketModel();
        model.Fields.AddRange(fields);
        return model;
    }

    public Result<char, PacketModel> Parse(string input) => Parser.Parse(input);
}
