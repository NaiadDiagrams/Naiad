using MermaidSharp.Models;

namespace MermaidSharp.Diagrams.Sequence;

public class SequenceModel : DiagramBase
{
    public List<Participant> Participants { get; } = [];
    public List<SequenceElement> Elements { get; } = [];
    public bool AutoNumber { get; set; }
}

public class Participant
{
    public required string Id { get; init; }
    public string? Alias { get; set; }
    public ParticipantType Type { get; set; } = ParticipantType.Participant;

    public string DisplayName => Alias ?? Id;
}

public enum ParticipantType
{
    Participant,
    Actor
}

public abstract class SequenceElement { }

public class Message : SequenceElement
{
    public required string FromId { get; init; }
    public required string ToId { get; init; }
    public string? Text { get; set; }
    public MessageType Type { get; set; } = MessageType.Solid;
    public bool Activate { get; set; }
    public bool Deactivate { get; set; }
}

public enum MessageType
{
    Solid,           // ->>
    SolidArrow,      // ->>
    Dotted,          // -->>
    DottedArrow,     // -->>
    SolidOpen,       // ->
    DottedOpen,      // -->
    SolidCross,      // -x
    DottedCross,     // --x
    SolidAsync,      // -)
    DottedAsync      // --)
}

public class Note : SequenceElement
{
    public required string Text { get; init; }
    public NotePosition Position { get; set; } = NotePosition.RightOf;
    public required string ParticipantId { get; init; }
    public string? OverParticipantId2 { get; set; }
}

public enum NotePosition
{
    LeftOf,
    RightOf,
    Over
}

public class Activation : SequenceElement
{
    public required string ParticipantId { get; init; }
    public bool IsActivate { get; set; }
}

public class Loop : SequenceElement
{
    public string? Label { get; set; }
    public List<SequenceElement> Elements { get; } = [];
}

public class Alt : SequenceElement
{
    public string? Condition { get; set; }
    public List<SequenceElement> Elements { get; } = [];
    public List<AltElse> ElseBranches { get; } = [];
}

public class AltElse
{
    public string? Condition { get; set; }
    public List<SequenceElement> Elements { get; } = [];
}

public class Opt : SequenceElement
{
    public string? Condition { get; set; }
    public List<SequenceElement> Elements { get; } = [];
}

public class Par : SequenceElement
{
    public string? Label { get; set; }
    public List<SequenceElement> Elements { get; } = [];
    public List<ParAnd> AndBranches { get; } = [];
}

public class ParAnd
{
    public string? Label { get; set; }
    public List<SequenceElement> Elements { get; } = [];
}

public class Critical : SequenceElement
{
    public string? Label { get; set; }
    public List<SequenceElement> Elements { get; } = [];
}

public class Break : SequenceElement
{
    public string? Label { get; set; }
    public List<SequenceElement> Elements { get; } = [];
}

public class Rect : SequenceElement
{
    public string? Color { get; set; }
    public List<SequenceElement> Elements { get; } = [];
}
