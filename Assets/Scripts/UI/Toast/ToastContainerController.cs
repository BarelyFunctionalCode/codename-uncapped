using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;


[UxmlElement]
public partial class Toast : CustomUIElementBase
{
    private VisualElement ToastElement => this.Q<VisualElement>("Toast");
    private Label TitleLabel => ToastElement.Q<Label>("Title");
    private Label MessageLabel => ToastElement.Q<Label>("Message");

    private float hideTime = 0f;
    private float hideTimer = 0f;


    public void Initialize(NotificationData data, float hideTime)
    {
        this.hideTime = hideTime;

        TitleLabel.text = data.title;
        if (string.IsNullOrEmpty(data.title)) TitleLabel.style.display = DisplayStyle.None;
        else TitleLabel.style.display = DisplayStyle.Flex;

        MessageLabel.text = data.content;
        // if (data.color != default)
        // {
        //     if (!string.IsNullOrEmpty(data.title)) TitleLabel.style.color = data.color;
        //     else MessageLabel.style.color = data.color;
        // }
    }

    public bool Update()
    {
        if (hideTimer >= 0) hideTimer += Time.deltaTime;
        if (ToastElement != null && hideTimer >= hideTime && ToastElement.parent.IndexOf(ToastElement) == 0)
        {
            hideTimer = -1f;
            ToastElement.style.opacity = 0f;
            ToastElement.style.marginTop = -(
                ToastElement.layout.height +
                ToastElement.resolvedStyle.marginBottom +
                ToastElement.parent.resolvedStyle.marginTop
            );
            ToastElement.schedule.Execute(() =>
            {
                ToastElement.RemoveFromHierarchy();
            }).ExecuteLater(500);
        }
        return ToastElement == null || ToastElement.parent == null;
    }
}


[UxmlElement]
public partial class ToastContainer : CustomUIElementBase
{
    private VisualElement ToastContainerElement => this.Q<VisualElement>("ToastContainer");
    private List<Toast> activeToasts = new();


    public void CreateToast(NotificationData data, float hideTime)
    {
        Toast newToast = (Toast)UIManager.Spawn("UI/Toast/Toast", ToastContainerElement);
        newToast.Initialize(data, hideTime);
        activeToasts.Add(newToast);
    }

    public void Update()
    {
        for (int i = activeToasts.Count - 1; i >= 0; i--)
        {
            if (activeToasts[i].Update()) activeToasts.RemoveAt(i);
        }
    }
}


[RequireComponent(typeof(UIDocument))]
public class ToastContainerController : MonoBehaviour
{
    private UIDocument uiDocument;
    private ToastContainer toastContainer;
    [SerializeField] private NotificationType type;
    [SerializeField] private float hideTime = 5f;


    void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        toastContainer = uiDocument.rootVisualElement.Q<ToastContainer>();
    }
    
    void Update()
    {
        toastContainer.Update();
    }

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

        toastContainer.CreateToast(data, hideTime);
    }
}
