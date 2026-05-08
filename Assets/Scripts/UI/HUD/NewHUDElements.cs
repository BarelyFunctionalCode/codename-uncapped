using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;


[UxmlElement]
public partial class VectorFillShape : CustomUIVectorElementBase
{
    // Vector Fill Color
    static protected CustomStyleProperty<Color> s_VectorFillColor = new("--vector-fill-color");
    // Animation transition time for vector fill color changes
    static private CustomStyleProperty<int> s_VectorFillColorTransitionTime = new("--vector-fill-color-transition-time");

    // Vector Line Color
    static protected CustomStyleProperty<Color> s_VectorLineColor = new("--vector-line-color");


    // Resolves custom styles to resolved styles based on style and animation properties defined for this element
    protected override void CustomStylesResolved(CustomStyleResolvedEvent evt)
    {
        // Vector Fill Color
        ResolveCustomStyleColor(evt, s_VectorFillColor, s_VectorFillColorTransitionTime);

        // Vector Line Color
        ResolveCustomStyleColor(evt, s_VectorLineColor);
    }
}

[UxmlElement]
public partial class HUDLeftSide : VectorFillShape
{
    // Used to generate the visual content for this element
    protected override void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        Color fillColor = resolvedColors.GetValueOrDefault(s_VectorFillColor.name, Color.clear);
        Color lineColor = resolvedColors.GetValueOrDefault(s_VectorLineColor.name, Color.clear);
        var gradient = new Gradient();

        // Blend color from red at 0% to blue at 100%
        var colors = new GradientColorKey[3];
        colors[0] = new GradientColorKey(fillColor, 0.0f);
        colors[2] = new GradientColorKey(fillColor, 0.5f);
        colors[1] = new GradientColorKey(Color.white, 1.0f);

        // Blend alpha from opaque at 0% to transparent at 100%
        var alphas = new GradientAlphaKey[2];
        alphas[0] = new GradientAlphaKey(fillColor.a, 0.0f);
        alphas[1] = new GradientAlphaKey(0.0f, 0.5f);

        gradient.SetKeys(colors, alphas);

        var fillGradient = FillGradient.MakeLinearGradient(
            gradient,
            new Vector2(50, layout.height - 220),
            new Vector2(layout.width, layout.height),
            AddressMode.Clamp
        );

        List<Vector2> points = new()
        {
            Vector2.zero,
            new Vector2(30, 30),
            new Vector2(30, layout.height - 150),
            new Vector2(30 + layout.width * 0.1f, layout.height - 150),
            new Vector2(30 + layout.width * 0.2f, layout.height - 200),
            new Vector2(layout.width, layout.height - 200),
            new Vector2(layout.width, layout.height - 180),
            new Vector2(layout.width - 20, layout.height - 150),
            new Vector2(layout.width - 20, layout.height - 100),
            new Vector2(layout.width - 80, layout.height),
            new Vector2(0, layout.height),
        };

        BuildFillShape(mgc, points, fillGradient);
        BuildLineShape(mgc, points.GetRange(0, 7), lineColor, 5, LineJoin.Bevel, LineCap.Butt);
    }
}

[UxmlElement]
public partial class HUDRightSide : VectorFillShape
{
    // Used to generate the visual content for this element
    protected override void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        Color fillColor = resolvedColors.GetValueOrDefault(s_VectorFillColor.name, Color.clear);
        Color lineColor = resolvedColors.GetValueOrDefault(s_VectorLineColor.name, Color.clear);
        var gradient = new Gradient();

        // Blend color from red at 0% to blue at 100%
        var colors = new GradientColorKey[3];
        colors[0] = new GradientColorKey(fillColor, 0.0f);
        colors[2] = new GradientColorKey(fillColor, 0.5f);
        colors[1] = new GradientColorKey(Color.white, 1.0f);

        // Blend alpha from opaque at 0% to transparent at 100%
        var alphas = new GradientAlphaKey[2];
        alphas[0] = new GradientAlphaKey(fillColor.a, 0.0f);
        alphas[1] = new GradientAlphaKey(0.0f, 0.5f);

        gradient.SetKeys(colors, alphas);

        var fillGradient = FillGradient.MakeLinearGradient(
            gradient,
            new Vector2(layout.width - 50, layout.height - 220),
            new Vector2(0, layout.height),
            AddressMode.Clamp
        );

        List<Vector2> points = new()
        {
            new Vector2(layout.width, 0),
            new Vector2(layout.width - 30, 30),
            new Vector2(layout.width - 30, layout.height - 150),
            new Vector2(layout.width - 30 - layout.width * 0.1f, layout.height - 150),
            new Vector2(layout.width - 30 - layout.width * 0.2f, layout.height - 200),
            new Vector2(0, layout.height - 200),
            new Vector2(0, layout.height - 180),
            new Vector2(20, layout.height - 150),
            new Vector2(20, layout.height - 100),
            new Vector2(80, layout.height),
            new Vector2(layout.width, layout.height),
        };

        BuildFillShape(mgc, points, fillGradient);
        BuildLineShape(mgc, points.GetRange(0, 7), lineColor, 5, LineJoin.Bevel, LineCap.Butt);
    }
}