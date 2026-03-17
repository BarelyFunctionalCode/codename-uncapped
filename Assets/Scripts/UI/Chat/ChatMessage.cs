using TMPro;
using UnityEngine;

public class ChatMessage : MonoBehaviour
{
    [SerializeField] private TMP_Text messageNameText;
    [SerializeField] private TMP_Text messageContentText;

    public void Initialize(NotificationData messageData)
    {
        messageNameText.text = messageData.title;
        messageNameText.color = messageData.color;
        messageContentText.text = messageData.content;
    }
}
