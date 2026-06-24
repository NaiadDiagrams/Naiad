class PieParser : IDiagramParser<PieModel>
{
    static readonly Parser<char, PieSection> sectionParser;

    static readonly Parser<char, string> titleLine;

    static readonly Parser<char, bool> showDataParser;

    static readonly Parser<char, Unit> skipLine;

    // Inline title: pie title My Title (on same line)
    static readonly Parser<char, string> inlineTitleParser;

    static readonly Parser<char, (string? title, List<PieSection> sections)> ParseContent;

    static readonly Parser<char, PieModel> Parser;

    static PieParser()
    {
        sectionParser =
            from _ in CommonParsers.InlineWhitespace
            from label in CommonParsers.QuotedString
            from __ in CommonParsers.InlineWhitespace
            from colon in Char(':')
            from ___ in CommonParsers.InlineWhitespace
            from value in CommonParsers.Number
            from ____ in CommonParsers.InlineWhitespace
            from _____ in CommonParsers.LineEnd
            select new PieSection { Label = label, Value = value };

        titleLine =
            from _ in CommonParsers.InlineWhitespace
            from keyword in String("title")
            from __ in CommonParsers.RequiredWhitespace
            from title in Token(_ => _ != '\r' && _ != '\n').ManyString()
            from ___ in CommonParsers.LineEnd
            select title;

        showDataParser =
            Try(String("showData")).ThenReturn(true).Or(Return(false));

        skipLine =
            CommonParsers.InlineWhitespace
                .Then(Try(CommonParsers.Comment).Or(CommonParsers.Newline));

        inlineTitleParser =
            from keyword in String("title")
            from _ in CommonParsers.RequiredWhitespace
            from title in Token(_ => _ != '\r' && _ != '\n').ManyString()
            select title;

        ParseContent =
            from lines in Try(titleLine.Select(_ => (title: (string?)_, section: (PieSection?)null)))
                .Or(Try(sectionParser.Select(_ => (title: (string?)null, section: (PieSection?)_))))
                .Or(skipLine.ThenReturn((title: (string?)null, section: (PieSection?)null))).Many()
            select (
                title: lines.FirstOrDefault(_ => _.title != null).title,
                sections: lines.Where(_ => _.section != null).Select(_ => _.section!).ToList()
            );

        Parser =
            from _ in CommonParsers.InlineWhitespace
            from keyword in String("pie")
            from __ in CommonParsers.InlineWhitespace
            from showData in showDataParser
            from ___ in CommonParsers.InlineWhitespace
            from inlineTitle in Try(inlineTitleParser).Optional()
            from ____ in CommonParsers.InlineWhitespace
            from _____ in CommonParsers.LineEnd
            from content in ParseContent
            select BuildModel(showData, inlineTitle.HasValue ? inlineTitle.Value : content.title, content.sections);
    }

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
