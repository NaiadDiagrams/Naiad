using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace MermaidSharp.Diagrams.Mindmap;

public class MindmapParser : IDiagramParser<MindmapModel>
{
    public DiagramType DiagramType => DiagramType.Mindmap;

    // Parse indentation (spaces or tabs)
    static readonly Parser<char, int> IndentationParser =
        Token(c => c == ' ' || c == '\t')
            .Many()
            .Select(chars => chars.Count(c => c == '\t') * 4 + chars.Count(c => c == ' '));

    // Icon: ::icon(fa fa-book)
    static readonly Parser<char, string> IconParser =
        from _ in String("::icon(")
        from icon in Token(c => c != ')').AtLeastOnceString()
        from __ in Char(')')
        select icon;

    // CSS class: :::className
    static readonly Parser<char, string> CssClassParser =
        from _ in String(":::")
        from cls in Token(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').AtLeastOnceString()
        select cls;

    // Node with shape: ((circle)), (rounded), [square], {{hexagon}}, ))bang((, )cloud(
    static Parser<char, (string text, MindmapShape shape)> ShapedNodeParser =>
        OneOf(
            // Circle: ((text))
            Try(
                from _ in String("((")
                from text in Token(c => c != ')').AtLeastOnceString()
                from __ in String("))")
                select (text, MindmapShape.Circle)
            ),
            // Bang/explosion: ))text((
            Try(
                from _ in String("))")
                from text in Token(c => c != '(').AtLeastOnceString()
                from __ in String("((")
                select (text, MindmapShape.Bang)
            ),
            // Cloud: )text(
            Try(
                from _ in Char(')')
                from text in Token(c => c != '(').AtLeastOnceString()
                from __ in Char('(')
                select (text, MindmapShape.Cloud)
            ),
            // Hexagon: {{text}}
            Try(
                from _ in String("{{")
                from text in Token(c => c != '}').AtLeastOnceString()
                from __ in String("}}")
                select (text, MindmapShape.Hexagon)
            ),
            // Rounded: (text)
            Try(
                from _ in Char('(')
                from text in Token(c => c != ')').AtLeastOnceString()
                from __ in Char(')')
                select (text, MindmapShape.Rounded)
            ),
            // Square: [text]
            Try(
                from _ in Char('[')
                from text in Token(c => c != ']').AtLeastOnceString()
                from __ in Char(']')
                select (text, MindmapShape.Square)
            )
        );

    // Node line: indentation + optional shape + text + optional icon/class
    static readonly Parser<char, (int indent, string text, MindmapShape shape, string? icon, string? cssClass)> NodeLineParser =
        from indent in IndentationParser
        from shaped in Try(ShapedNodeParser).Optional()
        from plainText in shaped.HasValue
            ? Return("")
            : Token(c => c != ':' && c != '\r' && c != '\n').ManyString()
        from _ in CommonParsers.InlineWhitespace
        from icon in Try(IconParser).Optional()
        from __ in CommonParsers.InlineWhitespace
        from cssClass in Try(CssClassParser).Optional()
        from ___ in CommonParsers.InlineWhitespace
        from ____ in CommonParsers.LineEnd
        select (
            indent,
            shaped.HasValue ? shaped.Value.text.Trim() : plainText.Trim(),
            shaped.HasValue ? shaped.Value.shape : MindmapShape.Default,
            icon.HasValue ? icon.Value : null,
            cssClass.HasValue ? cssClass.Value : null
        );

    // Content line - node line, skip line (comment/empty), or end
    static Parser<char, (int indent, string text, MindmapShape shape, string? icon, string? cssClass)?> ContentLine =>
        OneOf(
            Try(NodeLineParser.Select(n => ((int, string, MindmapShape, string?, string?)?)n)),
            Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
                .ThenReturn(((int, string, MindmapShape, string?, string?)?)null),
            Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline))
                .ThenReturn(((int, string, MindmapShape, string?, string?)?)null)
        );

    public Parser<char, MindmapModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("mindmap")
        from ___ in CommonParsers.InlineWhitespace
        from ____ in CommonParsers.LineEnd
        from result in ContentLine.ManyThen(End)
        select BuildModel(result.Item1.Where(l => l.HasValue).Select(l => l!.Value).ToList());

    static MindmapModel BuildModel(List<(int indent, string text, MindmapShape shape, string? icon, string? cssClass)> lines)
    {
        var model = new MindmapModel();

        if (lines.Count == 0)
            return model;

        // Build tree from indentation
        var nodes = lines.Select((line, index) => new MindmapNode
        {
            Text = line.text,
            Shape = line.shape,
            Icon = line.icon,
            CssClass = line.cssClass,
            Level = index == 0 ? 0 : -1 // Root is level 0, others TBD
        }).ToList();

        // First node is root
        model.Root = nodes[0];
        model.Root.Level = 0;

        if (nodes.Count == 1)
            return model;

        // Calculate base indentation (from first node after root)
        var baseIndent = lines[0].indent;
        var indentStack = new Stack<(int indent, MindmapNode node)>();
        indentStack.Push((baseIndent, model.Root));

        for (int i = 1; i < lines.Count; i++)
        {
            var (indent, text, shape, icon, cssClass) = lines[i];
            var node = nodes[i];

            // Pop stack until we find a parent with smaller indentation
            while (indentStack.Count > 0 && indentStack.Peek().indent >= indent)
            {
                indentStack.Pop();
            }

            if (indentStack.Count == 0)
            {
                // This shouldn't happen with valid input, but treat as child of root
                indentStack.Push((baseIndent, model.Root));
            }

            var parent = indentStack.Peek().node;
            node.Level = parent.Level + 1;
            parent.Children.Add(node);

            indentStack.Push((indent, node));
        }

        return model;
    }

    public Result<char, MindmapModel> Parse(string input) => Parser.Parse(input);
}
