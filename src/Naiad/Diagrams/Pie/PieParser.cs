namespace MermaidSharp.Diagrams.Pie;

public class PieParser : IDiagramParser<PieModel>
{
    public DiagramType DiagramType => DiagramType.Pie;

    static readonly Parser<char, PieSection> SectionParser =
        from _ in CommonParsers.InlineWhitespace
        from label in CommonParsers.QuotedString
        from __ in CommonParsers.InlineWhitespace
        from colon in Char(':')
        from ___ in CommonParsers.InlineWhitespace
        from value in CommonParsers.Number
        from ____ in CommonParsers.InlineWhitespace
        from _____ in CommonParsers.LineEnd
        select new PieSection { Label = label, Value = value };

    static readonly Parser<char, string> TitleLine =
        from _ in CommonParsers.InlineWhitespace
        from keyword in String("title")
        from __ in CommonParsers.RequiredWhitespace
        from title in Token(c => c != '\r' && c != '\n').ManyString()
        from ___ in CommonParsers.LineEnd
        select title;

    static readonly Parser<char, bool> ShowDataParser =
        Try(String("showData")).ThenReturn(true).Or(Return(false));

    static readonly Parser<char, Unit> SkipLine =
        CommonParsers.InlineWhitespace
            .Then(Try(CommonParsers.Comment).Or(CommonParsers.Newline));

    public static Parser<char, PieModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from keyword in String("pie")
        from __ in CommonParsers.InlineWhitespace
        from showData in ShowDataParser
        from ___ in CommonParsers.InlineWhitespace
        from ____ in CommonParsers.LineEnd
        from content in ParseContent()
        select BuildModel(showData, content.title, content.sections);

    static Parser<char, (string? title, List<PieSection> sections)> ParseContent() =>
        from lines in (
            Try(TitleLine.Select(t => (title: (string?)t, section: (PieSection?)null)))
                .Or(Try(SectionParser.Select(s => (title: (string?)null, section: (PieSection?)s))))
                .Or(SkipLine.ThenReturn((title: (string?)null, section: (PieSection?)null)))
        ).Many()
        select (
            title: lines.FirstOrDefault(l => l.title != null).title,
            sections: lines.Where(l => l.section != null).Select(l => l.section!).ToList()
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
