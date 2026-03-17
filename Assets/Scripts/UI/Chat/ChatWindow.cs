using TMPro;
using UnityEngine;

public class ChatWindow : MonoBehaviour
{
    [SerializeField] private Transform messagesContainer;
    [SerializeField] private GameObject chatMessagePrefabObj;
    [SerializeField] private GameObject chatInputFieldObj;
    [SerializeField] private TMP_InputField chatInputField;

    private bool isInitialized = false;
    private HUD hud;

    private float messageDisplayDuration = 10f;
    private float messageTimer = 0f;

    private void Awake()
    {
        NotificationManager.Instance.newNotificationReceivedEvent.AddListener(OnNewMessageReceived);

        gameObject.SetActive(false);
        chatInputFieldObj.SetActive(false);

        chatInputField.onSubmit.AddListener(OnChatInputSubmit);
        chatInputField.onDeselect.AddListener(OnChatInputCancel);
    }

    private void Update()
    {
        if (!gameObject.activeSelf || chatInputFieldObj.activeSelf) return;
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
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.newNotificationReceivedEvent.RemoveListener(OnNewMessageReceived);
        }
    }

    public void Initialize(HUD hud)
    {
        if (isInitialized) return;

        this.hud = hud;
        isInitialized = true;
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
            chatInputField.DeactivateInputField();
        }
        return isActive;
    }

    private void OnNewMessageReceived(NotificationData messageData)
    {
        if (messageData.type != NotificationType.ChatMessage) return;
        
        messageTimer = messageDisplayDuration;
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        GameObject newMessageObj = Instantiate(chatMessagePrefabObj, messagesContainer);
        newMessageObj.GetComponent<ChatMessage>().Initialize(messageData);
    }

    private void OnChatInputSubmit(string _)
    {
        string message = chatInputField.text;
        if (!string.IsNullOrWhiteSpace(message))
        {
            NotificationManager.Instance.SendChatNotificationRpc(message);
            chatInputField.text = string.Empty;
            hud.ToggleMenu(HUDMenu.Chat);
        }
    }

    private void OnChatInputCancel(string _)
    {
        if (!gameObject.activeSelf || !chatInputFieldObj.activeSelf) return;
        hud.ToggleMenu(HUDMenu.Chat);
    }
}
