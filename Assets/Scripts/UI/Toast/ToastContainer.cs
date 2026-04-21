using UnityEngine;

public class ToastContainer : MonoBehaviour
{
    [SerializeField] private NotificationType type;
    [SerializeField] private float notificationDuration = 5f;
    [SerializeField] private GameObject toastNotificationPrefabObj;
    [SerializeField] private Transform toastNotificationContainerObj;

    public void Initialize()
    {
        NotificationManager.Instance.newNotificationReceivedEvent.AddListener(OnNewToastNotification);
    }

    public void Deinitialize()
    {
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.newNotificationReceivedEvent.RemoveListener(OnNewToastNotification);
        }
    }

    private void OnNewToastNotification(NotificationData data)
    {
        if (data.type != type) return;
        ToastNotification toast = Instantiate(toastNotificationPrefabObj, toastNotificationContainerObj).GetComponent<ToastNotification>();
        toast.Initialize(data, notificationDuration);
    }
}
