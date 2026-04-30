using NotePosition = Naiad.Diagrams.State.NotePosition;

class StateParser : IDiagramParser<StateModel>
{
    // State identifier (alphanumeric, underscore, or [*] for start/end)
    static Parser<char, string> stateIdentifier =
        Try(String("[*]")).Or(
            Token(_ => char.IsLetterOrDigit(_) || _ == '_')
                .AtLeastOnceString()
        ).Labelled("state identifier");

    // State type annotations
    static Parser<char, StateType> stateTypeAnnotation =
        String("<<")
            .Then(OneOf(
                Try(String("fork")).ThenReturn(StateType.Fork),
                Try(String("join")).ThenReturn(StateType.Join),
                String("choice").ThenReturn(StateType.Choice)
            ))
            .Before(String(">>"));

    // Transition arrow
    static Parser<char, Unit> transitionArrow =
        String("-->").ThenReturn(Unit.Value);

    // State declaration: state "Description" as StateName
    static Parser<char, State> stateDeclarationWithAlias =
        from _ in CommonParsers.InlineWhitespace
        from keyword in String("state")
        from __ in CommonParsers.RequiredWhitespace
        from description in CommonParsers.DoubleQuotedString
        from ___ in CommonParsers.RequiredWhitespace
        from asKeyword in String("as")
        from ____ in CommonParsers.RequiredWhitespace
        from id in stateIdentifier
        from _____ in CommonParsers.InlineWhitespace
        from ______ in CommonParsers.LineEnd
        select new State
        {
            Id = id,
            Description = description
        };

    // State declaration with type: state StateName <<fork>>
    static Parser<char, State> stateDeclarationWithType =
        from _ in CommonParsers.InlineWhitespace
        from keyword in String("state")
        from __ in CommonParsers.RequiredWhitespace
        from id in stateIdentifier
        from ___ in CommonParsers.InlineWhitespace
        from stateType in stateTypeAnnotation
        from ____ in CommonParsers.InlineWhitespace
        from _____ in CommonParsers.LineEnd
        select new State
        {
            Id = id,
            Type = stateType
        };

    // Simple state declaration: state StateName
    static Parser<char, State> simpleStateDeclaration =
        from _ in CommonParsers.InlineWhitespace
        from keyword in String("state")
        from __ in CommonParsers.RequiredWhitespace
        from id in stateIdentifier
        from ___ in CommonParsers.InlineWhitespace
        from ____ in CommonParsers.LineEnd
        select new State { Id = id };

    // State with description on same line: StateName : Description
    static Parser<char, State> stateWithDescription =
        from _ in CommonParsers.InlineWhitespace
        from id in stateIdentifier
        from __ in CommonParsers.InlineWhitespace
        from colon in Char(':')
        from ___ in CommonParsers.InlineWhitespace
        from description in Token(_ => _ != '\r' && _ != '\n').ManyString()
        from ____ in CommonParsers.LineEnd
        select new State
        {
            Id = id,
            Description = description
        };

