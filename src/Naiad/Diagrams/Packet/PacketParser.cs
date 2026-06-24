class PacketParser : IDiagramParser<PacketModel>
{
    static readonly Parser<char, string> quotedLabel;

    // Unquoted label (rest of line)
    static readonly Parser<char, string> unquotedLabel;

    // Label (quoted or unquoted)
    static readonly Parser<char, string> labelParser;

    // "+bits": a width relative to where the previous field ended.
    static readonly Parser<char, RawField> relativeSpec;

    // "start" (single bit) or "start-end" (explicit range).
    static readonly Parser<char, RawField> explicitSpec;

    // Field: a bit spec ("start", "start-end", or "+bits") then ": label". Mermaid accepts all three;
    // absolute positions and contiguity are resolved in BuildModel since "+bits" depends on order.
    static readonly Parser<char, RawField> fieldParser;

    // Skip line (comments, empty lines)
    static readonly Parser<char, Unit> skipLine;

    static readonly Parser<char, RawField?> ContentItem;

    static readonly Parser<char, PacketModel> Parser;

    static PacketParser()
    {
        quotedLabel =
            Char('"').Then(Token(_ => _ != '"').ManyString()).Before(Char('"'));

        unquotedLabel =
            Token(_ => _ != '\r' && _ != '\n').AtLeastOnceString()
                .Select(_ => _.Trim());

        labelParser =
            quotedLabel.Or(unquotedLabel);

        relativeSpec =
            from _ in Char('+')
            from bits in Digit.AtLeastOnceString().Select(int.Parse)
            select new RawField(null, null, bits, "");

        explicitSpec =
            from start in Digit.AtLeastOnceString().Select(int.Parse)
            from end in Try(Char('-').Then(Digit.AtLeastOnceString().Select(int.Parse))).Optional()
            select new RawField(start, end.HasValue ? end.Value : null, null, "");

        fieldParser =
            from _ in CommonParsers.InlineWhitespace
            from spec in relativeSpec.Or(explicitSpec)
            from __ in Char(':')
            from ___ in CommonParsers.InlineWhitespace
            from label in labelParser
            from ____ in CommonParsers.LineEnd
            select spec with
            {
                Label = label
            };

        skipLine =
            Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
                .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

        ContentItem =
            OneOf(
                Try(fieldParser.Select<RawField?>(_ => _)),
                skipLine.ThenReturn<RawField?>(null)
            );

        Parser =
            from _ in CommonParsers.InlineWhitespace
            from __ in OneOf(CIString("packet-beta"), CIString("packet"))
            from ___ in CommonParsers.InlineWhitespace
            from ____ in CommonParsers.LineEnd
            from result in ContentItem.ManyThen(End)
            select BuildModel(result.Item1.Where(_ => _ != null).ToList());
    }

    // Resolve each raw field to absolute bit positions and validate the sequence the way Mermaid does:
    // fields must tile the packet contiguously from bit 0 with no gaps, overlaps, or reversed ranges.
    static PacketModel BuildModel(List<RawField> fields)
    {
        var model = new PacketModel();
        var nextBit = 0;

        foreach (var field in fields)
        {
            int start;
            int end;

            if (field.Bits is { } bits)
            {
                // "+bits" continues from the previous field; its start is contiguous by construction.
                start = nextBit;
                if (bits == 0)
                {
                    throw new MermaidParseException(
                        $"Packet block {start} is invalid. Cannot have a zero bit field.");
                }

                end = start + bits - 1;
            }
            else
            {
                start = field.Start!.Value;
                end = field.End ?? start;

                if (start > end)
                {
                    throw new MermaidParseException(
                        $"Packet block {start} - {end} is invalid. End must be greater than start.");
                }

                if (start != nextBit)
                {
                    throw new MermaidParseException(
                        $"Packet block {start} - {end} is not contiguous. It should start from {nextBit}.");
                }
            }

            model.Fields.Add(
                new()
                {
                    StartBit = start,
                    EndBit = end,
                    Label = field.Label
                });

            nextBit = end + 1;
        }

        return model;
    }

    public Result<char, PacketModel> Parse(string input) => Parser.Parse(input);

    // A field as written, before resolution: an explicit Start (with optional End), or a relative
    // width in Bits ("+N"). Exactly one of (Start, Bits) is set.
    internal sealed record RawField(int? Start, int? End, int? Bits, string Label);
}
