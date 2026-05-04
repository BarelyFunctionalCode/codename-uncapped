using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;


public static class UIManager
{
    // Keeps a cached list of the UI elements being spawned for later use
    static Dictionary<string, VisualTreeAsset> uiElements = new();


    // Spawns a UI element based on the provided file path to the uxml in the Resources folder.
    public static VisualElement Spawn(string uiPath, VisualElement parent)
    {
        if (!uiElements.ContainsKey(uiPath))
        {
            uiElements[uiPath] = Resources.Load<VisualTreeAsset>(uiPath);
        }

        return Spawn(uiElements[uiPath], parent);
    }

    // Spawns a UI element based on the provided VisualTreeAsset reference.
    public static VisualElement Spawn(VisualTreeAsset uiAsset, VisualElement parent)
    {
        var elementInstance = uiAsset.Instantiate();
        var elementToAdd = elementInstance.Children().FirstOrDefault();
        parent.Add(elementToAdd);

        CustomUIElementBase customElement = elementToAdd.Q<CustomUIElementBase>();
        if (customElement != null) return customElement;

        return elementToAdd;
    }
}
