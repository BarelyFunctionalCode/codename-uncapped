using UnityEngine;
using Unity.Netcode;
using UnityEngine.Events;
using Steamworks;

public enum NotificationType
{
    ChatMessage,
    SystemMessage,
    KillFeed,
    ObjectiveUpdate
}

public struct NotificationData: INetworkSerializable
{

    public NotificationType type;
    public string title;
    public string content;
    public Color color;

    public NotificationData(NotificationType type, string title, string content, Color color = default)
    {
        this.type = type;
        this.title = title;
        this.content = content;
        this.color = color;   
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref type);
        serializer.SerializeValue(ref title);
        serializer.SerializeValue(ref content);
        serializer.SerializeValue(ref color);
    }
}

public class NotificationManager : NetworkBehaviour
{
    public static NotificationManager Instance { get; private set; } = null;
    public UnityEvent<NotificationData> newNotificationReceivedEvent = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [Rpc(SendTo.Server)]
    public void SendNotificationRpc(NotificationData data) => BroadcastNotificationRpc(data);

    [Rpc(SendTo.Everyone)]
    private void BroadcastNotificationRpc(NotificationData data) => newNotificationReceivedEvent.Invoke(data);



    [Rpc(SendTo.Server)]
    public void SendChatNotificationRpc(string message, RpcParams rpcParams = default)
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
        NotificationData notificationData = new NotificationData(NotificationType.ChatMessage, senderName, message, messageColor);
        SendNotificationRpc(notificationData);
    }
}