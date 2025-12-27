public class UserJourneyRendererTests
{
    [Test]
    public Task Render_SimpleJourney()
    {
        const string input = """
            journey
                title My Working Day
                section Morning
                    Make coffee: 5: Me
                    Check emails: 3: Me
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg, extension: "svg");
    }

    [Test]
    public Task Render_MultipleSections()
    {
        const string input = """
            journey
                title Customer Journey
                section Discovery
                    Visit website: 4: Customer
                    Browse products: 3: Customer
                section Purchase
                    Add to cart: 4: Customer
                    Checkout: 2: Customer
                section Delivery
                    Track order: 5: Customer
                    Receive package: 5: Customer
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg, extension: "svg");
    }

    [Test]
    public Task Render_MultipleActors()
    {
        const string input = """
            journey
                title Team Collaboration
                section Planning
                    Define requirements: 4: PM, Dev
                    Create design: 3: Designer, PM
                section Development
                    Write code: 4: Dev
                    Code review: 3: Dev, Lead
                section Testing
                    Test features: 4: QA, Dev
                    Fix bugs: 2: Dev
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg, extension: "svg");
    }

    [Test]
    public Task Render_AllScores()
    {
        const string input = """
            journey
                title Score Examples
                section Experience
                    Terrible: 1: User
                    Bad: 2: User
                    Okay: 3: User
                    Good: 4: User
                    Great: 5: User
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg, extension: "svg");
    }

    [Test]
    public Task Render_WithoutTitle()
    {
        const string input = """
            journey
                section Tasks
                    First task: 4: Alice
                    Second task: 5: Bob
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg, extension: "svg");
    }

    [Test]
    public Task Render_SingleSection()
    {
        const string input = """
            journey
                title Quick Journey
                section Main
                    Start: 3: User
                    Process: 4: User
                    End: 5: User
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg, extension: "svg");
    }

    [Test]
    public Task Render_ManyActors()
    {
        const string input = """
            journey
                title Big Team Project
                section Kickoff
                    Initial meeting: 4: PM, Dev, QA, Designer, Lead, Stakeholder
                section Execution
                    Development: 3: Dev, Lead
                    Testing: 4: QA
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg, extension: "svg");
    }
}
