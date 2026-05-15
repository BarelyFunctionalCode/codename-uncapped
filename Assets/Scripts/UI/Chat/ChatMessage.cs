using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;


// A single item in an ExpandableList, consisting of a label and an optional disabled overlay.
[UxmlElement(libraryPath = "Chat/ChatMessage")]
public partial class ChatMessage : VectorFillShape
{
    static float outlineWidth = 5f;
    static float cornerWidth = 20f;

    Label senderLabel;
    Label messageLabel;

    public ChatMessage()
    {
        style.paddingLeft = outlineWidth;
        style.paddingRight = outlineWidth;
        style.paddingTop = outlineWidth;
        style.paddingBottom = outlineWidth;

        senderLabel = new Label
        {
            name = "sender-label",
            text = "Sender Name",
        };
        Add(senderLabel);

        messageLabel = new Label
        {
            name = "message-label",
            text = "Message content goes here. This is a placeholder for the actual message text.",
        };
        Add(messageLabel);
    }

    protected override void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        Color lineColor = resolvedColors.GetValueOrDefault(s_VectorLineColor.name, Color.clear);

        List<Vector2> points = new()
        {
            new Vector2(outlineWidth * 0.5f, outlineWidth * 0.5f),
            new Vector2(layout.width - outlineWidth * 0.5f - cornerWidth, outlineWidth * 0.5f),
            new Vector2(layout.width - outlineWidth * 0.5f, outlineWidth * 0.5f + cornerWidth * 0.5f),
            new Vector2(layout.width - outlineWidth * 0.5f, layout.height - outlineWidth * 0.5f),
            new Vector2(outlineWidth * 0.5f + cornerWidth, layout.height - outlineWidth * 0.5f),
            new Vector2(outlineWidth * 0.5f, layout.height - outlineWidth * 0.5f - cornerWidth * 0.5f),
        };
        BuildLineShape(mgc, points, lineColor, outlineWidth, LineJoin.Bevel, LineCap.Butt, true);
    }
}