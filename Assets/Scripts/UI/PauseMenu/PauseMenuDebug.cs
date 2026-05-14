using UnityEngine;
using UnityEngine.UIElements;
using System;

[UxmlElement(libraryPath = "PauseMenu/PauseMenuDebug")]
public partial class PauseMenuDebug : CustomUIElementBase
{
    private Toggle toggle;


    public void Initialize(string name, bool value, Action<bool> onValueChanged)
    {
        toggle = this.Q<Toggle>();

        toggle.value = value;
        toggle.label = name;

        toggle.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
    }
}