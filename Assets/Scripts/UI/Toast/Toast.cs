using UnityEngine.UIElements;


[UxmlElement(libraryPath = "Toast/Toast")]
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

    public bool Update(float deltaTime)
    {
        if (hideTimer >= 0) hideTimer += deltaTime;
        if (ToastElement != null && ToastElement.parent != null && hideTimer >= hideTime && ToastElement.parent.IndexOf(ToastElement) == 0)
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