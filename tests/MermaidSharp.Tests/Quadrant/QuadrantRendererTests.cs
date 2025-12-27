public class QuadrantRendererTests
{
    [Test]
    public Task Render_SimpleQuadrant()
    {
        const string input = """
            quadrantChart
                title Campaign Analysis
                x-axis Low Reach --> High Reach
                y-axis Low Engagement --> High Engagement
                Campaign A: [0.3, 0.6]
                Campaign B: [0.7, 0.8]
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_WithQuadrantLabels()
    {
        const string input = """
            quadrantChart
                title Priority Matrix
                x-axis Low Urgency --> High Urgency
                y-axis Low Impact --> High Impact
                quadrant-1 Do First
                quadrant-2 Schedule
                quadrant-3 Delegate
                quadrant-4 Eliminate
                Task A: [0.8, 0.9]
                Task B: [0.2, 0.8]
                Task C: [0.2, 0.2]
                Task D: [0.9, 0.3]
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_ManyPoints()
    {
        const string input = """
            quadrantChart
                title Product Portfolio
                x-axis Low Growth --> High Growth
                y-axis Low Share --> High Share
                Product A: [0.1, 0.9]
                Product B: [0.9, 0.9]
                Product C: [0.1, 0.1]
                Product D: [0.9, 0.1]
                Product E: [0.5, 0.5]
                Product F: [0.3, 0.7]
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_MinimalQuadrant()
    {
        const string input = """
            quadrantChart
                Point 1: [0.25, 0.75]
                Point 2: [0.75, 0.25]
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_WithTitleOnly()
    {
        const string input = """
            quadrantChart
                title Skills Assessment
                x-axis Beginner --> Expert
                y-axis Low Priority --> High Priority
                Python: [0.8, 0.9]
                JavaScript: [0.7, 0.7]
                Rust: [0.3, 0.5]
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_EdgePositions()
    {
        const string input = """
            quadrantChart
                title Edge Cases
                x-axis Left --> Right
                y-axis Bottom --> Top
                Top Right: [1.0, 1.0]
                Top Left: [0.0, 1.0]
                Bottom Left: [0.0, 0.0]
                Bottom Right: [1.0, 0.0]
                Center: [0.5, 0.5]
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }
}
