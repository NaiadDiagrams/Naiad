namespace Benchmarks;

static class LargeFixtures
{
    public static readonly string Flowchart = BuildFlowchart();
    public static readonly string Sequence = BuildSequence();
    public static readonly string Class = BuildClass();
    public static readonly string State = BuildState();
    public static readonly string ER = BuildEr();
    public static readonly string Mindmap = BuildMindmap();
    public static readonly string Gantt = BuildGantt();

    static string BuildFlowchart()
    {
        // 5 layers x 10 nodes, each node fans out to 2 nodes in the next layer
        var builder = new StringBuilder();
        builder.AppendLine("flowchart TD");
        const int layers = 5;
        const int width = 10;
        string[] icons = ["server", "database", "cog", "cloud", "user", "lock"];
        string[] prefixes = ["fa", "fas", "fab", "far"];
        var declared = new HashSet<string>();

        string Decl(int layer, int index)
        {
            var id = $"L{layer}_{index}";
            if (!declared.Add(id))
            {
                return id;
            }
            var idx = layer * width + index;
            if (idx % 2 == 0)
            {
                var prefix = prefixes[idx % prefixes.Length];
                var icon = icons[idx % icons.Length];
                return $"{id}[{prefix}:fa-{icon} Service {layer}-{index}]";
            }
            return $"{id}[Service {layer}-{index}]";
        }

        for (var l = 0; l < layers - 1; l++)
        {
            for (var i = 0; i < width; i++)
            {
                builder.AppendLine($"    {Decl(l, i)} --> {Decl(l + 1, i)}");
                builder.AppendLine($"    {Decl(l, i)} --> {Decl(l + 1, (i + 1) % width)}");
            }
        }
        return builder.ToString();
    }

    static string BuildSequence()
    {
        var builder = new StringBuilder();
        builder.AppendLine("sequenceDiagram");
        string[] actors = ["Alice", "Bob", "Carol", "Dave", "Eve", "Frank", "Grace", "Heidi"];
        for (var i = 0; i < 60; i++)
        {
            var from = actors[i % actors.Length];
            var to = actors[(i + 1) % actors.Length];
            var arrow = i % 2 == 0 ? "->>" : "-->>";
            builder.AppendLine($"    {from}{arrow}{to}: Message {i}");
        }
        return builder.ToString();
    }

    static string BuildClass()
    {
        // 20 classes: a chain of inheritance plus a few cross associations
        var builder = new StringBuilder();
        builder.AppendLine("classDiagram");
        const int count = 20;
        for (var i = 0; i < count; i++)
        {
            builder.AppendLine($"    class C{i}");
        }
        for (var i = 1; i < count; i++)
        {
            builder.AppendLine($"    C{i - 1} <|-- C{i}");
        }
        for (var i = 0; i < count - 2; i += 2)
        {
            builder.AppendLine($"    C{i} --> C{i + 2}");
        }
        return builder.ToString();
    }

    static string BuildState()
    {
        var builder = new StringBuilder();
        builder.AppendLine("stateDiagram-v2");
        builder.AppendLine("    [*] --> S0");
        const int count = 20;
        for (var i = 0; i < count - 1; i++)
        {
            builder.AppendLine($"    S{i} --> S{i + 1}");
        }
        for (var i = 0; i < count - 2; i += 3)
        {
            builder.AppendLine($"    S{i} --> S{i + 2}");
        }
        builder.AppendLine($"    S{count - 1} --> [*]");
        return builder.ToString();
    }

    static string BuildEr()
    {
        var builder = new StringBuilder();
        builder.AppendLine("erDiagram");
        const int count = 15;
        for (var i = 0; i < count - 1; i++)
        {
            builder.AppendLine($"    E{i} ||--o{{ E{i + 1} : has");
        }
        return builder.ToString();
    }

    static string BuildMindmap()
    {
        // depth 4, branching 3 -> 1 + 3 + 9 + 27 = 40 nodes
        var builder = new StringBuilder();
        builder.AppendLine("mindmap");
        builder.AppendLine("  Root");
        for (var a = 0; a < 3; a++)
        {
            builder.AppendLine($"    Branch{a}");
            for (var b = 0; b < 3; b++)
            {
                builder.AppendLine($"      Sub{a}_{b}");
                for (var c = 0; c < 3; c++)
                {
                    builder.AppendLine($"        Leaf{a}_{b}_{c}");
                }
            }
        }
        return builder.ToString();
    }

    static string BuildGantt()
    {
        var builder = new StringBuilder();
        builder.AppendLine("gantt");
        builder.AppendLine("    title Large Gantt");
        builder.AppendLine("    dateFormat YYYY-MM-DD");
        for (var s = 0; s < 3; s++)
        {
            builder.AppendLine($"    section Section{s}");
            for (var t = 0; t < 10; t++)
            {
                var idx = s * 10 + t;
                var day = idx % 28 + 1;
                builder.AppendLine($"    Task {idx} :t{idx}, 2024-01-{day:D2}, {3 + t % 5}d");
            }
        }
        return builder.ToString();
    }
}
