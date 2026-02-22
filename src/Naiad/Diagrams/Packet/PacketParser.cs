namespace MermaidSharp.Diagrams.Packet;

public class PacketParser : IDiagramParser<PacketModel>
{
    public DiagramType DiagramType => DiagramType.Packet;

    // Quoted label
    static Parser<char, string> QuotedLabel =
        Char('"').Then(Token(_ => _ != '"').ManyString()).Before(Char('"'));

    // Unquoted label (rest of line)
    static Parser<char, string> UnquotedLabel =
        Token(_ => _ != '\r' && _ != '\n').AtLeastOnceString()
            .Select(_ => _.Trim());

    // Label (quoted or unquoted)
    static Parser<char, string> Label =
        QuotedLabel.Or(UnquotedLabel);

    // Field: start-end: "label" or start-end: label
    static Parser<char, PacketField> FieldParser =
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
    static Parser<char, Unit> SkipLine =
        Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
            .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

    // Content item
    static Parser<char, PacketField?> ContentItem =>
        OneOf(
            Try(FieldParser.Select(_ => (PacketField?)_)),
            SkipLine.ThenReturn((PacketField?)null)
        );

    public static Parser<char, PacketModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from __ in OneOf(CIString("packet-beta"), CIString("packet"))
        from ___ in CommonParsers.InlineWhitespace
        from ____ in CommonParsers.LineEnd
        from result in ContentItem.ManyThen(End)
        select BuildModel(result.Item1.Where(_ => _ != null).ToList());

    static PacketModel BuildModel(List<PacketField> fields)
    {
        var model = new PacketModel();
        model.Fields.AddRange(fields);
        return model;
    }

    public Result<char, PacketModel> Parse(string input) => Parser.Parse(input);
}
