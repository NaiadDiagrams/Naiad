namespace MermaidSharp.Diagrams.State;

public enum StateType
{
    Normal,
    Start,      // [*] as source
    End,        // [*] as target
    Fork,       // <<fork>>
    Join,       // <<join>>
    Choice      // <<choice>>
}