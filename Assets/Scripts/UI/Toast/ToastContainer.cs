using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;


public class ToastController
{
    private VisualElement toast;
    private Label titleLabel;
    private Label messageLabel;

    private float hideTime = 0f;
    private float hideTimer = 0f;


    public void SetVisualElement(VisualElement visualElement)
    {
        toast = visualElement.Q<VisualElement>("Toast");
        titleLabel = toast.Q<Label>("Title");
        messageLabel = toast.Q<Label>("Message");
    }
    
    public void SetData(NotificationData data, float hideTime)
    {
        this.hideTime = hideTime;

        titleLabel.text = data.title;
        if (string.IsNullOrEmpty(data.title)) titleLabel.style.display = DisplayStyle.None;
        else titleLabel.style.display = DisplayStyle.Flex;

        messageLabel.text = data.content;
        // if (data.color != default)
        // {
        //     if (!string.IsNullOrEmpty(data.title)) titleLabel.style.color = data.color;
        //     else messageLabel.style.color = data.color;
        // }
    }

    public bool Update()
    {
        if (hideTimer >= 0) hideTimer += Time.deltaTime;
        if (toast != null && hideTimer >= hideTime && toast.parent.IndexOf(toast) == 0)
        {
            hideTimer = -1f;
            toast.style.opacity = 0f;
            toast.style.marginTop = -(
                toast.layout.height +
                toast.resolvedStyle.marginBottom +
                toast.parent.resolvedStyle.marginTop
            );
            toast.schedule.Execute(() =>
            {
                toast.RemoveFromHierarchy();
                toast = null;
            }).ExecuteLater(500);
        }
        return toast == null || toast.parent == null;
    }
}


public class ToastContainerController
{
    private VisualTreeAsset toastTemplate;
    private VisualElement toastContainer;

    private List<ToastController> activeToasts = new();


    public void InitializeContainer(VisualElement root, VisualTreeAsset toastTemplate)
    {
        this.toastTemplate = toastTemplate;
        toastContainer = root.Q<VisualElement>("ToastContainer");
    }
    
    public void CreateToast(NotificationData data, float hideTime)
    {
        var newToast = toastTemplate.Instantiate();
        var newToastLogic = new ToastController();

        newToastLogic.SetVisualElement(newToast);
        newToastLogic.SetData(data, hideTime);

        toastContainer.Add(newToast);
        activeToasts.Add(newToastLogic);
    }

    public void Update()
    {
        for (int i = activeToasts.Count - 1; i >= 0; i--)
        {
            if (activeToasts[i].Update())
            {
                activeToasts.RemoveAt(i);
            }
        }
    }
}


[RequireComponent(typeof(UIDocument))]
public class ToastContainer : MonoBehaviour
{
    [SerializeField] private NotificationType type;
    [SerializeField] private float hideTime = 5f;
    [SerializeField] private VisualTreeAsset toastTemplate;

    private ToastContainerController toastContainerController;


    void Awake()
    {
        var uiDocument = GetComponent<UIDocument>();
    
        toastContainerController = new ToastContainerController();
        toastContainerController.InitializeContainer(uiDocument.rootVisualElement, toastTemplate);
    }

    void Update()
    {
        toastContainerController.Update();
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

        toastContainerController.CreateToast(data, hideTime);
    }
}
