namespace Naiad;

public static class Mermaid
{
    public static string Render(string input, RenderOptions? options = null)
    {
        options ??= RenderOptions.Default;
        return ToXml(RenderToSvgDocument(input, options), options);
    }

    /// <summary>
    /// Renders to the in-memory <see cref="SvgDocument"/> rather than serialised markup. This is the
    /// seam the PNG render packages (Naiad.Skia, Naiad.ImageSharp) rasterize, so they share the exact
    /// parse → layout → model pipeline that produces the SVG and differ only in how the document is
    /// turned into pixels.
    /// </summary>
    internal static SvgDocument RenderToSvgDocument(string input, RenderOptions? options = null)
    {
        IconPackRegistry.MarkRendered();
        input = input.Trim();
        options ??= RenderOptions.Default;
        input = StripInitBlock(input);
        var diagramType = DetectDiagramType(input);

        return diagramType switch
        {
            DiagramType.Pie => RenderPie(input, options),
            DiagramType.Flowchart => RenderFlowchart(input, options),
            DiagramType.Sequence => RenderSequence(input, options),
            DiagramType.Class => RenderClass(input, options),
            DiagramType.State => RenderState(input, options),
            DiagramType.EntityRelationship => RenderEntityRelationship(input, options),
            DiagramType.GitGraph => RenderGitGraph(input, options),
            DiagramType.Gantt => RenderGantt(input, options),
            DiagramType.Mindmap => RenderMindmap(input, options),
            DiagramType.Timeline => RenderTimeline(input, options),
            DiagramType.UserJourney => RenderUserJourney(input, options),
            DiagramType.Quadrant => RenderQuadrant(input, options),
            DiagramType.XYChart => RenderXYChart(input, options),
            DiagramType.Sankey => RenderSankey(input, options),
            DiagramType.Block => RenderBlock(input, options),
            DiagramType.Kanban => RenderKanban(input, options),
            DiagramType.Packet => RenderPacket(input, options),
            DiagramType.C4Context => RenderC4(input, options),
            DiagramType.C4Container => RenderC4(input, options),
            DiagramType.C4Component => RenderC4(input, options),
            DiagramType.C4Deployment => RenderC4(input, options),
            DiagramType.Requirement => RenderRequirement(input, options),
            DiagramType.Architecture => RenderArchitecture(input, options),
            DiagramType.Radar => RenderRadar(input, options),
            DiagramType.Treemap => RenderTreemap(input, options),
            _ => throw new MermaidException($"Unsupported diagram type: {diagramType}")
        };
    }

