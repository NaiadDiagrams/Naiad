class UserJourneyParser : IDiagramParser<UserJourneyModel>
{
    // Rest of line (for text content)
    static readonly Parser<char, string> restOfLine;

    // Title: title My Journey
    static readonly Parser<char, string> titleParser;

    // Section: section Section Name
    static readonly Parser<char, string> sectionParser;

    // Actor list: Me, Cat, Dog
    static readonly Parser<char, List<string>> actorListParser;

    // Task: Task Name: 5: Me, Cat
    static readonly Parser<char, JourneyTask> taskParser;

    // Skip line (comments, empty lines)
    static readonly Parser<char, Unit> skipLine;

    // Content item
    static readonly Parser<char, IUserJourneyContent?> ContentItem;

    static readonly Parser<char, UserJourneyModel> parser;

    static UserJourneyParser()
    {
        restOfLine =
            Token(_ => _ != '\r' && _ != '\n').ManyString();

        titleParser =
            from whitespace in CommonParsers.InlineWhitespace
            from title in CIString("title")
            from requiredWhitespace in CommonParsers.RequiredWhitespace
            from restOfLine in restOfLine
            from lineEnd in CommonParsers.LineEnd
            select restOfLine.Trim();

        sectionParser =
            from whiteSpace in CommonParsers.InlineWhitespace
            from section in CIString("section")
            from requireWhitespace in CommonParsers.RequiredWhitespace
            from name in restOfLine
            from lineEnd in CommonParsers.LineEnd
            select name.Trim();

        actorListParser =
            Token(_ => _ != ',' && _ != '\r' && _ != '\n').AtLeastOnceString()
                .SeparatedAtLeastOnce(Char(',').Then(CommonParsers.InlineWhitespace))
                .Select(actors => actors.Select(_ => _.Trim()).ToList());

        taskParser =
            from _ in CommonParsers.InlineWhitespace
            from name in Token(_ => _ != ':' && _ != '\r' && _ != '\n').AtLeastOnceString()
            from colon in Char(':')
            from whitespace in CommonParsers.InlineWhitespace
            from score in Digit.AtLeastOnceString().Select(int.Parse)
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

        skipLine =
            Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
                .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

        ContentItem =
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
            from result in ContentItem.ManyThen(End)
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
