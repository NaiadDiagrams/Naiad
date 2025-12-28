public class ArchitectureTests
{
    [Test]
    public Task BasicService()
    {
        var input =
            """
            architecture-beta
            service db(database)[Database]
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task ServiceWithDifferentIcons()
    {
        var input =
            """
            architecture-beta
            service db(database)[Database]
            service srv(server)[Server]
            service disk1(disk)[Storage]
            service cloud1(cloud)[Cloud]
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task ServiceWithGroup()
    {
        var input =
            """
            architecture-beta
            group api(cloud)[API]
            service db(database)[Database] in api
            service server(server)[Server] in api
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task ServicesWithEdge()
    {
        var input = """
                    architecture-beta
                    service db(database)[Database]
                    service server(server)[Server]
                    db:R -- L:server
                    """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task ComplexDiagram()
    {
        var input =
            """
            architecture-beta
            group api(cloud)[API Layer]
            service server(server)[API Server] in api
            service db(database)[Database]
            service disk1(disk)[Storage]
            server:B -- T:db
            server:R -- L:disk1
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    [Explicit("Uses arrow syntax not supported by mermaid.ink/kroki.io")]
    public Task EdgeWithArrows()
    {
        var input =
            """
            architecture-beta
            service client(internet)[Client]
            service server(server)[Server]
            service db(database)[Database]
            <client:R -- L>:server
            server:B -- T>:db
            """;

        return SvgVerify.Verify(input);
    }
}