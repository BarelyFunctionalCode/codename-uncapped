using TMPro;
using UnityEngine;

public class ChatMessage : MonoBehaviour
{
    [SerializeField] private TMP_Text messageNameText;
    [SerializeField] private TMP_Text messageContentText;

    public void Initialize(ChatMessageData messageData)
    {
        messageNameText.text = messageData.senderName;
        messageNameText.color = messageData.messageColor;
        messageContentText.text = messageData.messageContent;
    }
}
