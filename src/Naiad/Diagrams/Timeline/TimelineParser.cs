class TimelineParser : IDiagramParser<TimelineModel>
{
    static readonly Parser<char, TimelineModel> parser;

    static TimelineParser()
    {
        // Rest of line (for text content)
        var restOfLine =
            Token(_ => _ != '\r' && _ != '\n').ManyString();

        // Title: title My Timeline
        var titleParser =
            from _ in CommonParsers.InlineWhitespace
            from __ in CIString("title")
            from ___ in CommonParsers.RequiredWhitespace
            from title in restOfLine
            from ____ in CommonParsers.LineEnd
            select title.Trim();

        // Section: section Section Name
        var sectionParser =
            from _ in CommonParsers.InlineWhitespace
            from __ in CIString("section")
            from ___ in CommonParsers.RequiredWhitespace
            from name in restOfLine
            from ____ in CommonParsers.LineEnd
            select name.Trim();

        // Period with event: 2020 : Event description
        var periodEventParser =
            from _ in CommonParsers.InlineWhitespace
            from period in Token(_ => _ != ':' && _ != '\r' && _ != '\n').AtLeastOnceString()
            from __ in CommonParsers.InlineWhitespace
            from ___ in Char(':')
            from ____ in CommonParsers.InlineWhitespace
            from eventText in restOfLine
            from _____ in CommonParsers.LineEnd
            select (period: period.Trim(), eventText: eventText.Trim());

        // Continuation event: : Another event (no period, just event)
        var continuationEventParser =
            from _ in CommonParsers.InlineWhitespace
            from __ in Char(':')
            from ___ in CommonParsers.InlineWhitespace
            from eventText in restOfLine
            from ____ in CommonParsers.LineEnd
            select eventText.Trim();

        // Skip line (comments, empty lines)
        var skipLine =
            Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
                .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

        // Content item
        var contentItem =
            OneOf(
                Try(titleParser.Select<ITimelineContent?>(_ => new TitleItem(_))),
                Try(sectionParser.Select<ITimelineContent?>(_ => new SectionItem(_))),
                Try(periodEventParser.Select<ITimelineContent?>(_ => new PeriodItem(_.period, _.eventText))),
                Try(continuationEventParser.Select<ITimelineContent?>(_ => new ContinuationItem(_))),
                skipLine.ThenReturn<ITimelineContent?>(null)
            );

        parser =
            from _ in CommonParsers.InlineWhitespace
            from __ in CIString("timeline")
            from ___ in CommonParsers.InlineWhitespace
            from ____ in CommonParsers.LineEnd
            from result in contentItem.ManyThen(End)
            select BuildModel(result.Item1);
    }

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
                    currentPeriod.Events.AddRange(SplitEvents(period.EventText));
                    currentSection.Periods.Add(currentPeriod);
                    break;

                case ContinuationItem continuation:
                    currentPeriod?.Events.AddRange(SplitEvents(continuation.EventText));
                    break;
            }
        }

        return model;
    }

    /// <summary>
    /// Everything past a period's colon is itself a colon-separated list of events, so
    /// <c>2004 : Facebook : Gmail</c> hangs two events off 2004 rather than one reading
    /// "Facebook : Gmail". Blank entries — a trailing colon, or a period declared with no event at
    /// all — contribute nothing.
    /// </summary>
    static IEnumerable<string> SplitEvents(string text) =>
        text.Split(':')
            .Select(_ => _.Trim())
            .Where(_ => _.Length > 0);

    public Result<char, TimelineModel> Parse(string input) => parser.Parse(input);

    interface ITimelineContent;
    readonly record struct TitleItem(string Value) : ITimelineContent;
    readonly record struct SectionItem(string Name) : ITimelineContent;
    readonly record struct PeriodItem(string Period, string EventText) : ITimelineContent;
    readonly record struct ContinuationItem(string EventText) : ITimelineContent;
}
