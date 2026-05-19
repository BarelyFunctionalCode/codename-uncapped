using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement(libraryPath = "HUD/LoadoutItem")]
public partial class LoadoutItemUI : VectorFillShape
{
    private float _maxCooldown = 0f;
    [UxmlAttribute] public float MaxCooldown
    {
        get => _maxCooldown;
        set => _maxCooldown = value;
    }

    private float _currentCooldown = 0f;
    [UxmlAttribute] public float CurrentCooldown
    {       
        get => _currentCooldown;
        set
        {   
            _currentCooldown = Mathf.Clamp(value, 0, MaxCooldown);
            if (_currentCooldown <= 0)
            {
                if (cooldownLabel != null) cooldownLabel.style.display = DisplayStyle.None;
            }
            else
            {
                if (cooldownLabel != null)
                {
                    cooldownLabel.style.display = DisplayStyle.Flex;
                    cooldownLabel.Text = _currentCooldown.ToString("00");
                }
                currentCooldownRatio = _currentCooldown / MaxCooldown;
            }
            MarkDirtyRepaint();

            SetAvailability();
        }
    }

    private int _ammo = 0;
    [UxmlAttribute] public int Ammo
    {
        get => _ammo;
        set
        {
            int newAmmo = Mathf.Max(0, value);
            if (newAmmo < _ammo)
            {
                EnableInClassList("loadout-item-transition", true);
                schedule.Execute(() => EnableInClassList("loadout-item-transition", false)).StartingIn(50);
            }
            if (numberAmmoLabel != null) numberAmmoLabel.Text = newAmmo.ToString("00");
            SetAvailability();

            _ammo = newAmmo;
        }
    }

    private Sprite _iconSprite = null;
    [UxmlAttribute] public Sprite IconSprite
    {
        get => _iconSprite;
        set
        {
            _iconSprite = value;
            if (icon != null) icon.style.backgroundImage = new StyleBackground(_iconSprite);
        }
    }

    private LoadoutItem item;
    private Image icon;
    private RetroNumber numberAmmoLabel;
    private RetroNumber cooldownLabel;

    private float cornerBevelSlope = 1.5f;
    private float cornerBevelSize = 15f;
    private float lineWidth = 5.0f;

    private float currentCooldownRatio = 0f;
    private bool isAvailable = true;

    static protected CustomStyleProperty<Color> s_CooldownColor = new("--cooldown-color");

    private Color outlineColor;
    private Color disabledOutlineColor = new(1, 1, 1, 0.1f);


    public LoadoutItemUI()
    {
        style.paddingTop = new StyleLength(lineWidth);
        style.paddingBottom = new StyleLength(lineWidth);
        style.paddingLeft = new StyleLength(lineWidth);
        style.paddingRight = new StyleLength(lineWidth);

        icon = new Image();
        Add(icon);

        numberAmmoLabel = new RetroNumber();
        Add(numberAmmoLabel);

        cooldownLabel = new RetroNumber()
        {
            name = "cooldown-label",
        };
        Add(cooldownLabel);
        BringToFront();
        currentCooldownRatio = 0.6f;

        RegisterCallback<GeometryChangedEvent>(evt =>
        {
            float contentOffset = cornerBevelSize * 0.5f;

            icon.style.marginTop = new StyleLength(contentOffset);
            icon.style.marginBottom = new StyleLength(contentOffset);
            icon.style.marginLeft = new StyleLength(contentOffset);
            icon.style.marginRight = new StyleLength(contentOffset);
        });

        SetAvailability();

        SetEnabled(false);
    }

    protected override void CustomStylesResolved(CustomStyleResolvedEvent evt)
    {
        base.CustomStylesResolved(evt);

        // Vector Fill Color
        ResolveCustomStyleColor(evt, s_CooldownColor);
        SetAvailability();
    }

    public void Initialize(LoadoutItem loadoutItem)
    {
        item = loadoutItem;
        if (item == null) return;

        icon.style.backgroundImage = new StyleBackground(item.iconSprite);
        Ammo = item.MaxAmmo;
        MaxCooldown = item.Cooldown;

        item.isEquiped.OnValueChanged += UpdateEquipped;
        item.ammo.OnValueChanged += UpdateAmmo;
        if (MaxCooldown > 1f) item.cooldownTimer.OnValueChanged += UpdateCooldown;
    }

    public void Deinitialize()
    {
        if (item != null)
        {
            item.isEquiped.OnValueChanged -= UpdateEquipped;
            item.ammo.OnValueChanged -= UpdateAmmo;
            if (MaxCooldown > 1f) item.cooldownTimer.OnValueChanged -= UpdateCooldown;
        }

        RemoveFromHierarchy();
    }
    private void UpdateEquipped(bool _, bool isEquipped) => SetEnabled(isEquipped);
    private void UpdateAmmo(int _, int ammoCount) => Ammo = ammoCount;
    private void UpdateCooldown(float _, float cooldown) => CurrentCooldown = cooldown;

    protected override void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        Rect bounds = new(
            lineWidth / 2.0f,
            lineWidth / 2.0f,
            layout.width - lineWidth / 2.0f,
            layout.height - lineWidth / 2.0f
        );
    
        List<Vector2> points = new()
        {
            new Vector2(cornerBevelSize, bounds.height),
            new Vector2(lineWidth / 2.0f, bounds.height - cornerBevelSize * cornerBevelSlope),
            new Vector2(lineWidth / 2.0f, cornerBevelSize * cornerBevelSlope),
            new Vector2(cornerBevelSize, lineWidth / 2.0f),
            new Vector2(bounds.width - cornerBevelSize, lineWidth / 2.0f),
            new Vector2(bounds.width, cornerBevelSize * cornerBevelSlope),
            new Vector2(bounds.width, bounds.height - cornerBevelSize * cornerBevelSlope),
            new Vector2(bounds.width - cornerBevelSize, bounds.height),
        };
        BuildLineShape(mgc, points, outlineColor, lineWidth, LineJoin.Miter, LineCap.Round);

        if (MaxCooldown <= 0 || CurrentCooldown <= 0) return;
        float arcRadius = layout.width * 0.5f;
        Vector2 arcCenter = layout.size * 0.5f;
        float startAngle = -90.0f;
        float endAngle = Mathf.Clamp(startAngle + currentCooldownRatio * 360.0f, startAngle, startAngle + 359.0f);
        Color fillColor = resolvedColors.GetValueOrDefault(s_CooldownColor.name, Color.clear);

        var painter2D = mgc.painter2D;
        painter2D.fillColor = fillColor;
        painter2D.BeginPath();
        painter2D.MoveTo(arcCenter);
        painter2D.Arc(arcCenter, arcRadius, startAngle, endAngle, ArcDirection.Clockwise);
        painter2D.ClosePath();
        painter2D.Fill();
        painter2D.fillColor = new Color(1, 1, 1, fillColor.a * 0.5f);
        painter2D.BeginPath();
        painter2D.MoveTo(arcCenter);
        painter2D.Arc(arcCenter, arcRadius, startAngle, endAngle, ArcDirection.CounterClockwise);
        painter2D.ClosePath();
        painter2D.Fill();
    }

    private void SetAvailability()
    {
        bool available = _currentCooldown <= 0 && _ammo > 0;
        isAvailable = available;

        outlineColor = isAvailable ? resolvedColors.GetValueOrDefault(s_VectorLineColor.name, Color.clear) : disabledOutlineColor;
        icon.SetEnabled(available);
        numberAmmoLabel.SetEnabled(available);
    }
}