using MermaidSharp.Layout;

namespace MermaidSharp.Diagrams.State;

public class StateRenderer : IDiagramRenderer<StateModel>
{
    readonly ILayoutEngine _layoutEngine;

    const double StateWidth = 120;
    const double StateHeight = 40;
    const double StateRadius = 5;
    const double SpecialStateSize = 20;
    const double NoteWidth = 100;
    const double NoteHeight = 40;
    const double NotePadding = 10;

    public StateRenderer(ILayoutEngine? layoutEngine = null)
    {
        _layoutEngine = layoutEngine ?? new DagreLayoutEngine();
    }

    public SvgDocument Render(StateModel model, RenderOptions options)
    {
        // Convert to graph model for layout
        var graphModel = ConvertToGraphModel(model, options);

        // Run layout
        var layoutOptions = new LayoutOptions
        {
            Direction = model.Direction,
            NodeSeparation = 80,
            RankSeparation = 80
        };
        var layoutResult = _layoutEngine.Layout(graphModel, layoutOptions);

        // Copy positions back to state model
        CopyPositionsToModel(model, graphModel);

        // Adjust fork/join bar widths to span connected nodes
        AdjustForkJoinWidths(model);

        // Build SVG
        var builder = new SvgBuilder()
            .Size(layoutResult.Width, layoutResult.Height)
            .Padding(options.Padding)
            .AddArrowMarker("arrowhead", "#333");

        // Render transitions first (behind states)
        RenderTransitions(builder, model, options);

        // Render states
        RenderStates(builder, model.States, options);

        // Render notes
        RenderNotes(builder, model, options);

        return builder.Build();
    }

    GraphDiagramBase ConvertToGraphModel(StateModel model, RenderOptions options)
    {
        var graph = new StateLayoutGraph { Direction = model.Direction };

        // Add nodes for each state
        AddStatesToGraph(graph, model.States, options);

        // Add edges for transitions
        foreach (var transition in model.Transitions)
        {
            var edge = new Edge
            {
                SourceId = transition.FromId,
                TargetId = transition.ToId,
                Label = transition.Label
            };
            graph.AddEdge(edge);
        }

        return graph;
    }

    void AddStatesToGraph(StateLayoutGraph graph, List<State> states, RenderOptions options)
    {
        foreach (var state in states)
        {
            var (width, height) = CalculateStateSize(state, options);
            var node = new Node
            {
                Id = state.Id,
                Label = state.Description ?? state.Id,
                Width = width,
                Height = height
            };
            graph.AddNode(node);

            // Add nested states for composite states
            if (state.IsComposite)
            {
                AddStatesToGraph(graph, state.NestedStates, options);
                foreach (var nestedTransition in state.NestedTransitions)
                {
                    var edge = new Edge
                    {
                        SourceId = nestedTransition.FromId,
                        TargetId = nestedTransition.ToId,
                        Label = nestedTransition.Label
                    };
                    graph.AddEdge(edge);
                }
            }
        }
    }

    (double width, double height) CalculateStateSize(State state, RenderOptions options)
    {
        if (state.Type is StateType.Start or StateType.End)
            return (SpecialStateSize, SpecialStateSize);

        if (state.Type is StateType.Fork or StateType.Join)
            return (StateWidth, 8);

        if (state.Type == StateType.Choice)
            return (SpecialStateSize * 2, SpecialStateSize * 2);

        var label = state.Description ?? state.Id;
        var textWidth = MeasureText(label, options.FontSize);
        var width = Math.Max(StateWidth, textWidth + 20);

        return (width, StateHeight);
    }

    void CopyPositionsToModel(StateModel model, GraphDiagramBase graph)
    {
        CopyPositionsToStates(model.States, graph);
    }

    void CopyPositionsToStates(List<State> states, GraphDiagramBase graph)
    {
        foreach (var state in states)
        {
            var node = graph.GetNode(state.Id);
            if (node != null)
            {
                state.Position = node.Position;
                state.Width = node.Width;
                state.Height = node.Height;
            }

            if (state.IsComposite)
            {
                CopyPositionsToStates(state.NestedStates, graph);
            }
        }
    }

    void AdjustForkJoinWidths(StateModel model)
    {
        var stateMap = BuildStateMap(model.States);

        foreach (var state in model.States)
        {
            if (state.Type is StateType.Fork or StateType.Join)
            {
                // Find all connected states
                var connectedStates = new List<State>();

                foreach (var transition in model.Transitions)
                {
                    // Fork: outgoing transitions (fork --> target)
                    if (state.Type == StateType.Fork && transition.FromId == state.Id)
                    {
                        if (stateMap.TryGetValue(transition.ToId, out var target))
                            connectedStates.Add(target);
                    }
                    // Join: incoming transitions (source --> join)
                    if (state.Type == StateType.Join && transition.ToId == state.Id)
                    {
                        if (stateMap.TryGetValue(transition.FromId, out var source))
                            connectedStates.Add(source);
                    }
                }

                if (connectedStates.Count >= 2)
                {
                    // Calculate width to span from leftmost to rightmost connected state
                    var minX = connectedStates.Min(s => s.Position.X - s.Width / 2);
                    var maxX = connectedStates.Max(s => s.Position.X + s.Width / 2);
                    state.Width = maxX - minX;
                    state.Position = new Position((minX + maxX) / 2, state.Position.Y);
                }
            }
        }
    }

