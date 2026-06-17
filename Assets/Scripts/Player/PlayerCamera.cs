using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerCamera : MonoBehaviour
{
    // Lense Distortion: 0 --- -0.5
    // Vignette: 0 --- 0.5
    // FOV: preset --- +20

    [SerializeField] private CinemachineCamera cam;
    [SerializeField] private Camera impactFrameCamera;
    private float defaultFOV = 90f;
    private CinemachineBasicMultiChannelPerlin noise;
    private Vignette vignette;
    private LensDistortion lensDistortion;
    [SerializeField] ParticleSystem windLinesParticleSystem;

    private readonly float minShakeValue = 0;
    private readonly float maxShakeValue = 5;

    private readonly float minCharacterSpeed = 40;
    private readonly float maxCharacterSpeed = 120;
    private readonly float maxVignetteIntensity = 0.5f;
    private readonly float maxLensDistortionIntensity = -0.3f;
    private readonly float maxFOVIncrease = 15f;
    private readonly float windLinesMinSpeed = 10f;
    private readonly float windLinesMaxSpeed = 35f;
    private readonly float windLinesMinEmissionRate = 4f;
    private readonly float windLinesMaxEmissionRate = 20f;
    private readonly float windLinesMaxColorAlpha = 0.25f;

    public bool IsEnabled => cam.Priority.Value > 0;

    void Awake()
    {

        noise = cam.GetComponent<CinemachineBasicMultiChannelPerlin>();
        CinemachineVolumeSettings volumeSettings = cam.GetComponent<CinemachineVolumeSettings>();
        volumeSettings.Profile.TryGet(out vignette);
        volumeSettings.Profile.TryGet(out lensDistortion);

        Player.Instance.Character.health.onHealthChanged.AddListener(DamageScreenshake);
        Player.Instance.settings.SubscribeToChanges(OnSettingsChanged);
    }

    void OnDestroy()
    {
        if (Player.Instance && Player.Instance.Character)
            Player.Instance.Character.health.onHealthChanged.RemoveListener(DamageScreenshake);
    }

    // Update is called once per frame
    void Update()
    {
        noise.FrequencyGain = Mathf.Lerp(noise.FrequencyGain, minShakeValue, Time.deltaTime * 5);
        if (noise.FrequencyGain < 0.01f) noise.FrequencyGain = 0;
    }

    private void OnSettingsChanged(string settingName, object value)
    {
        if (settingName == nameof(PlayerSettings.fieldOfView))
        {
            defaultFOV = (float)value;
            cam.Lens.FieldOfView = defaultFOV;
            if (impactFrameCamera != null)
                impactFrameCamera.fieldOfView = defaultFOV;
        }
        if (settingName == nameof(PlayerSettings.displayModeIndex))
        {
            int displayModeIndex = (int)value;
            if (displayModeIndex == 0) Screen.fullScreenMode = FullScreenMode.Windowed;
            else if (displayModeIndex == 1) Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        }
        if (settingName == nameof(PlayerSettings.resolutionIndex))
        {
            Resolution res = Screen.resolutions[Player.Instance.settings.resolutionIndex];
            Screen.SetResolution(res.width, res.height, Screen.fullScreenMode, res.refreshRateRatio);
        }
    }


    public void SetState(bool enabled) => cam.Priority.Value = enabled ? 1 : 0;

    public void SetFollowTarget(Transform target) 
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        cam.Follow = target;
    }
    public void SetLookAtTarget(Transform target) => cam.LookAt = target;

    private void DamageScreenshake(float healthDeltaRatio)
    {
        if (healthDeltaRatio > 0) return;

        noise.FrequencyGain += -healthDeltaRatio;
        noise.FrequencyGain = Mathf.Clamp(noise.FrequencyGain, minShakeValue, maxShakeValue);
    }

    public void SpeedBasedCameraEffects(float speed)
    {
        float effectIntensity = Mathf.InverseLerp(minCharacterSpeed, maxCharacterSpeed, speed);

        float vignetteIntensity = effectIntensity * maxVignetteIntensity;
        float lensDistortionIntensity = effectIntensity * maxLensDistortionIntensity;
        float fovIncrease = effectIntensity * maxFOVIncrease;
        vignette.intensity.value = vignetteIntensity;
        lensDistortion.intensity.value = lensDistortionIntensity;
        cam.Lens.FieldOfView = defaultFOV + fovIncrease;

        float windLinesEmissionRate = Mathf.Lerp(windLinesMinEmissionRate, windLinesMaxEmissionRate, effectIntensity);
        float windLinesSpeed = Mathf.Lerp(windLinesMinSpeed, windLinesMaxSpeed, effectIntensity);

        var emission = windLinesParticleSystem.emission;
        emission.rateOverTime = windLinesEmissionRate;
        var main = windLinesParticleSystem.main;
        main.startSpeed = windLinesSpeed;
        var startColor = main.startColor.color;
        startColor.a = Mathf.Lerp(0, windLinesMaxColorAlpha, effectIntensity);
        main.startColor = startColor;
    }
}
