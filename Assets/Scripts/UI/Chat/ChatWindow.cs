using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;


// A single item in an ExpandableList, consisting of a label and an optional disabled overlay.
[UxmlElement(libraryPath = "Chat/ChatWindow")]
public partial class ChatWindow : VectorFillShape
{
    private static float outlineWidth = 15f;

    private VisualElement messagesContainer;
    private VisualElement textInputContainer;
    private ScrollView messagesScrollView;
    private TextField textInput;

    private ToastContainer passiveChatNotificationsContainer;

    private HUDController hud;

    private long chatAutoHideDelay = 5;

    private bool isInitialized = false;
    private bool isMenuActive = false;
    private bool isPointerInMessageList = false;


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
        };
        textInput.textEdition.hidePlaceholderOnFocus = true;
        textInput.textEdition.placeholder = "Type a message...";
        textInput.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
        textInputContainer.Add(textInput);


        messagesScrollView.RegisterCallback<PointerEnterEvent>(evt => isPointerInMessageList = true);
        messagesScrollView.RegisterCallback<PointerLeaveEvent>(evt => isPointerInMessageList = false);
        textInput.RegisterCallback<NavigationCancelEvent>(OnChatInputCancel);
        textInput.RegisterCallback<NavigationSubmitEvent>(OnChatInputSubmit, TrickleDown.TrickleDown);

        RegisterCallback<GeometryChangedEvent>(evt => {
            messagesContainer.style.height = new Length(layout.height * 0.6f + outlineWidth);
            textInputContainer.style.height = new Length(layout.height * 0.4f - outlineWidth * 2f);

            if (passiveChatNotificationsContainer == null)
            {
                passiveChatNotificationsContainer = (ToastContainer)UIManager.Spawn("UI/Toast/ToastContainer", parent ?? this);
                passiveChatNotificationsContainer.name = "passive-chat-notifications";
                for (int i = 0; i < styleSheets.count; i++)
                    passiveChatNotificationsContainer.styleSheets.Add(styleSheets[i]);
                passiveChatNotificationsContainer.Initialize(NotificationType.ChatMessage, 5f);
            }

            passiveChatNotificationsContainer.style.width = messagesContainer.resolvedStyle.width;
            passiveChatNotificationsContainer.style.height = messagesContainer.style.height;
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

    public void Initialize(HUDController hud)
    {
        if (isInitialized) return;

        NotificationManager.Instance.newNotificationReceivedEvent.AddListener(OnNewMessageReceived);

        EnableInClassList("active-menu", false);

        this.hud = hud;
        isInitialized = true;
    }

    public void Deinitialize()
    {
        if (!isInitialized) return;
        isInitialized = false;

        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.newNotificationReceivedEvent.RemoveListener(OnNewMessageReceived);
        }
        passiveChatNotificationsContainer.RemoveFromHierarchy();
        hud = null;
    }

    public bool ToggleMenu()
    {
        isMenuActive = !isMenuActive;
        EnableInClassList("active-menu", isMenuActive);
        pickingMode = isMenuActive ? PickingMode.Position : PickingMode.Ignore;
        passiveChatNotificationsContainer.EnableInClassList("active-menu", isMenuActive);
        if (isMenuActive)
        {
            BringToFront();
            schedule.Execute(() =>
            {
                textInput.Focus();
                messagesScrollView.verticalScroller.value = messagesScrollView.verticalScroller.highValue;
            }).ExecuteLater(100);
        }
        else
        {
            isPointerInMessageList = false;
            textInput.Blur();
        }
        return isMenuActive;
    }

    private void OnNewMessageReceived(NotificationData messageData)
    {
        if (messageData.type != NotificationType.ChatMessage) return;

        ChatMessage newMessage = (ChatMessage)UIManager.Spawn("UI/Chat/ChatMessage", messagesScrollView.contentContainer);
        newMessage.Initialize(messageData, Color.clear);
        if (!isPointerInMessageList)
        schedule.Execute(
            () => messagesScrollView.verticalScroller.value = messagesScrollView.verticalScroller.highValue
        ).StartingIn(100);
    }

    private void OnChatInputSubmit(NavigationSubmitEvent evt)
    {
        string message = textInput.value;
        if (!string.IsNullOrWhiteSpace(message))
        {
            NotificationManager.Instance.SendChatNotificationRpc(message);
            textInput.value = string.Empty;
            schedule.Execute(CheckToHideChat).ExecuteLater(chatAutoHideDelay * 1000);
        }
        evt.StopPropagation();
    }

    private void OnChatInputCancel(EventBase evt)
    {
        if (hud) hud.ToggleMenu(HUDMenu.Chat);
    }

    private void CheckToHideChat()
    {
        if (textInput.value != string.Empty) return;
        if (hud && style.display == DisplayStyle.Flex) hud.ToggleMenu(HUDMenu.Chat);
    }
}