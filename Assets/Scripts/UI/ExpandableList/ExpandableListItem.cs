using System;
using UnityEngine.UIElements;


// A single item in an ExpandableList, consisting of a label and an optional disabled overlay.
[UxmlElement(libraryPath = "BasicElements")]
public partial class ExpandableListItem : CustomUIElementBase
{
    private Label itemNameLabel;
    private VisualElement disabledOverlay;

    public string ItemName => itemNameLabel.text;
    public string ItemValue { get; private set; }


    public void Initialize(Action<string> onItemSelected, string itemName, string itemValue, bool isEnabled = true)
    {
        ItemValue = itemValue;

        itemNameLabel = this.Q<Label>("item-name");
        disabledOverlay = this.Q<VisualElement>("disabled-overlay");

        itemNameLabel.text = itemName;
        SetEnabled(isEnabled);
        RegisterCallback<ClickEvent>(_ => onItemSelected?.Invoke(itemValue));
    }
}