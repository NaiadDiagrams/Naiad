namespace MermaidSharp.Diagrams.Radar;

public class RadarParser : IDiagramParser<RadarModel>
{
    public DiagramType DiagramType => DiagramType.Radar;

    // identifier
    static Parser<char, string> identifier =
        Token(_ => char.IsLetterOrDigit(_) || _ == '_' || _ == '-').AtLeastOnceString();

    // Number
    static Parser<char, double> number =
        from neg in Char('-').Optional()
        from digits in Digit.AtLeastOnceString()
        from dec in Char('.').Then(Digit.AtLeastOnceString()).Optional()
        select double.Parse((neg.HasValue ? "-" : "") + digits + (dec.HasValue ? "." + dec.Value : ""));

    // Quoted label: ["label"]
    static Parser<char, string> quotedLabel =
        Char('[').Then(Char('"')).Then(Token(_ => _ != '"').ManyString()).Before(Char('"')).Before(Char(']'));

    // Axis list: axis id1, id2, id3
    static Parser<char, List<RadarAxis>> axisParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("axis")
        from ___ in CommonParsers.RequiredWhitespace
        from axes in identifier.SeparatedAtLeastOnce(
            CommonParsers.InlineWhitespace.Then(Char(',')).Then(CommonParsers.InlineWhitespace))
        from ____ in CommonParsers.InlineWhitespace
        from _____ in CommonParsers.LineEnd
        select axes.Select(_ => new RadarAxis { Id = _, Label = _ }).ToList();

    // Value list: {1, 2, 3}
    static Parser<char, List<double>> valueList =
        Char('{')
            .Then(CommonParsers.InlineWhitespace)
            .Then(number.SeparatedAtLeastOnce(
                CommonParsers.InlineWhitespace.Then(Char(',')).Then(CommonParsers.InlineWhitespace)))
            .Before(CommonParsers.InlineWhitespace)
            .Before(Char('}'))
            .Select(_ => _.ToList());

    // Curve definition: curve id["label"]{1, 2, 3}
    static Parser<char, RadarCurve> curveItemParser =
        from id in identifier
        from label in quotedLabel.Optional()
        from values in valueList
        select new RadarCurve
        {
            Id = id,
            Label = label.GetValueOrDefault() ?? id
        }.WithValues(values);

    // Curve line: curve id1["label"]{1, 2, 3}, id2{4, 5, 6}
    static Parser<char, List<RadarCurve>> curveLineParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("curve")
        from ___ in CommonParsers.RequiredWhitespace
        from curves in curveItemParser.SeparatedAtLeastOnce(
            CommonParsers.InlineWhitespace.Then(Char(',')).Then(CommonParsers.InlineWhitespace))
        from ____ in CommonParsers.InlineWhitespace
        from _____ in CommonParsers.LineEnd
        select curves.ToList();

    // Title line
    static Parser<char, string> titleParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("title")
        from ___ in CommonParsers.RequiredWhitespace
        from title in Token(_ => _ != '\r' && _ != '\n').ManyString()
        from ____ in CommonParsers.LineEnd
        select title.Trim();

    // Skip line
    static Parser<char, Unit> skipLine =
        Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
            .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

    // Content item
    static Parser<char, object?> ContentItem =>
        OneOf(
            Try(titleParser.Select(_ => (object?)(ItemType.Title, _))),
            Try(axisParser.Select(_ => (object?)(ItemType.Axis, _))),
            Try(curveLineParser.Select(_ => (object?)(ItemType.Curve, _))),
            skipLine.ThenReturn((object?)null)
        );

    enum ItemType { Title, Axis, Curve }

    public static Parser<char, RadarModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("radar-beta")
        from ___ in CommonParsers.InlineWhitespace
        from ____ in CommonParsers.LineEnd
        from result in ContentItem.ManyThen(End)
        select BuildModel(result.Item1.Where(_ => _ != null).ToList());

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
