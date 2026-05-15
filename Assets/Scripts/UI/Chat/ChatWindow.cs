using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;


// A single item in an ExpandableList, consisting of a label and an optional disabled overlay.
[UxmlElement(libraryPath = "Chat/ChatWindow")]
public partial class ChatWindow : VectorFillShape
{
    static float outlineWidth = 15f;

    VisualElement messagesContainer;
    VisualElement textInputContainer;
    ScrollView messagesScrollView;
    TextField textInput;

    public ChatWindow()
    {
        style.paddingLeft = outlineWidth;
        style.paddingBottom = outlineWidth;

        messagesContainer = new VisualElement();
        messagesContainer.style.marginBottom = outlineWidth;
        messagesContainer.name = "messages-container";
        Add(messagesContainer);
        messagesScrollView = new ScrollView(ScrollViewMode.Vertical);
        messagesScrollView.style.flexGrow = 1;
        messagesContainer.Add(messagesScrollView);

        textInputContainer = new VisualElement
        {
            name = "text-input-container"
        };
        Add(textInputContainer);

        textInput = new TextField
        {
            name = "text-input",
            label = "",
            multiline = true,
            isDelayed = true,
        };
        textInput.textEdition.hidePlaceholderOnFocus = true;
        textInput.textEdition.placeholder = "Type a message...";
        textInput.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
        textInputContainer.Add(textInput);

        RegisterCallback<GeometryChangedEvent>(evt =>
        {
            messagesContainer.style.height = new Length(layout.height * 0.6f + outlineWidth);
            textInputContainer.style.height = new Length(layout.height * 0.4f - outlineWidth * 2f);
        });
    }

    protected override void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        Color lineColor = resolvedColors.GetValueOrDefault(s_VectorLineColor.name, Color.clear);
        Color fillColor = resolvedColors.GetValueOrDefault(s_VectorFillColor.name, Color.clear);

        List<Vector2> points = new()
        {
            Vector2.zero,
            new Vector2(outlineWidth, 0),
            new Vector2(outlineWidth, layout.height * 0.6f + outlineWidth * 0.5f),
            new Vector2(outlineWidth + outlineWidth, layout.height * 0.6f + outlineWidth),
            new Vector2(layout.width, layout.height * 0.6f + outlineWidth),
            new Vector2(layout.width, layout.height * 0.6f + outlineWidth + outlineWidth),
            new Vector2(outlineWidth, layout.height * 0.6f + outlineWidth + outlineWidth),
            new Vector2(outlineWidth, layout.height - outlineWidth * 2f),
            new Vector2(outlineWidth + outlineWidth * 2, layout.height - outlineWidth * 2f + outlineWidth),
            new Vector2(layout.width, layout.height - outlineWidth * 2f + outlineWidth),
            new Vector2(layout.width, layout.height - outlineWidth * 2f + outlineWidth + outlineWidth),
            new Vector2(outlineWidth + outlineWidth * 1.5f, layout.height - outlineWidth * 2f + outlineWidth + outlineWidth),
            new Vector2(0, layout.height - outlineWidth * 2f + outlineWidth * 0.5f),
        };
        BuildFillShape(mgc, points, lineColor);

        points = new()
        {
            new Vector2(outlineWidth, 0),
            new Vector2(outlineWidth, layout.height * 0.6f + outlineWidth * 0.5f),
            new Vector2(outlineWidth + outlineWidth, layout.height * 0.6f + outlineWidth),
            new Vector2(layout.width, layout.height * 0.6f + outlineWidth),
            new Vector2(layout.width, 0),
        };
        BuildFillShape(mgc, points, fillColor);

        points = new()
        {
            new Vector2(outlineWidth, layout.height * 0.6f + outlineWidth + outlineWidth),
            new Vector2(outlineWidth, layout.height - outlineWidth * 2f),
            new Vector2(outlineWidth + outlineWidth * 2, layout.height - outlineWidth * 2f + outlineWidth),
            new Vector2(layout.width, layout.height - outlineWidth * 2f + outlineWidth),
            new Vector2(layout.width, layout.height * 0.6f + outlineWidth + outlineWidth),
        };
        BuildFillShape(mgc, points, fillColor);
    }
}