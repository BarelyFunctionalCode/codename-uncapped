using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[UxmlElement(libraryPath = "PauseMenu/PauseMenuControl")]
public partial class PauseMenuControl : CustomUIElementBase
{
    private Label nameLabel;
    private Label valueLabel;
    private Button remapButton;


    public void Initialize(InputAction action)
    {
        nameLabel = this.Q<Label>("ControlName");
        valueLabel = this.Q<Label>("ControlValue");
        remapButton = this.Q<Button>("RemapButton");

        nameLabel.text = action.name;
        valueLabel.text = action.bindings[0].ToDisplayString();

        remapButton.clicked += () => OnRemapButtonClicked(action);
    }

    private void OnRemapButtonClicked(InputAction action)
    {
        valueLabel.text = "Press a key...";

        action.PerformInteractiveRebinding()
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation =>
            {
                operation.Dispose();
                valueLabel.text = action.bindings[0].ToDisplayString();
            })
            .Start();
    }
}