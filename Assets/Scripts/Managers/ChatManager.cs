using UnityEngine;
using Unity.Netcode;
using UnityEngine.Events;
using Steamworks;

public struct ChatMessageData
{
    public string senderName;
    public string messageContent;
    public Color messageColor;

    public ChatMessageData(string senderName, string messageContent, Color messageColor = default)
    {
        this.senderName = senderName;
        this.messageContent = messageContent;
        this.messageColor = messageColor;   
    }
}

public class ChatManager : NetworkBehaviour
{
    public static ChatManager Instance { get; private set; } = null;
    public UnityEvent<ChatMessageData> newMessageReceivedEvent = new();
    public UnityEvent<bool> chatInputToggledEvent = new();
    public bool isChatInputActive = false;


    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        Debug.Log("ChatManager Awake");

        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    [Rpc(SendTo.Server)]
    public void SendMessageRpc(string message, RpcParams rpcParams = default)
    {
        // TODO: Add some checks here, like message length, profanity filter, etc.
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        NetworkManager.Singleton.ConnectedClients[senderClientId].PlayerObject.TryGetComponent(out PlayerController playerController);
        string senderName = $"Fragger {senderClientId}";
        Color messageColor = playerController.GetTeamId() == 0 ? Color.blue : Color.red;
        if (GameManager.Instance.usingSteam)
        {
            senderName = new Friend(playerController.PlayerSteamId).Name;
        }
        BroadcastMessageRpc(senderName, message, messageColor);
    }

    [Rpc(SendTo.Everyone)]
    private void BroadcastMessageRpc(string name, string message, Color messageColor)
    {
        newMessageReceivedEvent.Invoke(new ChatMessageData(name, message, messageColor));
    }

    public void ToggleChatInput(bool isEnabled)
    {
        Debug.Log($"Toggling chat input: {(isEnabled ? "ON" : "OFF")}");
        isChatInputActive = isEnabled;
        chatInputToggledEvent.Invoke(isChatInputActive);
    }
}