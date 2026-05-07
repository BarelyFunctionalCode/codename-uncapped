using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;


[UxmlElement]
public partial class VectorTesting : VisualElement
{
    static CustomStyleProperty<Color> s_VectorFillColor = new CustomStyleProperty<Color>("--vector-fill-color");
    public Color vectorFillColor;


    public VectorTesting()
    {
        generateVisualContent += OnGenerateVisualContent;
        RegisterCallback<CustomStyleResolvedEvent>(evt => CustomStylesResolved(evt));
    }

    static void CustomStylesResolved(CustomStyleResolvedEvent evt)
    {
        VectorTesting element = (VectorTesting)evt.currentTarget;
        element.UpdateCustomStyles();
    }

    void UpdateCustomStyles()
    {
        if (customStyle.TryGetValue(s_VectorFillColor, out vectorFillColor)) MarkDirtyRepaint();
    }

    void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        List<Vector2> points = new()
        {
            Vector2.zero,
            new Vector2(30, 30),
            new Vector2(30, layout.height - 150),
            new Vector2(80, layout.height - 150),
            new Vector2(120, layout.height - 200),
            new Vector2(250, layout.height - 200),
            new Vector2(250, layout.height - 180),
            new Vector2(230, layout.height - 150),
            new Vector2(230, layout.height - 100),
            new Vector2(180, layout.height),
            new Vector2(0, layout.height),
        };

        BuildFillShape(points, mgc);
    }

    private void BuildFillShape(List<Vector2> points, MeshGenerationContext mgc)
    {
        if (points.Count < 2) return;

        var painter2D = mgc.painter2D;
        painter2D.fillColor = vectorFillColor;
        painter2D.BeginPath();
        painter2D.MoveTo(points[0]);
        for (int i = 1; i < points.Count; i++)
        {
            painter2D.LineTo(points[i]);
        }
        painter2D.ClosePath();
        painter2D.Fill();
    }
}