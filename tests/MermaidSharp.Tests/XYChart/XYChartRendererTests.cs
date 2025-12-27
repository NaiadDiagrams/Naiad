using TUnit.Core;

namespace MermaidSharp.Tests.XYChart;

public class XYChartRendererTests
{
    [Test]
    public Task Render_SimpleBarChart()
    {
        const string input = """
            xychart-beta
                title "Monthly Sales"
                x-axis [Jan, Feb, Mar, Apr, May]
                y-axis "Revenue" 0 --> 100
                bar [50, 60, 75, 80, 90]
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_SimpleLineChart()
    {
        const string input = """
            xychart-beta
                title "Temperature Trend"
                x-axis [Mon, Tue, Wed, Thu, Fri]
                y-axis "Celsius" 0 --> 40
                line [20, 22, 25, 23, 28]
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_BarAndLine()
    {
        const string input = """
            xychart-beta
                title "Sales vs Target"
                x-axis [Q1, Q2, Q3, Q4]
                y-axis "Amount" 0 --> 200
                bar [120, 150, 180, 160]
                line [100, 140, 170, 190]
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_MultipleBarSeries()
    {
        const string input = """
            xychart-beta
                title "Product Comparison"
                x-axis [2020, 2021, 2022, 2023]
                y-axis "Units" 0 --> 500
                bar [100, 150, 200, 250]
                bar [80, 120, 180, 220]
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_WithoutTitle()
    {
        const string input = """
            xychart-beta
                x-axis [A, B, C, D]
                bar [10, 20, 30, 40]
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_WithQuotedCategories()
    {
        const string input = """
            xychart-beta
                title "Regional Sales"
                x-axis ["North America", "Europe", "Asia Pacific"]
                y-axis "Revenue (M$)" 0 --> 100
                bar [85, 72, 90]
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_LargeDataset()
    {
        const string input = """
            xychart-beta
                title "Annual Data"
                x-axis [Jan, Feb, Mar, Apr, May, Jun, Jul, Aug, Sep, Oct, Nov, Dec]
                y-axis "Value" 0 --> 100
                bar [45, 52, 61, 58, 72, 85, 91, 88, 76, 65, 55, 48]
                line [40, 48, 58, 55, 70, 82, 88, 85, 73, 62, 52, 45]
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }
}
