namespace MermaidSharp;

public static class CommonParsers
{
    // Whitespace
    public static Parser<char, Unit> OptionalWhitespace =>
        SkipWhitespaces;

    public static Parser<char, Unit> RequiredWhitespace =>
        Token(char.IsWhiteSpace).SkipAtLeastOnce();

    public static Parser<char, Unit> InlineWhitespace =>
        Token(_ => _ is ' ' or '\t').SkipMany();

    // Line handling
    public static Parser<char, Unit> Newline =>
        Try(String("\r\n")).Or(String("\n")).ThenReturn(Unit.Value);

    public static Parser<char, Unit> LineEnd =>
        Newline.Or(End);

    public static Parser<char, Unit> SkipRestOfLine =>
        Token(_ => _ != '\r' && _ != '\n').SkipMany().Then(LineEnd.Optional()).ThenReturn(Unit.Value);

    // Comments (Mermaid uses %% for comments)
    public static Parser<char, Unit> Comment =>
        String("%%")
            .Then(Token(_ => _ != '\r' && _ != '\n').SkipMany())
            .Then(LineEnd.Optional())
            .ThenReturn(Unit.Value);

    public static Parser<char, Unit> SkipCommentsAndWhitespace =>
        Comment.Or(SkipWhitespaces).SkipMany();

    // Identifiers
    public static Parser<char, string> Identifier =>
        Token(_ => char.IsLetterOrDigit(_) || _ == '_' || _ == '-')
            .AtLeastOnceString()
            .Labelled("identifier");

    public static Parser<char, string> AlphanumericIdentifier =>
        Token(char.IsLetterOrDigit)
            .AtLeastOnceString()
            .Labelled("alphanumeric identifier");

    // Quoted strings
    public static Parser<char, string> DoubleQuotedString =>
        Char('"')
            .Then(Token(_ => _ != '"').ManyString())
            .Before(Char('"'))
            .Labelled("double-quoted string");

    public static Parser<char, string> SingleQuotedString =>
        Char('\'')
            .Then(Token(_ => _ != '\'').ManyString())
            .Before(Char('\''))
            .Labelled("single-quoted string");

    public static Parser<char, string> QuotedString =>
        DoubleQuotedString.Or(SingleQuotedString);

    public static Parser<char, string> TextOrQuoted =>
        QuotedString.Or(Identifier);

    // Numbers
    public static Parser<char, double> Number =>
        Real.Labelled("number");

    public static Parser<char, int> Integer =>
        Num.Labelled("integer");

    public static Parser<char, double> Percentage =>
        Real.Before(Char('%').Optional());

    // Direction parsing (TB, BT, LR, RL, TD)
    public static Parser<char, Direction> DirectionParser =>
        OneOf(
            Try(String("TB")).ThenReturn(Direction.TopToBottom),
            Try(String("TD")).ThenReturn(Direction.TopToBottom),
            Try(String("BT")).ThenReturn(Direction.BottomToTop),
            Try(String("LR")).ThenReturn(Direction.LeftToRight),
            String("RL").ThenReturn(Direction.RightToLeft)
        ).Labelled("direction");

    // Arrow parsing for flowcharts
    public static Parser<char, EdgeType> ArrowParser =>
        OneOf(
            Try(String("<-->")).ThenReturn(EdgeType.BiDirectional),
            Try(String("o--o")).ThenReturn(EdgeType.BiDirectionalCircle),
            Try(String("x--x")).ThenReturn(EdgeType.BiDirectionalCross),
            Try(String("-.->")).ThenReturn(EdgeType.DottedArrow),
            Try(String("-.-")).ThenReturn(EdgeType.Dotted),
            Try(String("==>")).ThenReturn(EdgeType.ThickArrow),
            Try(String("===")).ThenReturn(EdgeType.Thick),
            Try(String("--o")).ThenReturn(EdgeType.CircleEnd),
            Try(String("--x")).ThenReturn(EdgeType.CrossEnd),
            Try(String("-->")).ThenReturn(EdgeType.Arrow),
            String("---").ThenReturn(EdgeType.Open)
        ).Labelled("arrow");

    // Edge label (text between |text|)
    public static Parser<char, string?> EdgeLabel =>
        Char('|')
            .Then(Token(_ => _ != '|').ManyString())
            .Before(Char('|'))
            .Optional()
            .Select(_ => _.HasValue ? _.Value : null);

    // Accessibility title and description
    public static Parser<char, string> AccTitle =>
        String("accTitle")
            .Then(InlineWhitespace)
            .Then(Char(':'))
            .Then(InlineWhitespace)
            .Then(Token(_ => _ != '\r' && _ != '\n').ManyString())
            .Before(LineEnd);

    public static Parser<char, string> AccDescr =>
        String("accDescr")
            .Then(InlineWhitespace)
            .Then(Char(':'))
            .Then(InlineWhitespace)
            .Then(Token(_ => _ != '\r' && _ != '\n').ManyString())
            .Before(LineEnd);

    // Title
    public static Parser<char, string> Title =>
        String("title")
            .Then(RequiredWhitespace)
            .Then(Token(_ => _ != '\r' && _ != '\n').ManyString())
            .Before(LineEnd);

    // Helper to skip empty lines and comments
    public static Parser<char, Unit> SkipBlankLines =>
        InlineWhitespace.Then(Comment).Or(InlineWhitespace.Then(Newline))
            .SkipMany();

    // Indentation for hierarchical diagrams (mindmap, timeline)
    public static Parser<char, int> Indentation =>
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

    // Keyword helpers
    public static Parser<char, Unit> Keyword(string keyword) =>
        Try(String(keyword)).ThenReturn(Unit.Value);

    public static Parser<char, Unit> KeywordLine(string keyword) =>
        InlineWhitespace
            .Then(Keyword(keyword))
            .Then(InlineWhitespace)
            .Then(LineEnd);

    // Color parsing (#rgb, #rrggbb, named colors)
    public static Parser<char, string> HexColor =>
        Char('#')
            .Then(Token(_ => char.IsLetterOrDigit(_)).AtLeastOnceString())
            .Select(_ => "#" + _);

    public static Parser<char, string> NamedColor =>
        Token(char.IsLetter).AtLeastOnceString();

    public static Parser<char, string> Color =>
        HexColor.Or(NamedColor);

    // CSS class reference
    public static Parser<char, string> CssClass =>
        String(":::")
            .Then(Identifier);

    // Link (click action)
    public static Parser<char, string> Link =>
        String("click")
            .Then(RequiredWhitespace)
            .Then(Identifier)
            .Then(RequiredWhitespace)
            .Then(QuotedString);
}
