// ReSharper disable MemberCanBeMadeStatic.Local
namespace MermaidSharp.Diagrams.State;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class StateRenderer(ILayoutEngine? layoutEngine = null) :
    IDiagramRenderer<StateModel>
{
    readonly ILayoutEngine layoutEngine = layoutEngine ?? new DagreLayoutEngine();

#if DEBUG
    readonly List<TextBounds> _textBounds = [];
    readonly List<LineBounds> _lineBounds = [];
    readonly List<NodeBounds> _nodeBounds = [];
    double _svgWidth;
    double _svgHeight;

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

        // Calculate extra space needed for bidirectional forward edges (curve left)
        var curveExtraLeft = CalculateCurveExtraLeft(model, stateMap);
        var totalExtraLeft = Math.Max(noteExtraLeft, curveExtraLeft);

        // Calculate extra space needed for back-edges (curve right)
        var curveExtraRight = CalculateCurveExtraRight(model, stateMap);

        // Calculate extra height for end node if it was repositioned
        var endExtraHeight = CalculateEndNodeExtraHeight(model, layoutResult.Height);

        // Calculate extra height for routed transitions that go around obstacles
        var routedExtraHeight = CalculateRoutedTransitionExtraHeight(model, stateMap, layoutResult.Height);

        // Shift all positions right if notes or curves extend past left edge
        if (totalExtraLeft > 0)
        {
            foreach (var state in model.States)
                state.Position = new(state.Position.X + totalExtraLeft, state.Position.Y);
        }

        // Ensure end nodes don't overlap with other states (run after position shift)
        AdjustEndNodePosition(model);

        // Build SVG
        var svgWidth = layoutResult.Width + noteExtraWidth + totalExtraLeft + curveExtraRight;
        var svgHeight = layoutResult.Height + noteExtraHeight + endExtraHeight + routedExtraHeight;
#if DEBUG
        _svgWidth = svgWidth;
        _svgHeight = svgHeight;
#endif
        var builder = new SvgBuilder()
            .Size(svgWidth, svgHeight)
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
        CheckForNodeOverlaps();
        CheckForElementsOutsideBounds();
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

        _textBounds.Add(new(left, top, width, height, text));
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
        _lineBounds.Add(new(x1, y1, x2, y2, label));

    void TrackNode(double x, double y, double width, double height, string label) =>
        _nodeBounds.Add(new(x - width / 2, y - height / 2, width, height, label));

    void CheckForLinesUnderNodes()
    {
        foreach (var line in _lineBounds)
        {
            foreach (var node in _nodeBounds)
            {
                // Skip if line is connected to this node (endpoint is near/inside the node)
                var nodeRight = node.X + node.Width;
                var nodeBottom = node.Y + node.Height;
                var margin = 10.0; // Allow endpoints near edges

                var startInNode = line.X1 >= node.X - margin && line.X1 <= nodeRight + margin &&
                                  line.Y1 >= node.Y - margin && line.Y1 <= nodeBottom + margin;
                var endInNode = line.X2 >= node.X - margin && line.X2 <= nodeRight + margin &&
                                line.Y2 >= node.Y - margin && line.Y2 <= nodeBottom + margin;

                if (startInNode || endInNode)
                    continue; // This line is connected to this node, not passing under it

                // Check if line segment passes through node's bounding box
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

    void CheckForNodeOverlaps()
    {
        for (var i = 0; i < _nodeBounds.Count; i++)
        {
            for (var j = i + 1; j < _nodeBounds.Count; j++)
            {
                var a = _nodeBounds[i];
                var b = _nodeBounds[j];

                // Check for rectangle overlap with margin
                var margin = 2.0;
                var overlapsX = a.X < b.X + b.Width - margin && a.X + a.Width > b.X + margin;
                var overlapsY = a.Y < b.Y + b.Height - margin && a.Y + a.Height > b.Y + margin;

                if (overlapsX && overlapsY)
                {
                    throw new InvalidOperationException(
                        $"Node overlap detected: \"{a.Label}\" at ({a.X:F1},{a.Y:F1},{a.Width:F1}x{a.Height:F1}) " +
                        $"overlaps with \"{b.Label}\" at ({b.X:F1},{b.Y:F1},{b.Width:F1}x{b.Height:F1})");
                }
            }
        }
    }

    void CheckForElementsOutsideBounds()
    {
        // Check nodes
        foreach (var node in _nodeBounds)
        {
            if (node.X < 0 || node.Y < 0 ||
                node.X + node.Width > _svgWidth ||
                node.Y + node.Height > _svgHeight)
            {
                throw new InvalidOperationException(
                    $"Node outside bounds: \"{node.Label}\" at ({node.X:F1},{node.Y:F1},{node.Width:F1}x{node.Height:F1}) " +
                    $"is outside SVG bounds (0,0,{_svgWidth:F1}x{_svgHeight:F1})");
            }
        }

        // Check text
        foreach (var text in _textBounds)
        {
            if (text.X < 0 || text.Y < 0 ||
                text.X + text.Width > _svgWidth ||
                text.Y + text.Height > _svgHeight)
            {
                throw new InvalidOperationException(
                    $"Text outside bounds: \"{text.Label}\" at ({text.X:F1},{text.Y:F1},{text.Width:F1}x{text.Height:F1}) " +
                    $"is outside SVG bounds (0,0,{_svgWidth:F1}x{_svgHeight:F1})");
            }
        }

        // Check lines
        foreach (var line in _lineBounds)
        {
            if (line.X1 < 0 || line.Y1 < 0 || line.X2 < 0 || line.Y2 < 0 ||
                line.X1 > _svgWidth || line.Y1 > _svgHeight ||
                line.X2 > _svgWidth || line.Y2 > _svgHeight)
            {
                throw new InvalidOperationException(
                    $"Line outside bounds: \"{line.Label}\" from ({line.X1:F1},{line.Y1:F1}) to ({line.X2:F1},{line.Y2:F1}) " +
                    $"is outside SVG bounds (0,0,{_svgWidth:F1}x{_svgHeight:F1})");
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

    static double CalculateCurveExtraLeft(StateModel model, Dictionary<string, State> stateMap)
    {
        // Check if any bidirectional forward edges will curve left
        var bidirectionalPairs = FindBidirectionalPairs(model.Transitions);
        if (bidirectionalPairs.Count == 0)
            return 0;

        var leftEdge = model.States.Min(s => s.Position.X - s.Width / 2);
        double maxExtraNeeded = 0;

        foreach (var transition in model.Transitions)
        {
            var pairKey = GetPairKey(transition.FromId, transition.ToId);
            if (!bidirectionalPairs.Contains(pairKey))
                continue;

            // Check if this is a forward edge (not back edge)
            if (IsBackEdge(transition, stateMap))
                continue;

            // Forward edge of bidirectional pair - calculate how far left it extends
            // The curve goes to baseLeftEdge - 50
            var baseLeftEdge = leftEdge - 50;
            var curveExtraNeeded = -baseLeftEdge; // How much past x=0 it goes

            // Also account for label width if present (label is centered on vertical line)
            var labelExtraNeeded = 0.0;
            if (!string.IsNullOrEmpty(transition.Label))
            {
                var labelWidth = MeasureText(transition.Label, 12); // FontSize - 2
                var labelLeft = baseLeftEdge - labelWidth / 2;
                labelExtraNeeded = -labelLeft;
            }

            maxExtraNeeded = Math.Max(maxExtraNeeded, Math.Max(curveExtraNeeded, labelExtraNeeded));
        }

        return maxExtraNeeded > 0 ? maxExtraNeeded + 10 : 0; // Add margin
    }

    static double CalculateCurveExtraRight(StateModel model, Dictionary<string, State> stateMap)
    {
        // Check if any back-edges or bidirectional back-edges will curve right
        var bidirectionalPairs = FindBidirectionalPairs(model.Transitions);
        var rightEdge = model.States.Max(s => s.Position.X + s.Width / 2);

        // Count back-edges to determine spacing
        var backEdgeCount = model.Transitions
            .Count(t => IsBackEdge(t, stateMap) && !bidirectionalPairs.Contains(GetPairKey(t.FromId, t.ToId)));

        // Also count bidirectional back-edges
        var bidirectionalBackEdgeCount = model.Transitions
            .Count(t => IsBackEdge(t, stateMap) && bidirectionalPairs.Contains(GetPairKey(t.FromId, t.ToId)));

        var totalBackEdges = backEdgeCount + bidirectionalBackEdgeCount;
        if (totalBackEdges == 0)
            return 0;

        // Back-edges go to baseRightEdge + 50, with spacing of 50 between each
        // The rightmost curve extends to rightEdge + 50 + (backEdgeCount - 1) * 50
        var baseRightEdge = rightEdge + 50;
        var maxRightExtent = baseRightEdge + (totalBackEdges - 1) * 50;

        // Extra width needed beyond the layout width (which includes states up to rightEdge)
        var extraNeeded = maxRightExtent - rightEdge;
        return extraNeeded > 0 ? extraNeeded + 10 : 0; // Add margin
    }

    static double CalculateEndNodeExtraHeight(StateModel model, double layoutHeight)
    {
        var endNode = model.States.FirstOrDefault(s => s.Type == StateType.End);
        if (endNode == null)
            return 0;

        var endBottom = endNode.Position.Y + SpecialStateSize / 2;
        var extraNeeded = endBottom - layoutHeight;
        return extraNeeded > 0 ? extraNeeded + 10 : 0; // Add margin
    }

    static double CalculateRoutedTransitionExtraHeight(StateModel model, Dictionary<string, State> stateMap, double layoutHeight)
    {
        double maxExtraNeeded = 0;

        foreach (var transition in model.Transitions)
        {
            if (!stateMap.TryGetValue(transition.FromId, out var fromState) ||
                !stateMap.TryGetValue(transition.ToId, out var toState))
                continue;

            var (startX, startY) = GetConnectionPoint(fromState, toState);
            var (endX, endY) = GetConnectionPoint(toState, fromState);

            var obstacle = FindObstacleState(startX, startY, endX, endY, transition, stateMap);
            if (obstacle == null)
                continue;

            // Calculate how far down the routed path goes
            var obstacleBottom = obstacle.Position.Y + obstacle.Height / 2;
            var targetBottom = toState.Type == StateType.End
                ? toState.Position.Y + SpecialStateSize / 2
                : toState.Position.Y + toState.Height / 2;
            var margin = 30.0;
            var horizontalY = Math.Max(obstacleBottom, targetBottom) + margin;

            var extraNeeded = horizontalY - layoutHeight;
            maxExtraNeeded = Math.Max(maxExtraNeeded, extraNeeded);
        }

        return maxExtraNeeded > 0 ? maxExtraNeeded + 10 : 0;
    }

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

    static void AdjustEndNodePosition(StateModel model)
    {
        var endNode = model.States.FirstOrDefault(s => s.Type == StateType.End);
        if (endNode == null) return;

        const double margin = 30;
        var endHalfSize = SpecialStateSize / 2;

        // Find siblings at similar Y level (within 100 pixels) and move end node to the right
        foreach (var state in model.States)
        {
            if (state.Type is StateType.End or StateType.Start or StateType.Fork or StateType.Join or StateType.Choice)
                continue;

            // Check if this state is at a similar vertical level as the end node
            var yDistance = Math.Abs(state.Position.Y - endNode.Position.Y);
            if (yDistance > 100)
                continue;

            // Check if they're horizontally close (would overlap in a straight line from parent)
            var xDistance = Math.Abs(state.Position.X - endNode.Position.X);
            if (xDistance > state.Width)
                continue;

            // Move end node to the right of this state, at the same Y level
            var stateRight = state.Position.X + state.Width / 2;
            var newX = stateRight + margin + endHalfSize;
            endNode.Position = new(newX, state.Position.Y);
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
#if DEBUG
                TrackNode(x, y, SpecialStateSize, SpecialStateSize, state.Id);
#endif
                break;

            case StateType.End:
                // Double circle
                builder.AddCircle(x, y, SpecialStateSize / 2,
                    fill: "none", stroke: "#333", strokeWidth: 2);
                builder.AddCircle(x, y, SpecialStateSize / 4,
                    fill: "#333", stroke: "#333", strokeWidth: 1);
#if DEBUG
                TrackNode(x, y, SpecialStateSize, SpecialStateSize, state.Id);
#endif
                break;

            case StateType.Fork:
            case StateType.Join:
                // Horizontal bar
                builder.AddRect(
                    x - state.Width / 2, y - state.Height / 2,
                    state.Width, state.Height,
                    fill: "#333", stroke: "#333");
#if DEBUG
                TrackNode(x, y, state.Width, state.Height, state.Id);
#endif
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
#if DEBUG
                TrackNode(x, y, state.Width, state.Height, state.Id);
#endif
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
                // Bidirectional pair - use curves (forward curves left, back curves right)
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
            // Exclude special states (Start/End) from edge calculation since they may be repositioned
            var normalStates = model.States.Where(s => s.Type == StateType.Normal).ToList();
            var baseRightEdge = (normalStates.Count > 0 ? normalStates.Max(s => s.Position.X + s.Width / 2) : 100) + 50;

            // Use spacing of 50px between lines - enough for typical labels
            var lineSpacing = 50;
            var rightEdge = baseRightEdge + backEdgeIndex * lineSpacing;

            // Back-edges use smooth curves: angle out, go vertical, angle back in
            // Exit from right side of source state (center Y)
            var startX = fromState.Position.X + fromState.Width / 2;
            var startY = fromState.Position.Y;
            // Enter right side of target state - offset each line so they don't overlap
            // Outer lines (higher index, further right) enter higher to avoid crossing
            var endX = toState.Position.X + toState.Width / 2;
            var entrySpacing = 15.0;
            var endY = toState.Position.Y - backEdgeIndex * entrySpacing;

            // Radius for the quarter-circle curves at corners
            var curveRadius = Math.Min(80, (rightEdge - startX) / 2);

            // Path: smooth curve out, vertical line, smooth curve in
            // Curves gradually transition - tangent horizontal at state, tangent vertical at line
            var path = $"M {Fmt(startX)} {Fmt(startY)} " +
                       // Exit curve: gradual from horizontal to vertical
                       // P1 horizontal from start, P2 vertical from end
                       $"C {Fmt(startX + curveRadius)} {Fmt(startY)}, " +
                       $"{Fmt(rightEdge)} {Fmt(startY - curveRadius)}, " +
                       $"{Fmt(rightEdge)} {Fmt(startY - curveRadius * 2)} " +
                       // Vertical line up
                       $"L {Fmt(rightEdge)} {Fmt(endY + curveRadius * 2)} " +
                       // Entry curve: gradual from vertical to horizontal (mirrored)
                       // P1 vertical from start, P2 horizontal from end
                       $"C {Fmt(rightEdge)} {Fmt(endY + curveRadius)}, " +
                       $"{Fmt(endX + curveRadius)} {Fmt(endY)}, " +
                       $"{Fmt(endX)} {Fmt(endY)}";

            builder.AddPath(path, fill: "none", stroke: "#333", strokeWidth: 1);

#if DEBUG
            var lineLabel = transition.Label ?? $"{transition.FromId}->{transition.ToId}";
            // Track segments for collision detection (symmetric at both ends)
            // Exit: only track initial horizontal portion before curve rises
            TrackLine(startX, startY, startX + curveRadius, startY, lineLabel);
            // Vertical segment
            TrackLine(rightEdge, startY - curveRadius * 2, rightEdge, endY + curveRadius * 2, lineLabel);
            // Entry: only track final horizontal portion after curve flattens
            TrackLine(endX + curveRadius, endY, endX, endY, lineLabel);
#endif

            // Arrowhead comes in horizontally from the right
            DrawArrowhead(builder, endX + curveRadius, endY, endX, endY);

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
            // Forward edge (mirror of back-edge) - curves to the LEFT
            // Route around the left side of the diagram
            // Exclude special states (Start/End) from edge calculation
            var normalStates = model.States.Where(s => s.Type == StateType.Normal).ToList();
            var baseLeftEdge = (normalStates.Count > 0 ? normalStates.Min(s => s.Position.X - s.Width / 2) : 0) - 50;

            // Use same spacing as back-edges
            var lineSpacing = 50;
            var leftEdge = baseLeftEdge - backEdgeIndex * lineSpacing;

            // Exit from left side of source state (center Y)
            var startX = fromState.Position.X - fromState.Width / 2;
            var startY = fromState.Position.Y;
            // Enter left side of target state
            var endX = toState.Position.X - toState.Width / 2;
            var entrySpacing = 15.0;
            var endY = toState.Position.Y + backEdgeIndex * entrySpacing;

            // Radius for the quarter-circle curves at corners (mirror of back-edge)
            var curveRadius = Math.Min(80, (startX - leftEdge) / 2);

            // Path: smooth curve out to left, vertical line down, smooth curve in
            // Mirror of back-edge algorithm
            var path = $"M {Fmt(startX)} {Fmt(startY)} " +
                       // Exit curve: gradual from horizontal to vertical (going left then down)
                       $"C {Fmt(startX - curveRadius)} {Fmt(startY)}, " +
                       $"{Fmt(leftEdge)} {Fmt(startY + curveRadius)}, " +
                       $"{Fmt(leftEdge)} {Fmt(startY + curveRadius * 2)} " +
                       // Vertical line down
                       $"L {Fmt(leftEdge)} {Fmt(endY - curveRadius * 2)} " +
                       // Entry curve: gradual from vertical to horizontal (mirrored)
                       $"C {Fmt(leftEdge)} {Fmt(endY - curveRadius)}, " +
                       $"{Fmt(endX - curveRadius)} {Fmt(endY)}, " +
                       $"{Fmt(endX)} {Fmt(endY)}";

            builder.AddPath(path, fill: "none", stroke: "#333", strokeWidth: 1);

#if DEBUG
            var lineLabel = transition.Label ?? $"{transition.FromId}->{transition.ToId}";
            // Track segments for collision detection (mirror of back-edge)
            TrackLine(startX, startY, startX - curveRadius, startY, lineLabel);
            TrackLine(leftEdge, startY + curveRadius * 2, leftEdge, endY - curveRadius * 2, lineLabel);
            TrackLine(endX - curveRadius, endY, endX, endY, lineLabel);
#endif

            // Arrowhead comes in horizontally from the left
            DrawArrowhead(builder, endX - curveRadius, endY, endX, endY);

            if (!string.IsNullOrEmpty(transition.Label))
            {
                // Position label centered on this edge's vertical line
                var labelX = leftEdge;
                var labelY = (fromState.Position.Y + toState.Position.Y) / 2;

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

        // Check if line would pass through any other state
        var obstacleState = FindObstacleState(startX, startY, endX, endY, transition, stateMap);

        if (obstacleState != null)
        {
            // Route around the obstacle
            RenderRoutedTransition(builder, transition, fromState, toState, obstacleState, options);
        }
        else
        {
            // Draw straight arrow line
            builder.AddLine(startX, startY, endX, endY, stroke: "#333", strokeWidth: 1);

#if DEBUG
            var lineLabel = transition.Label ?? $"{transition.FromId}->{transition.ToId}";
            TrackLine(startX, startY, endX, endY, lineLabel);
#endif

            // Draw arrowhead
            DrawArrowhead(builder, startX, startY, endX, endY);

            // Draw label if present
            if (!string.IsNullOrEmpty(transition.Label))
            {
                // Position label along the line - at 85% for transitions to End nodes to avoid overlap with curves
                var t = toState.Type == StateType.End ? 0.85 : 0.5;
                var labelX = startX + t * (endX - startX);
                var labelY = startY + t * (endY - startY) - 10;

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

    static State? FindObstacleState(double x1, double y1, double x2, double y2,
        StateTransition transition, Dictionary<string, State> stateMap)
    {
        // Don't route transitions to end nodes - their position is adjusted to avoid overlap
        if (stateMap.TryGetValue(transition.ToId, out var toState) && toState.Type == StateType.End)
            return null;

        foreach (var kvp in stateMap)
        {
            var state = kvp.Value;
            // Skip source and target states
            if (state.Id == transition.FromId || state.Id == transition.ToId)
                continue;
            // Skip special states (start/end circles are small)
            if (state.Type is StateType.Start or StateType.End)
                continue;

            // Check if line passes through this state
            var left = state.Position.X - state.Width / 2 - 5;
            var right = state.Position.X + state.Width / 2 + 5;
            var top = state.Position.Y - state.Height / 2 - 5;
            var bottom = state.Position.Y + state.Height / 2 + 5;

            // Sample points along the line
            for (var i = 1; i < 20; i++)
            {
                var t = i / 20.0;
                var px = x1 + t * (x2 - x1);
                var py = y1 + t * (y2 - y1);

                if (px > left && px < right && py > top && py < bottom)
                    return state;
            }
        }
        return null;
    }

    void RenderRoutedTransition(SvgBuilder builder, StateTransition transition,
        State fromState, State toState, State obstacle, RenderOptions options)
    {
        // Route around the obstacle
        var obstacleLeft = obstacle.Position.X - obstacle.Width / 2;
        var obstacleRight = obstacle.Position.X + obstacle.Width / 2;

        // Determine which side to route around (pick the closer side)
        var fromX = fromState.Position.X;
        var routeLeft = Math.Abs(fromX - obstacleLeft) < Math.Abs(fromX - obstacleRight);

        // Connection points
        var startX = fromState.Position.X;
        var startY = fromState.Position.Y + fromState.Height / 2;
        var endX = toState.Position.X;
        // Since we're routing around and approaching from below, connect to BOTTOM of target
        var endY = toState.Type == StateType.End
            ? toState.Position.Y + SpecialStateSize / 2
            : toState.Position.Y + toState.Height / 2;

        // Route around obstacle
        var margin = 30.0;
        var routeX = routeLeft
            ? obstacleLeft - margin
            : obstacleRight + margin;

        // Create path: down from start, horizontal to route position, down past obstacle and target, then to end
        var obstacleTop = obstacle.Position.Y - obstacle.Height / 2;
        var obstacleBottom = obstacle.Position.Y + obstacle.Height / 2;

        // The horizontal return segment should be below both the obstacle AND the target
        var targetBottom = toState.Type == StateType.End
            ? toState.Position.Y + SpecialStateSize / 2
            : toState.Position.Y + toState.Height / 2;
        var horizontalY = Math.Max(obstacleBottom, targetBottom) + margin;

        var path = $"M {Fmt(startX)} {Fmt(startY)} " +
                   $"L {Fmt(startX)} {Fmt(obstacleTop - margin)} " +
                   $"L {Fmt(routeX)} {Fmt(obstacleTop - margin)} " +
                   $"L {Fmt(routeX)} {Fmt(horizontalY)} " +
                   $"L {Fmt(endX)} {Fmt(horizontalY)} " +
                   $"L {Fmt(endX)} {Fmt(endY)}";

        builder.AddPath(path, fill: "none", stroke: "#333", strokeWidth: 1);

#if DEBUG
        var lineLabel = transition.Label ?? $"{transition.FromId}->{transition.ToId}";
        // Track the segments
        TrackLine(startX, startY, startX, obstacleTop - margin, lineLabel);
        TrackLine(startX, obstacleTop - margin, routeX, obstacleTop - margin, lineLabel);
        TrackLine(routeX, obstacleTop - margin, routeX, horizontalY, lineLabel);
        TrackLine(routeX, horizontalY, endX, horizontalY, lineLabel);
        TrackLine(endX, horizontalY, endX, endY, lineLabel);
#endif

        // Draw arrowhead (pointing up since we approach from below)
        DrawArrowhead(builder, endX, horizontalY, endX, endY);

        // Draw label if present
        if (!string.IsNullOrEmpty(transition.Label))
        {
            var labelX = routeX;
            var labelY = obstacle.Position.Y;

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
