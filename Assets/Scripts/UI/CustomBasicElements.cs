using UnityEngine;
using UnityEngine.UIElements;



[UxmlElement(libraryPath = "BasicElements/RetroNumber")]
public partial class RetroNumber : VisualElement
{
    private string _text = "0";
    [UxmlAttribute] public string Text
    {
        get => _text;
        set
        {
            _text = value;
            OnTextChanged(_text);
        }
    }

    private Label TextLabel;
    private Label BackgroundLabel;

    private void OnTextChanged(string newText)
    {
        if (TextLabel != null) TextLabel.text = newText;
        if (BackgroundLabel != null)
        {
            // For each charcter in the text, put a "~" if it's a number, or the same character if it's not
            string backgroundText = "";
            foreach (char c in newText)
            {
                if (char.IsDigit(c)) backgroundText += "~";
                else backgroundText += c;
            }
            BackgroundLabel.text = backgroundText;
        }
    }

    public RetroNumber()
    {
        TextLabel = new Label("")
        {
            name = "Text"
        };
        Add(TextLabel);

        BackgroundLabel = new Label("")
        {
            name = "BackgroundLabel"
        };
        Add(BackgroundLabel);
        BackgroundLabel.SendToBack();

        TextLabel.RegisterCallback<GeometryChangedEvent>(evt =>
        {
            BackgroundLabel.style.height = TextLabel.resolvedStyle.height;
            BackgroundLabel.style.width = TextLabel.resolvedStyle.width;
        });
    }
}