    /// <summary>
    /// Detects the <see cref="DiagramType"/> from the opening keyword of Mermaid <paramref name="input"/>,
    /// skipping any leading <c>%%{init:...}%%</c> configuration blocks. Unlike rendering this never throws:
    /// it returns <see langword="false"/> for empty input or markup whose first line names no known diagram,
    /// so callers such as the live editor can label whatever is currently being typed.
    /// </summary>
    public static bool TryDetectType(string? input, out DiagramType type)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            type = default;
            return false;
        }

        return TryMatchType(StripInitBlock(input.Trim()), out type);
    }

    static DiagramType DetectDiagramType(string input)
    {
        var firstLine = input.TrimStart();

        // Skip %%{init:...}%% configuration blocks
        while (firstLine.StartsWith("%%{", StringComparison.Ordinal))
        {
            // Find end of init block and move to next line
            var endIndex = firstLine.IndexOf("}%%", StringComparison.Ordinal);
            if (endIndex < 0)
                break;

            firstLine = firstLine[(endIndex + 3)..].TrimStart();

            // Skip past any newline
            var newlineIndex = firstLine.IndexOfAny(['\r', '\n']);
            if (newlineIndex >= 0)
            {
                firstLine = firstLine[(newlineIndex + 1)..].TrimStart();
            }
        }

        if (TryMatchType(firstLine, out var type))
            return type;

        throw new MermaidException($"Unknown diagram type in: {firstLine.Split('\n')[0]}");
    }

    static bool TryMatchType(string firstLine, out DiagramType type)
    {
        if (firstLine.StartsWith("pie", StringComparison.OrdinalIgnoreCase))
        {
            type = DiagramType.Pie;
        }
        else if (firstLine.StartsWith("flowchart", StringComparison.OrdinalIgnoreCase) ||
                 firstLine.StartsWith("graph", StringComparison.OrdinalIgnoreCase))
        {
            type = DiagramType.Flowchart;
        }
        else if (firstLine.StartsWith("sequenceDiagram", StringComparison.OrdinalIgnoreCase))
        {
            type = DiagramType.Sequence;
        }
        else if (firstLine.StartsWith("classDiagram", StringComparison.OrdinalIgnoreCase))
        {
            type = DiagramType.Class;
        }
        else if (firstLine.StartsWith("stateDiagram", StringComparison.OrdinalIgnoreCase))
        {
            type = DiagramType.State;
        }
        else if (firstLine.StartsWith("erDiagram", StringComparison.OrdinalIgnoreCase))
        {
            type = DiagramType.EntityRelationship;
        }
        else if (firstLine.StartsWith("gantt", StringComparison.OrdinalIgnoreCase))
        {
            type = DiagramType.Gantt;
        }
        else if (firstLine.StartsWith("gitGraph", StringComparison.OrdinalIgnoreCase))
        {
            type = DiagramType.GitGraph;
        }
        else if (firstLine.StartsWith("mindmap", StringComparison.OrdinalIgnoreCase))
        {
            type = DiagramType.Mindmap;
        }
        else if (firstLine.StartsWith("timeline", StringComparison.OrdinalIgnoreCase))
        {
            type = DiagramType.Timeline;
        }
        else if (firstLine.StartsWith("journey", StringComparison.OrdinalIgnoreCase))
        {
            type = DiagramType.UserJourney;
        }
        else if (firstLine.StartsWith("quadrantChart", StringComparison.OrdinalIgnoreCase))
        {
            type = DiagramType.Quadrant;
        }
        else if (firstLine.StartsWith("xychart", StringComparison.OrdinalIgnoreCase))
        {
            type = DiagramType.XYChart;
        }
        else if (firstLine.StartsWith("sankey", StringComparison.OrdinalIgnoreCase))
        {
            type = DiagramType.Sankey;
        }
        else if (firstLine.StartsWith("block", StringComparison.OrdinalIgnoreCase))
        {
            type = DiagramType.Block;
        }
        else if (firstLine.StartsWith("packet", StringComparison.OrdinalIgnoreCase))
        {
            type = DiagramType.Packet;
        }
        else if (firstLine.StartsWith("kanban", StringComparison.OrdinalIgnoreCase))
        {
            type = DiagramType.Kanban;
        }
        else if (firstLine.StartsWith("architecture-beta", StringComparison.OrdinalIgnoreCase) ||
                 firstLine.StartsWith("architecture", StringComparison.OrdinalIgnoreCase))
        {
            type = DiagramType.Architecture;
        }
        else if (firstLine.StartsWith("C4Context", StringComparison.OrdinalIgnoreCase))
        {
            type = DiagramType.C4Context;
        }
        else if (firstLine.StartsWith("C4Container", StringComparison.OrdinalIgnoreCase))
        {
            type = DiagramType.C4Container;
        }
        else if (firstLine.StartsWith("C4Component", StringComparison.OrdinalIgnoreCase))
        {
            type = DiagramType.C4Component;
        }
        else if (firstLine.StartsWith("C4Deployment", StringComparison.OrdinalIgnoreCase))
        {
            type = DiagramType.C4Deployment;
        }
        else if (firstLine.StartsWith("requirementDiagram", StringComparison.OrdinalIgnoreCase))
        {
            type = DiagramType.Requirement;
        }
        else if (firstLine.StartsWith("radar-beta", StringComparison.OrdinalIgnoreCase) ||
                 firstLine.StartsWith("radar", StringComparison.OrdinalIgnoreCase))
        {
            type = DiagramType.Radar;
        }
        else if (firstLine.StartsWith("treemap-beta", StringComparison.OrdinalIgnoreCase) ||
                 firstLine.StartsWith("treemap", StringComparison.OrdinalIgnoreCase))
        {
            type = DiagramType.Treemap;
        }
        else
        {
            type = default;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Strips %%{init:...}%% configuration blocks from the beginning of input.
    /// </summary>
    static string StripInitBlock(string input)
    {
        var result = input.TrimStart();

        while (result.StartsWith("%%{", StringComparison.Ordinal))
        {
            var endIndex = result.IndexOf("}%%", StringComparison.Ordinal);
            if (endIndex < 0)
                break;

            result = result[(endIndex + 3)..].TrimStart();
        }

        return result;
    }

    static string ToXml(SvgDocument svg, RenderOptions options)
    {
        if (!options.AllowHtmlElements)
        {
            // The Font Awesome @import is the only HTML (xhtml-namespaced) markup not produced
            // through the label seam, so drop it here when HTML output is disabled.
            svg.FontAwesomeImport = null;
        }

        var builder = new StringBuilder();
        svg.ToXml(builder);
        return builder.ToString();
    }

    static SvgDocument RenderPie(string input, RenderOptions options)
    {
        var parser = new PieParser();
        var result = parser.Parse(input);

        if (!result.Success)
        {
            throw new MermaidParseException($"Failed to parse pie chart: {result.Error}");
        }

        var renderer = new PieRenderer();
        return renderer.Render(result.Value, options);
    }

    static SvgDocument RenderFlowchart(string input, RenderOptions options)
    {
        var parser = new FlowchartParser();
        var result = parser.Parse(input);

        if (!result.Success)
        {
            throw new MermaidParseException($"Failed to parse flowchart: {result.Error}");
        }

        var renderer = new FlowchartRenderer();
        return renderer.Render(result.Value, options);
    }

    static SvgDocument RenderSequence(string input, RenderOptions options)
    {
        var parser = new SequenceParser();
        var result = parser.Parse(input);

        if (!result.Success)
        {
            throw new MermaidParseException($"Failed to parse sequence diagram: {result.Error}");
        }

        var renderer = new SequenceRenderer();
        return renderer.Render(result.Value, options);
    }

    static SvgDocument RenderClass(string input, RenderOptions options)
    {
        var parser = new ClassParser();
        var result = parser.Parse(input);

        if (!result.Success)
        {
            throw new MermaidParseException($"Failed to parse class diagram: {result.Error}");
        }

        var renderer = new ClassRenderer();
        return renderer.Render(result.Value, options);
    }

    static SvgDocument RenderState(string input, RenderOptions options)
    {
        var parser = new StateParser();
        var result = parser.Parse(input);

        if (!result.Success)
        {
            throw new MermaidParseException($"Failed to parse state diagram: {result.Error}");
        }

        var renderer = new StateRenderer();
        return renderer.Render(result.Value, options);
    }

    static SvgDocument RenderEntityRelationship(string input, RenderOptions options)
    {
        var parser = new ERParser();
        var result = parser.Parse(input);

        if (!result.Success)
        {
            throw new MermaidParseException($"Failed to parse ER diagram: {result.Error}");
        }

        var renderer = new ERRenderer();
        return renderer.Render(result.Value, options);
    }

    static SvgDocument RenderGitGraph(string input, RenderOptions options)
    {
        var parser = new GitGraphParser();
        var result = parser.Parse(input);

        if (!result.Success)
        {
            throw new MermaidParseException($"Failed to parse git graph: {result.Error}");
        }

        var renderer = new GitGraphRenderer();
        return renderer.Render(result.Value, options);
    }

    static SvgDocument RenderGantt(string input, RenderOptions options)
    {
        var parser = new GanttParser();
        var result = parser.Parse(input);

        if (!result.Success)
        {
            throw new MermaidParseException($"Failed to parse gantt chart: {result.Error}");
        }

        var renderer = new GanttRenderer();
        return renderer.Render(result.Value, options);
    }

    static SvgDocument RenderMindmap(string input, RenderOptions options)
    {
        var parser = new MindmapParser();
        var result = parser.Parse(input);

        if (!result.Success)
        {
            throw new MermaidParseException($"Failed to parse mindmap: {result.Error}");
        }

        var renderer = new MindmapRenderer();
        return renderer.Render(result.Value, options);
    }

    static SvgDocument RenderTimeline(string input, RenderOptions options)
    {
        var parser = new TimelineParser();
        var result = parser.Parse(input);

        if (!result.Success)
        {
            throw new MermaidParseException($"Failed to parse timeline: {result.Error}");
        }

        var renderer = new TimelineRenderer();
        return renderer.Render(result.Value, options);
    }

    static SvgDocument RenderUserJourney(string input, RenderOptions options)
    {
        var parser = new UserJourneyParser();
        var result = parser.Parse(input);

        if (!result.Success)
        {
            throw new MermaidParseException($"Failed to parse user journey: {result.Error}");
        }

        var renderer = new UserJourneyRenderer();
        return renderer.Render(result.Value, options);
    }

    static SvgDocument RenderQuadrant(string input, RenderOptions options)
    {
        var parser = new QuadrantParser();
        var result = parser.Parse(input);

        if (!result.Success)
        {
            throw new MermaidParseException($"Failed to parse quadrant chart: {result.Error}");
        }

        var renderer = new QuadrantRenderer();
        return renderer.Render(result.Value, options);
    }

    static SvgDocument RenderXYChart(string input, RenderOptions options)
    {
        var parser = new XYChartParser();
        var result = parser.Parse(input);

        if (!result.Success)
        {
            throw new MermaidParseException($"Failed to parse XY chart: {result.Error}");
        }

        var renderer = new XYChartRenderer();
        return renderer.Render(result.Value, options);
    }

    static SvgDocument RenderSankey(string input, RenderOptions options)
    {
        var parser = new SankeyParser();
        var result = parser.Parse(input);

        if (!result.Success)
        {
            throw new MermaidParseException($"Failed to parse Sankey diagram: {result.Error}");
        }

        var renderer = new SankeyRenderer();
        return renderer.Render(result.Value, options);
    }

    static SvgDocument RenderBlock(string input, RenderOptions options)
    {
        var parser = new BlockParser();
        var result = parser.Parse(input);

        if (!result.Success)
        {
            throw new MermaidParseException($"Failed to parse block diagram: {result.Error}");
        }

        var renderer = new BlockRenderer();
        return renderer.Render(result.Value, options);
    }

    static SvgDocument RenderKanban(string input, RenderOptions options)
    {
        var parser = new KanbanParser();
        var result = parser.Parse(input);

        if (!result.Success)
        {
            throw new MermaidParseException($"Failed to parse kanban board: {result.Error}");
        }

        var renderer = new KanbanRenderer();
        return renderer.Render(result.Value, options);
    }

    static SvgDocument RenderPacket(string input, RenderOptions options)
    {
        var parser = new PacketParser();
        var result = parser.Parse(input);

        if (!result.Success)
        {
            throw new MermaidParseException($"Failed to parse packet diagram: {result.Error}");
        }

        var renderer = new PacketRenderer();
        return renderer.Render(result.Value, options);
    }

    static SvgDocument RenderC4(string input, RenderOptions options)
    {
        var parser = new C4Parser();
        var result = parser.Parse(input);

        if (!result.Success)
        {
            throw new MermaidParseException($"Failed to parse C4 diagram: {result.Error}");
        }

        var renderer = new C4Renderer();
        return renderer.Render(result.Value, options);
    }

    static SvgDocument RenderRequirement(string input, RenderOptions options)
    {
        var parser = new RequirementParser();
        var result = parser.Parse(input);

        if (!result.Success)
        {
            throw new MermaidParseException($"Failed to parse requirement diagram: {result.Error}");
        }

        var renderer = new RequirementRenderer();
        return renderer.Render(result.Value, options);
    }

    static SvgDocument RenderArchitecture(string input, RenderOptions options)
    {
        var parser = new ArchitectureParser();
        var result = parser.Parse(input);

        if (!result.Success)
        {
            throw new MermaidParseException($"Failed to parse architecture diagram: {result.Error}");
        }

        var renderer = new ArchitectureRenderer();
        return renderer.Render(result.Value, options);
    }

    static SvgDocument RenderRadar(string input, RenderOptions options)
    {
        var parser = new RadarParser();
        var result = parser.Parse(input);

        if (!result.Success)
        {
            throw new MermaidParseException($"Failed to parse radar diagram: {result.Error}");
        }

        var renderer = new RadarRenderer();
        return renderer.Render(result.Value, options);
    }

    static SvgDocument RenderTreemap(string input, RenderOptions options)
    {
        var parser = new TreemapParser();
        var result = parser.Parse(input);

        if (!result.Success)
        {
            throw new MermaidParseException($"Failed to parse treemap diagram: {result.Error}");
        }

        var renderer = new TreemapRenderer();
        return renderer.Render(result.Value, options);
    }
}
