namespace MermaidSharp.Diagrams.State;

public class StateModel : DiagramBase
{
    public List<State> States { get; } = [];
    public List<StateTransition> Transitions { get; } = [];
    public List<StateNote> Notes { get; } = [];
}