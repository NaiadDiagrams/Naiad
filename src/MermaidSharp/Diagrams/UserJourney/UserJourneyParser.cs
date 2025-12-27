using MermaidSharp;
using MermaidSharp.Parsing;
using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace MermaidSharp.Diagrams.UserJourney;

public class UserJourneyParser : IDiagramParser<UserJourneyModel>
{
    public DiagramType DiagramType => DiagramType.UserJourney;

    // Rest of line (for text content)
    static readonly Parser<char, string> RestOfLine =
        Token(c => c != '\r' && c != '\n').ManyString();

    // Title: title My Journey
    static readonly Parser<char, string> TitleParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("title")
        from ___ in CommonParsers.RequiredWhitespace
        from title in RestOfLine
        from ____ in CommonParsers.LineEnd
        select title.Trim();

    // Section: section Section Name
    static readonly Parser<char, string> SectionParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("section")
        from ___ in CommonParsers.RequiredWhitespace
        from name in RestOfLine
        from ____ in CommonParsers.LineEnd
        select name.Trim();

    // Actor list: Me, Cat, Dog
    static readonly Parser<char, List<string>> ActorListParser =
        Token(c => c != ',' && c != '\r' && c != '\n').AtLeastOnceString()
            .SeparatedAtLeastOnce(Char(',').Then(CommonParsers.InlineWhitespace))
            .Select(actors => actors.Select(a => a.Trim()).ToList());

    // Task: Task Name: 5: Me, Cat
    static readonly Parser<char, JourneyTask> TaskParser =
        from _ in CommonParsers.InlineWhitespace
        from name in Token(c => c != ':' && c != '\r' && c != '\n').AtLeastOnceString()
        from __ in Char(':')
        from ___ in CommonParsers.InlineWhitespace
        from score in Digit.AtLeastOnceString().Select(int.Parse)
        from ____ in Char(':')
        from _____ in CommonParsers.InlineWhitespace
        from actors in ActorListParser
        from ______ in CommonParsers.LineEnd
        select new JourneyTask
        {
            Name = name.Trim(),
            Score = Math.Clamp(score, 1, 5),
            Actors = actors
        };

    // Skip line (comments, empty lines)
    static readonly Parser<char, Unit> SkipLine =
        Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
            .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

    // Content item
    static Parser<char, object?> ContentItem =>
        OneOf(
            Try(TitleParser.Select(t => (object?)("title", t))),
            Try(SectionParser.Select(s => (object?)("section", s))),
            Try(TaskParser.Select(task => (object?)("task", task))),
            SkipLine.ThenReturn((object?)null)
        );

    public Parser<char, UserJourneyModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("journey")
        from ___ in CommonParsers.InlineWhitespace
        from ____ in CommonParsers.LineEnd
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
                case (string key, string value) when key == "title":
                    model.Title = value;
                    break;

                case (string key, string sectionName) when key == "section":
                    currentSection = new JourneySection { Name = sectionName };
                    model.Sections.Add(currentSection);
                    break;

                case (string key, JourneyTask task) when key == "task":
                    if (currentSection == null)
                    {
                        currentSection = new JourneySection();
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
