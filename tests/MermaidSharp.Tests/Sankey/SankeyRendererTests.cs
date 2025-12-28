public class SankeyRendererTests
{
    [Test]
    public Task SimpleSankey()
    {
        const string input =
            """
            sankey-beta
            A,B,10
            A,C,20
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task ThreeColumns()
    {
        const string input =
            """
            sankey-beta
            Source,Middle,30
            Middle,Target,30
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task EnergyFlow()
    {
        const string input =
            """
            sankey-beta
            Coal,Electricity,100
            Gas,Electricity,50
            Nuclear,Electricity,30
            Electricity,Industry,80
            Electricity,Residential,60
            Electricity,Commercial,40
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task BudgetFlow()
    {
        const string input =
            """
            sankey-beta
            Salary,Savings,1000
            Salary,Expenses,3000
            Expenses,Housing,1500
            Expenses,Food,800
            Expenses,Transport,400
            Expenses,Other,300
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task MultipleSourcesAndTargets()
    {
        const string input =
            """
            sankey-beta
            A,X,10
            A,Y,15
            B,X,20
            B,Y,25
            B,Z,5
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task SingleLink()
    {
        const string input =
            """
            sankey-beta
            Input,Output,100
            """;

        return SvgVerify.Verify(input);
    }
}
