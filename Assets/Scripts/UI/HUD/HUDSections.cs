using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;


[UxmlElement(visibility = LibraryVisibility.Hidden)]
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

[UxmlElement(libraryPath = "HUD/LeftSide")]
public partial class HUDLeftSide : VectorFillShape
{
    float contentHeight = 140f;

    public HUDLeftSide()
    {
        RegisterCallback<GeometryChangedEvent>(evt =>
        {
            style.paddingTop = new StyleLength(layout.height - contentHeight);
            style.paddingBottom = new StyleLength(10);
            style.paddingLeft = new StyleLength(35 + layout.width * 0.1f);
            style.paddingRight = new StyleLength(30);
        });
    }

    // Used to generate the visual content for this element
    protected override void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        Color fillColor = resolvedColors.GetValueOrDefault(s_VectorFillColor.name, Color.clear);
        Color lineColor = resolvedColors.GetValueOrDefault(s_VectorLineColor.name, Color.clear);
        var gradient = new Gradient();

        // Blend color from red at 0% to blue at 100%
        var colors = new GradientColorKey[3];
        colors[0] = new GradientColorKey(fillColor, 0.0f);
        colors[1] = new GradientColorKey(fillColor, 0.5f);
        colors[2] = new GradientColorKey(Color.black, 1.0f);

        // Blend alpha from opaque at 0% to transparent at 100%
        var alphas = new GradientAlphaKey[2];
        alphas[0] = new GradientAlphaKey(fillColor.a, 0.0f);
        alphas[1] = new GradientAlphaKey(0.2f, 0.7f);

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
            new Vector2(15, 30),
            new Vector2(15, layout.height - (contentHeight - 35)),
            new Vector2(15 + layout.width * 0.1f, layout.height - (contentHeight - 35)),
            new Vector2(45 + layout.width * 0.1f, layout.height - (contentHeight + 10)),
            new Vector2(layout.width, layout.height - (contentHeight + 10)),
            new Vector2(layout.width, layout.height - (contentHeight - 10)),
            new Vector2(layout.width - 20, layout.height - (contentHeight - 35)),
            new Vector2(layout.width - 20, layout.height - 45),
            new Vector2(layout.width - 50, layout.height),
            new Vector2(0, layout.height),
        };

        BuildFillShape(mgc, points, fillGradient);
        BuildLineShape(mgc, points.GetRange(0, 7), lineColor, 5, LineJoin.Bevel, LineCap.Butt);
    }
}

[UxmlElement(libraryPath = "HUD/RightSide")]
public partial class HUDRightSide : VectorFillShape
{
    float contentHeight = 140f;

    public HUDRightSide()
    {
        RegisterCallback<GeometryChangedEvent>(evt =>
        {
            style.paddingTop = new StyleLength(layout.height - contentHeight);
            style.paddingBottom = new StyleLength(10);
            style.paddingLeft = new StyleLength(30);
            style.paddingRight = new StyleLength(35 + layout.width * 0.1f);
        });
    }

    // Used to generate the visual content for this element
    protected override void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        Color fillColor = resolvedColors.GetValueOrDefault(s_VectorFillColor.name, Color.clear);
        Color lineColor = resolvedColors.GetValueOrDefault(s_VectorLineColor.name, Color.clear);
        var gradient = new Gradient();

        // Blend color from red at 0% to blue at 100%
        var colors = new GradientColorKey[3];
        colors[0] = new GradientColorKey(fillColor, 0.0f);
        colors[1] = new GradientColorKey(fillColor, 0.5f);
        colors[2] = new GradientColorKey(Color.black, 1.0f);

        // Blend alpha from opaque at 0% to transparent at 100%
        var alphas = new GradientAlphaKey[2];
        alphas[0] = new GradientAlphaKey(fillColor.a, 0.0f);
        alphas[1] = new GradientAlphaKey(0.2f, 0.7f);

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
            new Vector2(layout.width - 15, 30),
            new Vector2(layout.width - 15, layout.height - (contentHeight - 35)),
            new Vector2(layout.width - 15 - layout.width * 0.1f, layout.height - (contentHeight - 35)),
            new Vector2(layout.width - 45 - layout.width * 0.1f, layout.height - (contentHeight + 10)),
            new Vector2(0, layout.height - (contentHeight + 10)),
            new Vector2(0, layout.height - (contentHeight - 10)),
            new Vector2(20, layout.height - (contentHeight - 35)),
            new Vector2(20, layout.height - 45),
            new Vector2(50, layout.height),
            new Vector2(layout.width, layout.height),
        };

        BuildFillShape(mgc, points, fillGradient);
        BuildLineShape(mgc, points.GetRange(0, 7), lineColor, 5, LineJoin.Bevel, LineCap.Butt);
    }
}


