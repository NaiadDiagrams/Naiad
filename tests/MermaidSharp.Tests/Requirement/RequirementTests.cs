public class RequirementTests
{
    [Test]
    public async Task BasicRequirement()
    {
        var input = """
            requirementDiagram

            requirement test_req {
                id: 1
                text: The system shall do something
                risk: high
                verifymethod: test
            }
            """;

        var svg = Mermaid.Render(input);
        await Verify(svg);
    }

    [Test]
    public async Task FunctionalRequirement()
    {
        var input = """
            requirementDiagram

            functionalRequirement login_req {
                id: REQ-001
                text: User must be able to log in
                risk: medium
                verifymethod: demonstration
            }
            """;

        var svg = Mermaid.Render(input);
        await Verify(svg);
    }

    [Test]
    public async Task RequirementWithElement()
    {
        var input = """
            requirementDiagram

            requirement test_req {
                id: 1
                text: System requirement
                risk: low
            }

            element test_entity {
                type: simulation
            }

            test_entity - satisfies -> test_req
            """;

        var svg = Mermaid.Render(input);
        await Verify(svg);
    }

    [Test]
    public async Task MultipleRequirements()
    {
        var input = """
            requirementDiagram

            requirement req1 {
                id: REQ-001
                text: First requirement
                risk: low
            }

            requirement req2 {
                id: REQ-002
                text: Second requirement
                risk: medium
            }

            performanceRequirement perf1 {
                id: PERF-001
                text: Performance must be good
                risk: high
            }
            """;

        var svg = Mermaid.Render(input);
        await Verify(svg);
    }

    [Test]
    public async Task ComplexDiagram()
    {
        var input = """
            requirementDiagram

            requirement user_auth {
                id: REQ-001
                text: Users must authenticate
                risk: high
                verifymethod: test
            }

            functionalRequirement login_page {
                id: REQ-002
                text: System provides login page
                risk: medium
            }

            element web_app {
                type: application
                docref: /docs/webapp
            }

            element login_module {
                type: module
            }

            web_app - contains -> login_module
            login_module - satisfies -> login_page
            login_page - derives -> user_auth
            """;

        var svg = Mermaid.Render(input);
        await Verify(svg);
    }

    [Test]
    public async Task AllRelationTypes()
    {
        var input = """
            requirementDiagram

            requirement req1 {
                id: 1
                text: Main requirement
            }

            requirement req2 {
                id: 2
                text: Derived requirement
            }

            element elem1 {
                type: component
            }

            req2 - derives -> req1
            elem1 - satisfies -> req1
            elem1 - verifies -> req2
            """;

        var svg = Mermaid.Render(input);
        await Verify(svg);
    }
}
