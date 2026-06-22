namespace Naiad.Diagrams.Sequence;

public class SequenceRenderer : IDiagramRenderer<SequenceModel>
{
    const double participantWidth = 100;
    const double participantHeight = 40;
    const double participantSpacing = 150;
    const double messageSpacing = 50;
    const double activationWidth = 10;
    const double noteWidth = 120;
    const double noteHeight = 40;
    const double actorHeadRadius = 15;

    public SvgDocument Render(SequenceModel model, RenderOptions options)
    {
        var participantPositions = CalculateParticipantPositions(model, options);
        var (height, elementYPositions) = CalculateHeight(model, options);
        var width = CalculateWidth(model, options);

        var builder = new SvgBuilder()
            .Size(width, height)
            .AddArrowMarker()
            .AddArrowMarker("arrowhead-dotted")
            .AddCrossMarker();

        // Add title if present
        var titleOffset = 0.0;
        if (!string.IsNullOrEmpty(model.Title))
        {
            titleOffset = 30;
            builder.AddText(
                width / 2,
                20,
                model.Title,
                anchor: "middle",
                fontSize: 16,
                fontFamily: options.FontFamily,
                fontWeight: "bold");
        }

        var startY = options.Padding + titleOffset;

        // Draw participants (top)
        DrawParticipants(builder, model, participantPositions, startY, options);

        // Draw lifelines
        var lifelineStartY = startY + participantHeight;
        var lifelineEndY = height - options.Padding - participantHeight;
        DrawLifelines(builder, model, participantPositions, lifelineStartY, lifelineEndY);

        // Draw elements (messages, notes, activations)
        var activations = new Dictionary<string, List<(double startY, double endY)>>();
        DrawElements(builder, model, participantPositions, elementYPositions, options, activations);

        // Draw activation boxes
        DrawActivations(builder, activations, participantPositions);

        // Draw participants (bottom) - optional, mimics Mermaid behavior
        DrawParticipants(builder, model, participantPositions, lifelineEndY, options);

        return builder.Build();
    }

    static Dictionary<string, double> CalculateParticipantPositions(SequenceModel model, RenderOptions options)
    {
        var positions = new Dictionary<string, double>();
        var x = options.Padding + participantWidth / 2;

        foreach (var participant in model.Participants)
        {
            positions[participant.Id] = x;
            x += participantSpacing;
        }

        return positions;
    }

    static (double height, Dictionary<int, double> elementYPositions) CalculateHeight(
        SequenceModel model, RenderOptions options)
    {
        var elementYPositions = new Dictionary<int, double>();
        var y = options.Padding + participantHeight + messageSpacing;
        var titleOffset = string.IsNullOrEmpty(model.Title) ? 0 : 30;

        for (var i = 0; i < model.Elements.Count; i++)
        {
            elementYPositions[i] = y + titleOffset;
            y += GetElementHeight(model.Elements[i]);
        }

        var totalHeight = y + participantHeight + options.Padding + titleOffset;
        return (totalHeight, elementYPositions);
    }

    static double GetElementHeight(SequenceElement element) =>
        element switch
        {
            Message => messageSpacing,
            Note => noteHeight + 10,
            Activation => 0, // Activations don't add height
            _ => messageSpacing
        };

    static double CalculateWidth(SequenceModel model, RenderOptions options)
    {
        var participantCount = Math.Max(1, model.Participants.Count);
        return options.Padding * 2 + participantWidth + (participantCount - 1) * participantSpacing;
    }

    static void DrawParticipants(SvgBuilder builder, SequenceModel model,
        Dictionary<string, double> positions, double y, RenderOptions options)
    {
        foreach (var participant in model.Participants)
        {
            var x = positions[participant.Id];

            if (participant.Type == ParticipantType.Actor)
            {
                DrawActor(builder, x, y, participant.DisplayName, options);
            }
            else
            {
                DrawParticipantBox(builder, x, y, participant.DisplayName, options);
            }
        }
    }