[UxmlElement(libraryPath = "HUD/Center")]
public partial class HUDCenter : VectorFillShape
{
    private float _energyRatio;
    [UxmlAttribute] public float EnergyRatio
    {
        get => _energyRatio;
        set
        {
            _energyRatio = Mathf.Clamp01(value);
            MarkDirtyRepaint();
        }
    }

    private float _healthRatio;
    [UxmlAttribute] public float HealthRatio
    {
        get => _healthRatio;
        set
        {            
            _healthRatio = Mathf.Clamp01(value);
            MarkDirtyRepaint();
        }
    }

    static protected CustomStyleProperty<Color> s_EnergyFillColor = new("--energy-fill-color");
    static protected CustomStyleProperty<Color> s_HealthFillColor = new("--health-fill-color");

    public HUDCenter()
    {
        RegisterCallback<GeometryChangedEvent>(evt =>
        {
            style.paddingTop = new StyleLength(10);
            style.paddingBottom = new StyleLength(10);
            style.paddingLeft = new StyleLength(layout.height / 1.5f + 50);
            style.paddingRight = new StyleLength(layout.height / 1.5f + 50);
        });
    }

    protected override void CustomStylesResolved(CustomStyleResolvedEvent evt)
    {
        base.CustomStylesResolved(evt);

        ResolveCustomStyleColor(evt, s_EnergyFillColor);
        ResolveCustomStyleColor(evt, s_HealthFillColor);
    }

    // Used to generate the visual content for this element
    protected override void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        Color fillColor = resolvedColors.GetValueOrDefault(s_VectorFillColor.name, Color.clear);
        Color energyFillColor = resolvedColors.GetValueOrDefault(s_EnergyFillColor.name, Color.clear);
        Color healthFillColor = resolvedColors.GetValueOrDefault(s_HealthFillColor.name, Color.clear);
        Color lineColor = resolvedColors.GetValueOrDefault(s_VectorLineColor.name, Color.clear);
        var gradient = new Gradient();
        var colors = new GradientColorKey[3];
        colors[0] = new GradientColorKey(fillColor, 0.0f);
        colors[1] = new GradientColorKey(fillColor, 0.5f);
        colors[2] = new GradientColorKey(Color.black, 1.0f);
        var alphas = new GradientAlphaKey[2];
        alphas[0] = new GradientAlphaKey(fillColor.a, 0.0f);
        alphas[1] = new GradientAlphaKey(0.2f, 0.7f);
        gradient.SetKeys(colors, alphas);

        var fillGradient = FillGradient.MakeLinearGradient(
            gradient,
            new Vector2(layout.width * 0.5f, layout.height),
            new Vector2(layout.width * 0.5f, 0),
            AddressMode.Clamp
        );

        List<Vector2> points = new()
        {
            new Vector2(layout.height / 1.5f + 50, 0),
            new Vector2(50, layout.height),
            new Vector2(layout.width - 50, layout.height),
            new Vector2(layout.width - layout.height / 1.5f - 50, 0),
        };
        BuildFillShape(mgc, points, fillGradient);

        var energyGradient = new Gradient();
        colors = new GradientColorKey[2];
        colors[0] = new GradientColorKey(energyFillColor, 0.0f);
        colors[1] = new GradientColorKey(energyFillColor, 0.1f);
        alphas = new GradientAlphaKey[3];
        alphas[0] = new GradientAlphaKey(energyFillColor.a, 0.0f);
        alphas[1] = new GradientAlphaKey(energyFillColor.a, 0.3f);
        alphas[2] = new GradientAlphaKey(0.0f, 1.0f);
        energyGradient.SetKeys(colors, alphas);

        var energyFillGradient = FillGradient.MakeLinearGradient(
            energyGradient,
            new Vector2(50, layout.height),
            new Vector2(5, layout.height - 30),
            AddressMode.Clamp
        );

        float startY = layout.height * (1 - EnergyRatio);
        float startX = layout.height / 1.5f * EnergyRatio;
        points = new()
        {
            new Vector2(startX, startY),
            new Vector2(startX + 50, startY),
            new Vector2(50, layout.height),
            new Vector2(0, layout.height),
        };
        BuildFillShape(mgc, points, energyFillGradient);




        var healthGradient = new Gradient();
        colors = new GradientColorKey[2];
        colors[0] = new GradientColorKey(healthFillColor, 0.0f);
        colors[1] = new GradientColorKey(healthFillColor, 0.1f);
        alphas = new GradientAlphaKey[3];
        alphas[0] = new GradientAlphaKey(healthFillColor.a, 0.0f);
        alphas[1] = new GradientAlphaKey(healthFillColor.a, 0.3f);
        alphas[2] = new GradientAlphaKey(0.0f, 1.0f);
        healthGradient.SetKeys(colors, alphas);

        var healthFillGradient = FillGradient.MakeLinearGradient(
            healthGradient,
            new Vector2(layout.width - 50, layout.height),
            new Vector2(layout.width - 5, layout.height - 30),
            AddressMode.Clamp
        );

