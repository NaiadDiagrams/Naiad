namespace MermaidSharp.Diagrams.Timeline;

public class TimelineParser : IDiagramParser<TimelineModel>
{
    public DiagramType DiagramType => DiagramType.Timeline;

    // Rest of line (for text content)
    static Parser<char, string> RestOfLine =
        Token(c => c != '\r' && c != '\n').ManyString();

    // Title: title My Timeline
    static Parser<char, string> TitleParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("title")
        from ___ in CommonParsers.RequiredWhitespace
        from title in RestOfLine
        from ____ in CommonParsers.LineEnd
        select title.Trim();

    // Section: section Section Name
    static Parser<char, string> SectionParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("section")
        from ___ in CommonParsers.RequiredWhitespace
        from name in RestOfLine
        from ____ in CommonParsers.LineEnd
        select name.Trim();

    // Period with event: 2020 : Event description
    static Parser<char, (string period, string eventText)> PeriodEventParser =
        from _ in CommonParsers.InlineWhitespace
        from period in Token(c => c != ':' && c != '\r' && c != '\n').AtLeastOnceString()
        from __ in CommonParsers.InlineWhitespace
        from ___ in Char(':')
        from ____ in CommonParsers.InlineWhitespace
        from eventText in RestOfLine
        from _____ in CommonParsers.LineEnd
        select (period.Trim(), eventText.Trim());

    // Continuation event: : Another event (no period, just event)
    static Parser<char, string> ContinuationEventParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in Char(':')
        from ___ in CommonParsers.InlineWhitespace
        from eventText in RestOfLine
        from ____ in CommonParsers.LineEnd
        select eventText.Trim();

    // Skip line (comments, empty lines)
    static Parser<char, Unit> SkipLine =
        Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
            .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

    // Content item
    static Parser<char, object?> ContentItem =>
        OneOf(
            Try(TitleParser.Select(t => (object?)("title", t))),
            Try(SectionParser.Select(s => (object?)("section", s))),
            Try(PeriodEventParser.Select(pe => (object?)("period", pe.period, pe.eventText))),
            Try(ContinuationEventParser.Select(e => (object?)("continuation", e))),
            SkipLine.ThenReturn((object?)null)
        );

    public static Parser<char, TimelineModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("timeline")
        from ___ in CommonParsers.InlineWhitespace
        from ____ in CommonParsers.LineEnd
        from result in ContentItem.ManyThen(End)
        select BuildModel(result.Item1.Where(c => c != null).ToList());

    static TimelineModel BuildModel(List<object?> content)
    {
        var model = new TimelineModel();
        TimelineSection? currentSection = null;
        TimePeriod? currentPeriod = null;

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
                    currentPeriod = null;
                    break;

                case ("period", string period, string eventText):
                    if (currentSection == null)
                    {
                        currentSection = new();
                        model.Sections.Add(currentSection);
                    }
                    currentPeriod = new()
                    {
                        Label = period
                    };
                    if (!string.IsNullOrEmpty(eventText))
                    {
                        currentPeriod.Events.Add(eventText);
                    }
                    currentSection.Periods.Add(currentPeriod);
                    break;

                case ("continuation", string eventText):
                    if (currentPeriod != null && !string.IsNullOrEmpty(eventText))
                    {
                        currentPeriod.Events.Add(eventText);
                    }
                    break;
            }
        }

        return model;
    }

    public Result<char, TimelineModel> Parse(string input) => Parser.Parse(input);
}
