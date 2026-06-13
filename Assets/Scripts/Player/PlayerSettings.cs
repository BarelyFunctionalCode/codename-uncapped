using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerSettings
{
    public UnityEvent<string, object> OnSettingChanged = new();

    [PauseMenuOption("FOV", "Gameplay", "Camera", 60f, 120f)]
    public float fieldOfView = 90f;
    [PauseMenuOption("Horizontal Look", "Gameplay", "Controls", 0f, 100f)]
    public float horizontalRotationSpeed = 20f;

    [PauseMenuOption("Vertical Look", "Gameplay", "Controls", 0f, 100f)]
    public float verticalRotationSpeed = 24f;


    public List<string> displayModeOptions = new() { "Windowed", "Borderless Fullscreen" };
    [PauseMenuOption("Display Mode", "Graphics", "Display", listOptionsVariableName = nameof(displayModeOptions))]
    public int displayModeIndex = 0;

    public List<string> resolutionOptions = Screen.resolutions != null ?
        new List<string>(
            Array.ConvertAll(Screen.resolutions, res => $"{res.width} x {res.height} @ {res.refreshRateRatio.value:0.##}Hz")
        ) :
        new List<string>();
    [PauseMenuOption("Resolution", "Graphics", "Display", listOptionsVariableName = nameof(resolutionOptions))]
    public int resolutionIndex = 0;


    private bool isPlayerOwned;


    public PlayerSettings(bool isPlayerOwned = false)
    {
        this.isPlayerOwned = isPlayerOwned;
        LoadSettings();
    }

    public void SubscribeToChanges(UnityAction<string, object> listener)
    {
        OnSettingChanged.AddListener(listener);

        foreach (var field in GetType().GetFields())
        {
            string settingName = field.Name;
            object value = field.GetValue(this);
            OnSettingChanged.Invoke(settingName, value);
        }
    }

    public void UpdateSetting(string settingName, object value)
    {
        var field = GetType().GetField(settingName);
        if (field != null)
        {
            field.SetValue(this, Convert.ChangeType(value, field.FieldType));
            OnSettingChanged.Invoke(settingName, value);
            
            if (isPlayerOwned) SaveSettings(settingName);
        }
    }

    private void LoadSettings()
    {
        foreach (var field in GetType().GetFields())
        {
            string settingName = field.Name;
            Type valueType = field.FieldType;

            if (!PlayerPrefs.HasKey(settingName))
                continue;

            if (valueType == typeof(float))
                field.SetValue(this, PlayerPrefs.GetFloat(settingName, (float)field.GetValue(this)));
            else if (valueType == typeof(int))
                field.SetValue(this, PlayerPrefs.GetInt(settingName, (int)field.GetValue(this)));
            else if (valueType == typeof(bool))
                field.SetValue(this, PlayerPrefs.GetInt(settingName, (bool)field.GetValue(this) ? 1 : 0) == 1);
            else if (valueType == typeof(string))
                field.SetValue(this, PlayerPrefs.GetString(settingName, (string)field.GetValue(this)));
        }
    }

    private void SaveSettings(string settingName)
    {
        Type settingsType = GetType();
        var field = settingsType.GetField(settingName);
        if (field != null)
        {
            object value = field.GetValue(this);
            Type valueType = field.FieldType;
            if (valueType == typeof(float) || valueType == typeof(int) || valueType == typeof(bool) || valueType == typeof(string))
            {
                // Save the setting to PlayerPrefs
                if (valueType == typeof(float))
                    PlayerPrefs.SetFloat(settingName, (float)value);
                else if (valueType == typeof(int))
                    PlayerPrefs.SetInt(settingName, (int)value);
                else if (valueType == typeof(bool))
                    PlayerPrefs.SetInt(settingName, (bool)value ? 1 : 0);
                else if (valueType == typeof(string))
                    PlayerPrefs.SetString(settingName, (string)value);

                PlayerPrefs.Save();
            }
            else
            {
                Debug.LogWarning($"Unsupported setting type: {valueType}");
            }
        }
    }
}
