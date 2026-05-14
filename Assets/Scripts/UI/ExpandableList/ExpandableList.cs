using UnityEngine.Events;
using UnityEngine.UIElements;


// A vertically expandable list of items, with a header that can be clicked to open/close the list.
[UxmlElement(libraryPath = "BasicElements/ExpandableList")]
public partial class ExpandableList : CustomUIElementBase
{
    public UnityEvent<string, string> OnListItemSelected = new();
    private VisualElement ListHeader => this.Q<VisualElement>("Header");
    private Label NameLabel => this.Q<Label>("ListName");
    private VisualElement ListOptionsContainer => this.Q<VisualElement>("Options");

    float buttonHeight;
    bool isOpen = false;


    protected override void OnSpawned()
    {
        buttonHeight = resolvedStyle.height;
    }

    public void Initialize(string listName, UnityAction<string, string> onItemSelectedCallback = null)
    {
        // Set list header label and register click event to open/close list.
        NameLabel.text = listName;
        RegisterCallback<BlurEvent>(_ => CloseList());
        ListHeader.RegisterCallback<PointerDownEvent>(OnListHeaderClicked);

        if (onItemSelectedCallback != null) OnListItemSelected.AddListener(onItemSelectedCallback);
    }

    // Functions for opening/closing the list based on click and focus events.
    private void OnListHeaderClicked(PointerDownEvent evt)
    {
        if (evt.currentTarget != ListHeader) return;

        if (isOpen) Blur();
        else OpenList();
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

    // Adds an item to the list with the provided name, value, and enabled state.
    // Triggers UnityEvent OnListItemSelected when clicked, passing the list name and item value as parameters.
    public void AddListItem(string itemName, string itemValue, bool isEnabled = true)
    {
        ExpandableListItem newItem = (ExpandableListItem)UIManager.Spawn("UI/ExpandableList/ExpandableListItem", ListOptionsContainer);
        newItem.Initialize(OnItemSelected, itemName, itemValue, isEnabled);
    }
    private void OnItemSelected(string itemValue)
    {
        OnListItemSelected.Invoke(NameLabel.text, itemValue);
    }
}