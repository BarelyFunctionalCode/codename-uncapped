using UnityEngine.UIElements;


// Base class for all custom UI elements.
[UxmlElement(visibility = LibraryVisibility.Hidden)]
public partial class CustomUIElementBase : VisualElement
{
    public CustomUIElementBase()
    {
        RegisterCallbackOnce<GeometryChangedEvent>(_ => OnSpawned());
    }

    // Virtual function called once the element has been added to the hierarchy and has valid geometry.
    // Used for anything that requires reading initial layout values of the element being spawned.
    virtual protected void OnSpawned() { }
}