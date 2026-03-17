using TMPro;
using UnityEngine;

public class ChatWindow : MonoBehaviour
{
    [SerializeField] private GameObject chatContainerObj;
    [SerializeField] private Transform messagesContainer;
    [SerializeField] private GameObject chatMessagePrefabObj;
    [SerializeField] private GameObject chatInputFieldObj;
    [SerializeField] private TMP_InputField chatInputField;

    private float messageDisplayDuration = 10f;
    private float messageTimer = 0f;

    private void Awake()
    {
        Debug.Log("ChatWindow Awake");
        ChatManager.Instance.newMessageReceivedEvent.AddListener(OnNewMessageReceived);
        ChatManager.Instance.chatInputToggledEvent.AddListener(OnChatInputToggled);

        chatContainerObj.SetActive(false);
    }

    private void Update()
    {
        if (!chatContainerObj.activeSelf || ChatManager.Instance.isChatInputActive) return;
        messageTimer -= Time.deltaTime;
        if (messageTimer <= 0f)
        {
            chatContainerObj.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (ChatManager.Instance != null)
        {
            ChatManager.Instance.newMessageReceivedEvent.RemoveListener(OnNewMessageReceived);
            ChatManager.Instance.chatInputToggledEvent.RemoveListener(OnChatInputToggled);
        }
    }

    private void OnNewMessageReceived(ChatMessageData messageData)
    {
        messageTimer = messageDisplayDuration;
        GameObject newMessageObj = Instantiate(chatMessagePrefabObj, messagesContainer);
        newMessageObj.GetComponent<ChatMessage>().Initialize(messageData);
    }

    private void OnChatInputToggled(bool isActive)
    {
        chatInputFieldObj.SetActive(isActive);
        if (isActive)
        {
            if (!chatContainerObj.activeSelf) chatContainerObj.SetActive(true);
            chatInputField.ActivateInputField();
        }
    }

    public void OnChatInputSubmit()
    {
        string message = chatInputField.text;
        if (!string.IsNullOrWhiteSpace(message))
        {
            ChatManager.Instance.SendMessageRpc(message);
            chatInputField.text = string.Empty;
            ChatManager.Instance.ToggleChatInput(false);
        }
    }
}
