using UnityEngine;
using UnityEngine.UIElements;


[UxmlElement(libraryPath = "HUD")]
public partial class DriveUI : CustomUIElementBase
{
    // TEST PROPERTIES
    // private float _cooldownRatio = 0f;
    // [UxmlAttribute] public float CooldownRatio
    // {
    //     get => _cooldownRatio;
    //     set
    //     {
    //         _cooldownRatio = Mathf.Clamp01(value);
    //         OnCooldownRatioChanged(0, _cooldownRatio);
    //     }
    // }

    // private float _effectDurationRatio = 0f;
    // [UxmlAttribute] public float EffectDurationRatio
    // {
    //     get => _effectDurationRatio;
    //     set
    //     {            
    //         _effectDurationRatio = Mathf.Clamp01(value);
    //         OnEffectDurationRatioChanged(0, _effectDurationRatio);
    //     }
    // }

    // private bool _isOnline = false;
    // [UxmlAttribute] public bool IsOnline
    // {
    //     get => _isOnline;
    //     set
    //     {            
    //         _isOnline = value;
    //         OnDriveOnlineChanged(false, _isOnline);
    //     }
    // }

    // private DriveState _driveState = DriveState.Ready;
    // [UxmlAttribute] public DriveState DriveState
    // {
    //     get => _driveState;
    //     set
    //     {            
    //         _driveState = value;
    //         OnDriveStateChanged(_driveState);
    //     }
    // }

    private Label onlineText;
    private RetroNumber numberText;
    private VisualElement activeFillBar;
    private VisualElement cooldownFillBar;

    private Drive drive;

    private DriveState currentState = DriveState.Ready;


    public DriveUI()
    {
        EnableInClassList("ready", true);

        onlineText = new Label("DRIVE IS ONLINE")
        {
            name = "online-text"
        };
        Add(onlineText);

        numberText = new RetroNumber()
        {
            name = "number-text",
            Text = "100"
        };
        Add(numberText);

        activeFillBar = new VisualElement()
        {
            name = "active-fill-bar"
        };
        Add(activeFillBar);
        activeFillBar.SendToBack();

        cooldownFillBar = new VisualElement()
        {
            name = "cooldown-fill-bar"
        };
        Add(cooldownFillBar);
        cooldownFillBar.SendToBack();
    }

    public void Initialize(Drive drive)
    {
        EnableInClassList("ready", true);
        EnableInClassList("active", false);
        EnableInClassList("cooldown", false);
        this.drive = drive;


        LoadoutItemSO driveLoadoutItem = CharacterLoadout.GetLoadoutItemSOFromPrefab(drive.gameObject);
        if (driveLoadoutItem == null)
        {
            Debug.LogError("Could not find LoadoutItemSO for drive prefab: " + drive.gameObject.name);
            return;
        }
        string driveName = driveLoadoutItem.itemName ?? "";
        string uiText = $"{driveName.ToUpper()} DRIVE ONLINE";
        onlineText.text = uiText;

        drive.driveState.OnValueChanged += OnDriveStateChanged;
        drive.isOnline.OnValueChanged += OnDriveOnlineChanged;
        drive.cooldownRatio.OnValueChanged += OnCooldownRatioChanged;
        drive.effectDurationRatio.OnValueChanged += OnEffectDurationRatioChanged;
    }

    public void Deinitialize()
    {
        EnableInClassList("ready", false);
        EnableInClassList("active", false);
        EnableInClassList("cooldown", false);

        if (drive != null)
        {
            drive.driveState.OnValueChanged -= OnDriveStateChanged;
            drive.isOnline.OnValueChanged -= OnDriveOnlineChanged;
            drive.cooldownRatio.OnValueChanged -= OnCooldownRatioChanged;
            drive.effectDurationRatio.OnValueChanged -= OnEffectDurationRatioChanged;
        }

        RemoveFromHierarchy();
    }

    private void OnDriveStateChanged(DriveState previousState, DriveState newState)
    {
        currentState = newState;
        EnableInClassList("online", currentState == DriveState.Ready && drive.isOnline.Value);
        switch (currentState)
        {
            case DriveState.Ready:
                EnableInClassList("ready", true);
                EnableInClassList("active", false);
                EnableInClassList("cooldown", false);
                break;
            case DriveState.Active:
                EnableInClassList("ready", false);
                EnableInClassList("active", true);
                EnableInClassList("cooldown", false);
                break;
            case DriveState.Cooldown:
                EnableInClassList("ready", false);
                EnableInClassList("active", false);
                EnableInClassList("cooldown", true);
                break;
        }
    }

    private void OnDriveOnlineChanged(bool _, bool isOnline)
    {
        if (currentState == DriveState.Ready) EnableInClassList("online", isOnline);
    }

    private void OnCooldownRatioChanged(float _, float cooldownRatio)
    {
        cooldownFillBar.style.width = new StyleLength(new Length(cooldownRatio * 100, LengthUnit.Percent));
        if (drive == null) return;
        numberText.Text = drive.cooldownSeconds.Value.ToString("00");
    }

    private void OnEffectDurationRatioChanged(float _, float effectDurationRatio)
    {
        activeFillBar.style.height = new StyleLength(new Length(effectDurationRatio * 100, LengthUnit.Percent));
        numberText.Text = Mathf.CeilToInt(effectDurationRatio * 100).ToString("000");
    }
}