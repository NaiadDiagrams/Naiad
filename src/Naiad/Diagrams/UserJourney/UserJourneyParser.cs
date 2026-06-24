class UserJourneyParser : IDiagramParser<UserJourneyModel>
{
    static readonly Parser<char, UserJourneyModel> parser;

    static UserJourneyParser()
    {
        // Rest of line (for text content)
        var restOfLine =
            Token(_ => _ != '\r' && _ != '\n').ManyString();

        // Title: title My Journey
        var titleParser =
            from whitespace in CommonParsers.InlineWhitespace
            from title in CIString("title")
            from requiredWhitespace in CommonParsers.RequiredWhitespace
            from text in restOfLine
            from lineEnd in CommonParsers.LineEnd
            select text.Trim();

        // Section: section Section Name
        var sectionParser =
            from whiteSpace in CommonParsers.InlineWhitespace
            from section in CIString("section")
            from requireWhitespace in CommonParsers.RequiredWhitespace
            from name in restOfLine
            from lineEnd in CommonParsers.LineEnd
            select name.Trim();

        // Actor list: Me, Cat, Dog
        var actorListParser =
            Token(_ => _ != ',' && _ != '\r' && _ != '\n').AtLeastOnceString()
                .SeparatedAtLeastOnce(Char(',').Then(CommonParsers.InlineWhitespace))
                .Select(actors => actors.Select(_ => _.Trim()).ToList());

        // Task: Task Name: 5: Me, Cat
        var taskParser =
            from _ in CommonParsers.InlineWhitespace
            from name in Token(_ => _ != ':' && _ != '\r' && _ != '\n').AtLeastOnceString()
            from colon in Char(':')
            from whitespace in CommonParsers.InlineWhitespace
            from score in CommonParsers.UnsignedInt
            from innerColon in Char(':')
            from innerWhitespace in CommonParsers.InlineWhitespace
            from actors in actorListParser
            from lineEnd in CommonParsers.LineEnd
            select new JourneyTask
            {
                Name = name.Trim(),
                Score = score,
                Actors = actors
            };

        // Skip line (comments, empty lines)
        var skipLine =
            Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
                .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

        // Content item
        var contentItem =
            OneOf(
                Try(titleParser.Select<IUserJourneyContent?>(_ => new TitleItem(_))),
                Try(sectionParser.Select<IUserJourneyContent?>(_ => new SectionItem(_))),
                Try(taskParser.Select<IUserJourneyContent?>(_ => new TaskItem(_))),
                skipLine.ThenReturn<IUserJourneyContent?>(null)
            );

        parser =
            from whitespace in CommonParsers.InlineWhitespace
            from journey in CIString("journey")
            from inerWhitespace in CommonParsers.InlineWhitespace
            from lineEnd in CommonParsers.LineEnd
            from result in contentItem.ManyThen(End)
            select BuildModel(result.Item1);
    }

    static UserJourneyModel BuildModel(IEnumerable<IUserJourneyContent?> content)
    {
        var model = new UserJourneyModel();
        JourneySection? currentSection = null;

        foreach (var item in content)
        {
            switch (item)
            {
                case TitleItem title:
                    model.Title = title.Value;
                    break;

                case SectionItem section:
                    currentSection = new() { Name = section.Name };
                    model.Sections.Add(currentSection);
                    break;

                case TaskItem taskItem:
                    if (currentSection == null)
                    {
                        currentSection = new();
                        model.Sections.Add(currentSection);
                    }
                    currentSection.Tasks.Add(taskItem.Task);
                    break;
            }
        }

        return model;
    }

    public Result<char, UserJourneyModel> Parse(string input) => parser.Parse(input);

    internal interface IUserJourneyContent;
    readonly record struct TitleItem(string Value) : IUserJourneyContent;
    readonly record struct SectionItem(string Name) : IUserJourneyContent;
    readonly record struct TaskItem(JourneyTask Task) : IUserJourneyContent;
}
