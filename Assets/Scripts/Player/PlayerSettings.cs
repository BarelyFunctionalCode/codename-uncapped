using System;
using UnityEngine;
using UnityEngine.Events;

public class PlayerSettings
{
    public UnityEvent<string, object> OnSettingChanged = new();

    [PauseMenuOption("Horizontal Look", "Gameplay", "Controls", 0f, 100f)]
    public float horizontalRotationSpeed = 20f;

    [PauseMenuOption("Vertical Look", "Gameplay", "Controls", 0f, 100f)]
    public float verticalRotationSpeed = 24f;

    [PauseMenuOption("test-gameplay", "Gameplay", "Huh")]
    public bool testGameplayOption = true;

    [PauseMenuOption("test-video", "Video", "Huh")]
    public bool testVideoOption = true;

    [PauseMenuOption("test-audio", "Audio", "Huh")]
    public bool testAudioOption = true;


    private bool isPlayerOwned;

    public PlayerSettings(bool isPlayerOwned = false)
    {
        this.isPlayerOwned = isPlayerOwned;
        LoadSettings();
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
