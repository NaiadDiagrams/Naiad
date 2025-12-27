using System.Globalization;
using MermaidSharp;
using MermaidSharp.Parsing;
using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace MermaidSharp.Diagrams.Quadrant;

public class QuadrantParser : IDiagramParser<QuadrantModel>
{
    public DiagramType DiagramType => DiagramType.Quadrant;

    // Rest of line (for text content)
    static readonly Parser<char, string> RestOfLine =
        Token(c => c != '\r' && c != '\n').ManyString();

    // Title: title My Chart
    static readonly Parser<char, string> TitleParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("title")
        from ___ in CommonParsers.RequiredWhitespace
        from title in RestOfLine
        from ____ in CommonParsers.LineEnd
        select title.Trim();

    // X-axis: x-axis Low --> High
    static readonly Parser<char, (string left, string right)> XAxisParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("x-axis")
        from ___ in CommonParsers.RequiredWhitespace
        from left in Token(c => c != '-' || c == ' ').AtLeastOnceString()
            .Where(s => !s.TrimEnd().EndsWith("-"))
            .Or(Token(c => c != '\r' && c != '\n' && c != '-').ManyString())
        from arrow in String("-->")
        from ____ in CommonParsers.InlineWhitespace
        from right in RestOfLine
        from _____ in CommonParsers.LineEnd
        select (left.Trim().TrimEnd('-').Trim(), right.Trim());

    // Y-axis: y-axis Low --> High
    static readonly Parser<char, (string bottom, string top)> YAxisParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("y-axis")
        from ___ in CommonParsers.RequiredWhitespace
        from bottom in Token(c => c != '-' || c == ' ').AtLeastOnceString()
            .Where(s => !s.TrimEnd().EndsWith("-"))
            .Or(Token(c => c != '\r' && c != '\n' && c != '-').ManyString())
        from arrow in String("-->")
        from ____ in CommonParsers.InlineWhitespace
        from top in RestOfLine
        from _____ in CommonParsers.LineEnd
        select (bottom.Trim().TrimEnd('-').Trim(), top.Trim());

    // Quadrant labels: quadrant-1 Label
    static readonly Parser<char, (int quadrant, string label)> QuadrantLabelParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("quadrant-")
        from num in Digit.Select(c => c - '0')
        from ___ in CommonParsers.RequiredWhitespace
        from label in RestOfLine
        from ____ in CommonParsers.LineEnd
        select (num, label.Trim());

    // Number parser for coordinates
    static readonly Parser<char, double> NumberParser =
        from sign in Char('-').Optional()
        from integer in Digit.AtLeastOnceString()
        from frac in Char('.').Then(Digit.AtLeastOnceString()).Optional()
        select double.Parse(
            (sign.HasValue ? "-" : "") + integer + (frac.HasValue ? "." + frac.Value : ""),
            CultureInfo.InvariantCulture);

    // Point: Name: [0.5, 0.7]
    static readonly Parser<char, QuadrantPoint> PointParser =
        from _ in CommonParsers.InlineWhitespace
        from name in Token(c => c != ':' && c != '\r' && c != '\n').AtLeastOnceString()
        from __ in Char(':')
        from ___ in CommonParsers.InlineWhitespace
        from ____ in Char('[')
        from _____ in CommonParsers.InlineWhitespace
        from x in NumberParser
        from ______ in CommonParsers.InlineWhitespace
        from _______ in Char(',')
        from ________ in CommonParsers.InlineWhitespace
        from y in NumberParser
        from _________ in CommonParsers.InlineWhitespace
        from __________ in Char(']')
        from ___________ in CommonParsers.InlineWhitespace
        from ____________ in CommonParsers.LineEnd
        select new QuadrantPoint
        {
            Name = name.Trim(),
            X = Math.Clamp(x, 0, 1),
            Y = Math.Clamp(y, 0, 1)
        };

    // Skip line (comments, empty lines)
    static readonly Parser<char, Unit> SkipLine =
        Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
            .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

    // Content item
    static Parser<char, object?> ContentItem =>
        OneOf(
            Try(TitleParser.Select(t => (object?)("title", t))),
            Try(XAxisParser.Select(x => (object?)("x-axis", x.left, x.right))),
            Try(YAxisParser.Select(y => (object?)("y-axis", y.bottom, y.top))),
            Try(QuadrantLabelParser.Select(q => (object?)("quadrant", q.quadrant, q.label))),
            Try(PointParser.Select(p => (object?)("point", p))),
            SkipLine.ThenReturn((object?)null)
        );

    public Parser<char, QuadrantModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("quadrantChart")
        from ___ in CommonParsers.InlineWhitespace
        from ____ in CommonParsers.LineEnd
        from result in ContentItem.ManyThen(End)
        select BuildModel(result.Item1.Where(c => c != null).ToList());

    static QuadrantModel BuildModel(List<object?> content)
    {
        var model = new QuadrantModel();

        foreach (var item in content)
        {
            switch (item)
            {
                case (string key, string value) when key == "title":
                    model.Title = value;
                    break;

                case (string key, string left, string right) when key == "x-axis":
                    model.XAxisLeft = left;
                    model.XAxisRight = right;
                    break;

                case (string key, string bottom, string top) when key == "y-axis":
                    model.YAxisBottom = bottom;
                    model.YAxisTop = top;
                    break;

                case (string key, int quadrant, string label) when key == "quadrant":
                    switch (quadrant)
                    {
                        case 1: model.Quadrant1Label = label; break;
                        case 2: model.Quadrant2Label = label; break;
                        case 3: model.Quadrant3Label = label; break;
                        case 4: model.Quadrant4Label = label; break;
                    }
                    break;

                case (string key, QuadrantPoint point) when key == "point":
                    model.Points.Add(point);
                    break;
            }
        }

        return model;
    }

    public Result<char, QuadrantModel> Parse(string input) => Parser.Parse(input);
}
