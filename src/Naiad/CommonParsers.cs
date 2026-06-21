namespace Naiad;

public static class CommonParsers
{
    // Whitespace
    public static Parser<char, Unit> RequiredWhitespace =
        Token(char.IsWhiteSpace).SkipAtLeastOnce();

    public static Parser<char, Unit> InlineWhitespace =
        Token(_ => _ is ' ' or '\t').SkipMany();

    // Line handling
    public static Parser<char, Unit> Newline =
        Try(String("\r\n")).Or(String("\n")).ThenReturn(Unit.Value);

    public static Parser<char, Unit> LineEnd =
        Newline.Or(End);

    // Comments (Mermaid uses %% for comments)
    public static Parser<char, Unit> Comment =
        String("%%")
            .Then(Token(_ => _ != '\r' && _ != '\n').SkipMany())
            .Then(LineEnd.Optional())
            .ThenReturn(Unit.Value);

    // Identifiers
    public static Parser<char, string> Identifier =
        Token(_ => char.IsLetterOrDigit(_) || _ == '_' || _ == '-')
            .AtLeastOnceString()
            .Labelled("identifier");

    // Quoted strings
    public static Parser<char, string> DoubleQuotedString =
        Char('"')
            .Then(Token(_ => _ != '"').ManyString())
            .Before(Char('"'))
            .Labelled("double-quoted string");

    static Parser<char, string> SingleQuotedString =
        Char('\'')
            .Then(Token(_ => _ != '\'').ManyString())
            .Before(Char('\''))
            .Labelled("single-quoted string");

    public static Parser<char, string> QuotedString =
        DoubleQuotedString.Or(SingleQuotedString);

    // Numbers
    public static Parser<char, double> Number =
        Real.Labelled("number");

    public static Parser<char, int> Integer =
        Num.Labelled("integer");

    // Mermaid's numeric token [+-]?(?:\d+(?:\.\d+)?|\.\d+): an optional sign, then digits with an
    // optional fraction, or a bare-dot fraction like .5. No exponent (Mermaid rejects 1e5) and the
    // parse is culture-invariant. Shared by the value-bearing diagrams (xychart, sankey, radar, treemap).
    public static Parser<char, double> SignedDecimal =
        (from sign in OneOf(Char('+'), Char('-')).Optional()
         from magnitude in
             (from integer in Digit.AtLeastOnceString()
              from fraction in Try(Char('.').Then(Digit.AtLeastOnceString())).Optional()
              select integer + (fraction.HasValue ? "." + fraction.Value : ""))
             .Or(
                 from _ in Char('.')
                 from fraction in Digit.AtLeastOnceString()
                 select "." + fraction)
         select double.Parse(
             (sign is { HasValue: true, Value: '-' } ? "-" : "") + magnitude,
             CultureInfo.InvariantCulture))
        .Labelled("number");

    // Direction parsing (TB, BT, LR, RL, TD)
    public static Parser<char, Direction> DirectionParser =
        OneOf(
            Try(String("TB")).ThenReturn(Direction.TopToBottom),
            Try(String("TD")).ThenReturn(Direction.TopToBottom),
            Try(String("BT")).ThenReturn(Direction.BottomToTop),
            Try(String("LR")).ThenReturn(Direction.LeftToRight),
            String("RL").ThenReturn(Direction.RightToLeft)
        ).Labelled("direction");

    // Indentation for hierarchical diagrams (mindmap, timeline)
    public static Parser<char, int> Indentation =
        Token(_ => _ is ' ' or '\t')
            .Many()
            .Select(chars =>
            {
                var level = 0;
                foreach (var c in chars)
                {
                    level += c == '\t' ? 4 : 1;
                }

                return level;
            });
}
