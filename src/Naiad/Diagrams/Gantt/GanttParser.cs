namespace MermaidSharp.Diagrams.Gantt;

public class GanttParser : IDiagramParser<GanttModel>
{
    public DiagramType DiagramType => DiagramType.Gantt;

    // Basic parsers
    static readonly Parser<char, string> RestOfLine =
        Token(c => c != '\r' && c != '\n').ManyString();

    static readonly Parser<char, string> Identifier =
        Token(c => char.IsLetterOrDigit(c) || c == '_' || c == '-')
            .AtLeastOnceString();

    // Title: title My Chart Title
    static readonly Parser<char, string> TitleParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("title")
        from ___ in CommonParsers.RequiredWhitespace
        from title in RestOfLine
        from ____ in CommonParsers.LineEnd
        select title.Trim();

    // Date format: dateFormat YYYY-MM-DD
    static readonly Parser<char, string> DateFormatParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("dateFormat")
        from ___ in CommonParsers.RequiredWhitespace
        from format in RestOfLine
        from ____ in CommonParsers.LineEnd
        select format.Trim();

    // Axis format: axisFormat %Y-%m-%d
    static readonly Parser<char, string> AxisFormatParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("axisFormat")
        from ___ in CommonParsers.RequiredWhitespace
        from format in RestOfLine
        from ____ in CommonParsers.LineEnd
        select format.Trim();

    // Excludes: excludes weekends
    static readonly Parser<char, List<string>> ExcludesParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("excludes")
        from ___ in CommonParsers.RequiredWhitespace
        from excludes in RestOfLine
        from ____ in CommonParsers.LineEnd
        select excludes.Trim().Split(',').Select(e => e.Trim()).ToList();

    // Section: section Section Name
    static readonly Parser<char, string> SectionParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("section")
        from ___ in CommonParsers.RequiredWhitespace
        from name in RestOfLine
        from ____ in CommonParsers.LineEnd
        select name.Trim();

    // Task modifiers
    static readonly Parser<char, (bool active, bool done, bool crit, bool milestone)> TaskModifiers =
        from modifiers in Try(
            from parts in Identifier.SeparatedAndOptionallyTerminated(
                CommonParsers.InlineWhitespace.Then(Char(',')).Then(CommonParsers.InlineWhitespace)
            )
            select parts.ToList()
        ).Optional()
        select ParseModifiers(modifiers.HasValue ? modifiers.Value : []);

    static (bool active, bool done, bool crit, bool milestone) ParseModifiers(List<string> parts)
    {
        bool active = false, done = false, crit = false, milestone = false;
        foreach (var part in parts)
        {
            var lower = part.ToLowerInvariant();
            if (lower == "active") active = true;
            else if (lower == "done") done = true;
            else if (lower == "crit") crit = true;
            else if (lower == "milestone") milestone = true;
        }

        return (active, done, crit, milestone);
    }

    // Duration: 30d, 2w, or until date
    static readonly Parser<char, TimeSpan?> DurationParser =
        from num in CommonParsers.Integer
        from unit in Token(char.IsLetter).Optional()
        select (TimeSpan?) (unit.HasValue ? ParseDuration(num, unit.Value) : TimeSpan.FromDays(num));

    static TimeSpan ParseDuration(int num, char unit) =>
        unit switch
        {
            'd' => TimeSpan.FromDays(num),
            'w' => TimeSpan.FromDays(num * 7),
            'h' => TimeSpan.FromHours(num),
            _ => TimeSpan.FromDays(num)
        };

    // Date: YYYY-MM-DD format
    static readonly Parser<char, DateTime> DateParser =
        from year in Digit.RepeatString(4)
        from _ in Char('-')
        from month in Digit.RepeatString(2)
        from __ in Char('-')
        from day in Digit.RepeatString(2)
        select new DateTime(int.Parse(year), int.Parse(month), int.Parse(day));

    // After reference: after taskId
    static readonly Parser<char, string> AfterParser =
        from _ in CIString("after")
        from __ in CommonParsers.RequiredWhitespace
        from taskId in Identifier
        select taskId;

    // Task line parser - handles multiple formats
    // Format: Task name :modifiers, id, start, duration
    // Examples:
    //   Task A :a1, 2024-01-01, 30d
    //   Task B :done, after a1, 20d
    //   Task C :crit, milestone, 2024-02-01, 0d
    static readonly Parser<char, GanttTask> TaskParser =
        from _ in CommonParsers.InlineWhitespace
        from name in Token(c => c != ':' && c != '\r' && c != '\n').AtLeastOnceString()
        from __ in CommonParsers.InlineWhitespace
        from ___ in Char(':')
        from ____ in CommonParsers.InlineWhitespace
        from parts in Token(c => c != '\r' && c != '\n').ManyString()
        from _____ in CommonParsers.LineEnd
        select ParseTaskLine(name.Trim(), parts.Trim());