    void RenderStates(SvgBuilder builder, List<State> states, RenderOptions options)
    {
        foreach (var state in states)
        {
            RenderState(builder, state, options);
        }
    }

    void RenderState(SvgBuilder builder, State state, RenderOptions options)
    {
        var x = state.Position.X;
        var y = state.Position.Y;

        switch (state.Type)
        {
            case StateType.Start:
                // Filled circle
                builder.AddCircle(x, y, SpecialStateSize / 2,
                    fill: "#333", stroke: "#333", strokeWidth: 1);
                break;

            case StateType.End:
                // Double circle
                builder.AddCircle(x, y, SpecialStateSize / 2,
                    fill: "none", stroke: "#333", strokeWidth: 2);
                builder.AddCircle(x, y, SpecialStateSize / 4,
                    fill: "#333", stroke: "#333", strokeWidth: 1);
                break;

            case StateType.Fork:
            case StateType.Join:
                // Horizontal bar
                builder.AddRect(
                    x - state.Width / 2, y - state.Height / 2,
                    state.Width, state.Height,
                    fill: "#333", stroke: "#333");
                break;

            case StateType.Choice:
                // Diamond
                var halfW = state.Width / 2;
                var halfH = state.Height / 2;
                var diamondPath = $"M{Fmt(x)},{Fmt(y - halfH)} " +
                                  $"L{Fmt(x + halfW)},{Fmt(y)} " +
                                  $"L{Fmt(x)},{Fmt(y + halfH)} " +
                                  $"L{Fmt(x - halfW)},{Fmt(y)} Z";
                builder.AddPath(diamondPath, fill: "#fff", stroke: "#333", strokeWidth: 1);
                break;

            default:
                if (state.IsComposite)
                {
                    // Composite state - render as container with nested content
                    RenderCompositeState(builder, state, options);
                }
                else
                {
                    // Normal state - rounded rectangle
                    RenderNormalState(builder, state, options);
                }
                break;
        }
    }

    void RenderNormalState(SvgBuilder builder, State state, RenderOptions options)
    {
        var x = state.Position.X - state.Width / 2;
        var y = state.Position.Y - state.Height / 2;

        builder.AddRect(x, y, state.Width, state.Height,
            rx: StateRadius,
            fill: "#ECECFF",
            stroke: "#9370DB",
            strokeWidth: 1);

        var label = state.Description ?? state.Id;
        if (state.Type == StateType.Normal)
        {
            builder.AddText(state.Position.X, state.Position.Y, label,
                anchor: "middle",
                baseline: "middle",
                fontSize: $"{options.FontSize}px",
                fontFamily: options.FontFamily);
        }
    }

    void RenderCompositeState(SvgBuilder builder, State state, RenderOptions options)
    {
        // For now, render as a larger box with nested states inside
        // In a full implementation, we'd calculate the bounding box of nested states
        var x = state.Position.X - state.Width / 2;
        var y = state.Position.Y - state.Height / 2;

        builder.AddRect(x, y, state.Width, state.Height,
            rx: StateRadius,
            fill: "#F4F4F4",
            stroke: "#666",
            strokeWidth: 2);

        // Title
        builder.AddText(state.Position.X, y + 15, state.Id,
            anchor: "middle",
            baseline: "middle",
            fontSize: $"{options.FontSize}px",
            fontFamily: options.FontFamily,
            fontWeight: "bold");

        // Separator line
        builder.AddLine(x, y + 30, x + state.Width, y + 30,
            stroke: "#666", strokeWidth: 1);

        // Render nested states
        RenderStates(builder, state.NestedStates, options);
    }

    void RenderTransitions(SvgBuilder builder, StateModel model, RenderOptions options)
    {
        var stateMap = BuildStateMap(model.States);

        foreach (var transition in model.Transitions)
        {
            RenderTransition(builder, transition, stateMap, options);
        }

        // Render nested transitions
        foreach (var state in model.States)
        {
            if (state.IsComposite)
            {
                var nestedMap = BuildStateMap(state.NestedStates);
                foreach (var map in stateMap)
                    nestedMap.TryAdd(map.Key, map.Value);

                foreach (var transition in state.NestedTransitions)
                {
                    RenderTransition(builder, transition, nestedMap, options);
                }
            }
        }
    }

    Dictionary<string, State> BuildStateMap(List<State> states)
    {
        var map = new Dictionary<string, State>();
        foreach (var state in states)
        {
            map[state.Id] = state;
            if (state.IsComposite)
            {
                foreach (var nested in BuildStateMap(state.NestedStates))
                {
                    map.TryAdd(nested.Key, nested.Value);
                }
            }
        }
        return map;
    }

