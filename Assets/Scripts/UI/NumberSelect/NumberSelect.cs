using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;


[UxmlElement(libraryPath = "BasicElements")]
public partial class NumberSelect : CustomUIElementBase
{
    public UnityEvent<int> OnValueChanged = new();

    private Label nameLabel;
    private TextField selectedNumberLabel;
    private Button decreaseButton;
    private Button increaseButton;

    private int currentValue = 0;
    private int minValue = 0;
    private int maxValue = 666;


    public void Initialize(string label, int defaultValue, UnityAction<int> onValueChangedCallback = null, int minValue = 0, int maxValue = 666)
    {
        nameLabel = this.Q<Label>("name-label");
        selectedNumberLabel = this.Q<TextField>("value-field");
        decreaseButton = this.Q<Button>("left-arrow");
        increaseButton = this.Q<Button>("right-arrow");

        if (onValueChangedCallback != null)
        {
            OnValueChanged.AddListener(onValueChangedCallback);
        }

        nameLabel.text = label;
        selectedNumberLabel.value = defaultValue.ToString();
        currentValue = defaultValue;
        this.minValue = minValue;
        this.maxValue = maxValue;

        decreaseButton.clicked += OnDecreaseButtonClicked;
        increaseButton.clicked += OnIncreaseButtonClicked;
        selectedNumberLabel.RegisterValueChangedCallback(evt =>
        {
            if (int.TryParse(evt.newValue, out int newValue))
            {
                currentValue = Mathf.Clamp(newValue, minValue, maxValue);
                selectedNumberLabel.value = currentValue.ToString();
            }
            else
            {
                selectedNumberLabel.value = currentValue.ToString();
            }

            OnValueChanged.Invoke(currentValue);
        });
    }

    private void OnDecreaseButtonClicked()
    {
        if (currentValue > minValue)
        {
            currentValue--;
            selectedNumberLabel.value = currentValue.ToString();
        }
    }

    private void OnIncreaseButtonClicked()
    {
        if (currentValue < maxValue)
        {
            currentValue++;
            selectedNumberLabel.value = currentValue.ToString();
        }
    }

    public void SetValue(int newValue)
    {
        currentValue = Mathf.Clamp(newValue, minValue, maxValue);
        selectedNumberLabel.value = currentValue.ToString();
    }
}