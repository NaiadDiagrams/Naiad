class PacketParser : IDiagramParser<PacketModel>
{
    static Parser<char, string> quotedLabel =
        Char('"').Then(Token(_ => _ != '"').ManyString()).Before(Char('"'));

    // Unquoted label (rest of line)
    static Parser<char, string> unquotedLabel =
        Token(_ => _ != '\r' && _ != '\n').AtLeastOnceString()
            .Select(_ => _.Trim());

    // Label (quoted or unquoted)
    static Parser<char, string> labelParser =
        quotedLabel.Or(unquotedLabel);

    // Field: start-end: "label" or start-end: label
    static Parser<char, PacketField> fieldParser =
        from _ in CommonParsers.InlineWhitespace
        from start in Digit.AtLeastOnceString().Select(int.Parse)
        from __ in Char('-')
        from end in Digit.AtLeastOnceString().Select(int.Parse)
        from ___ in Char(':')
        from ____ in CommonParsers.InlineWhitespace
        from label in labelParser
        from _____ in CommonParsers.LineEnd
        select new PacketField
        {
            StartBit = start,
            EndBit = end,
            Label = label
        };

    // Skip line (comments, empty lines)
    static Parser<char, Unit> skipLine =
        Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
            .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

    static Parser<char, PacketField?> ContentItem =>
        OneOf(
            Try(fieldParser.Select<PacketField?>(_ => _)),
            skipLine.ThenReturn<PacketField?>(null)
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
