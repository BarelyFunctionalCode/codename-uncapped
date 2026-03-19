using UnityEngine;

public class ToastContainer : MonoBehaviour
{
    [SerializeField] private NotificationType type;
    [SerializeField] private GameObject toastNotificationPrefabObj;
    [SerializeField] private Transform toastNotificationContainerObj;

    private void Awake()
    {
        NotificationManager.Instance.newNotificationReceivedEvent.AddListener(OnNewToastNotification);
    }

    private void OnDestroy()
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
        toast.Initialize(data);
    }
}
