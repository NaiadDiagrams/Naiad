class KanbanParser : IDiagramParser<KanbanModel>
{
    static Parser<char, string> identifier =
        Token(_ => char.IsLetterOrDigit(_) || _ == '_' || _ == '-').AtLeastOnceString();

    // Label in brackets: [Label Text]
    static Parser<char, string> labelParser =
        from _ in Char('[')
        from label in Token(_ => _ != ']').ManyString()
        from __ in Char(']')
        select label.Trim();

    // Column: id[Name] (no leading whitespace or minimal)
    static Parser<char, (string id, string name)> columnParser =
        from indent in CommonParsers.Indentation.Where(_ => _ < 4)
        from id in identifier
        from name in labelParser
        from __ in CommonParsers.InlineWhitespace
        from ___ in CommonParsers.LineEnd
        select (id, name);

    // Task: id[Name] (with significant leading whitespace - 4+ spaces or tabs)
    static Parser<char, (string id, string name)> taskParser =
        from indent in CommonParsers.Indentation.Where(_ => _ >= 4)
        from id in identifier
        from name in labelParser
        from __ in CommonParsers.InlineWhitespace
        from ___ in CommonParsers.LineEnd
        select (id, name);

    // Skip line (comments, empty lines)
    static Parser<char, Unit> skipLine =
        Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
            .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

    static Parser<char, IKanbanContent?> ContentItem =>
        OneOf(
            Try(taskParser.Select<IKanbanContent?>(_ => new TaskItem(_.id, _.name))),
            Try(columnParser.Select<IKanbanContent?>(_ => new ColumnItem(_.id, _.name))),
            skipLine.ThenReturn<IKanbanContent?>(null)
        );

    public static Parser<char, KanbanModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("kanban")
        from ___ in CommonParsers.InlineWhitespace
        from ____ in CommonParsers.LineEnd
        from result in ContentItem.ManyThen(End)
        select BuildModel(result.Item1);

    static KanbanModel BuildModel(IEnumerable<IKanbanContent?> content)
    {
        var model = new KanbanModel();
        KanbanColumn? currentColumn = null;

        foreach (var item in content)
        {
            switch (item)
            {
                case ColumnItem column:
                    currentColumn = new()
                    {
                        Id = column.Id,
                        Name = column.Name
                    };
                    model.Columns.Add(currentColumn);
                    break;

                case TaskItem task:
                    currentColumn?.Tasks.Add(
                        new()
                        {
                            Id = task.Id,
                            Name = task.Name
                        });
                    break;
            }
        }

        return model;
    }

    public Result<char, KanbanModel> Parse(string input) => Parser.Parse(input);

    internal interface IKanbanContent;
    readonly record struct ColumnItem(string Id, string Name) : IKanbanContent;
    readonly record struct TaskItem(string Id, string Name) : IKanbanContent;
}
