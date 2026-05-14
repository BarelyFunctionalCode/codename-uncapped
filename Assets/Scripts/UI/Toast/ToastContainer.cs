using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;


[UxmlElement(libraryPath = "Toast/ToastContainer")]
public partial class ToastContainer : CustomUIElementBase
{
    private List<Toast> activeToasts = new();
    private NotificationType type;
    private float hideTime;
    private int maxToasts;

    private bool isActive = false;

    private float updateTime;

    public void Initialize(NotificationType type, float hideTime, int maxToasts = 5)
    {
        this.type = type;
        this.hideTime = hideTime;
        this.maxToasts = maxToasts;
        NotificationManager.Instance.newNotificationReceivedEvent.AddListener(OnNewToastNotification);
        isActive = true;
        Update();
    }

    public void Deinitialize()
    {
        isActive = false;
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.newNotificationReceivedEvent.RemoveListener(OnNewToastNotification);
        }
    }

    private void OnNewToastNotification(NotificationData data)
    {
        if (data.type != type) return;

        CreateToast(data, hideTime);
    }

    public void CreateToast(NotificationData data, float hideTime)
    {
        Toast newToast = (Toast)UIManager.Spawn("UI/Toast/Toast", this);
        newToast.Initialize(data, hideTime);
        activeToasts.Add(newToast);
    }

    public void Update()
    {
        if (!isActive) return;
        if (activeToasts.Count > maxToasts) activeToasts[0].RemoveFromHierarchy();
        for (int i = activeToasts.Count - 1; i >= 0; i--)
        {
            if (activeToasts[i].Update(Time.time - updateTime)) activeToasts.RemoveAt(i);
        }

        updateTime = Time.time;
        schedule.Execute(Update).ExecuteLater(20);
    }
}