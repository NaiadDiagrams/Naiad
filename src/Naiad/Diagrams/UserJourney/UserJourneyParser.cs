namespace MermaidSharp.Diagrams.UserJourney;

public class UserJourneyParser : IDiagramParser<UserJourneyModel>
{
    public DiagramType DiagramType => DiagramType.UserJourney;

    // Rest of line (for text content)
    static Parser<char, string> restOfLine =
        Token(c => c != '\r' && c != '\n').ManyString();

    // Title: title My Journey
    static Parser<char, string> titleParser =
        from whitespace in CommonParsers.InlineWhitespace
        from title in CIString("title")
        from requiredWhitespace in CommonParsers.RequiredWhitespace
        from restOfLine in restOfLine
        from lineEnd in CommonParsers.LineEnd
        select restOfLine.Trim();

    // Section: section Section Name
    static Parser<char, string> sectionParser =
        from whiteSpace in CommonParsers.InlineWhitespace
        from section in CIString("section")
        from requireWhitespace in CommonParsers.RequiredWhitespace
        from name in restOfLine
        from lineEnd in CommonParsers.LineEnd
        select name.Trim();

    // Actor list: Me, Cat, Dog
    static Parser<char, List<string>> actorListParser =
        Token(c => c != ',' && c != '\r' && c != '\n').AtLeastOnceString()
            .SeparatedAtLeastOnce(Char(',').Then(CommonParsers.InlineWhitespace))
            .Select(actors => actors.Select(a => a.Trim()).ToList());

    // Task: Task Name: 5: Me, Cat
    static Parser<char, JourneyTask> taskParser =
        from _ in CommonParsers.InlineWhitespace
        from name in Token(c => c != ':' && c != '\r' && c != '\n').AtLeastOnceString()
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
            Score = Math.Clamp(score, 1, 5),
            Actors = actors
        };

    // Skip line (comments, empty lines)
    static Parser<char, Unit> skipLine =
        Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
            .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

    // Content item
    static Parser<char, object?> ContentItem =>
        OneOf(
            Try(titleParser.Select(t => (object?)("title", t))),
            Try(sectionParser.Select(s => (object?)("section", s))),
            Try(taskParser.Select(task => (object?)("task", task))),
            skipLine.ThenReturn((object?)null)
        );

    public static Parser<char, UserJourneyModel> Parser =>
        from whitespace in CommonParsers.InlineWhitespace
        from journey in CIString("journey")
        from inerWhitespace in CommonParsers.InlineWhitespace
        from lineEnd in CommonParsers.LineEnd
        from result in ContentItem.ManyThen(End)
        select BuildModel(result.Item1.Where(c => c != null).ToList());

    static UserJourneyModel BuildModel(List<object?> content)
    {
        var model = new UserJourneyModel();
        JourneySection? currentSection = null;

        foreach (var item in content)
        {
            switch (item)
            {
                case ("title", string value):
                    model.Title = value;
                    break;

                case ("section", string sectionName):
                    currentSection = new() { Name = sectionName };
                    model.Sections.Add(currentSection);
                    break;

                case ("task", JourneyTask task):
                    if (currentSection == null)
                    {
                        currentSection = new();
                        model.Sections.Add(currentSection);
                    }
                    currentSection.Tasks.Add(task);
                    break;
            }
        }

        return model;
    }

    public Result<char, UserJourneyModel> Parse(string input) => Parser.Parse(input);
}
