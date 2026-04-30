class TimelineParser : IDiagramParser<TimelineModel>
{
    // Rest of line (for text content)
    static Parser<char, string> restOfLine =
        Token(_ => _ != '\r' && _ != '\n').ManyString();

    // Title: title My Timeline
    static Parser<char, string> titleParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("title")
        from ___ in CommonParsers.RequiredWhitespace
        from title in restOfLine
        from ____ in CommonParsers.LineEnd
        select title.Trim();

    // Section: section Section Name
    static Parser<char, string> sectionParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("section")
        from ___ in CommonParsers.RequiredWhitespace
        from name in restOfLine
        from ____ in CommonParsers.LineEnd
        select name.Trim();

    // Period with event: 2020 : Event description
    static Parser<char, (string period, string eventText)> periodEventParser =
        from _ in CommonParsers.InlineWhitespace
        from period in Token(_ => _ != ':' && _ != '\r' && _ != '\n').AtLeastOnceString()
        from __ in CommonParsers.InlineWhitespace
        from ___ in Char(':')
        from ____ in CommonParsers.InlineWhitespace
        from eventText in restOfLine
        from _____ in CommonParsers.LineEnd
        select (period.Trim(), eventText.Trim());

    // Continuation event: : Another event (no period, just event)
    static Parser<char, string> continuationEventParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in Char(':')
        from ___ in CommonParsers.InlineWhitespace
        from eventText in restOfLine
        from ____ in CommonParsers.LineEnd
        select eventText.Trim();

    // Skip line (comments, empty lines)
    static Parser<char, Unit> skipLine =
        Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
            .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

    // Content item
    static Parser<char, ITimelineContent?> ContentItem =>
        OneOf(
            Try(titleParser.Select<ITimelineContent?>(_ => new TitleItem(_))),
            Try(sectionParser.Select<ITimelineContent?>(_ => new SectionItem(_))),
            Try(periodEventParser.Select<ITimelineContent?>(_ => new PeriodItem(_.period, _.eventText))),
            Try(continuationEventParser.Select<ITimelineContent?>(_ => new ContinuationItem(_))),
            skipLine.ThenReturn<ITimelineContent?>(null)
        );

    static Parser<char, TimelineModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("timeline")
        from ___ in CommonParsers.InlineWhitespace
        from ____ in CommonParsers.LineEnd
        from result in ContentItem.ManyThen(End)
        select BuildModel(result.Item1);

    static TimelineModel BuildModel(IEnumerable<ITimelineContent?> content)
    {
        var model = new TimelineModel();
        TimelineSection? currentSection = null;
        TimePeriod? currentPeriod = null;

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
                    currentPeriod = null;
                    break;

                case PeriodItem period:
                    if (currentSection == null)
                    {
                        currentSection = new();
                        model.Sections.Add(currentSection);
                    }
                    currentPeriod = new()
                    {
                        Label = period.Period
                    };
                    if (!string.IsNullOrEmpty(period.EventText))
                    {
                        currentPeriod.Events.Add(period.EventText);
                    }
                    currentSection.Periods.Add(currentPeriod);
                    break;

                case ContinuationItem cont:
                    if (currentPeriod != null && !string.IsNullOrEmpty(cont.EventText))
                    {
                        currentPeriod.Events.Add(cont.EventText);
                    }
                    break;
            }
        }

        return model;
    }

    public Result<char, TimelineModel> Parse(string input) => Parser.Parse(input);

    internal interface ITimelineContent;
    readonly record struct TitleItem(string Value) : ITimelineContent;
    readonly record struct SectionItem(string Name) : ITimelineContent;
    readonly record struct PeriodItem(string Period, string EventText) : ITimelineContent;
    readonly record struct ContinuationItem(string EventText) : ITimelineContent;
}
