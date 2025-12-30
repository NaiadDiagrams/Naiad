namespace MermaidSharp.Diagrams.Sequence;

public class SequenceParser : IDiagramParser<SequenceModel>
{
    public DiagramType DiagramType => DiagramType.Sequence;

    // Sequence diagram identifier (no dash to avoid conflicts with arrows)
    static readonly Parser<char, string> SeqIdentifier =
        Token(c => char.IsLetterOrDigit(c) || c == '_')
            .AtLeastOnceString()
            .Labelled("identifier");

    // Participant declaration: participant/actor Name as Alias
    static readonly Parser<char, Participant> ParticipantParser =
        from _ in CommonParsers.InlineWhitespace
        from type in OneOf(
            Try(String("actor")).ThenReturn(ParticipantType.Actor),
            String("participant").ThenReturn(ParticipantType.Participant)
        )
        from __ in CommonParsers.RequiredWhitespace
        from id in SeqIdentifier
        from alias in Try(
            CommonParsers.RequiredWhitespace
                .Then(String("as"))
                .Then(CommonParsers.RequiredWhitespace)
                .Then(Token(c => c != '\r' && c != '\n').AtLeastOnceString())
        ).Optional()
        from ___ in CommonParsers.LineEnd
        select new Participant
        {
            Id = id,
            Alias = alias.HasValue ? alias.Value : null,
            Type = type
        };

    // Message arrows
    static readonly Parser<char, MessageType> MessageArrowParser =
        OneOf(
            Try(String("-->>")).ThenReturn(MessageType.DottedArrow),
            Try(String("->>")).ThenReturn(MessageType.SolidArrow),
            Try(String("--x")).ThenReturn(MessageType.DottedCross),
            Try(String("-x")).ThenReturn(MessageType.SolidCross),
            Try(String("--)")).ThenReturn(MessageType.DottedAsync),
            Try(String("-)")).ThenReturn(MessageType.SolidAsync),
            Try(String("-->")).ThenReturn(MessageType.DottedOpen),
            String("->").ThenReturn(MessageType.SolidOpen)
        );

    // Message: From->>To: Text
    static readonly Parser<char, Message> MessageParser =
        from _ in CommonParsers.InlineWhitespace
        from fromId in SeqIdentifier
        from __ in CommonParsers.InlineWhitespace
        from arrow in MessageArrowParser
        from activate in Char('+').Optional()
        from deactivate in Char('-').Optional()
        from ___ in CommonParsers.InlineWhitespace
        from toId in SeqIdentifier
        from ____ in CommonParsers.InlineWhitespace
        from text in Try(
            Char(':')
                .Then(CommonParsers.InlineWhitespace)
                .Then(Token(c => c != '\r' && c != '\n').ManyString())
        ).Optional()
        from _____ in CommonParsers.LineEnd
        select new Message
        {
            FromId = fromId,
            ToId = toId,
            Text = text.HasValue ? text.Value : null,
            Type = arrow,
            Activate = activate.HasValue,
            Deactivate = deactivate.HasValue
        };

    // Note: Note right of/left of/over Participant: Text
    static readonly Parser<char, Note> NoteParser =
        from _ in CommonParsers.InlineWhitespace
        from keyword in Try(String("Note")).Or(String("note"))
        from __ in CommonParsers.RequiredWhitespace
        from position in OneOf(
            Try(String("right of")).ThenReturn(NotePosition.RightOf),
            Try(String("left of")).ThenReturn(NotePosition.LeftOf),
            String("over").ThenReturn(NotePosition.Over)
        )
        from ___ in CommonParsers.RequiredWhitespace
        from participantId in SeqIdentifier
        from participant2 in Try(
            Char(',')
                .Then(CommonParsers.InlineWhitespace)
                .Then(SeqIdentifier)
        ).Optional()
        from ____ in CommonParsers.InlineWhitespace
        from colon in Char(':')
        from _____ in CommonParsers.InlineWhitespace
        from text in Token(c => c != '\r' && c != '\n').ManyString()
        from ______ in CommonParsers.LineEnd
        select new Note
        {
            Text = text,
            Position = position,
            ParticipantId = participantId,
            OverParticipantId2 = participant2.HasValue ? participant2.Value : null
        };

    // Activate/Deactivate
    static readonly Parser<char, Activation> ActivationParser =
        from _ in CommonParsers.InlineWhitespace
        from isActivate in OneOf(
            String("activate").ThenReturn(true),
            String("deactivate").ThenReturn(false)
        )
        from __ in CommonParsers.RequiredWhitespace
        from participantId in SeqIdentifier
        from ___ in CommonParsers.LineEnd
        select new Activation
        {
            ParticipantId = participantId,
            IsActivate = isActivate
        };

    // AutoNumber
    static readonly Parser<char, bool> AutoNumberParser =
        CommonParsers.InlineWhitespace
            .Then(String("autonumber"))
            .Then(CommonParsers.LineEnd)
            .ThenReturn(true);

    // Title
    static readonly Parser<char, string> TitleParser =
        CommonParsers.InlineWhitespace
            .Then(String("title"))
            .Then(CommonParsers.InlineWhitespace)
            .Then(Token(c => c != '\r' && c != '\n').ManyString())
            .Before(CommonParsers.LineEnd);

    // Skip line
    static readonly Parser<char, Unit> SkipLine =
        CommonParsers.InlineWhitespace
            .Then(Try(CommonParsers.Comment).Or(CommonParsers.Newline));

    public static Parser<char, SequenceModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from keyword in String("sequenceDiagram")
        from __ in CommonParsers.InlineWhitespace
        from ___ in CommonParsers.LineEnd
        from content in ParseContent()
        select BuildModel(content);

    static Parser<char, List<object>> ParseContent()
    {
        var element = OneOf(
            Try(ParticipantParser.Select(p => (object)p)),
            Try(MessageParser.Select(m => (object)m)),
            Try(NoteParser.Select(n => (object)n)),
            Try(ActivationParser.Select(a => (object)a)),
            Try(AutoNumberParser.Select(a => (object)a)),
            Try(TitleParser.Select(t => (object)("title:" + t))),
            SkipLine.ThenReturn((object)Unit.Value)
        );

        return element.Many().Select(e => e.Where(x => x is not Unit).ToList());
    }

    static SequenceModel BuildModel(List<object> content)
    {
        var model = new SequenceModel();
        var participantIds = new HashSet<string>();

        foreach (var item in content)
        {
            switch (item)
            {
                case Participant p:
                    model.Participants.Add(p);
                    participantIds.Add(p.Id);
                    break;

                case Message m:
                    // Auto-add participants from messages
                    if (!participantIds.Contains(m.FromId))
                    {
                        model.Participants.Add(new() { Id = m.FromId });
                        participantIds.Add(m.FromId);
                    }
                    if (!participantIds.Contains(m.ToId))
                    {
                        model.Participants.Add(new() { Id = m.ToId });
                        participantIds.Add(m.ToId);
                    }
                    model.Elements.Add(m);
                    break;

                case Note n:
                    model.Elements.Add(n);
                    break;

                case Activation a:
                    model.Elements.Add(a);
                    break;

                case bool autoNumber:
                    model.AutoNumber = autoNumber;
                    break;

                case string s when s.StartsWith("title:"):
                    model.Title = s[6..];
                    break;
            }
        }

        return model;
    }

    public Result<char, SequenceModel> Parse(string input) => Parser.Parse(input);
}
