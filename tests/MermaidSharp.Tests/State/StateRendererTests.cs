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


        return SvgVerify.Verify(input);
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


        return SvgVerify.Verify(input);
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


        return SvgVerify.Verify(input);
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


        return SvgVerify.Verify(input);
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


        return SvgVerify.Verify(input);
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


        return SvgVerify.Verify(input);
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


        return SvgVerify.Verify(input);
    }

    [Test]
    public Task Render_StateDiagramV1()
    {
        const string input = """
            stateDiagram
                [*] --> Still
                Still --> [*]
            """;


        return SvgVerify.Verify(input);
    }

    [Test]
    public Task Render_CompleteStateDiagram()
    {
        const string input = """
            stateDiagram-v2
                [*] --> Idle

                state "Processing State" as Processing
                state fork_state <<fork>>
                state join_state <<join>>
                state choice_state <<choice>>

                Idle --> Processing : start
                Processing --> fork_state
                fork_state --> TaskA
                fork_state --> TaskB
                TaskA --> join_state
                TaskB --> join_state
                join_state --> choice_state
                choice_state --> Success : if valid
                choice_state --> Error : if invalid
                Success --> Idle : reset
                Error --> Idle : retry
                Success --> [*] : complete

                note right of Processing : This is a processing note
                note left of Error : Error handling
            """;


        return SvgVerify.Verify(input);
    }
}
