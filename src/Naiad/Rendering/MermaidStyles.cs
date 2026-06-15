namespace Naiad.Rendering;

/// <summary>
/// CSS styles embedded in rendered diagrams.
///
/// These were originally copied verbatim from mermaid.ink, but the bulk of those rules
/// target classes/elements Naiad never emits (animations, error icons, tooltips, .node
/// descendant selectors, clusters, etc. — Naiad sets node/edge fills as attributes). Only
/// the rules that match elements Naiad actually produces are kept; each is byte-identical to
/// its mermaid.ink original, so rendering is unchanged. The class attributes remain on the
/// elements, so consumers can still hook their own styles onto them.
/// </summary>
public static class MermaidStyles
{
    /// <summary>
    /// Document-wide text defaults shared by all diagram types.
    /// </summary>
    public const string BaseStyles = """#mermaid-svg{font-family:"trebuchet ms",verdana,arial,sans-serif;font-size:16px;fill:#333;}""";

    /// <summary>
    /// CSS styles for pie charts. Pie slices, circles, labels and the legend get their
    /// stroke/opacity/fill/font from these rules rather than from element attributes.
    /// </summary>
    public const string PieStyles = BaseStyles + """#mermaid-svg .pieCircle{stroke:black;stroke-width:2px;opacity:0.7;}#mermaid-svg .pieOuterCircle{stroke:black;stroke-width:2px;fill:none;}#mermaid-svg .pieTitleText{text-anchor:middle;font-size:25px;fill:black;font-family:"trebuchet ms",verdana,arial,sans-serif;}#mermaid-svg .slice{font-family:"trebuchet ms",verdana,arial,sans-serif;fill:#333;font-size:17px;}#mermaid-svg .legend text{fill:black;font-family:"trebuchet ms",verdana,arial,sans-serif;font-size:17px;}""";

    /// <summary>
    /// CSS styles for flowcharts. Node and edge fills are set as attributes; these rules cover
    /// marker fills, HTML (foreignObject) label text colour, and edge-label backgrounds.
    /// </summary>
    public const string FlowchartStyles = BaseStyles + "#mermaid-svg p{margin:0;}#mermaid-svg .marker{fill:#333333;stroke:#333333;}#mermaid-svg .marker.cross{stroke:#333333;}#mermaid-svg span{fill:#333;color:#333;}#mermaid-svg .flowchart-link{stroke:#333333;fill:none;}#mermaid-svg .edgeLabel{background-color:rgba(232,232,232, 0.8);text-align:center;}#mermaid-svg .edgeLabel p{background-color:rgba(232,232,232, 0.8);}#mermaid-svg .edgeLabel rect{opacity:0.5;background-color:rgba(232,232,232, 0.8);fill:rgba(232,232,232, 0.8);}";
}
