using UnityEngine;
using UnityEngine.UIElements;
using System;


[UxmlElement(libraryPath = "PauseMenu")]
public partial class PauseMenuOptionCategory : CustomUIElementBase
{
    private Label categoryLabel;
    private VisualElement categoryContainer;

    public override VisualElement contentContainer => this.Q<VisualElement>("CategoryContainer") ?? base.contentContainer;


    public PauseMenuOptionCategory()
    {
        categoryLabel = new Label(){
            name = "CategoryLabel",
            text = "Option Category"
        };
        Add(categoryLabel);

        categoryContainer = new VisualElement(){
            name = "CategoryContainer"
        };
        Add(categoryContainer);
    }

    public void Initialize(string name)
    {
        categoryLabel.text = name;
    }
}


[UxmlElement(libraryPath = "PauseMenu")]
public partial class PauseMenuOptionSlider : CustomUIElementBase
{
    private Slider slider;


    public void Initialize(string name, float value, Action<float> onValueChanged, float minValue = -1f, float maxValue = -1f)
    {
        slider = this.Q<Slider>();

        if (minValue != -1f) slider.lowValue = minValue;
        if (maxValue != -1f) slider.highValue = maxValue;

        slider.value = value;
        slider.label = name;

        slider.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
    }
}


[UxmlElement(libraryPath = "PauseMenu")]
public partial class PauseMenuOptionToggle : CustomUIElementBase
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