namespace MermaidSharp.Diagrams.Kanban;

public class KanbanParser : IDiagramParser<KanbanModel>
{
    public DiagramType DiagramType => DiagramType.Kanban;

    // Identifier
    static Parser<char, string> Identifier =
        Token(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').AtLeastOnceString();

    // Label in brackets: [Label Text]
    static Parser<char, string> LabelParser =
        from _ in Char('[')
        from label in Token(c => c != ']').ManyString()
        from __ in Char(']')
        select label.Trim();

    // Column: id[Name] (no leading whitespace or minimal)
    static Parser<char, (string id, string name)> ColumnParser =
        from indent in CommonParsers.Indentation.Where(i => i < 4)
        from id in Identifier
        from name in LabelParser
        from __ in CommonParsers.InlineWhitespace
        from ___ in CommonParsers.LineEnd
        select (id, name);

    // Task: id[Name] (with significant leading whitespace - 4+ spaces or tabs)
    static Parser<char, (string id, string name)> TaskParser =
        from indent in CommonParsers.Indentation.Where(i => i >= 4)
        from id in Identifier
        from name in LabelParser
        from __ in CommonParsers.InlineWhitespace
        from ___ in CommonParsers.LineEnd
        select (id, name);

    // Skip line (comments, empty lines)
    static Parser<char, Unit> SkipLine =
        Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
            .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

    // Content item
    static Parser<char, object?> ContentItem =>
        OneOf(
            Try(TaskParser.Select(t => (object?)("task", t.id, t.name))),
            Try(ColumnParser.Select(c => (object?)("column", c.id, c.name))),
            SkipLine.ThenReturn((object?)null)
        );

    public static Parser<char, KanbanModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("kanban")
        from ___ in CommonParsers.InlineWhitespace
        from ____ in CommonParsers.LineEnd
        from result in ContentItem.ManyThen(End)
        select BuildModel(result.Item1.Where(c => c != null).ToList());

    static KanbanModel BuildModel(List<object?> content)
    {
        var model = new KanbanModel();
        KanbanColumn? currentColumn = null;

        foreach (var item in content)
        {
            switch (item)
            {
                case ("column", string id, string name):
                    currentColumn = new() { Id = id, Name = name };
                    model.Columns.Add(currentColumn);
                    break;

                case ("task", string id, string name):
                    currentColumn?.Tasks.Add(new() { Id = id, Name = name });
                    break;
            }
        }

        return model;
    }

    public Result<char, KanbanModel> Parse(string input) => Parser.Parse(input);
}