    void RenderTransition(SvgBuilder builder, StateTransition transition,
        Dictionary<string, State> stateMap, RenderOptions options)
    {
        if (!stateMap.TryGetValue(transition.FromId, out var fromState) ||
            !stateMap.TryGetValue(transition.ToId, out var toState))
            return;

        var (startX, startY) = GetConnectionPoint(fromState, toState);
        var (endX, endY) = GetConnectionPoint(toState, fromState);

        // Draw arrow line
        builder.AddLine(startX, startY, endX, endY,
            stroke: "#333", strokeWidth: 1);

        // Draw arrowhead
        DrawArrowhead(builder, startX, startY, endX, endY);

        // Draw label if present
        if (!string.IsNullOrEmpty(transition.Label))
        {
            var labelX = (startX + endX) / 2;
            var labelY = (startY + endY) / 2 - 10;

            builder.AddRect(labelX - 30, labelY - 8, 60, 16,
                fill: "#fff", stroke: "none");
            builder.AddText(labelX, labelY, transition.Label,
                anchor: "middle",
                baseline: "middle",
                fontSize: $"{options.FontSize - 2}px",
                fontFamily: options.FontFamily);
        }
    }

    (double x, double y) GetConnectionPoint(State from, State to)
    {
        var dx = to.Position.X - from.Position.X;
        var dy = to.Position.Y - from.Position.Y;

        // For special states, use center with offset
        if (from.Type is StateType.Start or StateType.End)
        {
            var angle = Math.Atan2(dy, dx);
            var radius = SpecialStateSize / 2;
            return (from.Position.X + radius * Math.Cos(angle),
                    from.Position.Y + radius * Math.Sin(angle));
        }

        // For normal states, use edge intersection
        if (Math.Abs(dx) > Math.Abs(dy))
        {
            return dx > 0
                ? (from.Position.X + from.Width / 2, from.Position.Y)
                : (from.Position.X - from.Width / 2, from.Position.Y);
        }
        else
        {
            return dy > 0
                ? (from.Position.X, from.Position.Y + from.Height / 2)
                : (from.Position.X, from.Position.Y - from.Height / 2);
        }
    }

    void DrawArrowhead(SvgBuilder builder, double fromX, double fromY, double toX, double toY)
    {
        var angle = Math.Atan2(toY - fromY, toX - fromX);
        var arrowSize = 8;

        var backAngle1 = angle + Math.PI - Math.PI / 6;
        var backAngle2 = angle + Math.PI + Math.PI / 6;

        builder.AddPolygon([
            new Position(toX, toY),
            new Position(toX + arrowSize * Math.Cos(backAngle1), toY + arrowSize * Math.Sin(backAngle1)),
            new Position(toX + arrowSize * Math.Cos(backAngle2), toY + arrowSize * Math.Sin(backAngle2))
        ], fill: "#333");
    }

    void RenderNotes(SvgBuilder builder, StateModel model, RenderOptions options)
    {
        var stateMap = BuildStateMap(model.States);

        foreach (var note in model.Notes)
        {
            if (!stateMap.TryGetValue(note.StateId, out var state))
                continue;

            // Calculate note width based on text
            var noteWidth = Math.Max(NoteWidth, MeasureText(note.Text, options.FontSize - 2) + 20);

            var noteX = note.Position == NotePosition.RightOf
                ? state.Position.X + state.Width / 2 + NotePadding
                : state.Position.X - state.Width / 2 - noteWidth - NotePadding;

            var noteY = state.Position.Y - NoteHeight / 2;

            // Note box with folded corner
            var foldSize = 8;
            var path = $"M{Fmt(noteX)},{Fmt(noteY)} " +
                       $"L{Fmt(noteX + noteWidth - foldSize)},{Fmt(noteY)} " +
                       $"L{Fmt(noteX + noteWidth)},{Fmt(noteY + foldSize)} " +
                       $"L{Fmt(noteX + noteWidth)},{Fmt(noteY + NoteHeight)} " +
                       $"L{Fmt(noteX)},{Fmt(noteY + NoteHeight)} Z";

            builder.AddPath(path, fill: "#FFFFCC", stroke: "#AAAA33", strokeWidth: 1);

            // Fold corner
            builder.AddLine(noteX + noteWidth - foldSize, noteY,
                           noteX + noteWidth - foldSize, noteY + foldSize,
                           stroke: "#AAAA33", strokeWidth: 1);
            builder.AddLine(noteX + noteWidth - foldSize, noteY + foldSize,
                           noteX + noteWidth, noteY + foldSize,
                           stroke: "#AAAA33", strokeWidth: 1);

            // Note text
            builder.AddText(noteX + noteWidth / 2, noteY + NoteHeight / 2, note.Text,
                anchor: "middle",
                baseline: "middle",
                fontSize: $"{options.FontSize - 2}px",
                fontFamily: options.FontFamily);
        }
    }

    static double MeasureText(string text, double fontSize)
    {
        return text.Length * fontSize * 0.6;
    }

    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}

// Internal graph model for layout
internal class StateLayoutGraph : GraphDiagramBase { }