    static void DrawParticipantBox(SvgBuilder builder, double cx, double y,
        string text, RenderOptions options)
    {
        builder.AddRect(
            cx - participantWidth / 2,
            y,
            participantWidth,
            participantHeight,
            rx: 3,
            fill: "#ECECFF",
            stroke: "#9370DB",
            strokeWidth: 1);

        builder.AddText(
            cx,
            y + participantHeight / 2,
            text,
            anchor: "middle",
            baseline: "middle",
            fontSize: options.FontSize,
            fontFamily: options.FontFamily);
    }

    static void DrawActor(SvgBuilder builder, double cx, double y,
        string text, RenderOptions options)
    {
        // Stick figure
        var headY = y + actorHeadRadius;
        var bodyTop = headY + actorHeadRadius;
        var bodyBottom = bodyTop + 15;
        var armY = bodyTop + 5;
        var legBottom = y + participantHeight;

        // Head
        builder.AddCircle(
            cx,
            headY,
            actorHeadRadius,
            fill: "#ECECFF",
            stroke: "#9370DB",
            strokeWidth: 1);

        // Body
        builder.AddLine(
            cx,
            bodyTop,
            cx,
            bodyBottom,
            stroke: "#9370DB",
            strokeWidth: 1);

        // Arms
        builder.AddLine(
            cx - 15,
            armY,
            cx + 15,
            armY,
            stroke: "#9370DB",
            strokeWidth: 1);

        // Legs
        builder.AddLine(
            cx,
            bodyBottom,
            cx - 10,
            legBottom,
            stroke: "#9370DB",
            strokeWidth: 1);
        builder.AddLine(
            cx,
            bodyBottom,
            cx + 10,
            legBottom,
            stroke: "#9370DB",
            strokeWidth: 1);

        // Label below
        builder.AddText(
            cx,
            y + participantHeight + 15,
            text,
            anchor: "middle",
            baseline: "top",
            fontSize: options.FontSize,
            fontFamily: options.FontFamily);
    }

    static void DrawLifelines(SvgBuilder builder, SequenceModel model,
        Dictionary<string, double> positions, double startY, double endY)
    {
        foreach (var participant in model.Participants)
        {
            var x = positions[participant.Id];
            builder.AddLine(
                x,
                startY,
                x,
                endY,
                stroke: "#999",
                strokeWidth: 1,
                strokeDasharray: "5,5");
        }
    }

    static void DrawElements(SvgBuilder builder, SequenceModel model,
        Dictionary<string, double> positions,
        Dictionary<int, double> yPositions,
        RenderOptions options,
        Dictionary<string, List<(double startY, double endY)>> activations)
    {
        var messageNumber = 0;
        var activeLifelines = new Dictionary<string, double>(); // participantId -> activation start Y

        for (var i = 0; i < model.Elements.Count; i++)
        {
            var element = model.Elements[i];
            var y = yPositions[i];

            switch (element)
            {
                case Message msg:
                    messageNumber++;
                    DrawMessage(
                        builder,
                        msg,
                        positions,
                        y,
                        options,
                        model.AutoNumber ? messageNumber : null);

                    // Handle activation on message
                    if (msg.Activate)
                    {
                        activeLifelines[msg.ToId] = y;
                    }

                    if (msg.Deactivate && activeLifelines.TryGetValue(msg.ToId, out var startY))
                    {
                        if (!activations.ContainsKey(msg.ToId))
                            activations[msg.ToId] = [];
                        activations[msg.ToId].Add((startY, y));
                        activeLifelines.Remove(msg.ToId);
                    }

                    break;

                case Note note:
                    DrawNote(builder, note, positions, y, options);
                    break;

                case Activation activation:
                    if (activation.IsActivate)
                    {
                        activeLifelines[activation.ParticipantId] = y;
                    }
                    else if (activeLifelines.TryGetValue(activation.ParticipantId, out var actStartY))
                    {
                        if (!activations.ContainsKey(activation.ParticipantId))
                            activations[activation.ParticipantId] = [];
                        activations[activation.ParticipantId].Add((actStartY, y));
                        activeLifelines.Remove(activation.ParticipantId);
                    }

                    break;
            }
        }

        // Close any remaining activations
        // ReSharper disable once UseIndexFromEndExpression
        var lastY = yPositions.Count > 0 ? yPositions[yPositions.Count - 1] + messageSpacing : 0;
        foreach (var (participantId, startY) in activeLifelines)
        {
            if (!activations.TryGetValue(participantId, out var value))
            {
                value = [];
                activations[participantId] = value;
            }

            value.Add((startY, lastY));
        }
    }

