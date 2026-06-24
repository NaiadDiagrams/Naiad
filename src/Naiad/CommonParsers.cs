namespace Naiad;

public static class CommonParsers
{
    // Whitespace
    public static readonly Parser<char, Unit> RequiredWhitespace;

    public static readonly Parser<char, Unit> InlineWhitespace;

    // Line handling
    public static readonly Parser<char, Unit> Newline;

    public static readonly Parser<char, Unit> LineEnd;

    // Comments (Mermaid uses %% for comments)
    public static readonly Parser<char, Unit> Comment;

    // Identifiers
    public static readonly Parser<char, string> Identifier;

    // Quoted strings
    public static readonly Parser<char, string> DoubleQuotedString;

    static readonly Parser<char, string> SingleQuotedString;

    public static readonly Parser<char, string> QuotedString;

    // Numbers
    public static readonly Parser<char, double> Number;

    public static readonly Parser<char, int> Integer;

    // Mermaid's numeric token [+-]?(?:\d+(?:\.\d+)?|\.\d+): an optional sign, then digits with an
    // optional fraction, or a bare-dot fraction like .5. No exponent (Mermaid rejects 1e5) and the
    // parse is culture-invariant. Shared by the value-bearing diagrams (xychart, sankey, radar, treemap).
    public static readonly Parser<char, double> SignedDecimal;

    // Direction parsing (TB, BT, LR, RL, TD)
    public static readonly Parser<char, Direction> DirectionParser;

    // Indentation for hierarchical diagrams (mindmap, timeline)
    public static readonly Parser<char, int> Indentation;

    static CommonParsers()
    {
        RequiredWhitespace =
            Token(char.IsWhiteSpace).SkipAtLeastOnce();

        InlineWhitespace =
            Token(_ => _ is ' ' or '\t').SkipMany();

        Newline =
            Try(String("\r\n")).Or(String("\n")).ThenReturn(Unit.Value);

        LineEnd =
            Newline.Or(End);

        Comment =
            String("%%")
                .Then(Token(_ => _ != '\r' && _ != '\n').SkipMany())
                .Then(LineEnd.Optional())
                .ThenReturn(Unit.Value);

        Identifier =
            Token(_ => char.IsLetterOrDigit(_) || _ == '_' || _ == '-')
                .AtLeastOnceString()
                .Labelled("identifier");

        DoubleQuotedString =
            Char('"')
                .Then(Token(_ => _ != '"').ManyString())
                .Before(Char('"'))
                .Labelled("double-quoted string");

        SingleQuotedString =
            Char('\'')
                .Then(Token(_ => _ != '\'').ManyString())
                .Before(Char('\''))
                .Labelled("single-quoted string");

        QuotedString =
            DoubleQuotedString.Or(SingleQuotedString);

        Number =
            Real.Labelled("number");

        Integer =
            Num.Labelled("integer");

        SignedDecimal =
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

        DirectionParser =
            OneOf(
                Try(String("TB")).ThenReturn(Direction.TopToBottom),
                Try(String("TD")).ThenReturn(Direction.TopToBottom),
                Try(String("BT")).ThenReturn(Direction.BottomToTop),
                Try(String("LR")).ThenReturn(Direction.LeftToRight),
                String("RL").ThenReturn(Direction.RightToLeft)
            ).Labelled("direction");

        Indentation =
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
}
