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

        BuildFillShape(mgc, points, resolvedColors.GetValueOrDefault(s_VectorFillColor.name, Color.clear));
        BuildLineShape(mgc, points.GetRange(0, 7), resolvedColors.GetValueOrDefault(s_VectorLineColor.name, Color.clear), 5, LineJoin.Bevel, LineCap.Butt);
    }
}

[UxmlElement]
public partial class HUDRightSide : VectorFillShape
{
    // Used to generate the visual content for this element
    protected override void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
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

        BuildFillShape(mgc, points, resolvedColors.GetValueOrDefault(s_VectorFillColor.name, Color.clear));
        BuildLineShape(mgc, points.GetRange(0, 7), resolvedColors.GetValueOrDefault(s_VectorLineColor.name, Color.clear), 5, LineJoin.Bevel, LineCap.Butt);
    }
}