using UnityEngine.Events;
using UnityEngine.UIElements;


// A vertically expandable list of items, with a header that can be clicked to open/close the list.
[UxmlElement(libraryPath = "BasicElements/ExpandableList")]
public partial class ExpandableList : CustomUIElementBase
{
    public UnityEvent<string, string> OnListItemSelected = new();
    private VisualElement listHeader;
    private Label nameLabel;
    private VisualElement listOptionsContainer;

    float buttonHeight;
    bool isOpen = false;


    protected override void OnSpawned()
    {
        buttonHeight = resolvedStyle.height;
    }

    public void Initialize(string listName, UnityAction<string, string> onItemSelectedCallback = null)
    {
        listHeader = this.Q<VisualElement>("header");
        nameLabel = this.Q<Label>("list-name");
        listOptionsContainer = this.Q<VisualElement>("options-container");
        
        // Set list header label and register click event to open/close list.
        nameLabel.text = listName;
        RegisterCallback<BlurEvent>(_ => CloseList());
        listHeader.RegisterCallback<PointerDownEvent>(OnListHeaderClicked);

        if (onItemSelectedCallback != null) OnListItemSelected.AddListener(onItemSelectedCallback);
    }

    // Functions for opening/closing the list based on click and focus events.
    private void OnListHeaderClicked(PointerDownEvent evt)
    {
        if (evt.currentTarget != listHeader) return;

        if (isOpen) Blur();
        else OpenList();
    }
    private void OpenList()
    {
        isOpen = true;
        float targetHeight = listOptionsContainer.resolvedStyle.height;
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
        ExpandableListItem newItem = (ExpandableListItem)UIManager.Spawn("UI/ExpandableList/ExpandableListItem", listOptionsContainer);
        newItem.Initialize(OnItemSelected, itemName, itemValue, isEnabled);
    }
    private void OnItemSelected(string itemValue)
    {
        OnListItemSelected.Invoke(nameLabel.text, itemValue);
    }
}