    static void DrawMessage(SvgBuilder builder, Message msg,
        Dictionary<string, double> positions, double y,
        RenderOptions options, int? number)
    {
        var fromX = positions[msg.FromId];
        var toX = positions[msg.ToId];
        var isSelfMessage = msg.FromId == msg.ToId;

        var isDotted = msg.Type is MessageType.Dotted or MessageType.DottedArrow
            or MessageType.DottedOpen or MessageType.DottedCross or MessageType.DottedAsync;

        var markerEnd = msg.Type switch
        {
            MessageType.SolidCross or MessageType.DottedCross => "url(#cross)",
            MessageType.SolidOpen or MessageType.DottedOpen => null,
            _ => "url(#arrowhead)"
        };

        var dashArray = isDotted ? "5,5" : null;

        if (isSelfMessage)
        {
            // Self-referencing message - draw as a loop
            const int loopWidth = 40;
            const int loopHeight = 30;
            var path = string.Create(
                CultureInfo.InvariantCulture,
                $"M{fromX:0.##},{y:0.##} L{fromX + loopWidth:0.##},{y:0.##} L{fromX + loopWidth:0.##},{y + loopHeight:0.##} L{fromX:0.##},{y + loopHeight:0.##}");
            builder.AddPath(
                path,
                fill: "none",
                stroke: "#333",
                strokeWidth: 1,
                strokeDasharray: dashArray,
                markerEnd: markerEnd);

            // Text above
            if (!string.IsNullOrEmpty(msg.Text))
            {
                var labelText = number.HasValue ? $"{number}. {msg.Text}" : msg.Text;
                builder.AddText(
                    fromX + loopWidth + 5,
                    y + loopHeight / 2,
                    labelText,
                    anchor: "start",
                    baseline: "middle",
                    fontSize: options.FontSize,
                    fontFamily: options.FontFamily);
            }
        }
        else
        {
            builder.AddLine(
                fromX,
                y,
                toX,
                y,
                stroke: "#333",
                strokeWidth: 1,
                strokeDasharray: dashArray);

            // Draw arrowhead manually since line doesn't support marker
            DrawArrowhead(builder, fromX, toX, y, msg.Type);

            // Text above the line
            if (!string.IsNullOrEmpty(msg.Text) || number.HasValue)
            {
                var labelText = number.HasValue && !string.IsNullOrEmpty(msg.Text)
                    ? $"{number}. {msg.Text}"
                    : number.HasValue
                        ? $"{number}."
                        : msg.Text!;

                var midX = (fromX + toX) / 2;
                builder.AddText(
                    midX,
                    y - 8,
                    labelText,
                    anchor: "middle",
                    baseline: "bottom",
                    fontSize: options.FontSize,
                    fontFamily: options.FontFamily);
            }
        }
    }

