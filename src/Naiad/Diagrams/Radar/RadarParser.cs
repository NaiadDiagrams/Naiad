namespace MermaidSharp.Diagrams.Radar;

public class RadarParser : IDiagramParser<RadarModel>
{
    public DiagramType DiagramType => DiagramType.Radar;

    // Identifier
    static Parser<char, string> Identifier =
        Token(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').AtLeastOnceString();

    // Number
    static Parser<char, double> Number =
        from neg in Char('-').Optional()
        from digits in Digit.AtLeastOnceString()
        from dec in Char('.').Then(Digit.AtLeastOnceString()).Optional()
        select double.Parse((neg.HasValue ? "-" : "") + digits + (dec.HasValue ? "." + dec.Value : ""));

    // Quoted label: ["label"]
    static Parser<char, string> QuotedLabel =
        Char('[').Then(Char('"')).Then(Token(c => c != '"').ManyString()).Before(Char('"')).Before(Char(']'));

    // Axis list: axis id1, id2, id3
    static Parser<char, List<RadarAxis>> AxisParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("axis")
        from ___ in CommonParsers.RequiredWhitespace
        from axes in Identifier.SeparatedAtLeastOnce(
            CommonParsers.InlineWhitespace.Then(Char(',')).Then(CommonParsers.InlineWhitespace))
        from ____ in CommonParsers.InlineWhitespace
        from _____ in CommonParsers.LineEnd
        select axes.Select(a => new RadarAxis { Id = a, Label = a }).ToList();

    // Value list: {1, 2, 3}
    static Parser<char, List<double>> ValueList =
        Char('{')
            .Then(CommonParsers.InlineWhitespace)
            .Then(Number.SeparatedAtLeastOnce(
                CommonParsers.InlineWhitespace.Then(Char(',')).Then(CommonParsers.InlineWhitespace)))
            .Before(CommonParsers.InlineWhitespace)
            .Before(Char('}'))
            .Select(v => v.ToList());

    // Curve definition: curve id["label"]{1, 2, 3}
    static Parser<char, RadarCurve> CurveItemParser =
        from id in Identifier
        from label in QuotedLabel.Optional()
        from values in ValueList
        select new RadarCurve
        {
            Id = id,
            Label = label.GetValueOrDefault() ?? id
        }.WithValues(values);

    // Curve line: curve id1["label"]{1, 2, 3}, id2{4, 5, 6}
    static Parser<char, List<RadarCurve>> CurveLineParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("curve")
        from ___ in CommonParsers.RequiredWhitespace
        from curves in CurveItemParser.SeparatedAtLeastOnce(
            CommonParsers.InlineWhitespace.Then(Char(',')).Then(CommonParsers.InlineWhitespace))
        from ____ in CommonParsers.InlineWhitespace
        from _____ in CommonParsers.LineEnd
        select curves.ToList();

    // Title line
    static Parser<char, string> TitleParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("title")
        from ___ in CommonParsers.RequiredWhitespace
        from title in Token(c => c != '\r' && c != '\n').ManyString()
        from ____ in CommonParsers.LineEnd
        select title.Trim();

    // Skip line
    static Parser<char, Unit> SkipLine =
        Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
            .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

    // Content item
    static Parser<char, object?> ContentItem =>
        OneOf(
            Try(TitleParser.Select(t => (object?)(ItemType.Title, t))),
            Try(AxisParser.Select(a => (object?)(ItemType.Axis, a))),
            Try(CurveLineParser.Select(c => (object?)(ItemType.Curve, c))),
            SkipLine.ThenReturn((object?)null)
        );

    enum ItemType { Title, Axis, Curve }

    public static Parser<char, RadarModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("radar-beta")
        from ___ in CommonParsers.InlineWhitespace
        from ____ in CommonParsers.LineEnd
        from result in ContentItem.ManyThen(End)
        select BuildModel(result.Item1.Where(c => c != null).ToList());

    static RadarModel BuildModel(List<object?> content)
    {
        var model = new RadarModel();

        foreach (var item in content)
        {
            switch (item)
            {
                case (ItemType.Title, string title):
                    model.Title = title;
                    break;

                case (ItemType.Axis, List<RadarAxis> axes):
                    foreach (var axis in axes)
                        model.Axes.Add(axis);
                    break;

                case (ItemType.Curve, List<RadarCurve> curves):
                    foreach (var curve in curves)
                        model.Curves.Add(curve);
                    break;
            }
        }

        return model;
    }

    public Result<char, RadarModel> Parse(string input) => Parser.Parse(input);
}

static class RadarCurveExtensions
{
    public static RadarCurve WithValues(this RadarCurve curve, List<double> values)
    {
        foreach (var v in values)
            curve.Values.Add(v);
        return curve;
    }
}