    // Transition: StateA --> StateB : label
    static Parser<char, StateTransition> transitionParser =
        from _ in CommonParsers.InlineWhitespace
        from fromId in stateIdentifier
        from __ in CommonParsers.InlineWhitespace
        from arrow in transitionArrow
        from ___ in CommonParsers.InlineWhitespace
        from toId in stateIdentifier
        from label in Try(
            CommonParsers.InlineWhitespace
                .Then(Char(':'))
                .Then(CommonParsers.InlineWhitespace)
                .Then(Token(_ => _ != '\r' && _ != '\n').ManyString())
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
    static Parser<char, StateNote> noteParser =
        from _ in CommonParsers.InlineWhitespace
        from keyword in String("note")
        from __ in CommonParsers.RequiredWhitespace
        from position in OneOf(
            Try(String("right of")).ThenReturn(NotePosition.RightOf),
            String("left of").ThenReturn(NotePosition.LeftOf)
        )
        from ___ in CommonParsers.RequiredWhitespace
        from stateId in stateIdentifier
        from ____ in CommonParsers.InlineWhitespace
        from colon in Char(':')
        from _____ in CommonParsers.InlineWhitespace
        from text in Token(_ => _ != '\r' && _ != '\n').ManyString()
        from ______ in CommonParsers.LineEnd
        select new StateNote
        {
            StateId = stateId,
            Text = text,
            Position = position
        };

    static Parser<char, Direction> directionParser =
        CommonParsers.InlineWhitespace
            .Then(String("direction"))
            .Then(CommonParsers.RequiredWhitespace)
            .Then(CommonParsers.DirectionParser)
            .Before(CommonParsers.LineEnd);

    // Skip line (comments, empty lines)
    static Parser<char, Unit> skipLine =
        CommonParsers.InlineWhitespace
            .Then(Try(CommonParsers.Comment).Or(CommonParsers.Newline));

    // Composite state start: state StateName {
    static Parser<char, string> compositeStateStart =
        from _ in CommonParsers.InlineWhitespace
        from keyword in String("state")
        from __ in CommonParsers.RequiredWhitespace
        from id in stateIdentifier
        from ___ in CommonParsers.InlineWhitespace
        from open in Char('{')
        from ____ in CommonParsers.LineEnd
        select id;

    // Composite state end: }
    static Parser<char, Unit> compositeStateEnd =
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

    public static Parser<char, IEnumerable<IStateContent?>> ParseContent() =>
        ParseContentRecursive();

    public static Parser<char, IEnumerable<IStateContent?>> ParseContentRecursive() =>
        OneOf(
            Try(directionParser.Select<IStateContent?>(_ => new DirectionItem(_))),
            Try(noteParser.Select<IStateContent?>(_ => new NoteItem(_))),
            Try(stateDeclarationWithAlias.Select<IStateContent?>(_ => new StateItem(_))),
            Try(stateDeclarationWithType.Select<IStateContent?>(_ => new StateItem(_))),
            Try(compositeStateStart.Select<IStateContent?>(_ => new CompositeStartItem(_))),
            Try(compositeStateEnd.ThenReturn<IStateContent?>(new CompositeEndItem())),
            Try(transitionParser.Select<IStateContent?>(_ => new TransitionItem(_))),
            Try(stateWithDescription.Select<IStateContent?>(_ => new StateItem(_))),
            Try(simpleStateDeclaration.Select<IStateContent?>(_ => new StateItem(_))),
            skipLine.ThenReturn<IStateContent?>(null)
        ).Many();

    static StateModel BuildModel(IEnumerable<IStateContent?> content)
    {
        var model = new StateModel();
        var stateMap = new Dictionary<string, State>();
        var compositeStack = new Stack<State>();

        foreach (var item in content)
        {
            switch (item)
            {
                case DirectionItem dir:
                    model.Direction = dir.Value;
                    break;

                case StateItem stateItem:
                    var value = stateItem.Value;
                    if (stateMap.TryGetValue(value.Id, out var existing))
                    {
                        // Update existing state with description/type
                        if (!string.IsNullOrEmpty(value.Description))
                        {
                            existing.Description = value.Description;
                        }
                        if (value.Type != StateType.Normal)
                        {
                            existing.Type = value.Type;
                        }
                    }
                    else
                    {
                        stateMap[value.Id] = value;
                        if (compositeStack.TryPeek(out var parent))
                        {
                            parent.NestedStates.Add(value);
                        }
                        else
                        {
                            model.States.Add(value);
                        }
                    }

                    break;

                case TransitionItem transitionItem:
                    var t = transitionItem.Value;
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

                    var transition = new StateTransition
                    {
                        FromId = fromId,
                        ToId = toId,
                        Label = t.Label
                    };
                    if (compositeStack.TryPeek(out var transitionParent))
                    {
                        transitionParent.NestedTransitions.Add(transition);
                    }
                    else
                    {
                        model.Transitions.Add(transition);
                    }
                    break;

                case NoteItem note:
                    model.Notes.Add(note.Value);
                    break;

                case CompositeStartItem cs:
                    var compositeState = new State
                    {
                        Id = cs.Id
                    };
                    stateMap[cs.Id] = compositeState;

                    if (compositeStack.TryPeek(out var compositeParent))
                    {
                        compositeParent.NestedStates.Add(compositeState);
                    }
                    else
                    {
                        model.States.Add(compositeState);
                    }

                    compositeStack.Push(compositeState);
                    break;

                case CompositeEndItem:
                    if (compositeStack.Count > 0)
                    {
                        compositeStack.Pop();
                    }
                    break;
            }
        }

        return model;
    }

    static void EnsureState(string id, Dictionary<string, State> stateMap, StateModel model, Stack<State> compositeStack)
    {
        if (stateMap.ContainsKey(id))
        {
            return;
        }

        var stateType = id == "[*]"
            ? compositeStack.Count == 0 ? StateType.Start : StateType.Normal
            : StateType.Normal;

        var state = new State
        {
            Id = id,
            Type = stateType
        };
        stateMap[id] = state;

        if (compositeStack.TryPeek(out var parent))
        {
            parent.NestedStates.Add(state);
        }
        else
        {
            model.States.Add(state);
        }
    }

    static void EnsureSpecialState(string id, StateType type, Dictionary<string, State> stateMap, StateModel model, Stack<State> compositeStack)
    {
        if (stateMap.ContainsKey(id))
        {
            return;
        }

        var state = new State
        {
            Id = id,
            Type = type
        };
        stateMap[id] = state;

        if (compositeStack.TryPeek(out var parent))
        {
            parent.NestedStates.Add(state);
        }
        else
        {
            model.States.Add(state);
        }
    }

    public Result<char, StateModel> Parse(string input) => Parser.Parse(input);

    internal interface IStateContent;
    readonly record struct DirectionItem(Direction Value) : IStateContent;
    readonly record struct StateItem(State Value) : IStateContent;
    readonly record struct TransitionItem(StateTransition Value) : IStateContent;
    readonly record struct NoteItem(StateNote Value) : IStateContent;
    readonly record struct CompositeStartItem(string Id) : IStateContent;
    readonly record struct CompositeEndItem : IStateContent;
}
