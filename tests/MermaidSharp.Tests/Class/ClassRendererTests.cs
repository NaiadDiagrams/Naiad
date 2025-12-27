namespace MermaidSharp.Tests.Class;

public class ClassRendererTests
{
    [Test]
    public Task Render_SimpleClass()
    {
        const string input = """
            classDiagram
                class Animal
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_ClassWithMembers()
    {
        const string input = """
            classDiagram
                class Animal {
                    +String name
                    +int age
                }
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_ClassWithMethods()
    {
        const string input = """
            classDiagram
                class Animal {
                    +makeSound()
                    +move() : void
                }
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_ClassWithMembersAndMethods()
    {
        const string input = """
            classDiagram
                class Animal {
                    +String name
                    +int age
                    +makeSound() : void
                    +move(int distance) : void
                }
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_Inheritance()
    {
        const string input = """
            classDiagram
                Animal <|-- Dog
                Animal <|-- Cat
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_Composition()
    {
        const string input = """
            classDiagram
                Car *-- Engine
                Car *-- Wheel
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_Aggregation()
    {
        const string input = """
            classDiagram
                Library o-- Book
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_Association()
    {
        const string input = """
            classDiagram
                Student --> Course : enrolls
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_InterfaceAnnotation()
    {
        const string input = """
            classDiagram
                class IFlyable {
                    <<interface>>
                    +fly() : void
                }
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_CompleteClassDiagram()
    {
        const string input = """
            classDiagram
                class Animal {
                    +String name
                    +makeSound() : void
                }
                class Dog {
                    +bark() : void
                }
                class Cat {
                    +meow() : void
                }
                Animal <|-- Dog
                Animal <|-- Cat
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }
}
