using System.Globalization;
using MermaidSharp.Core;
using MermaidSharp.Core.Parsing;
using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace MermaidSharp.Diagrams.XYChart;

public class XYChartParser : IDiagramParser<XYChartModel>
{
    public DiagramType DiagramType => DiagramType.XYChart;

    // Rest of line (for text content)
    static readonly Parser<char, string> RestOfLine =
        Token(c => c != '\r' && c != '\n').ManyString();

    // Quoted string
    static readonly Parser<char, string> QuotedString =
        Char('"').Then(Token(c => c != '"').ManyString()).Before(Char('"'));

    // Title: title "My Chart" or title My Chart
    static readonly Parser<char, string> TitleParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("title")
        from ___ in CommonParsers.RequiredWhitespace
        from title in QuotedString.Or(RestOfLine)
        from ____ in CommonParsers.LineEnd
        select title.Trim();

    // Number parser
    static readonly Parser<char, double> NumberParser =
        from sign in Char('-').Optional()
        from integer in Digit.AtLeastOnceString()
        from frac in Char('.').Then(Digit.AtLeastOnceString()).Optional()
        select double.Parse(
            (sign.HasValue ? "-" : "") + integer + (frac.HasValue ? "." + frac.Value : ""),
            CultureInfo.InvariantCulture);

    // Category item (unquoted or quoted)
    static readonly Parser<char, string> CategoryItem =
        QuotedString.Or(
            Token(c => c != ',' && c != ']' && c != '\r' && c != '\n').AtLeastOnceString()
                .Select(s => s.Trim()));

    // Category list: [jan, feb, mar] or ["Jan", "Feb", "Mar"]
    static readonly Parser<char, List<string>> CategoryListParser =
        from _ in Char('[')
        from __ in CommonParsers.InlineWhitespace
        from items in CategoryItem.SeparatedAtLeastOnce(
            CommonParsers.InlineWhitespace.Then(Char(',')).Then(CommonParsers.InlineWhitespace))
        from ___ in CommonParsers.InlineWhitespace
        from ____ in Char(']')
        select items.ToList();

    // X-axis: x-axis [cat1, cat2] or x-axis "Label" [cat1, cat2]
    static readonly Parser<char, (string label, List<string> categories)> XAxisParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("x-axis")
        from ___ in CommonParsers.RequiredWhitespace
        from label in Try(QuotedString.Before(CommonParsers.RequiredWhitespace)).Optional()
        from categories in CategoryListParser
        from ____ in CommonParsers.InlineWhitespace
        from _____ in CommonParsers.LineEnd
        select (label.GetValueOrDefault() ?? "", categories);

    // Y-axis: y-axis "Label" min --> max or y-axis min --> max
    static readonly Parser<char, (string label, double min, double max)> YAxisParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("y-axis")
        from ___ in CommonParsers.RequiredWhitespace
        from label in Try(QuotedString.Before(CommonParsers.RequiredWhitespace)).Optional()
        from range in Try(
            from min in NumberParser
            from ____ in CommonParsers.InlineWhitespace
            from arrow in String("-->")
            from _____ in CommonParsers.InlineWhitespace
            from max in NumberParser
            select (min, max)
        ).Optional()
        from ______ in CommonParsers.InlineWhitespace
        from _______ in CommonParsers.LineEnd
        select (label.GetValueOrDefault() ?? "",
                range.HasValue ? range.Value.min : 0,
                range.HasValue ? range.Value.max : 100);

    // Data list: [100, 200, 300]
    static readonly Parser<char, List<double>> DataListParser =
        from _ in Char('[')
        from __ in CommonParsers.InlineWhitespace
        from items in NumberParser.SeparatedAtLeastOnce(
            CommonParsers.InlineWhitespace.Then(Char(',')).Then(CommonParsers.InlineWhitespace))
        from ___ in CommonParsers.InlineWhitespace
        from ____ in Char(']')
        select items.ToList();

    // Bar series: bar [100, 200, 300]
    static readonly Parser<char, ChartSeries> BarParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("bar")
        from ___ in CommonParsers.RequiredWhitespace
        from data in DataListParser
        from ____ in CommonParsers.InlineWhitespace
        from _____ in CommonParsers.LineEnd
        select new ChartSeries { Type = ChartSeriesType.Bar, Data = data };

    // Line series: line [100, 200, 300]
    static readonly Parser<char, ChartSeries> LineParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("line")
        from ___ in CommonParsers.RequiredWhitespace
        from data in DataListParser
        from ____ in CommonParsers.InlineWhitespace
        from _____ in CommonParsers.LineEnd
        select new ChartSeries { Type = ChartSeriesType.Line, Data = data };

    // Skip line (comments, empty lines)
    static readonly Parser<char, Unit> SkipLine =
        Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
            .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

    // Content item
    static Parser<char, object?> ContentItem =>
        OneOf(
            Try(TitleParser.Select(t => (object?)("title", t))),
            Try(XAxisParser.Select(x => (object?)("x-axis", x.label, x.categories))),
            Try(YAxisParser.Select(y => (object?)("y-axis", y.label, y.min, y.max))),
            Try(BarParser.Select(s => (object?)("series", s))),
            Try(LineParser.Select(s => (object?)("series", s))),
            SkipLine.ThenReturn((object?)null)
        );

    public Parser<char, XYChartModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from __ in OneOf(CIString("xychart-beta"), CIString("xychart"))
        from ___ in CommonParsers.InlineWhitespace
        from ____ in CommonParsers.LineEnd
        from result in ContentItem.ManyThen(End)
        select BuildModel(result.Item1.Where(c => c != null).ToList());

    static XYChartModel BuildModel(List<object?> content)
    {
        var model = new XYChartModel();

        foreach (var item in content)
        {
            switch (item)
            {
                case (string key, string value) when key == "title":
                    model.Title = value;
                    break;

                case (string key, string label, List<string> categories) when key == "x-axis":
                    model.XAxisLabel = string.IsNullOrEmpty(label) ? null : label;
                    model.XAxisCategories.AddRange(categories);
                    break;

                case (string key, string label, double min, double max) when key == "y-axis":
                    model.YAxisLabel = string.IsNullOrEmpty(label) ? null : label;
                    model.YAxisMin = min;
                    model.YAxisMax = max;
                    break;

                case (string key, ChartSeries series) when key == "series":
                    model.Series.Add(series);
                    break;
            }
        }

        return model;
    }

    public Result<char, XYChartModel> Parse(string input) => Parser.Parse(input);
}
