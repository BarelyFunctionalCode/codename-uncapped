using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;


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


[UxmlElement(visibility = LibraryVisibility.Hidden)]
public partial class CustomUIVectorElementBase : VisualElement, ITransitionAnimations
{
    // Collections to manage active animations and resolved colors for custom styles
    private Dictionary<string, int> animtationTransitionTimes = new();
    private Dictionary<string, ValueAnimation<Color>> animations = new();
    protected Dictionary<string, Color> resolvedColors = new();


    public CustomUIVectorElementBase()
    {
        generateVisualContent += BaseOnGenerateVisualContent;

        // Register event to resolve custom styles
        RegisterCallback<CustomStyleResolvedEvent>(BaseCustomStylesResolved);
    }

    private void BaseCustomStylesResolved(CustomStyleResolvedEvent evt)
    {
        // Resolve and animate Custom Styles
        CustomStylesResolved(evt);
    }
    protected virtual void CustomStylesResolved(CustomStyleResolvedEvent evt) { }

    protected void ResolveCustomStyleColor(CustomStyleResolvedEvent evt, CustomStyleProperty<Color> styleProperty, CustomStyleProperty<int> transitionTimeProperty = default)
    {
        // Check if there's a transition time defined for this element's custom style
        if (transitionTimeProperty != default && evt.customStyle.TryGetValue(transitionTimeProperty, out int newTransitionTime))
        {
            string key = styleProperty.name.Split("-transition-time")[0];
            animtationTransitionTimes[key] = newTransitionTime;
        }
        // Resolve the new color value for the custom style
        if (evt.customStyle.TryGetValue(styleProperty, out Color newVectorFillColor))
        {
            string key = styleProperty.name;

            // Check if there's a transition time defined for this custom style and animate if necessary
            int transitionTime = animtationTransitionTimes.ContainsKey(key) ? animtationTransitionTimes[key] : 0;
            if (transitionTime == 0)
            {
                resolvedColors[key] = newVectorFillColor;
                MarkDirtyRepaint();
                return;
            }

            // Stop any existing animation and start a new one from the current color to the new color
            if (animations.TryGetValue(key, out ValueAnimation<Color> existingAnimation))
            {
                if (existingAnimation.isRunning == true) existingAnimation.Stop();
            }
            Color currentColor = resolvedColors.ContainsKey(key) ? resolvedColors[key] : Color.clear;
            animations[key] = ((ITransitionAnimations)this).Start(
                currentColor,
                newVectorFillColor,
                transitionTime,
                (_, newColor) =>
                {
                    resolvedColors[key] = newColor;
                    MarkDirtyRepaint();
                }
            );
        }
    }

    private void BaseOnGenerateVisualContent(MeshGenerationContext mgc)
    {
        // Call the method to generate additional visual content defined in derived classes
        OnGenerateVisualContent(mgc);
    }
    protected virtual void OnGenerateVisualContent(MeshGenerationContext mgc) { }

    // Helper function to build a filled shape based on a list of points and a fill color
    public void BuildFillShape(MeshGenerationContext mgc, List<Vector2> points, Color fillColor)
    {
        if (points.Count < 3) return;

        var painter2D = mgc.painter2D;
        painter2D.fillColor = fillColor;
        painter2D.BeginPath();
        painter2D.MoveTo(points[0]);
        for (int i = 1; i < points.Count; i++) painter2D.LineTo(points[i]);
        painter2D.ClosePath();
        painter2D.Fill();
    }

    public void BuildFillShape(MeshGenerationContext mgc, List<Vector2> points, FillGradient fillGradient)
    {
        if (points.Count < 3) return;

        var painter2D = mgc.painter2D;
        painter2D.fillGradient = fillGradient;
        painter2D.BeginPath();
        painter2D.MoveTo(points[0]);
        for (int i = 1; i < points.Count; i++) painter2D.LineTo(points[i]);
        painter2D.ClosePath();
        painter2D.Fill();
    }

    public void BuildLineShape(MeshGenerationContext mgc, List<Vector2> points, Color lineColor, float lineWidth, LineJoin lineJoin = LineJoin.Miter, LineCap lineCap = LineCap.Round)
    {
        if (points.Count < 2) return;

        var painter2D = mgc.painter2D;
        painter2D.strokeColor = lineColor;
        painter2D.lineWidth = lineWidth;
        painter2D.lineJoin = lineJoin;
        painter2D.lineCap = lineCap;
        painter2D.BeginPath();
        painter2D.MoveTo(points[0]);
        for (int i = 1; i < points.Count; i++) painter2D.LineTo(points[i]);
        painter2D.Stroke();
    }
}