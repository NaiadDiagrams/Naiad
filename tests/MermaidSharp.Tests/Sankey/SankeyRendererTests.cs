public class SankeyRendererTests
{
    [Test]
    public Task Render_SimpleSankey()
    {
        const string input = """
            sankey-beta
            A,B,10
            A,C,20
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_ThreeColumns()
    {
        const string input = """
            sankey-beta
            Source,Middle,30
            Middle,Target,30
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_EnergyFlow()
    {
        const string input = """
            sankey-beta
            Coal,Electricity,100
            Gas,Electricity,50
            Nuclear,Electricity,30
            Electricity,Industry,80
            Electricity,Residential,60
            Electricity,Commercial,40
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_BudgetFlow()
    {
        const string input = """
            sankey-beta
            Salary,Savings,1000
            Salary,Expenses,3000
            Expenses,Housing,1500
            Expenses,Food,800
            Expenses,Transport,400
            Expenses,Other,300
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_MultipleSourcesAndTargets()
    {
        const string input = """
            sankey-beta
            A,X,10
            A,Y,15
            B,X,20
            B,Y,25
            B,Z,5
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_SingleLink()
    {
        const string input = """
            sankey-beta
            Input,Output,100
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }
}