        startY = layout.height * (1 - HealthRatio);
        startX = layout.width - layout.height / 1.5f * HealthRatio;
        points = new()
        {
            new Vector2(startX, startY),
            new Vector2(startX - 50, startY),
            new Vector2(layout.width - 50, layout.height),
            new Vector2(layout.width, layout.height),
        };
        BuildFillShape(mgc, points, healthFillGradient);

        points = new()
        {
            new Vector2(20, layout.height - 30),
            new Vector2(0, layout.height),
            new Vector2(layout.width, layout.height),
            new Vector2(layout.width - 20, layout.height - 30),
        };
        BuildLineShape(mgc, points, lineColor, 5, LineJoin.Bevel, LineCap.Butt);
        
        points = new()
        {
            new Vector2(layout.height / 1.5f - 20, 30),
            new Vector2(layout.height / 1.5f, 0),
            new Vector2(layout.width - layout.height / 1.5f, 0),
            new Vector2(layout.width - layout.height / 1.5f + 20, 30),
        };
        BuildLineShape(mgc, points, lineColor, 5, LineJoin.Bevel, LineCap.Butt);

        points = new()
        {
            new Vector2(layout.height / 1.5f + 50, 0),
            new Vector2(50, layout.height),
        };
        BuildLineShape(mgc, points, lineColor, 5, LineJoin.Bevel, LineCap.Butt);

        points = new()
        {
            new Vector2(layout.width - layout.height / 1.5f - 50, 0),
            new Vector2(layout.width - 50, layout.height),
        };
        BuildLineShape(mgc, points, lineColor, 5, LineJoin.Bevel, LineCap.Butt);
    }
}


[UxmlElement(libraryPath = "HUD/ObjectiveContainer")]
public partial class HUDObjectiveContainer : VectorFillShape
{
    public RetroNumber LeftObjectiveNumber { get; private set; }
    public RetroNumber Clock { get; private set; }
    public RetroNumber RightObjectiveNumber { get; private set; }

    public HUDObjectiveContainer()
    {
        VisualElement LeftContainer = new() { name = "LeftObjectiveContainer" };
        VisualElement CenterContainer = new() { name = "CenterObjectiveContainer" };
        VisualElement RightContainer = new() { name = "RightObjectiveContainer" };
        LeftContainer.AddToClassList("subcontainer");
        CenterContainer.AddToClassList("subcontainer");
        RightContainer.AddToClassList("subcontainer");
        Add(LeftContainer);
        Add(CenterContainer);
        Add(RightContainer);

        LeftObjectiveNumber = new() { name = "LeftObjectiveNumber", Text = "66" };
        Clock = new() { name = "CenterObjectiveNumber", Text = "10:25"  };
        RightObjectiveNumber = new() { name = "RightObjectiveNumber", Text = "77"  };
        LeftContainer.Add(LeftObjectiveNumber);
        CenterContainer.Add(Clock);
        RightContainer.Add(RightObjectiveNumber);
    }

    // Used to generate the visual content for this element
    protected override void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        Color fillColor = resolvedColors.GetValueOrDefault(s_VectorFillColor.name, Color.clear);
        Color lineColor = resolvedColors.GetValueOrDefault(s_VectorLineColor.name, Color.clear);
        var gradient = new Gradient();

        // Blend color from red at 0% to blue at 100%
        var colors = new GradientColorKey[3];
        colors[0] = new GradientColorKey(fillColor, 0.0f);
        colors[1] = new GradientColorKey(fillColor, 0.5f);
        colors[2] = new GradientColorKey(Color.black, 1.0f);

        // Blend alpha from opaque at 0% to transparent at 100%
        var alphas = new GradientAlphaKey[2];
        alphas[0] = new GradientAlphaKey(fillColor.a, 0.0f);
        alphas[1] = new GradientAlphaKey(0.2f, 0.7f);

        gradient.SetKeys(colors, alphas);

        var fillGradient = FillGradient.MakeLinearGradient(
            gradient,
            new Vector2(layout.width * 0.5f, 0),
            new Vector2(layout.width * 0.5f, layout.height),
            AddressMode.Clamp
        );

        List<Vector2> points = new()
        {
            new Vector2(layout.height / 1.5f, layout.height),
            new Vector2(0, 0),
            new Vector2(layout.width, 0),
            new Vector2(layout.width - layout.height / 1.5f, layout.height),
        };
        BuildFillShape(mgc, points, fillGradient);
        BuildLineShape(mgc, points, lineColor, 5, LineJoin.Bevel, LineCap.Butt, true);

        points = new()
        {
            new Vector2(layout.height / 1.5f + 100, layout.height),
            new Vector2(100, 0),
            new Vector2(layout.width - 100, 0),
            new Vector2(layout.width - layout.height / 1.5f - 100, layout.height),
        };
        BuildLineShape(mgc, points, lineColor, 5, LineJoin.Bevel, LineCap.Butt);
    }
}