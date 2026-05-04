using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

[UxmlElement]
public partial class ExpandableList : VisualElement
{
    public UnityEvent<string, string> OnListItemSelected = new();
    private VisualElement ListHeader => this.Q<VisualElement>("Header");
    private Label NameLabel => this.Q<Label>("ListName");
    private VisualElement ListOptionsContainer => this.Q<VisualElement>("Options");

    private VisualTreeAsset listItemTemplate;

    float buttonHeight;
    bool isOpen = false;


    public ExpandableList() { }

    public void Init(string listName, UnityAction<string, string> onItemSelectedCallback = null)
    {
        NameLabel.text = listName;
        // RegisterCallback<FocusEvent>(OnListFocus);
        RegisterCallback<BlurEvent>(_ => CloseList());
        ListHeader.RegisterCallback<ClickEvent>(OnListHeaderClicked);

        if (onItemSelectedCallback != null)
        {
            OnListItemSelected.AddListener(onItemSelectedCallback);
        }

        RegisterCallbackOnce<GeometryChangedEvent>(_ =>
        {
            buttonHeight = resolvedStyle.height;
        });

        listItemTemplate = Resources.Load<VisualTreeAsset>("UI/ExpandableList/ExpandableListItem");
    }

    private void OpenList()
    {
        isOpen = true;
        float targetHeight = ListOptionsContainer.resolvedStyle.height;
        style.height = targetHeight + buttonHeight;
    }

    private void CloseList()
    {
        isOpen = false;
        style.height = buttonHeight;
    }

    private void OnListHeaderClicked(ClickEvent evt)
    {
        if (evt.currentTarget != ListHeader) return;
        if (evt.clickCount > 1) return; // Ignore double clicks

        if (isOpen) Blur();
        else OpenList();
    }

    private void OnItemSelected(string itemValue)
    {
        OnListItemSelected.Invoke(NameLabel.text, itemValue);
    }

    public void AddListItem(string itemName, string itemValue, bool isEnabled = true)
    {
        VisualElement newItem = listItemTemplate.Instantiate();
        newItem.Q<Label>("ItemName").text = itemName;

        newItem.SetEnabled(isEnabled);
        ListOptionsContainer.Add(newItem);

        newItem.RegisterCallback<ClickEvent>(_ => OnItemSelected(itemValue));
    }
}