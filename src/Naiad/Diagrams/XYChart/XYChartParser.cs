namespace MermaidSharp.Diagrams.XYChart;

public class XYChartParser : IDiagramParser<XYChartModel>
{
    public DiagramType DiagramType => DiagramType.XYChart;

    // Rest of line (for text content)
    static Parser<char, string> restOfLine =
        Token(_ => _ != '\r' && _ != '\n').ManyString();

    // Quoted string
    static Parser<char, string> quotedString =
        Char('"').Then(Token(_ => _ != '"').ManyString()).Before(Char('"'));

    // Title: title "My Chart" or title My Chart
    static Parser<char, string> titleParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("title")
        from ___ in CommonParsers.RequiredWhitespace
        from title in quotedString.Or(restOfLine)
        from ____ in CommonParsers.LineEnd
        select title.Trim();

    // Number parser
    static Parser<char, double> numberParser =
        from sign in Char('-').Optional()
        from integer in Digit.AtLeastOnceString()
        from frac in Char('.').Then(Digit.AtLeastOnceString()).Optional()
        select double.Parse(
            (sign.HasValue ? "-" : "") + integer + (frac.HasValue ? "." + frac.Value : ""),
            CultureInfo.InvariantCulture);

    // Category item (unquoted or quoted)
    static Parser<char, string> categoryItem =
        quotedString.Or(
            Token(_ => _ != ',' && _ != ']' && _ != '\r' && _ != '\n').AtLeastOnceString()
                .Select(_ => _.Trim()));

    // Category list: [jan, feb, mar] or ["Jan", "Feb", "Mar"]
    static Parser<char, List<string>> categoryListParser =
        from _ in Char('[')
        from __ in CommonParsers.InlineWhitespace
        from items in categoryItem.SeparatedAtLeastOnce(
            CommonParsers.InlineWhitespace.Then(Char(',')).Then(CommonParsers.InlineWhitespace))
        from ___ in CommonParsers.InlineWhitespace
        from ____ in Char(']')
        select items.ToList();

    // X-axis: x-axis [cat1, cat2] or x-axis "Label" [cat1, cat2]
    static Parser<char, (string label, List<string> categories)> xAxisParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("x-axis")
        from ___ in CommonParsers.RequiredWhitespace
        from label in Try(quotedString.Before(CommonParsers.RequiredWhitespace)).Optional()
        from categories in categoryListParser
        from ____ in CommonParsers.InlineWhitespace
        from _____ in CommonParsers.LineEnd
        select (label.GetValueOrDefault() ?? "", categories);

    // Y-axis: y-axis "Label" min --> max or y-axis min --> max
    static Parser<char, (string label, double min, double max)> yAxisParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("y-axis")
        from ___ in CommonParsers.RequiredWhitespace
        from label in Try(quotedString.Before(CommonParsers.RequiredWhitespace)).Optional()
        from range in Try(
            from min in numberParser
            from ____ in CommonParsers.InlineWhitespace
            from arrow in String("-->")
            from _____ in CommonParsers.InlineWhitespace
            from max in numberParser
            select (min, max)
        ).Optional()
        from ______ in CommonParsers.InlineWhitespace
        from _______ in CommonParsers.LineEnd
        select (label.GetValueOrDefault() ?? "",
                range.HasValue ? range.Value.min : 0,
                range.HasValue ? range.Value.max : 100);

    // Data list: [100, 200, 300]
    static Parser<char, List<double>> dataListParser =
        from _ in Char('[')
        from __ in CommonParsers.InlineWhitespace
        from items in numberParser.SeparatedAtLeastOnce(
            CommonParsers.InlineWhitespace.Then(Char(',')).Then(CommonParsers.InlineWhitespace))
        from ___ in CommonParsers.InlineWhitespace
        from ____ in Char(']')
        select items.ToList();

    // Bar series: bar [100, 200, 300]
    static Parser<char, ChartSeries> barParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("bar")
        from ___ in CommonParsers.RequiredWhitespace
        from data in dataListParser
        from ____ in CommonParsers.InlineWhitespace
        from _____ in CommonParsers.LineEnd
        select new ChartSeries { Type = ChartSeriesType.Bar, Data = data };

    // Line series: line [100, 200, 300]
    static Parser<char, ChartSeries> lineParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("line")
        from ___ in CommonParsers.RequiredWhitespace
        from data in dataListParser
        from ____ in CommonParsers.InlineWhitespace
        from _____ in CommonParsers.LineEnd
        select new ChartSeries { Type = ChartSeriesType.Line, Data = data };

    // Skip line (comments, empty lines)
    static Parser<char, Unit> skipLine =
        Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
            .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

    // Content item
    static Parser<char, object?> ContentItem =>
        OneOf(
            Try(titleParser.Select(_ => (object?)("title", _))),
            Try(xAxisParser.Select(_ => (object?)("x-axis", _.label, _.categories))),
            Try(yAxisParser.Select(_ => (object?)("y-axis", _.label, _.min, _.max))),
            Try(barParser.Select(_ => (object?)("series", _))),
            Try(lineParser.Select(_ => (object?)("series", _))),
            skipLine.ThenReturn((object?)null)
        );

    public static Parser<char, XYChartModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from __ in OneOf(CIString("xychart-beta"), CIString("xychart"))
        from ___ in CommonParsers.InlineWhitespace
        from ____ in CommonParsers.LineEnd
        from result in ContentItem.ManyThen(End)
        select BuildModel(result.Item1.Where(_ => _ != null).ToList());

    static XYChartModel BuildModel(List<object?> content)
    {
        var model = new XYChartModel();

        foreach (var item in content)
        {
            switch (item)
            {
                case ("title", string value):
                    model.Title = value;
                    break;

                case ("x-axis", string label, List<string> categories):
                    model.XAxisLabel = string.IsNullOrEmpty(label) ? null : label;
                    model.XAxisCategories.AddRange(categories);
                    break;

                case ("y-axis", string label, double min, double max):
                    model.YAxisLabel = string.IsNullOrEmpty(label) ? null : label;
                    model.YAxisMin = min;
                    model.YAxisMax = max;
                    break;

                case ("series", ChartSeries series):
                    model.Series.Add(series);
                    break;
            }
        }

        return model;
    }

    public Result<char, XYChartModel> Parse(string input) => Parser.Parse(input);
}
