namespace MermaidSharp.Diagrams.Sequence;

public class SequenceRenderer : IDiagramRenderer<SequenceModel>
{
    const double ParticipantWidth = 100;
    const double ParticipantHeight = 40;
    const double ParticipantSpacing = 150;
    const double MessageSpacing = 50;
    const double ActivationWidth = 10;
    const double NoteWidth = 120;
    const double NoteHeight = 40;
    const double ActorHeadRadius = 15;

    public SvgDocument Render(SequenceModel model, RenderOptions options)
    {
        var participantPositions = CalculateParticipantPositions(model, options);
        var (height, elementYPositions) = CalculateHeight(model, options);
        var width = CalculateWidth(model, options);

        var builder = new SvgBuilder()
            .Size(width, height)
            .AddArrowMarker("arrowhead", "#333")
            .AddArrowMarker("arrowhead-dotted", "#333")
            .AddCrossMarker("cross", "#333");

        // Add title if present
        var titleOffset = 0.0;
        if (!string.IsNullOrEmpty(model.Title))
        {
            titleOffset = 30;
            builder.AddText(width / 2, 20, model.Title,
                anchor: "middle",
                fontSize: "16px",
                fontFamily: options.FontFamily,
                fontWeight: "bold");
        }

        var startY = options.Padding + titleOffset;

        // Draw participants (top)
        DrawParticipants(builder, model, participantPositions, startY, options);

        // Draw lifelines
        var lifelineStartY = startY + ParticipantHeight;
        var lifelineEndY = height - options.Padding - ParticipantHeight;
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
        var x = options.Padding + ParticipantWidth / 2;

        foreach (var participant in model.Participants)
        {
            positions[participant.Id] = x;
            x += ParticipantSpacing;
        }

        return positions;
    }

    static (double height, Dictionary<int, double> elementYPositions) CalculateHeight(
        SequenceModel model, RenderOptions options)
    {
        var elementYPositions = new Dictionary<int, double>();
        var y = options.Padding + ParticipantHeight + MessageSpacing;
        var titleOffset = string.IsNullOrEmpty(model.Title) ? 0 : 30;

        for (int i = 0; i < model.Elements.Count; i++)
        {
            elementYPositions[i] = y + titleOffset;
            y += GetElementHeight(model.Elements[i]);
        }

        var totalHeight = y + ParticipantHeight + options.Padding + titleOffset;
        return (totalHeight, elementYPositions);
    }

    static double GetElementHeight(SequenceElement element)
    {
        return element switch
        {
            MessageElement => MessageSpacing,
            NoteElement => NoteHeight + 10,
            ActivationElement => 0, // Activations don't add height
            _ => MessageSpacing
        };
    }

    static double CalculateWidth(SequenceModel model, RenderOptions options)
    {
        var participantCount = Math.Max(1, model.Participants.Count);
        return options.Padding * 2 + ParticipantWidth + (participantCount - 1) * ParticipantSpacing;
    }

