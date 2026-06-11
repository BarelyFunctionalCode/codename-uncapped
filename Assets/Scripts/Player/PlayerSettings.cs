using UnityEngine;

public class PlayerSettings
{
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

    public PlayerSettings(string filePath)
    {
    }
}
