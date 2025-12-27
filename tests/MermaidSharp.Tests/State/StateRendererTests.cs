using TUnit.Core;

namespace MermaidSharp.Tests.State;

public class StateRendererTests
{
    [Test]
    public Task Render_SimpleState()
    {
        const string input = """
            stateDiagram-v2
                [*] --> Still
                Still --> [*]
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_MultipleStates()
    {
        const string input = """
            stateDiagram-v2
                [*] --> Still
                Still --> Moving
                Moving --> Still
                Moving --> Crash
                Crash --> [*]
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_StateWithTransitionLabels()
    {
        const string input = """
            stateDiagram-v2
                [*] --> Active
                Active --> Inactive : timeout
                Inactive --> Active : reset
                Active --> [*] : shutdown
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_StateWithDescription()
    {
        const string input = """
            stateDiagram-v2
                state "This is a state description" as s1
                [*] --> s1
                s1 --> [*]
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_ForkJoinState()
    {
        const string input = """
            stateDiagram-v2
                state fork_state <<fork>>
                [*] --> fork_state
                fork_state --> State2
                fork_state --> State3
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_ChoiceState()
    {
        const string input = """
            stateDiagram-v2
                state choice_state <<choice>>
                [*] --> IsPositive
                IsPositive --> choice_state
                choice_state --> Positive : if n > 0
                choice_state --> Negative : if n < 0
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_StateWithNote()
    {
        const string input = """
            stateDiagram-v2
                [*] --> Active
                Active --> [*]
                note right of Active : Important note
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_StateDiagramV1()
    {
        const string input = """
            stateDiagram
                [*] --> Still
                Still --> [*]
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }
}