    void DrawParticipants(SvgBuilder builder, SequenceModel model,
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

    void DrawParticipantBox(SvgBuilder builder, double cx, double y,
        string text, RenderOptions options)
    {
        builder.AddRect(
            cx - ParticipantWidth / 2, y,
            ParticipantWidth, ParticipantHeight,
            rx: 3,
            fill: "#ECECFF",
            stroke: "#9370DB",
            strokeWidth: 1);

        builder.AddText(
            cx, y + ParticipantHeight / 2,
            text,
            anchor: "middle",
            baseline: "middle",
            fontSize: $"{options.FontSize}px",
            fontFamily: options.FontFamily);
    }

    void DrawActor(SvgBuilder builder, double cx, double y,
        string text, RenderOptions options)
    {
        // Stick figure
        var headY = y + ActorHeadRadius;
        var bodyTop = headY + ActorHeadRadius;
        var bodyBottom = bodyTop + 15;
        var armY = bodyTop + 5;
        var legBottom = y + ParticipantHeight;

        // Head
        builder.AddCircle(cx, headY, ActorHeadRadius,
            fill: "#ECECFF",
            stroke: "#9370DB",
            strokeWidth: 1);

        // Body
        builder.AddLine(cx, bodyTop, cx, bodyBottom,
            stroke: "#9370DB",
            strokeWidth: 1);

        // Arms
        builder.AddLine(cx - 15, armY, cx + 15, armY,
            stroke: "#9370DB",
            strokeWidth: 1);

        // Legs
        builder.AddLine(cx, bodyBottom, cx - 10, legBottom,
            stroke: "#9370DB",
            strokeWidth: 1);
        builder.AddLine(cx, bodyBottom, cx + 10, legBottom,
            stroke: "#9370DB",
            strokeWidth: 1);

        // Label below
        builder.AddText(
            cx, y + ParticipantHeight + 15,
            text,
            anchor: "middle",
            baseline: "top",
            fontSize: $"{options.FontSize}px",
            fontFamily: options.FontFamily);
    }

    static void DrawLifelines(SvgBuilder builder, SequenceModel model,
        Dictionary<string, double> positions, double startY, double endY)
    {
        foreach (var participant in model.Participants)
        {
            var x = positions[participant.Id];
            builder.AddLine(x, startY, x, endY,
                stroke: "#999",
                strokeWidth: 1,
                strokeDasharray: "5,5");
        }
    }

    void DrawElements(SvgBuilder builder, SequenceModel model,
        Dictionary<string, double> positions,
        Dictionary<int, double> yPositions,
        RenderOptions options,
        Dictionary<string, List<(double startY, double endY)>> activations)
    {
        var messageNumber = 0;
        var activeLifelines = new Dictionary<string, double>(); // participantId -> activation start Y

        for (int i = 0; i < model.Elements.Count; i++)
        {
            var element = model.Elements[i];
            var y = yPositions[i];

            switch (element)
            {
                case MessageElement msg:
                    messageNumber++;
                    DrawMessage(builder, msg, positions, y, options,
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

                case NoteElement note:
                    DrawNote(builder, note, positions, y, options);
                    break;

                case ActivationElement activation:
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
        var lastY = yPositions.Count > 0 ? yPositions[yPositions.Count - 1] + MessageSpacing : 0;
        foreach (var (participantId, startY) in activeLifelines)
        {
            if (!activations.ContainsKey(participantId))
                activations[participantId] = [];
            activations[participantId].Add((startY, lastY));
        }
    }

    void DrawMessage(SvgBuilder builder, MessageElement msg,
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
            var loopWidth = 40;
            var loopHeight = 30;
            var path = $"M{Fmt(fromX)},{Fmt(y)} " +
                       $"L{Fmt(fromX + loopWidth)},{Fmt(y)} " +
                       $"L{Fmt(fromX + loopWidth)},{Fmt(y + loopHeight)} " +
                       $"L{Fmt(fromX)},{Fmt(y + loopHeight)}";
            builder.AddPath(path,
                fill: "none",
                stroke: "#333",
                strokeWidth: 1,
                strokeDasharray: dashArray,
                markerEnd: markerEnd);

            // Text above
            if (!string.IsNullOrEmpty(msg.Text))
            {
                var labelText = number.HasValue ? $"{number}. {msg.Text}" : msg.Text;
                builder.AddText(fromX + loopWidth + 5, y + loopHeight / 2, labelText,
                    anchor: "start",
                    baseline: "middle",
                    fontSize: $"{options.FontSize}px",
                    fontFamily: options.FontFamily);
            }
        }
        else
        {
            builder.AddLine(fromX, y, toX, y,
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
                    : number.HasValue ? $"{number}." : msg.Text!;

                var midX = (fromX + toX) / 2;
                builder.AddText(midX, y - 8, labelText,
                    anchor: "middle",
                    baseline: "bottom",
                    fontSize: $"{options.FontSize}px",
                    fontFamily: options.FontFamily);
            }
        }
    }

    void DrawArrowhead(SvgBuilder builder, double fromX, double toX, double y, MessageType type)
    {
        var direction = Math.Sign(toX - fromX);
        var arrowSize = 8;

        switch (type)
        {
            case MessageType.SolidArrow:
            case MessageType.DottedArrow:
            case MessageType.Solid:
            case MessageType.Dotted:
            case MessageType.SolidAsync:
            case MessageType.DottedAsync:
                // Filled arrowhead
                var tipX = toX;
                var backX = toX - direction * arrowSize;
                builder.AddPolygon([
                    new(tipX, y),
                    new(backX, y - arrowSize / 2),
                    new(backX, y + arrowSize / 2)
                ], fill: "#333");
                break;

            case MessageType.SolidOpen:
            case MessageType.DottedOpen:
                // Open arrowhead (just lines)
                builder.AddLine(toX - direction * arrowSize, y - arrowSize / 2, toX, y,
                    stroke: "#333", strokeWidth: 1);
                builder.AddLine(toX - direction * arrowSize, y + arrowSize / 2, toX, y,
                    stroke: "#333", strokeWidth: 1);
                break;

            case MessageType.SolidCross:
            case MessageType.DottedCross:
                // X mark
                builder.AddLine(toX - arrowSize / 2, y - arrowSize / 2, toX + arrowSize / 2, y + arrowSize / 2,
                    stroke: "#333", strokeWidth: 2);
                builder.AddLine(toX - arrowSize / 2, y + arrowSize / 2, toX + arrowSize / 2, y - arrowSize / 2,
                    stroke: "#333", strokeWidth: 2);
                break;
        }
    }

    void DrawNote(SvgBuilder builder, NoteElement noteElement,
        Dictionary<string, double> positions, double y, RenderOptions options)
    {
        var participantX = positions[noteElement.ParticipantId];
        double noteX;

        switch (noteElement.Position)
        {
            case NotePosition.RightOf:
                noteX = participantX + ParticipantWidth / 2 + 10;
                break;
            case NotePosition.LeftOf:
                noteX = participantX - ParticipantWidth / 2 - NoteWidth - 10;
                break;
            case NotePosition.Over:
            default:
                if (!string.IsNullOrEmpty(noteElement.OverParticipantId2) &&
                    positions.TryGetValue(noteElement.OverParticipantId2, out var participant2X))
                {
                    noteX = (participantX + participant2X) / 2 - NoteWidth / 2;
                }
                else
                {
                    noteX = participantX - NoteWidth / 2;
                }
                break;
        }

        // Note box (folded corner style)
        var foldSize = 8;
        var path = $"M{Fmt(noteX)},{Fmt(y)} " +
                   $"L{Fmt(noteX + NoteWidth - foldSize)},{Fmt(y)} " +
                   $"L{Fmt(noteX + NoteWidth)},{Fmt(y + foldSize)} " +
                   $"L{Fmt(noteX + NoteWidth)},{Fmt(y + NoteHeight)} " +
                   $"L{Fmt(noteX)},{Fmt(y + NoteHeight)} Z";

        builder.AddPath(path, fill: "#FFFFCC", stroke: "#AAAA33", strokeWidth: 1);

        // Fold line
        builder.AddLine(noteX + NoteWidth - foldSize, y,
                       noteX + NoteWidth - foldSize, y + foldSize,
                       stroke: "#AAAA33", strokeWidth: 1);
        builder.AddLine(noteX + NoteWidth - foldSize, y + foldSize,
                       noteX + NoteWidth, y + foldSize,
                       stroke: "#AAAA33", strokeWidth: 1);

        // Note text
        builder.AddText(noteX + NoteWidth / 2, y + NoteHeight / 2, noteElement.Text,
            anchor: "middle",
            baseline: "middle",
            fontSize: $"{options.FontSize}px",
            fontFamily: options.FontFamily);
    }

    static void DrawActivations(SvgBuilder builder,
        Dictionary<string, List<(double startY, double endY)>> activations,
        Dictionary<string, double> positions)
    {
        foreach (var (participantId, ranges) in activations)
        {
            var x = positions[participantId];
            foreach (var (startY, endY) in ranges)
            {
                builder.AddRect(
                    x - ActivationWidth / 2, startY,
                    ActivationWidth, endY - startY,
                    fill: "#F4F4F4",
                    stroke: "#666",
                    strokeWidth: 1);
            }
        }
    }

    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
