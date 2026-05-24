using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;


// A single item in an ExpandableList, consisting of a label and an optional disabled overlay.
[UxmlElement(libraryPath = "Chat")]
public partial class ChatMessage : VectorFillShape
{
    static float outlineWidth = 4f;
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
        Color fillColor = resolvedColors.GetValueOrDefault(s_VectorFillColor.name, Color.clear);

        List<Vector2> points = new()
        {
            new Vector2(outlineWidth * 0.5f, outlineWidth * 0.5f),
            new Vector2(layout.width - outlineWidth * 0.5f - cornerWidth, outlineWidth * 0.5f),
            new Vector2(layout.width - outlineWidth * 0.5f, outlineWidth * 0.5f + cornerWidth * 0.5f),
            new Vector2(layout.width - outlineWidth * 0.5f, layout.height - outlineWidth * 0.5f),
            new Vector2(outlineWidth * 0.5f + cornerWidth, layout.height - outlineWidth * 0.5f),
            new Vector2(outlineWidth * 0.5f, layout.height - outlineWidth * 0.5f - cornerWidth * 0.5f),
        };
        BuildLineShape(mgc, points, lineColor, outlineWidth, LineJoin.Miter, LineCap.Butt, true);

        points = new()
        {
            new Vector2(outlineWidth, outlineWidth),
            new Vector2(layout.width - outlineWidth - cornerWidth + outlineWidth * 0.4f, outlineWidth),
            new Vector2(layout.width - outlineWidth, outlineWidth + cornerWidth * 0.5f - outlineWidth * 0.2f),
            new Vector2(layout.width - outlineWidth, layout.height - outlineWidth),
            new Vector2(outlineWidth + cornerWidth - outlineWidth * 0.4f, layout.height - outlineWidth),
            new Vector2(outlineWidth, layout.height - outlineWidth - cornerWidth * 0.5f + outlineWidth * 0.2f),
        };
        BuildFillShape(mgc, points, fillColor);
    }

    public void Initialize(NotificationData messageData, Color containerColor)
    {
        // messageContainerImage.color = containerColor;
        senderLabel.text = messageData.title;
        // messageNameText.color = messageData.color;
        messageLabel.text = messageData.content;
        // Color contentColor = messageContainerImage.color;
        // contentColor = Color.Lerp(contentColor, Color.black, 0.8f);
        // messageContentText.color = contentColor;
    }
}