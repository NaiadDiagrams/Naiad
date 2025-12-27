public class ArchitectureTests
{
    [Test]
    public async Task BasicService()
    {
        var input = """
            architecture-beta
            service db(database)[Database]
            """;

        var svg = Mermaid.Render(input);
        await Verify(svg);
    }

    [Test]
    public async Task ServiceWithDifferentIcons()
    {
        var input = """
            architecture-beta
            service db(database)[Database]
            service srv(server)[Server]
            service disk1(disk)[Storage]
            service cloud1(cloud)[Cloud]
            """;

        var svg = Mermaid.Render(input);
        await Verify(svg);
    }

    [Test]
    public async Task ServiceWithGroup()
    {
        var input = """
            architecture-beta
            group api(cloud)[API]
            service db(database)[Database] in api
            service server(server)[Server] in api
            """;

        var svg = Mermaid.Render(input);
        await Verify(svg);
    }

    [Test]
    public async Task ServicesWithEdge()
    {
        var input = """
            architecture-beta
            service db(database)[Database]
            service server(server)[Server]
            db:R -- L:server
            """;

        var svg = Mermaid.Render(input);
        await Verify(svg);
    }

    [Test]
    public async Task ComplexDiagram()
    {
        var input = """
            architecture-beta
            group api(cloud)[API Layer]
            service server(server)[API Server] in api
            service db(database)[Database]
            service disk1(disk)[Storage]
            server:B -- T:db
            server:R -- L:disk1
            """;

        var svg = Mermaid.Render(input);
        await Verify(svg);
    }

    [Test]
    public async Task EdgeWithArrows()
    {
        var input = """
            architecture-beta
            service client(internet)[Client]
            service server(server)[Server]
            service db(database)[Database]
            <client:R -- L>:server
            server:B -- T>:db
            """;

        var svg = Mermaid.Render(input);
        await Verify(svg);
    }
}
