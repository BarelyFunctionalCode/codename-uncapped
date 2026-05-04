using System;
using UnityEngine.UIElements;


// A single item in an ExpandableList, consisting of a label and an optional disabled overlay.
[UxmlElement]
public partial class ExpandableListItem : CustomUIElementBase
{
    private Label ItemNameLabel => this.Q<Label>("ItemName");
    private VisualElement DisabledOverlay => this.Q<VisualElement>("DisabledOverlay");


    public void Initialize(Action<string> onItemSelected, string itemName, string itemValue, bool isEnabled = true)
    {
        ItemNameLabel.text = itemName;
        SetEnabled(isEnabled);
        RegisterCallback<ClickEvent>(_ => onItemSelected?.Invoke(itemValue));
    }
}