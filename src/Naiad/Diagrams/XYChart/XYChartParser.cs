class XYChartParser : IDiagramParser<XYChartModel>
{
    // Rest of line (for text content)
    static readonly Parser<char, string> restOfLine;

    // Quoted string
    static readonly Parser<char, string> quotedString;

    // Title: title "My Chart" or title My Chart
    static readonly Parser<char, string> titleParser;

    // Number parser
    static readonly Parser<char, double> numberParser;

    // Category item (unquoted or quoted)
    static readonly Parser<char, string> categoryItem;

    // Category list: [jan, feb, mar] or ["Jan", "Feb", "Mar"]
    static readonly Parser<char, List<string>> categoryListParser;

    // X-axis: x-axis [cat1, cat2] or x-axis "Label" [cat1, cat2]
    static readonly Parser<char, (string label, List<string> categories)> xAxisParser;

    // Y-axis: y-axis "Label" min --> max or y-axis min --> max
    static readonly Parser<char, (string label, double min, double max)> yAxisParser;

    // Data list: [100, 200, 300]
    static readonly Parser<char, List<double>> dataListParser;

    // Bar series: bar [100, 200, 300]
    static readonly Parser<char, ChartSeries> barParser;

    // Line series: line [100, 200, 300]
    static readonly Parser<char, ChartSeries> lineParser;

    // Skip line (comments, empty lines)
    static readonly Parser<char, Unit> skipLine;

    static readonly Parser<char, IXYContent?> ContentItem;

    static readonly Parser<char, XYChartModel> parser;

    static XYChartParser()
    {
        restOfLine =
            Token(_ => _ != '\r' && _ != '\n').ManyString();

        quotedString =
            Char('"').Then(Token(_ => _ != '"').ManyString()).Before(Char('"'));

        titleParser =
            from _ in CommonParsers.InlineWhitespace
            from __ in CIString("title")
            from ___ in CommonParsers.RequiredWhitespace
            from title in quotedString.Or(restOfLine)
            from ____ in CommonParsers.LineEnd
            select title.Trim();

        numberParser = CommonParsers.SignedDecimal;

        categoryItem =
            quotedString.Or(
                Token(_ => _ != ',' && _ != ']' && _ != '\r' && _ != '\n').AtLeastOnceString()
                    .Select(_ => _.Trim()));

        categoryListParser =
            from _ in Char('[')
            from __ in CommonParsers.InlineWhitespace
            from items in categoryItem.SeparatedAtLeastOnce(
                CommonParsers.InlineWhitespace.Then(Char(',')).Then(CommonParsers.InlineWhitespace))
            from ___ in CommonParsers.InlineWhitespace
            from ____ in Char(']')
            select items.ToList();

        xAxisParser =
            from _ in CommonParsers.InlineWhitespace
            from __ in CIString("x-axis")
            from ___ in CommonParsers.RequiredWhitespace
            from label in Try(quotedString.Before(CommonParsers.RequiredWhitespace)).Optional()
            from categories in categoryListParser
            from ____ in CommonParsers.InlineWhitespace
            from _____ in CommonParsers.LineEnd
            select (label.GetValueOrDefault() ?? "", categories);

        yAxisParser =
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

        dataListParser =
            from _ in Char('[')
            from __ in CommonParsers.InlineWhitespace
            from items in numberParser.SeparatedAtLeastOnce(
                CommonParsers.InlineWhitespace.Then(Char(',')).Then(CommonParsers.InlineWhitespace))
            from ___ in CommonParsers.InlineWhitespace
            from ____ in Char(']')
            select items.ToList();

        barParser =
            from _ in CommonParsers.InlineWhitespace
            from __ in CIString("bar")
            from ___ in CommonParsers.RequiredWhitespace
            from data in dataListParser
            from ____ in CommonParsers.InlineWhitespace
            from _____ in CommonParsers.LineEnd
            select new ChartSeries { Type = ChartSeriesType.Bar, Data = data };

        lineParser =
            from _ in CommonParsers.InlineWhitespace
            from __ in CIString("line")
            from ___ in CommonParsers.RequiredWhitespace
            from data in dataListParser
            from ____ in CommonParsers.InlineWhitespace
            from _____ in CommonParsers.LineEnd
            select new ChartSeries { Type = ChartSeriesType.Line, Data = data };

        skipLine =
            Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
                .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

        ContentItem =
            OneOf(
                Try(titleParser.Select<IXYContent?>(_ => new TitleItem(_))),
                Try(xAxisParser.Select<IXYContent?>(_ => new XAxisItem(_.label, _.categories))),
                Try(yAxisParser.Select<IXYContent?>(_ => new YAxisItem(_.label, _.min, _.max))),
                Try(barParser.Select<IXYContent?>(_ => new SeriesItem(_))),
                Try(lineParser.Select<IXYContent?>(_ => new SeriesItem(_))),
                skipLine.ThenReturn<IXYContent?>(null)
            );

        parser =
            from _ in CommonParsers.InlineWhitespace
            from __ in OneOf(CIString("xychart-beta"), CIString("xychart"))
            from ___ in CommonParsers.InlineWhitespace
            from ____ in CommonParsers.LineEnd
            from result in ContentItem.ManyThen(End)
            select BuildModel(result.Item1);
    }

    static XYChartModel BuildModel(IEnumerable<IXYContent?> content)
    {
        var model = new XYChartModel();

        foreach (var item in content)
        {
            switch (item)
            {
                case TitleItem title:
                    model.Title = title.Value;
                    break;

                case XAxisItem xAxis:
                    model.XAxisLabel = string.IsNullOrEmpty(xAxis.Label) ? null : xAxis.Label;
                    model.XAxisCategories.AddRange(xAxis.Categories);
                    break;

                case YAxisItem yAxis:
                    model.YAxisLabel = string.IsNullOrEmpty(yAxis.Label) ? null : yAxis.Label;
                    model.YAxisMin = yAxis.Min;
                    model.YAxisMax = yAxis.Max;
                    break;

                case SeriesItem series:
                    model.Series.Add(series.Series);
                    break;
            }
        }

        return model;
    }

    public Result<char, XYChartModel> Parse(string input) => parser.Parse(input);

    interface IXYContent;
    readonly record struct TitleItem(string Value) : IXYContent;
    readonly record struct XAxisItem(string Label, List<string> Categories) : IXYContent;
    readonly record struct YAxisItem(string Label, double Min, double Max) : IXYContent;
    readonly record struct SeriesItem(ChartSeries Series) : IXYContent;
}
