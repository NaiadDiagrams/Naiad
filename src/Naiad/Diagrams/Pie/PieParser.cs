class PieParser : IDiagramParser<PieModel>
{
    static Parser<char, PieSection> sectionParser =
        from _ in CommonParsers.InlineWhitespace
        from label in CommonParsers.QuotedString
        from __ in CommonParsers.InlineWhitespace
        from colon in Char(':')
        from ___ in CommonParsers.InlineWhitespace
        from value in CommonParsers.Number
        from ____ in CommonParsers.InlineWhitespace
        from _____ in CommonParsers.LineEnd
        select new PieSection { Label = label, Value = value };

    static Parser<char, string> titleLine =
        from _ in CommonParsers.InlineWhitespace
        from keyword in String("title")
        from __ in CommonParsers.RequiredWhitespace
        from title in Token(_ => _ != '\r' && _ != '\n').ManyString()
        from ___ in CommonParsers.LineEnd
        select title;

    static Parser<char, bool> showDataParser =
        Try(String("showData")).ThenReturn(true).Or(Return(false));

    static Parser<char, Unit> skipLine =
        CommonParsers.InlineWhitespace
            .Then(Try(CommonParsers.Comment).Or(CommonParsers.Newline));

    // Inline title: pie title My Title (on same line)
    static Parser<char, string> inlineTitleParser =
        from keyword in String("title")
        from _ in CommonParsers.RequiredWhitespace
        from title in Token(_ => _ != '\r' && _ != '\n').ManyString()
        select title;

    public static Parser<char, PieModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from keyword in String("pie")
        from __ in CommonParsers.InlineWhitespace
        from showData in showDataParser
        from ___ in CommonParsers.InlineWhitespace
        from inlineTitle in Try(inlineTitleParser).Optional()
        from ____ in CommonParsers.InlineWhitespace
        from _____ in CommonParsers.LineEnd
        from content in ParseContent()
        select BuildModel(showData, inlineTitle.HasValue ? inlineTitle.Value : content.title, content.sections);

    public static Parser<char, (string? title, List<PieSection> sections)> ParseContent() =>
        from lines in Try(titleLine.Select(_ => (title: (string?)_, section: (PieSection?)null)))
            .Or(Try(sectionParser.Select(_ => (title: (string?)null, section: (PieSection?)_))))
            .Or(skipLine.ThenReturn((title: (string?)null, section: (PieSection?)null))).Many()
        select (
            title: lines.FirstOrDefault(_ => _.title != null).title,
            sections: lines.Where(_ => _.section != null).Select(_ => _.section!).ToList()
        );

    static PieModel BuildModel(bool showData, string? title, List<PieSection> sections)
    {
        var model = new PieModel
        {
            ShowData = showData,
            Title = title
        };
        model.Sections.AddRange(sections);
        return model;
    }

    public Result<char, PieModel> Parse(string input) => Parser.Parse(input);
}
