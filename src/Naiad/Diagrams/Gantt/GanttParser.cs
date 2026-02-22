namespace MermaidSharp.Diagrams.Gantt;

public class GanttParser : IDiagramParser<GanttModel>
{
    public DiagramType DiagramType => DiagramType.Gantt;

    // Basic parsers
    static Parser<char, string> restOfLine =
        Token(_ => _ != '\r' && _ != '\n').ManyString();

    // Title: title My Chart Title
    static Parser<char, string> titleParser =
        from inlineWhitespace in CommonParsers.InlineWhitespace
        from title in CIString("title")
        from requiredWhitespace in CommonParsers.RequiredWhitespace
        from innerTitle in restOfLine
        from lineEnd in CommonParsers.LineEnd
        select innerTitle.Trim();

    // Date format: dateFormat YYYY-MM-DD
    static Parser<char, string> dateFormatParser =
        from inlineWhitespace in CommonParsers.InlineWhitespace
        from dateFormat in CIString("dateFormat")
        from requiredWhitespace in CommonParsers.RequiredWhitespace
        from format in restOfLine
        from lineEnd in CommonParsers.LineEnd
        select format.Trim();

    // Axis format: axisFormat %Y-%m-%d
    static Parser<char, string> axisFormatParser =
        from inlineWhitespace in CommonParsers.InlineWhitespace
        from axisFormat in CIString("axisFormat")
        from requiredWhitespace in CommonParsers.RequiredWhitespace
        from format in restOfLine
        from lienEnd in CommonParsers.LineEnd
        select format.Trim();

    // Excludes: excludes weekends
    static Parser<char, List<string>> excludesParser =
        from whitespace in CommonParsers.InlineWhitespace
        from excludes in CIString("excludes")
        from requiredWhitespace in CommonParsers.RequiredWhitespace
        from innerExcludes in restOfLine
        from lineEnd in CommonParsers.LineEnd
        select innerExcludes.Trim().Split(',').Select(e => e.Trim()).ToList();

    // Section: section Section Name
    static Parser<char, string> sectionParser =
        from inlienWhitespace in CommonParsers.InlineWhitespace
        from section in CIString("section")
        from requiredWhitespace in CommonParsers.RequiredWhitespace
        from name in restOfLine
        from lineEnd in CommonParsers.LineEnd
        select name.Trim();

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

    static TimeSpan ParseDuration(int num, char unit) =>
        unit switch
        {
            'd' => TimeSpan.FromDays(num),
            'w' => TimeSpan.FromDays(num * 7),
            'h' => TimeSpan.FromHours(num),
            _ => TimeSpan.FromDays(num)
        };

    // Task line parser - handles multiple formats
    // Format: Task name :modifiers, id, start, duration
    // Examples:
    //   Task A :a1, 2024-01-01, 30d
    //   Task B :done, after a1, 20d
    //   Task C :crit, milestone, 2024-02-01, 0d
    static Parser<char, GanttTask> taskParser =
        from _ in CommonParsers.InlineWhitespace
        from name in Token(_ => _ != ':' && _ != '\r' && _ != '\n').AtLeastOnceString()
        from __ in CommonParsers.InlineWhitespace
        from colon in Char(':')
        from ____ in CommonParsers.InlineWhitespace
        from parts in Token(c => c != '\r' && c != '\n').ManyString()
        from lineEnd in CommonParsers.LineEnd
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
            if (part.All(_ => char.IsLetterOrDigit(_) || _ == '_' || _ == '-'))
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
    static Parser<char, Unit> skipLine =
        CommonParsers.InlineWhitespace
            .Then(Try(CommonParsers.Comment).Or(CommonParsers.Newline));

    // Content item
    static Parser<char, object?> ContentItem =>
        OneOf(
            Try(titleParser.Select(t => (object?) ("title", t))),
            Try(dateFormatParser.Select(f => (object?) ("dateFormat", f))),
            Try(axisFormatParser.Select(f => (object?) ("axisFormat", f))),
            Try(excludesParser.Select(e => (object?) ("excludes", e))),
            Try(sectionParser.Select(s => (object?) ("section", s))),
            Try(taskParser.Select(t => (object?) t)),
            skipLine.ThenReturn((object?) null)
        );

    public static Parser<char, GanttModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("gantt")
        from ___ in CommonParsers.InlineWhitespace
        from ____ in CommonParsers.LineEnd
        from content in ContentItem.Many()
        select BuildModel(content.Where(_ => _ != null).ToList());

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