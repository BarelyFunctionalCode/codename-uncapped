using UnityEngine.UIElements;


[UxmlElement(libraryPath = "Toast")]
public partial class Toast : CustomUIElementBase
{
    private VisualElement toastElement;
    private Label titleLabel;
    private Label messageLabel;

    private float hideTime = 0f;
    private float hideTimer = 0f;


    public void Initialize(NotificationData data, float hideTime)
    {
        toastElement = this.Q<VisualElement>("Toast");
        titleLabel = toastElement.Q<Label>("Title");
        messageLabel = toastElement.Q<Label>("Message");

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

    public bool Update(float deltaTime)
    {
        if (hideTimer >= 0) hideTimer += deltaTime;
        if (toastElement != null && toastElement.parent != null && hideTimer >= hideTime && toastElement.parent.IndexOf(toastElement) == 0)
        {
            hideTimer = -1f;
            toastElement.style.opacity = 0f;
            toastElement.style.marginTop = -(
                toastElement.layout.height +
                toastElement.resolvedStyle.marginBottom +
                toastElement.parent.resolvedStyle.marginTop
            );
            toastElement.schedule.Execute(() =>
            {
                toastElement.RemoveFromHierarchy();
            }).ExecuteLater(500);
        }
        return toastElement == null || toastElement.parent == null;
    }
}