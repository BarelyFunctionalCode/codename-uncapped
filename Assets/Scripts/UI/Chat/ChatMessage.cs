using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatMessage : MonoBehaviour
{
    [SerializeField] private TMP_Text messageNameText;
    [SerializeField] private TMP_Text messageContentText;
    [SerializeField] private Image messageContainerImage;

    public void Initialize(NotificationData messageData, Color containerColor)
    {
        messageContainerImage.color = containerColor;
        messageNameText.text = messageData.title;
        messageNameText.color = messageData.color;
        messageContentText.text = messageData.content;
        Color contentColor = messageContainerImage.color;
        contentColor = Color.Lerp(contentColor, Color.black, 0.8f);
        messageContentText.color = contentColor;
    }
}
