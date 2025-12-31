namespace MermaidSharp.Diagrams.State;

public class StateParser : IDiagramParser<StateModel>
{
    public DiagramType DiagramType => DiagramType.State;

    // State identifier (alphanumeric, underscore, or [*] for start/end)
    static readonly Parser<char, string> StateIdentifier =
        Try(String("[*]")).Or(
            Token(c => char.IsLetterOrDigit(c) || c == '_')
                .AtLeastOnceString()
        ).Labelled("state identifier");

    // State type annotations
    static readonly Parser<char, StateType> StateTypeAnnotation =
        String("<<")
            .Then(OneOf(
                Try(String("fork")).ThenReturn(StateType.Fork),
                Try(String("join")).ThenReturn(StateType.Join),
                String("choice").ThenReturn(StateType.Choice)
            ))
            .Before(String(">>"));

    // Transition arrow
    static readonly Parser<char, Unit> TransitionArrow =
        String("-->").ThenReturn(Unit.Value);

    // State declaration: state "Description" as StateName
    static readonly Parser<char, State> StateDeclarationWithAlias =
        from _ in CommonParsers.InlineWhitespace
        from keyword in String("state")
        from __ in CommonParsers.RequiredWhitespace
        from description in CommonParsers.DoubleQuotedString
        from ___ in CommonParsers.RequiredWhitespace
        from asKeyword in String("as")
        from ____ in CommonParsers.RequiredWhitespace
        from id in StateIdentifier
        from _____ in CommonParsers.InlineWhitespace
        from ______ in CommonParsers.LineEnd
        select new State
        {
            Id = id,
            Description = description
        };

    // State declaration with type: state StateName <<fork>>
    static readonly Parser<char, State> StateDeclarationWithType =
        from _ in CommonParsers.InlineWhitespace
        from keyword in String("state")
        from __ in CommonParsers.RequiredWhitespace
        from id in StateIdentifier
        from ___ in CommonParsers.InlineWhitespace
        from stateType in StateTypeAnnotation
        from ____ in CommonParsers.InlineWhitespace
        from _____ in CommonParsers.LineEnd
        select new State
        {
            Id = id,
            Type = stateType
        };

    // Simple state declaration: state StateName
    static readonly Parser<char, State> SimpleStateDeclaration =
        from _ in CommonParsers.InlineWhitespace
        from keyword in String("state")
        from __ in CommonParsers.RequiredWhitespace
        from id in StateIdentifier
        from ___ in CommonParsers.InlineWhitespace
        from ____ in CommonParsers.LineEnd
        select new State { Id = id };

    // State with description on same line: StateName : Description
    static readonly Parser<char, State> StateWithDescription =
        from _ in CommonParsers.InlineWhitespace
        from id in StateIdentifier
        from __ in CommonParsers.InlineWhitespace
        from colon in Char(':')
        from ___ in CommonParsers.InlineWhitespace
        from description in Token(c => c != '\r' && c != '\n').ManyString()
        from ____ in CommonParsers.LineEnd
        select new State
        {
            Id = id,
            Description = description
        };

    // Transition: StateA --> StateB : label
    static readonly Parser<char, StateTransition> TransitionParser =
        from _ in CommonParsers.InlineWhitespace
        from fromId in StateIdentifier
        from __ in CommonParsers.InlineWhitespace
        from arrow in TransitionArrow
        from ___ in CommonParsers.InlineWhitespace
        from toId in StateIdentifier
        from label in Try(
            CommonParsers.InlineWhitespace
                .Then(Char(':'))
                .Then(CommonParsers.InlineWhitespace)
                .Then(Token(c => c != '\r' && c != '\n').ManyString())
        ).Optional()
        from ____ in CommonParsers.InlineWhitespace
        from _____ in CommonParsers.LineEnd
        select new StateTransition
        {
            FromId = fromId,
            ToId = toId,
            Label = label.HasValue && !string.IsNullOrWhiteSpace(label.Value) ? label.Value : null
        };

    // Note: note right of State : Text
    static readonly Parser<char, StateNote> NoteParser =
        from _ in CommonParsers.InlineWhitespace
        from keyword in String("note")
        from __ in CommonParsers.RequiredWhitespace
        from position in OneOf(
            Try(String("right of")).ThenReturn(NotePosition.RightOf),
            String("left of").ThenReturn(NotePosition.LeftOf)
        )
        from ___ in CommonParsers.RequiredWhitespace
        from stateId in StateIdentifier
        from ____ in CommonParsers.InlineWhitespace
        from colon in Char(':')
        from _____ in CommonParsers.InlineWhitespace
        from text in Token(c => c != '\r' && c != '\n').ManyString()
        from ______ in CommonParsers.LineEnd
        select new StateNote
        {
            StateId = stateId,
            Text = text,
            Position = position
        };

    // Direction directive
    static readonly Parser<char, Direction> DirectionParser =
        CommonParsers.InlineWhitespace
            .Then(String("direction"))
            .Then(CommonParsers.RequiredWhitespace)
            .Then(CommonParsers.DirectionParser)
            .Before(CommonParsers.LineEnd);

    // Skip line (comments, empty lines)
    static readonly Parser<char, Unit> SkipLine =
        CommonParsers.InlineWhitespace
            .Then(Try(CommonParsers.Comment).Or(CommonParsers.Newline));

