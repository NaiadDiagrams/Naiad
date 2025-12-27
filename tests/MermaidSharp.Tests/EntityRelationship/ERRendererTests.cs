namespace MermaidSharp.Tests.EntityRelationship;

public class ERRendererTests
{
    [Test]
    public Task Render_SimpleRelationship()
    {
        const string input = """
            erDiagram
                CUSTOMER ||--o{ ORDER : places
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_MultipleRelationships()
    {
        const string input = """
            erDiagram
                CUSTOMER ||--o{ ORDER : places
                ORDER ||--|{ LINE-ITEM : contains
                PRODUCT ||--o{ LINE-ITEM : includes
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_EntityWithAttributes()
    {
        const string input = """
            erDiagram
                CUSTOMER {
                    string name
                    string email
                    int age
                }
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_EntityWithKeyTypes()
    {
        const string input = """
            erDiagram
                CUSTOMER {
                    int id PK
                    string name
                    string email UK
                }
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_EntityWithComments()
    {
        const string input = """
            erDiagram
                CUSTOMER {
                    int id PK "Primary key"
                    string name "Customer name"
                }
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_OneToOne()
    {
        const string input = """
            erDiagram
                PERSON ||--|| PASSPORT : has
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_ZeroOrOne()
    {
        const string input = """
            erDiagram
                EMPLOYEE |o--o| PARKING-SPACE : uses
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_NonIdentifying()
    {
        const string input = """
            erDiagram
                CUSTOMER ||..o{ ORDER : places
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_CompleteERDiagram()
    {
        const string input = """
            erDiagram
                CUSTOMER {
                    int id PK
                    string name
                    string email
                }
                ORDER {
                    int id PK
                    int customer_id FK
                    date order_date
                }
                CUSTOMER ||--o{ ORDER : places
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }
}
