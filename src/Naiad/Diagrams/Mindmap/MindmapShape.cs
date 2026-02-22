namespace MermaidSharp.Diagrams.Mindmap;

public enum MindmapShape
{
    Default,      // No brackets - rectangle with rounded corners
    Square,       // [text] - square
    Rounded,      // (text) - rounded rectangle
    Circle,       // ((text)) - circle
    Bang,         // ))text(( - cloud/explosion
    Cloud,        // )text( - cloud
    Hexagon       // {{text}} - hexagon
}