    static GanttTask ParseTaskLine(string name, string partsStr)
    {
        var task = new GanttTask {Name = name};
        var parts = partsStr.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList();

        foreach (var part in parts)
        {
            var lower = part.ToLowerInvariant();

            // Check for modifiers
            if (lower == "active")
            {
                task.Status = GanttTaskStatus.Active;
                continue;
            }

            if (lower == "done")
            {
                task.Status = GanttTaskStatus.Done;
                continue;
            }

            if (lower == "crit")
            {
                task.IsCritical = true;
                continue;
            }

            if (lower == "milestone")
            {
                task.IsMilestone = true;
                continue;
            }

            // Check for after reference
            if (lower.StartsWith("after "))
            {
                task.AfterTaskId = part[6..].Trim();
                continue;
            }

            // Check for duration (ends with d, w, h)
            if (part.Length > 1 && char.IsDigit(part[0]) && char.IsLetter(part[^1]))
            {
                var numStr = new string(part.TakeWhile(char.IsDigit).ToArray());
                var unit = part[^1];
                if (int.TryParse(numStr, out var num))
                {
                    task.Duration = unit switch
                    {
                        'd' => TimeSpan.FromDays(num),
                        'w' => TimeSpan.FromDays(num * 7),
                        'h' => TimeSpan.FromHours(num),
                        _ => TimeSpan.FromDays(num)
                    };
                    continue;
                }
            }

            // Check for date (YYYY-MM-DD)
            if (DateTime.TryParseExact(part, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var date))
            {
                if (task.StartDate == null && task.AfterTaskId == null)
                {
                    task.StartDate = date;
                }
                else
                {
                    task.EndDate = date;
                }

                continue;
            }

            // Must be an ID (alphanumeric identifier)
            if (part.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
            {
                if (task.Id == null)
                {
                    task.Id = part;
                }
            }
        }

        return task;
    }

    // Skip line (comments, empty lines)
    static readonly Parser<char, Unit> skipLine =
        CommonParsers.InlineWhitespace
            .Then(Try(CommonParsers.Comment).Or(CommonParsers.Newline));

    // Content item
    static Parser<char, object?> ContentItem =>
        OneOf(
            Try(TitleParser.Select(t => (object?) ("title", t))),
            Try(DateFormatParser.Select(f => (object?) ("dateFormat", f))),
            Try(AxisFormatParser.Select(f => (object?) ("axisFormat", f))),
            Try(ExcludesParser.Select(e => (object?) ("excludes", e))),
            Try(SectionParser.Select(s => (object?) ("section", s))),
            Try(TaskParser.Select(t => (object?) t)),
            skipLine.ThenReturn((object?) null)
        );

    public static Parser<char, GanttModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("gantt")
        from ___ in CommonParsers.InlineWhitespace
        from ____ in CommonParsers.LineEnd
        from content in ContentItem.Many()
        select BuildModel(content.Where(c => c != null).ToList());

    static GanttModel BuildModel(List<object?> content)
    {
        var model = new GanttModel();
        GanttSection? currentSection = null;

        foreach (var item in content)
        {
            switch (item)
            {
                case ("title", string value):
                    model.Title = value;
                    break;

                case ("dateFormat", string value):
                    model.DateFormat = value;
                    break;

                case ("axisFormat", string value):
                    model.AxisFormat = value;
                    break;

                case ("excludes", List<string> excludes):
                    foreach (var ex in excludes)
                    {
                        if (ex.Equals("weekends", StringComparison.InvariantCultureIgnoreCase))
                            model.ExcludeWeekends = true;
                        else
                            model.ExcludeDays.Add(ex);
                    }

                    break;

                case ("section", string sectionName):
                    currentSection = new() {Name = sectionName};
                    model.Sections.Add(currentSection);
                    break;

                case GanttTask task:
                    if (currentSection == null)
                    {
                        currentSection = new() {Name = ""};
                        model.Sections.Add(currentSection);
                    }

                    task.SectionName = currentSection.Name;
                    currentSection.Tasks.Add(task);
                    break;
            }
        }

        return model;
    }

    public Result<char, GanttModel> Parse(string input) => Parser.Parse(input);
}