    // Composite state start: state StateName {
    static readonly Parser<char, string> CompositeStateStart =
        from _ in CommonParsers.InlineWhitespace
        from keyword in String("state")
        from __ in CommonParsers.RequiredWhitespace
        from id in StateIdentifier
        from ___ in CommonParsers.InlineWhitespace
        from open in Char('{')
        from ____ in CommonParsers.LineEnd
        select id;

    // Composite state end: }
    static readonly Parser<char, Unit> CompositeStateEnd =
        CommonParsers.InlineWhitespace
            .Then(Char('}'))
            .Then(CommonParsers.LineEnd)
            .ThenReturn(Unit.Value);

    public static Parser<char, StateModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from keyword in Try(String("stateDiagram-v2")).Or(String("stateDiagram"))
        from __ in CommonParsers.InlineWhitespace
        from ___ in CommonParsers.LineEnd
        from content in ParseContent()
        select BuildModel(content);

    static Parser<char, List<object>> ParseContent() =>
        ParseContentRecursive();

    static Parser<char, List<object>> ParseContentRecursive()
    {
        var element = OneOf(
            Try(DirectionParser.Select(d => (object)d)),
            Try(NoteParser.Select(n => (object)n)),
            Try(StateDeclarationWithAlias.Select(s => (object)s)),
            Try(StateDeclarationWithType.Select(s => (object)s)),
            Try(CompositeStateStart.Select(id => (object)("composite:" + id))),
            Try(CompositeStateEnd.ThenReturn((object)"end_composite")),
            Try(TransitionParser.Select(t => (object)t)),
            Try(StateWithDescription.Select(s => (object)s)),
            Try(SimpleStateDeclaration.Select(s => (object)s)),
            SkipLine.ThenReturn((object)Unit.Value)
        );

        return element
            .Many()
            .Select(e => e.Where(x => x is not Unit)
                .ToList());
    }

    static StateModel BuildModel(List<object> content)
    {
        var model = new StateModel();
        var stateMap = new Dictionary<string, State>();
        var compositeStack = new Stack<State>();

        foreach (var item in content)
        {
            switch (item)
            {
                case Direction d:
                    model.Direction = d;
                    break;

                case State s:
                    if (stateMap.TryGetValue(s.Id, out var existing))
                    {
                        // Update existing state with description/type
                        if (!string.IsNullOrEmpty(s.Description))
                            existing.Description = s.Description;
                        if (s.Type != StateType.Normal)
                            existing.Type = s.Type;
                    }
                    else
                    {
                        stateMap[s.Id] = s;
                        if (compositeStack.Count > 0)
                            compositeStack.Peek().NestedStates.Add(s);
                        else
                            model.States.Add(s);
                    }

                    break;

                case StateTransition t:
                    // Handle [*] - create separate start and end states
                    var fromId = t.FromId;
                    var toId = t.ToId;

                    if (fromId == "[*]")
                    {
                        fromId = "[*]_start";
                        EnsureSpecialState(fromId, StateType.Start, stateMap, model, compositeStack);
                    }
                    else
                    {
                        EnsureState(fromId, stateMap, model, compositeStack);
                    }

                    if (toId == "[*]")
                    {
                        toId = "[*]_end";
                        EnsureSpecialState(toId, StateType.End, stateMap, model, compositeStack);
                    }
                    else
                    {
                        EnsureState(toId, stateMap, model, compositeStack);
                    }

                    var transition = new StateTransition { FromId = fromId, ToId = toId, Label = t.Label };
                    if (compositeStack.Count > 0)
                        compositeStack.Peek().NestedTransitions.Add(transition);
                    else
                        model.Transitions.Add(transition);
                    break;

                case StateNote n:
                    model.Notes.Add(n);
                    break;

                case string s when s.StartsWith("composite:"):
                    var compositeId = s[10..];
                    var compositeState = new State { Id = compositeId };
                    stateMap[compositeId] = compositeState;

                    if (compositeStack.Count > 0)
                        compositeStack.Peek().NestedStates.Add(compositeState);
                    else
                        model.States.Add(compositeState);

                    compositeStack.Push(compositeState);
                    break;

                case "end_composite":
                    if (compositeStack.Count > 0)
                        compositeStack.Pop();
                    break;
            }
        }

        return model;
    }

    static void EnsureState(string id, Dictionary<string, State> stateMap, StateModel model, Stack<State> compositeStack)
    {
        if (stateMap.ContainsKey(id))
            return;

        var stateType = id == "[*]"
            ? compositeStack.Count == 0 ? StateType.Start : StateType.Normal
            : StateType.Normal;

        var state = new State { Id = id, Type = stateType };
        stateMap[id] = state;

        if (compositeStack.Count > 0)
            compositeStack.Peek().NestedStates.Add(state);
        else
            model.States.Add(state);
    }

    static void EnsureSpecialState(string id, StateType type, Dictionary<string, State> stateMap, StateModel model, Stack<State> compositeStack)
    {
        if (stateMap.ContainsKey(id))
            return;

        var state = new State { Id = id, Type = type };
        stateMap[id] = state;

        if (compositeStack.Count > 0)
            compositeStack.Peek().NestedStates.Add(state);
        else
            model.States.Add(state);
    }

    public Result<char, StateModel> Parse(string input) => Parser.Parse(input);
}
