class QuadrantParser : IDiagramParser<QuadrantModel>
{
    static Parser<char, string> restOfLine =
        Token(_ => _ != '\r' && _ != '\n').ManyString();

    // Title: title My Chart
    static Parser<char, string> titleParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("title")
        from ___ in CommonParsers.RequiredWhitespace
        from title in restOfLine
        from ____ in CommonParsers.LineEnd
        select title.Trim();

    // X-axis: x-axis Low --> High
    static Parser<char, (string left, string right)> xAxisParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("x-axis")
        from ___ in CommonParsers.RequiredWhitespace
        from left in Token(_ => _ is not '-').AtLeastOnceString()
            .Where(s => !s.TrimEnd().EndsWith('-'))
            .Or(Token(_ => _ != '\r' && _ != '\n' && _ != '-').ManyString())
        from arrow in String("-->")
        from ____ in CommonParsers.InlineWhitespace
        from right in restOfLine
        from _____ in CommonParsers.LineEnd
        select (left.Trim().TrimEnd('-').Trim(), right.Trim());

    // Y-axis: y-axis Low --> High
    static Parser<char, (string bottom, string top)> yAxisParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("y-axis")
        from ___ in CommonParsers.RequiredWhitespace
        from bottom in Token(_ => _ is not '-').AtLeastOnceString()
            .Where(s => !s.TrimEnd().EndsWith('-'))
            .Or(Token(_ => _ != '\r' && _ != '\n' && _ != '-').ManyString())
        from arrow in String("-->")
        from ____ in CommonParsers.InlineWhitespace
        from top in restOfLine
        from _____ in CommonParsers.LineEnd
        select (bottom.Trim().TrimEnd('-').Trim(), top.Trim());

    // Quadrant labels: quadrant-1 Label
    static Parser<char, (int quadrant, string label)> quadrantLabelParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("quadrant-")
        from num in Digit.Select(_ => _ - '0')
        from ___ in CommonParsers.RequiredWhitespace
        from label in restOfLine
        from ____ in CommonParsers.LineEnd
        select (num, label.Trim());

    // Number parser for coordinates
    static Parser<char, double> numberParser =
        from sign in Char('-').Optional()
        from integer in Digit.AtLeastOnceString()
        from frac in Char('.').Then(Digit.AtLeastOnceString()).Optional()
        select double.Parse(
            (sign.HasValue ? "-" : "") + integer + (frac.HasValue ? "." + frac.Value : ""),
            CultureInfo.InvariantCulture);

    // Point: Name: [0.5, 0.7]
    static Parser<char, QuadrantPoint> pointParser =
        from _ in CommonParsers.InlineWhitespace
        from name in Token(_ => _ != ':' && _ != '\r' && _ != '\n').AtLeastOnceString()
        from __ in Char(':')
        from ___ in CommonParsers.InlineWhitespace
        from ____ in Char('[')
        from _____ in CommonParsers.InlineWhitespace
        from x in numberParser
        from ______ in CommonParsers.InlineWhitespace
        from _______ in Char(',')
        from ________ in CommonParsers.InlineWhitespace
        from y in numberParser
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
    static Parser<char, Unit> skipLine =
        Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
            .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

    public static Parser<char, IQuadrantContent?> ContentItem =>
        OneOf(
            Try(titleParser.Select<IQuadrantContent?>(_ => new TitleItem(_))),
            Try(xAxisParser.Select<IQuadrantContent?>(_ => new XAxisItem(_.left, _.right))),
            Try(yAxisParser.Select<IQuadrantContent?>(_ => new YAxisItem(_.bottom, _.top))),
            Try(quadrantLabelParser.Select<IQuadrantContent?>(_ => new QuadrantLabelItem(_.quadrant, _.label))),
            Try(pointParser.Select<IQuadrantContent?>(_ => new PointItem(_))),
            skipLine.ThenReturn<IQuadrantContent?>(null)
        );

    public static Parser<char, QuadrantModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("quadrantChart")
        from ___ in CommonParsers.InlineWhitespace
        from ____ in CommonParsers.LineEnd
        from result in ContentItem.ManyThen(End)
        select BuildModel(result.Item1);

    static QuadrantModel BuildModel(IEnumerable<IQuadrantContent?> content)
    {
        var model = new QuadrantModel();

        foreach (var item in content)
        {
            switch (item)
            {
                case TitleItem title:
                    model.Title = title.Value;
                    break;

                case XAxisItem xAxis:
                    model.XAxisLeft = xAxis.Left;
                    model.XAxisRight = xAxis.Right;
                    break;

                case YAxisItem yAxis:
                    model.YAxisBottom = yAxis.Bottom;
                    model.YAxisTop = yAxis.Top;
                    break;

                case QuadrantLabelItem label:
                    switch (label.Quadrant)
                    {
                        case 1: model.Quadrant1Label = label.Label; break;
                        case 2: model.Quadrant2Label = label.Label; break;
                        case 3: model.Quadrant3Label = label.Label; break;
                        case 4: model.Quadrant4Label = label.Label; break;
                    }
                    break;

                case PointItem point:
                    model.Points.Add(point.Point);
                    break;
            }
        }

        return model;
    }

    public Result<char, QuadrantModel> Parse(string input) => Parser.Parse(input);

    internal interface IQuadrantContent;
    readonly record struct TitleItem(string Value) : IQuadrantContent;
    readonly record struct XAxisItem(string Left, string Right) : IQuadrantContent;
    readonly record struct YAxisItem(string Bottom, string Top) : IQuadrantContent;
    readonly record struct QuadrantLabelItem(int Quadrant, string Label) : IQuadrantContent;
    readonly record struct PointItem(QuadrantPoint Point) : IQuadrantContent;
}
