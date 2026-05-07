using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatWindowOld : MonoBehaviour
{
    [SerializeField] private Transform messagesContainer;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private GameObject chatMessagePrefabObj;
    [SerializeField] private GameObject chatInputFieldObj;
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private bool isAlwaysActive = false;

    private bool isInitialized = false;
    private HUD hud;

    private float messageDisplayDuration = 10f;
    private float messageTimer = 0f;

    private Color messageContainerColor;

    private void Start()
    {
        gameObject.SetActive(false);
        chatInputFieldObj.SetActive(false);

        chatInputField.onSubmit.AddListener(OnChatInputSubmit);
        chatInputField.onDeselect.AddListener(OnChatInputCancel);

        messageContainerColor = chatInputFieldObj.GetComponent<Image>().color;

        if (isAlwaysActive)
        {
            NotificationManager.Instance.newNotificationReceivedEvent.AddListener(OnNewMessageReceived);
            gameObject.SetActive(true);
            chatInputFieldObj.SetActive(true);
            isInitialized = true;
        }
    }

    private void Update()
    {
        if (!gameObject.activeSelf || chatInputFieldObj.activeSelf || isAlwaysActive) return;
        messageTimer -= Time.deltaTime;
        if (messageTimer <= 0f)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        chatInputField.onSubmit.RemoveListener(OnChatInputSubmit);
        chatInputField.onDeselect.RemoveListener(OnChatInputCancel);
        if (isAlwaysActive && NotificationManager.Instance != null)
        {
            NotificationManager.Instance.newNotificationReceivedEvent.RemoveListener(OnNewMessageReceived);
        }
    }

    public void Initialize(HUD hud)
    {
        if (isInitialized) return;

        NotificationManager.Instance.newNotificationReceivedEvent.AddListener(OnNewMessageReceived);
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
        hud = null;
    }

    public bool ToggleMenu()
    {
        bool isActive = !chatInputFieldObj.activeSelf;
        chatInputFieldObj.SetActive(isActive);
        chatInputField.text = string.Empty;
        if (isActive)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            chatInputField.ActivateInputField();
        }
        else 
        {
            scrollRect.verticalNormalizedPosition = 0f;
            chatInputField.DeactivateInputField();
        }
        return isActive;
    }

    private void OnNewMessageReceived(NotificationData messageData)
    {
        if (messageData.type != NotificationType.ChatMessage) return;

        messageTimer = messageDisplayDuration;
        if (!isAlwaysActive && !gameObject.activeSelf) gameObject.SetActive(true);
        GameObject newMessageObj = Instantiate(chatMessagePrefabObj, messagesContainer);
        newMessageObj.GetComponent<ChatMessageOld>().Initialize(messageData, messageContainerColor);
    }

    private void OnChatInputSubmit(string _)
    {
        string message = chatInputField.text;
        if (!string.IsNullOrWhiteSpace(message))
        {
            NotificationManager.Instance.SendChatNotificationRpc(message);
            chatInputField.text = string.Empty;
            if (hud) hud.ToggleMenu(HUDMenu.Chat);
            else chatInputField.ActivateInputField();
        }
    }

    private void OnChatInputCancel(string _)
    {
        if (!gameObject.activeSelf || !chatInputFieldObj.activeSelf) return;
        if (hud) hud.ToggleMenu(HUDMenu.Chat);
    }
}