    static void DrawArrowhead(SvgBuilder builder, double fromX, double toX, double y, MessageType type)
    {
        var direction = Math.Sign(toX - fromX);
        const int arrowSize = 8;

        switch (type)
        {
            case MessageType.SolidArrow:
            case MessageType.DottedArrow:
            case MessageType.Solid:
            case MessageType.Dotted:
            case MessageType.SolidAsync:
            case MessageType.DottedAsync:
                // Filled arrowhead
                var backX = toX - direction * arrowSize;
                builder.AddPolygon([
                    new(toX, y),
                    new(backX, y - arrowSize / 2),
                    new(backX, y + arrowSize / 2)
                ], fill: "#333");
                break;

            case MessageType.SolidOpen:
            case MessageType.DottedOpen:
                // Open arrowhead (just lines)
                builder.AddLine(
                    toX - direction * arrowSize,
                    y - arrowSize / 2,
                    toX,
                    y,
                    stroke: "#333",
                    strokeWidth: 1);
                builder.AddLine(
                    toX - direction * arrowSize,
                    y + arrowSize / 2,
                    toX,
                    y,
                    stroke: "#333",
                    strokeWidth: 1);
                break;

            case MessageType.SolidCross:
            case MessageType.DottedCross:
                // X mark
                builder.AddLine(
                    toX - arrowSize / 2,
                    y - arrowSize / 2,
                    toX + arrowSize / 2,
                    y + arrowSize / 2,
                    stroke: "#333",
                    strokeWidth: 2);
                builder.AddLine(
                    toX - arrowSize / 2,
                    y + arrowSize / 2,
                    toX + arrowSize / 2,
                    y - arrowSize / 2,
                    stroke: "#333",
                    strokeWidth: 2);
                break;
        }
    }

    static void DrawNote(SvgBuilder builder, Note note,
        Dictionary<string, double> positions, double y, RenderOptions options)
    {
        var participantX = positions[note.ParticipantId];
        double noteX;

        switch (note.Position)
        {
            case NotePosition.RightOf:
                noteX = participantX + participantWidth / 2 + 10;
                break;
            case NotePosition.LeftOf:
                noteX = participantX - participantWidth / 2 - noteWidth - 10;
                break;
            case NotePosition.Over:
            default:
                if (!string.IsNullOrEmpty(note.OverParticipantId2) &&
                    positions.TryGetValue(note.OverParticipantId2, out var participant2X))
                {
                    noteX = (participantX + participant2X) / 2 - noteWidth / 2;
                }
                else
                {
                    noteX = participantX - noteWidth / 2;
                }

                break;
        }

        // Note box (folded corner style)
        const int foldSize = 8;
        var path = string.Create(
            CultureInfo.InvariantCulture,
            $"M{noteX:0.##},{y:0.##} L{noteX + noteWidth - foldSize:0.##},{y:0.##} L{noteX + noteWidth:0.##},{y + foldSize:0.##} L{noteX + noteWidth:0.##},{y + noteHeight:0.##} L{noteX:0.##},{y + noteHeight:0.##} Z");

        builder.AddPath(path, fill: "#FFFFCC", stroke: "#AAAA33", strokeWidth: 1);

        // Fold line
        builder.AddLine(
            noteX + noteWidth - foldSize,
            y,
            noteX + noteWidth - foldSize,
            y + foldSize,
            stroke: "#AAAA33",
            strokeWidth: 1);
        builder.AddLine(
            noteX + noteWidth - foldSize,
            y + foldSize,
            noteX + noteWidth,
            y + foldSize,
            stroke: "#AAAA33",
            strokeWidth: 1);

        // Note text
        builder.AddText(
            noteX + noteWidth / 2,
            y + noteHeight / 2,
            note.Text,
            anchor: "middle",
            baseline: "middle",
            fontSize: options.FontSize,
            fontFamily: options.FontFamily);
    }

    static void DrawActivations(
        SvgBuilder builder,
        Dictionary<string, List<(double startY, double endY)>> activations,
        Dictionary<string, double> positions)
    {
        foreach (var (participantId, ranges) in activations)
        {
            var x = positions[participantId];
            foreach (var (startY, endY) in ranges)
            {
                builder.AddRect(
                    x - activationWidth / 2,
                    startY,
                    activationWidth,
                    endY - startY,
                    fill: "#F4F4F4",
                    stroke: "#666",
                    strokeWidth: 1);
            }
        }
    }

}