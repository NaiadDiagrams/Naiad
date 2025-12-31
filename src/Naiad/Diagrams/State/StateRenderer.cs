using System.Diagnostics;

namespace MermaidSharp.Diagrams.State;

public class StateRenderer(ILayoutEngine? layoutEngine = null) :
    IDiagramRenderer<StateModel>
{
    readonly ILayoutEngine layoutEngine = layoutEngine ?? new DagreLayoutEngine();

#if DEBUG
    readonly List<TextBounds> _textBounds = [];
    readonly List<LineBounds> _lineBounds = [];
    readonly List<NodeBounds> _nodeBounds = [];

    record TextBounds(double X, double Y, double Width, double Height, string Label);
    record LineBounds(double X1, double Y1, double X2, double Y2, string Label);
    record NodeBounds(double X, double Y, double Width, double Height, string Label);
#endif

    const double StateMinWidth = 40;
    const double StateHeight = 40;
    const double StatePadding = 30;
    const double StateRadius = 5;
    const double SpecialStateSize = 20;
    const double NoteMinWidth = 60;
    const double NoteHeight = 40;
    const double NotePadding = 20;
    const double NoteHorizontalOffset = 60;
    const double NoteVerticalOffset = 50;

    public SvgDocument Render(StateModel model, RenderOptions options)
    {
#if DEBUG
        _textBounds.Clear();
        _lineBounds.Clear();
        _nodeBounds.Clear();
#endif

        // Convert to graph model for layout
        var graphModel = ConvertToGraphModel(model, options);

        // Run layout
        var layoutOptions = new LayoutOptions
        {
            Direction = model.Direction,
            NodeSeparation = 120,  // More horizontal space
            RankSeparation = 80
        };
        var layoutResult = layoutEngine.Layout(graphModel, layoutOptions);

        // Copy positions back to state model
        CopyPositionsToModel(model, graphModel);

        // Align start/end nodes and their single children
        AlignSingleChildNodes(model);

        // Calculate extra space needed for notes
        var stateMap = BuildStateMap(model.States);
        var (noteExtraWidth, noteExtraHeight, noteExtraLeft) = CalculateNoteExtraSpace(model, stateMap, options);

        // Shift all positions right if notes extend past left edge
        if (noteExtraLeft > 0)
        {
            foreach (var state in model.States)
                state.Position = new(state.Position.X + noteExtraLeft, state.Position.Y);
        }

        // Build SVG
        var builder = new SvgBuilder()
            .Size(layoutResult.Width + noteExtraWidth + noteExtraLeft, layoutResult.Height + noteExtraHeight)
            .Padding(options.Padding)
            .AddArrowMarker();

        // Render transitions first (behind states)
        RenderTransitions(builder, model, options);

        // Render states
        RenderStates(builder, model.States, options);

        // Render notes
        RenderNotes(builder, model, options);

#if DEBUG
        CheckForTextOverlaps();
        CheckForLinesUnderNodes();
#endif

        return builder.Build();
    }

#if DEBUG
    void TrackText(double x, double y, string text, string anchor, double fontSize)
    {
        var width = MeasureText(text, fontSize);
        var height = fontSize * 1.2; // Approximate line height

        // Adjust x based on anchor
        var left = anchor switch
        {
            "middle" => x - width / 2,
            "end" => x - width,
            _ => x // "start" or default
        };

        // Adjust y (text is typically centered vertically with dominant-baseline="middle")
        var top = y - height / 2;

        _textBounds.Add(new TextBounds(left, top, width, height, text));
    }

    void CheckForTextOverlaps()
    {
        for (var i = 0; i < _textBounds.Count; i++)
        {
            for (var j = i + 1; j < _textBounds.Count; j++)
            {
                var a = _textBounds[i];
                var b = _textBounds[j];

                // Check for rectangle overlap
                var overlapsX = a.X < b.X + b.Width && a.X + a.Width > b.X;
                var overlapsY = a.Y < b.Y + b.Height && a.Y + a.Height > b.Y;

                if (overlapsX && overlapsY)
                {
                    throw new InvalidOperationException(
                        $"Text overlap detected: \"{a.Label}\" at ({a.X:F1},{a.Y:F1},{a.Width:F1}x{a.Height:F1}) " +
                        $"overlaps with \"{b.Label}\" at ({b.X:F1},{b.Y:F1},{b.Width:F1}x{b.Height:F1})");
                }
            }
        }
    }

    void TrackLine(double x1, double y1, double x2, double y2, string label) =>
        _lineBounds.Add(new LineBounds(x1, y1, x2, y2, label));

    void TrackNode(double x, double y, double width, double height, string label) =>
        _nodeBounds.Add(new NodeBounds(x - width / 2, y - height / 2, width, height, label));

    void CheckForLinesUnderNodes()
    {
        foreach (var line in _lineBounds)
        {
            foreach (var node in _nodeBounds)
            {
                // Check if line segment passes through node's bounding box
                // Skip if line endpoints are the node itself (connection points)
                if (LineIntersectsRect(line.X1, line.Y1, line.X2, line.Y2,
                    node.X, node.Y, node.Width, node.Height))
                {
                    throw new InvalidOperationException(
                        $"Line passes under node: \"{line.Label}\" from ({line.X1:F1},{line.Y1:F1}) to ({line.X2:F1},{line.Y2:F1}) " +
                        $"passes under \"{node.Label}\" at ({node.X:F1},{node.Y:F1},{node.Width:F1}x{node.Height:F1})");
                }
            }
        }
    }

    static bool LineIntersectsRect(double x1, double y1, double x2, double y2,
        double rx, double ry, double rw, double rh)
    {
        // Check if line segment intersects rectangle interior (not just edges)
        // Use parametric line equation and check for intersection with rectangle

        var left = rx;
        var right = rx + rw;
        var top = ry;
        var bottom = ry + rh;

        // Shrink the rect slightly to avoid edge cases at connection points
        var margin = 2.0;
        left += margin;
        right -= margin;
        top += margin;
        bottom -= margin;

        if (right <= left || bottom <= top)
            return false;

        // Check if either endpoint is inside the rectangle (shouldn't happen for valid lines)
        // Skip endpoints since they might be at connection points

        // Use Cohen-Sutherland style clipping to find if line passes through interior
        // Sample points along the line and check if any are inside
        var steps = 20;
        for (var i = 1; i < steps; i++) // Skip endpoints (i=0 and i=steps)
        {
            var t = i / (double)steps;
            var px = x1 + t * (x2 - x1);
            var py = y1 + t * (y2 - y1);

            if (px > left && px < right && py > top && py < bottom)
            {
                return true;
            }
        }

        return false;
    }
#endif

    static (double extraWidth, double extraHeight, double extraLeft) CalculateNoteExtraSpace(StateModel model, Dictionary<string, State> stateMap, RenderOptions options)
    {
        double maxExtraWidth = 0;
        double maxExtraHeight = 0;
        double maxExtraLeft = 0;

        foreach (var note in model.Notes)
        {
            if (!stateMap.TryGetValue(note.StateId, out var state))
                continue;

            var noteWidth = Math.Max(NoteMinWidth, MeasureText(note.Text, options.FontSize - 2) + NotePadding);

            // Check horizontal space needed - notes go to outside of diagram
            var diagramCenterX = model.States.Average(s => s.Position.X);
            var placeToRight = state.Position.X >= diagramCenterX;
            double noteX;
            if (placeToRight)
            {
                noteX = state.Position.X + state.Width / 2 + NoteHorizontalOffset - noteWidth / 2;
            }
            else
            {
                noteX = state.Position.X - state.Width / 2 - NoteHorizontalOffset - noteWidth / 2;
            }

            // Check if note extends past right edge
            var noteRightEdge = noteX + noteWidth;
            var stateRightEdge = model.States.Max(s => s.Position.X + s.Width / 2);
            var extraWidthNeeded = noteRightEdge - stateRightEdge;

            // Check if note extends past left edge
            var stateLeftEdge = model.States.Min(s => s.Position.X - s.Width / 2);
            var extraLeftNeeded = stateLeftEdge - noteX;
            maxExtraWidth = Math.Max(maxExtraWidth, extraWidthNeeded);
            maxExtraLeft = Math.Max(maxExtraLeft, extraLeftNeeded);

            // Check if note extends below
            var spaceAbove = state.Position.Y;
            var maxY = model.States.Max(s => s.Position.Y + s.Height / 2);
            var spaceBelow = maxY - state.Position.Y;
            var placeBelow = spaceBelow >= spaceAbove;

            if (placeBelow)
            {
                var noteBottomEdge = state.Position.Y + state.Height / 2 + NoteVerticalOffset + NoteHeight;
                var extraHeightNeeded = noteBottomEdge - maxY;
                maxExtraHeight = Math.Max(maxExtraHeight, extraHeightNeeded);
            }
        }

        return (
            maxExtraWidth > 0 ? maxExtraWidth + 20 : 0,
            maxExtraHeight > 0 ? maxExtraHeight + 20 : 0,
            maxExtraLeft > 0 ? maxExtraLeft + 20 : 0
        );
    }

    static GraphDiagramBase ConvertToGraphModel(StateModel model, RenderOptions options)
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

    static void AddStatesToGraph(StateLayoutGraph graph, List<State> states, RenderOptions options)
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

    static (double width, double height) CalculateStateSize(State state, RenderOptions options)
    {
        if (state.Type is StateType.Start or StateType.End)
            return (SpecialStateSize, SpecialStateSize);

        if (state.Type is StateType.Fork or StateType.Join)
            return (100, 8); // Fixed compact width for fork/join bars

        if (state.Type == StateType.Choice)
            return (SpecialStateSize * 2, SpecialStateSize * 2);

        // Size based on content
        var label = state.Description ?? state.Id;
        var textWidth = MeasureText(label, options.FontSize);
        var width = Math.Max(StateMinWidth, textWidth + StatePadding);

        return (width, StateHeight);
    }

    static void CopyPositionsToModel(StateModel model, GraphDiagramBase graph) =>
        CopyPositionsToStates(model.States, graph);

    static void CopyPositionsToStates(List<State> states, GraphDiagramBase graph)
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

    static void AlignSingleChildNodes(StateModel model)
    {
        // Find the horizontal center of the diagram
        var contentStates = model.States
            .Where(s => s.Type != StateType.Start && s.Type != StateType.End)
            .ToList();
        if (contentStates.Count == 0) return;

        var diagramCenterX = (contentStates.Min(s => s.Position.X) + contentStates.Max(s => s.Position.X)) / 2;

        // Center start node
        var startNode = model.States.FirstOrDefault(s => s.Type == StateType.Start);
        if (startNode != null)
        {
            startNode.Position = new(diagramCenterX, startNode.Position.Y);

            // If start has only one child, align that child with start
            var startChildren = model.Transitions.Where(t => t.FromId == startNode.Id).ToList();
            if (startChildren.Count == 1)
            {
                var childState = model.States.FirstOrDefault(s => s.Id == startChildren[0].ToId);
                if (childState != null && childState.Type != StateType.Fork)
                {
                    childState.Position = new(diagramCenterX, childState.Position.Y);
                }
            }
        }

        // Center end node with its parent if it has only one
        var endNode = model.States.FirstOrDefault(s => s.Type == StateType.End);
        if (endNode != null)
        {
            var endParents = model.Transitions.Where(t => t.ToId == endNode.Id).ToList();
            if (endParents.Count == 1)
            {
                var parentState = model.States.FirstOrDefault(s => s.Id == endParents[0].FromId);
                if (parentState != null)
                {
                    endNode.Position = new(parentState.Position.X, endNode.Position.Y);
                }
            }
        }
    }

    static void AdjustForkJoinWidths(StateModel model)
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
                    // Calculate width based on number of connected states
                    // Keep bars compact - roughly 40px per connected state
                    var barWidth = Math.Max(80, connectedStates.Count * 50);
                    state.Width = barWidth;
                    // Center between leftmost and rightmost connected states
                    var leftState = connectedStates.OrderBy(s => s.Position.X).First();
                    var rightState = connectedStates.OrderBy(s => s.Position.X).Last();
                    state.Position = new((leftState.Position.X + rightState.Position.X) / 2, state.Position.Y);
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

#if DEBUG
        TrackNode(state.Position.X, state.Position.Y, state.Width, state.Height, state.Id);
#endif

        var label = state.Description ?? state.Id;
        if (state.Type == StateType.Normal)
        {
            builder.AddText(state.Position.X, state.Position.Y, label,
                anchor: "middle",
                baseline: "middle",
                fontSize: $"{options.FontSize}px",
                fontFamily: options.FontFamily);
#if DEBUG
            TrackText(state.Position.X, state.Position.Y, label, "middle", options.FontSize);
#endif
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
#if DEBUG
        TrackText(state.Position.X, y + 15, state.Id, "middle", options.FontSize);
#endif

        // Separator line
        builder.AddLine(x, y + 30, x + state.Width, y + 30,
            stroke: "#666", strokeWidth: 1);

        // Render nested states
        RenderStates(builder, state.NestedStates, options);
    }

    void RenderTransitions(SvgBuilder builder, StateModel model, RenderOptions options)
    {
        var stateMap = BuildStateMap(model.States);

        // Build set of bidirectional pairs (where A->B and B->A both exist)
        var bidirectionalPairs = FindBidirectionalPairs(model.Transitions);

        // Collect all back-edges to assign unique offsets
        var backEdges = model.Transitions
            .Where(t => IsBackEdge(t, stateMap) && !bidirectionalPairs.Contains(GetPairKey(t.FromId, t.ToId)))
            .OrderBy(t => stateMap.TryGetValue(t.FromId, out var s) ? s.Position.X : 0)
            .ToList();

        foreach (var transition in model.Transitions)
        {
            var pairKey = GetPairKey(transition.FromId, transition.ToId);
            if (bidirectionalPairs.Contains(pairKey))
            {
                // This is part of a bidirectional pair - curve it
                var isBackEdge = IsBackEdge(transition, stateMap);
                RenderCurvedTransition(builder, transition, stateMap, isBackEdge, model, 0, options);
            }
            else if (IsBackEdge(transition, stateMap))
            {
                // Single back-edge (no forward counterpart) - curve to the right with offset
                var backEdgeIndex = backEdges.IndexOf(transition);
                RenderCurvedTransition(builder, transition, stateMap, isBackEdge: true, model, backEdgeIndex, options);
            }
            else
            {
                // Regular forward transition with no back-edge - straight line
                RenderTransition(builder, transition, stateMap, options);
            }
        }

        // Render nested transitions
        foreach (var state in model.States)
        {
            if (state.IsComposite)
            {
                var nestedMap = BuildStateMap(state.NestedStates);
                foreach (var map in stateMap)
                    nestedMap.TryAdd(map.Key, map.Value);

                var nestedBidirectional = FindBidirectionalPairs(state.NestedTransitions);

                var nestedBackEdges = state.NestedTransitions
                    .Where(t => IsBackEdge(t, nestedMap) && !nestedBidirectional.Contains(GetPairKey(t.FromId, t.ToId)))
                    .OrderBy(t => nestedMap.TryGetValue(t.FromId, out var s) ? s.Position.X : 0)
                    .ToList();

                foreach (var transition in state.NestedTransitions)
                {
                    var pairKey = GetPairKey(transition.FromId, transition.ToId);
                    if (nestedBidirectional.Contains(pairKey))
                    {
                        var isBackEdge = IsBackEdge(transition, nestedMap);
                        RenderCurvedTransition(builder, transition, nestedMap, isBackEdge, model, 0, options);
                    }
                    else if (IsBackEdge(transition, nestedMap))
                    {
                        var backEdgeIndex = nestedBackEdges.IndexOf(transition);
                        RenderCurvedTransition(builder, transition, nestedMap, isBackEdge: true, model, backEdgeIndex, options);
                    }
                    else
                    {
                        RenderTransition(builder, transition, nestedMap, options);
                    }
                }
            }
        }
    }

    static HashSet<string> FindBidirectionalPairs(List<StateTransition> transitions)
    {
        var pairs = new HashSet<string>();
        var edgeSet = new HashSet<string>();

        foreach (var t in transitions)
        {
            var forward = $"{t.FromId}->{t.ToId}";
            var reverse = $"{t.ToId}->{t.FromId}";

            if (edgeSet.Contains(reverse))
            {
                // Found bidirectional pair
                pairs.Add(GetPairKey(t.FromId, t.ToId));
            }
            edgeSet.Add(forward);
        }

        return pairs;
    }

    static string GetPairKey(string a, string b) =>
        string.Compare(a, b, StringComparison.Ordinal) < 0 ? $"{a}|{b}" : $"{b}|{a}";

    static bool IsBackEdge(StateTransition transition, Dictionary<string, State> stateMap)
    {
        if (!stateMap.TryGetValue(transition.FromId, out var fromState) ||
            !stateMap.TryGetValue(transition.ToId, out var toState))
            return false;

        // Back-edge: source is below target (going upward in the diagram)
        return fromState.Position.Y > toState.Position.Y + 20;
    }

    void RenderCurvedTransition(SvgBuilder builder, StateTransition transition,
        Dictionary<string, State> stateMap, bool isBackEdge, StateModel model, int backEdgeIndex, RenderOptions options)
    {
        if (!stateMap.TryGetValue(transition.FromId, out var fromState) ||
            !stateMap.TryGetValue(transition.ToId, out var toState))
            return;

        if (isBackEdge)
        {
            // Route back-edges around the right side of the diagram
            // Space lines apart enough for labels to be centered on each line without overlap
            var baseRightEdge = model.States.Max(s => s.Position.X + s.Width / 2) + 50;

            // Use spacing of 50px between lines - enough for typical labels
            var lineSpacing = 50;
            var rightEdge = baseRightEdge + backEdgeIndex * lineSpacing;

            // Back-edges use smooth curves: angle out, go vertical, angle back in
            var startX = fromState.Position.X + fromState.Width / 2;
            var startY = fromState.Position.Y - fromState.Height / 2; // Exit from top-right
            var endX = toState.Position.X + toState.Width / 2;
            var endY = toState.Position.Y + toState.Height / 2; // Enter at bottom-right

            // Control point distance for the curves (how far the curve extends)
            var curveExtent = 40;

            // Path: curve out to rightEdge, go vertical, curve back into target
            var path = $"M {Fmt(startX)} {Fmt(startY)} " +
                       // Curve out: from top of source, curving right to the vertical line
                       $"C {Fmt(startX)} {Fmt(startY - curveExtent)}, " +
                       $"{Fmt(rightEdge)} {Fmt(startY - curveExtent)}, " +
                       $"{Fmt(rightEdge)} {Fmt(startY - curveExtent * 2)} " +
                       // Vertical line up
                       $"L {Fmt(rightEdge)} {Fmt(endY + curveExtent * 2)} " +
                       // Curve in: from vertical line, curving left into target
                       $"C {Fmt(rightEdge)} {Fmt(endY + curveExtent)}, " +
                       $"{Fmt(endX)} {Fmt(endY + curveExtent)}, " +
                       $"{Fmt(endX)} {Fmt(endY)}";

            builder.AddPath(path, fill: "none", stroke: "#333", strokeWidth: 1);

#if DEBUG
            var lineLabel = transition.Label ?? $"{transition.FromId}->{transition.ToId}";
            // Track the vertical segment for collision detection
            TrackLine(rightEdge, startY - curveExtent * 2, rightEdge, endY + curveExtent * 2, lineLabel);
#endif

            DrawArrowhead(builder, endX, endY + curveExtent, endX, endY);

            // Draw label centered on this back-edge's vertical line
            if (!string.IsNullOrEmpty(transition.Label))
            {
                // Position label centered on the vertical line segment
                var labelX = rightEdge;
                // Position at midpoint of the vertical segment
                var labelY = (fromState.Position.Y + toState.Position.Y) / 2;

                builder.AddText(labelX, labelY, transition.Label,
                    anchor: "middle",
                    baseline: "middle",
                    fontSize: $"{options.FontSize - 2}px",
                    fontFamily: options.FontFamily,
                    fill: "#666");
#if DEBUG
                TrackText(labelX, labelY, transition.Label, "middle", options.FontSize - 2);
#endif
            }
        }
        else
        {
            // Forward edge curves to the LEFT
            var curveOffset = -15;
            var connectionOffset = -8;

            var startX = fromState.Position.X + connectionOffset;
            var startY = fromState.Position.Y + fromState.Height / 2;

            var endX = toState.Position.X + connectionOffset;
            var endY = toState.Position.Y - toState.Height / 2;

            var midY = (startY + endY) / 2;

            var path = $"M {Fmt(startX)} {Fmt(startY)} " +
                       $"C {Fmt(startX + curveOffset)} {Fmt(midY)}, " +
                       $"{Fmt(endX + curveOffset)} {Fmt(midY)}, " +
                       $"{Fmt(endX)} {Fmt(endY)}";

            builder.AddPath(path, fill: "none", stroke: "#333", strokeWidth: 1);

            DrawArrowhead(builder, endX + curveOffset, midY, endX, endY);

            if (!string.IsNullOrEmpty(transition.Label))
            {
                // Push label left and up to avoid overlap with straight transition labels
                var labelX = startX + curveOffset * 2;
                var labelY = startY + (midY - startY) * 0.3; // Position at 30% of the way down

                builder.AddRect(labelX - 30, labelY - 8, 60, 16, fill: "#fff", stroke: "none");
                builder.AddText(labelX, labelY, transition.Label,
                    anchor: "middle",
                    baseline: "middle",
                    fontSize: $"{options.FontSize - 2}px",
                    fontFamily: options.FontFamily);
#if DEBUG
                TrackText(labelX, labelY, transition.Label, "middle", options.FontSize - 2);
#endif
            }
        }
    }

    static Dictionary<string, State> BuildStateMap(List<State> states)
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

    void RenderTransition(
        SvgBuilder builder,
        StateTransition transition,
        Dictionary<string, State> stateMap,
        RenderOptions options)
    {
        if (!stateMap.TryGetValue(transition.FromId, out var fromState) ||
            !stateMap.TryGetValue(transition.ToId, out var toState))
            return;

        var (startX, startY) = GetConnectionPoint(fromState, toState);
        var (endX, endY) = GetConnectionPoint(toState, fromState);

        // Draw arrow line
        builder.AddLine(startX, startY, endX, endY, stroke: "#333", strokeWidth: 1);

        // Draw arrowhead
        DrawArrowhead(builder, startX, startY, endX, endY);

        // Draw label if present
        if (!string.IsNullOrEmpty(transition.Label))
        {
            var labelX = (startX + endX) / 2;
            var labelY = (startY + endY) / 2 - 10;

            builder.AddRect(labelX - 30, labelY - 8, 60, 16, fill: "#fff", stroke: "none");
            builder.AddText(labelX, labelY, transition.Label,
                anchor: "middle",
                baseline: "middle",
                fontSize: $"{options.FontSize - 2}px",
                fontFamily: options.FontFamily);
#if DEBUG
            TrackText(labelX, labelY, transition.Label, "middle", options.FontSize - 2);
#endif
        }
    }

    // Calculate where a line from center to target intersects the node's edge
    static (double x, double y) GetEdgeIntersection(State state, double targetX, double targetY)
    {
        var cx = state.Position.X;
        var cy = state.Position.Y;
        var dx = targetX - cx;
        var dy = targetY - cy;

        if (Math.Abs(dx) < 0.001 && Math.Abs(dy) < 0.001)
            return (cx, cy);

        // For circular nodes (start/end)
        if (state.Type is StateType.Start or StateType.End)
        {
            var angle = Math.Atan2(dy, dx);
            var radius = SpecialStateSize / 2;
            return (cx + radius * Math.Cos(angle), cy + radius * Math.Sin(angle));
        }

        // For diamond (choice) - edge equation: |x| + |y| = size
        if (state.Type == StateType.Choice)
        {
            var size = SpecialStateSize;
            // For a diamond, intersection at parameter t where |t*dx| + |t*dy| = size
            var t = size / (Math.Abs(dx) + Math.Abs(dy));
            return (cx + dx * t, cy + dy * t);
        }

        // For fork/join (horizontal bar)
        if (state.Type is StateType.Fork or StateType.Join)
        {
            // Always connect from top or bottom of the bar
            var y = dy > 0 ? cy + state.Height / 2 : cy - state.Height / 2;
            // X position along the bar based on target direction
            var x = Math.Clamp(cx + dx * 0.1, cx - state.Width / 2 + 5, cx + state.Width / 2 - 5);
            return (x, y);
        }

        // For rectangular nodes - find edge intersection
        var halfW = state.Width / 2;
        var halfH = state.Height / 2;

        // Calculate intersection with rectangle edges
        var tX = Math.Abs(dx) > 0.001 ? halfW / Math.Abs(dx) : double.MaxValue;
        var tY = Math.Abs(dy) > 0.001 ? halfH / Math.Abs(dy) : double.MaxValue;
        var t2 = Math.Min(tX, tY);

        return (cx + dx * t2, cy + dy * t2);
    }

    // Line targets center of destination, clips at edge of source
    static (double x, double y) GetConnectionPoint(State from, State to) =>
        GetEdgeIntersection(from, to.Position.X, to.Position.Y);

    static void DrawArrowhead(SvgBuilder builder, double fromX, double fromY, double toX, double toY)
    {
        var angle = Math.Atan2(toY - fromY, toX - fromX);
        var arrowSize = 8;

        var backAngle1 = angle + Math.PI - Math.PI / 6;
        var backAngle2 = angle + Math.PI + Math.PI / 6;

        builder.AddPolygon([
            new(toX, toY),
            new(toX + arrowSize * Math.Cos(backAngle1), toY + arrowSize * Math.Sin(backAngle1)),
            new(toX + arrowSize * Math.Cos(backAngle2), toY + arrowSize * Math.Sin(backAngle2))
        ], fill: "#333");
    }

    void RenderNotes(SvgBuilder builder, StateModel model, RenderOptions options)
    {
        var stateMap = BuildStateMap(model.States);

        foreach (var note in model.Notes)
        {
            if (!stateMap.TryGetValue(note.StateId, out var state))
                continue;

            // Calculate note dimensions based on text content
            var noteWidth = Math.Max(NoteMinWidth, MeasureText(note.Text, options.FontSize - 2) + NotePadding);

            // Determine vertical placement based on available space
            // But if this state has back-edges AND note would be placed to the right,
            // prefer placing BELOW to avoid blocking the back-edge path
            var spaceAbove = state.Position.Y;
            var maxY = model.States.Max(s => s.Position.Y + s.Height / 2);
            var spaceBelow = maxY - state.Position.Y;

            var hasBackEdgeFromThisState = model.Transitions.Any(t =>
                t.FromId == state.Id &&
                stateMap.TryGetValue(t.ToId, out var to) &&
                state.Position.Y > to.Position.Y + 20);
            var diagramCenterX = model.States.Average(s => s.Position.X);
            var wouldPlaceToRight = state.Position.X >= diagramCenterX;

            // If this state has back-edges and note would be on the right, force placement below
            var placeBelow = (hasBackEdgeFromThisState && wouldPlaceToRight) || spaceBelow >= spaceAbove;

            // Position note to the outside of the diagram (away from center)
            double noteX, noteY;
            var placeToRight = wouldPlaceToRight;

            if (placeToRight)
            {
                // Place to the right of the state (outside edge)
                noteX = state.Position.X + state.Width / 2 + NoteHorizontalOffset - noteWidth / 2;
            }
            else
            {
                // Place to the left of the state (outside edge)
                noteX = state.Position.X - state.Width / 2 - NoteHorizontalOffset - noteWidth / 2;
            }

            noteY = placeBelow
                ? state.Position.Y + state.Height / 2 + NoteVerticalOffset
                : state.Position.Y - state.Height / 2 - NoteVerticalOffset - NoteHeight;

            // Check for overlaps with other states and adjust position
            const double minGap = 15;
            foreach (var otherState in model.States)
            {
                if (otherState.Id == state.Id) continue;

                var otherTop = otherState.Position.Y - otherState.Height / 2;
                var otherBottom = otherState.Position.Y + otherState.Height / 2;
                var otherLeft = otherState.Position.X - otherState.Width / 2;
                var otherRight = otherState.Position.X + otherState.Width / 2;

                var noteBottom = noteY + NoteHeight;
                var noteRight = noteX + noteWidth;

                // Check horizontal overlap
                var horizontalOverlap = noteX < otherRight + minGap && noteRight > otherLeft - minGap;

                if (horizontalOverlap)
                {
                    // If note bottom overlaps with other state top, move note up
                    if (noteBottom > otherTop - minGap && noteY < otherTop)
                    {
                        noteY = otherTop - NoteHeight - minGap;
                    }
                    // If note top overlaps with other state bottom, move note down
                    else if (noteY < otherBottom + minGap && noteBottom > otherBottom)
                    {
                        noteY = otherBottom + minGap;
                    }
                }
            }

            // Note box with folded corner
            var foldSize = 8;
            var path = $"M{Fmt(noteX)},{Fmt(noteY)} " +
                       $"L{Fmt(noteX + noteWidth - foldSize)},{Fmt(noteY)} " +
                       $"L{Fmt(noteX + noteWidth)},{Fmt(noteY + foldSize)} " +
                       $"L{Fmt(noteX + noteWidth)},{Fmt(noteY + NoteHeight)} " +
                       $"L{Fmt(noteX)},{Fmt(noteY + NoteHeight)} Z";

            builder.AddPath(path, fill: "#FFFFCC", stroke: "#AAAA33", strokeWidth: 1);

#if DEBUG
            // Track note as a node for line-under-node detection
            TrackNode(noteX + noteWidth / 2, noteY + NoteHeight / 2, noteWidth, NoteHeight, $"Note: {note.Text}");
#endif

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
#if DEBUG
            TrackText(noteX + noteWidth / 2, noteY + NoteHeight / 2, note.Text, "middle", options.FontSize - 2);
#endif

            // Curved dashed line connecting note to state using center-targeting algorithm
            var noteCenterX = noteX + noteWidth / 2;
            var noteCenterY = noteY + NoteHeight / 2;

            // State connection point - target note center, clip at state edge
            var (stateConnectX, stateConnectY) = GetEdgeIntersection(state, noteCenterX, noteCenterY);

            // Note connection point - target state center, clip at note edge (rectangle)
            var dx = state.Position.X - noteCenterX;
            var dy = state.Position.Y - noteCenterY;
            var noteHalfW = noteWidth / 2;
            var noteHalfH = NoteHeight / 2;
            var tX = Math.Abs(dx) > 0.001 ? noteHalfW / Math.Abs(dx) : double.MaxValue;
            var tY = Math.Abs(dy) > 0.001 ? noteHalfH / Math.Abs(dy) : double.MaxValue;
            var t = Math.Min(tX, tY);
            var noteConnectX = noteCenterX + dx * t;
            var noteConnectY = noteCenterY + dy * t;

            // Draw curved dashed line
            var midY = (stateConnectY + noteConnectY) / 2;
            var curvePath = $"M {Fmt(stateConnectX)} {Fmt(stateConnectY)} " +
                           $"Q {Fmt(stateConnectX)} {Fmt(midY)}, {Fmt(noteConnectX)} {Fmt(noteConnectY)}";

            builder.AddPath(curvePath, fill: "none", stroke: "#333", strokeWidth: 1, strokeDasharray: "5,5");
        }
    }

    static double MeasureText(string text, double fontSize) =>
        text.Length * fontSize * 0.6;

    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}

// Internal graph model for layout
internal class StateLayoutGraph : GraphDiagramBase;
