using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private CinemachineCamera cam;
    private Camera mainCamera;

    private CinemachineBasicMultiChannelPerlin noise;

    private AudioListener audioListener;

    private float minShakeValue = 0;
    private float maxShakeValue = 5;


    public bool IsEnabled => cam.Priority.Value > 0;

    void Awake()
    {
        mainCamera = Camera.main;
        noise = cam.GetComponent<CinemachineBasicMultiChannelPerlin>();
        audioListener = mainCamera.GetComponent<AudioListener>();
        audioListener.enabled = true;

        NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>().health.onHealthChanged.AddListener(DamageScreenshake);
    }

    void OnDestroy()
    {
        if (mainCamera != null)
        {
            var cameraData = mainCamera.GetUniversalAdditionalCameraData();
            cameraData.cameraStack.Clear();

            audioListener.enabled = false;
        }
        
        if (NetworkManager.Singleton && NetworkManager.Singleton.LocalClient.PlayerObject)
            NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>().health.onHealthChanged.RemoveListener(DamageScreenshake);
    }

    // Update is called once per frame
    void Update()
    {
        noise.FrequencyGain = Mathf.Lerp(noise.FrequencyGain, minShakeValue, Time.deltaTime * 5);
        if (noise.FrequencyGain < 0.01f) noise.FrequencyGain = 0;
    }

    public void SetState(bool enabled) => cam.Priority.Value = enabled ? 1 : 0;

    public void SetFollowTarget(Transform target) => cam.Follow = target;

    public void AddCameraToStack(Camera cam)
    {
        var cameraData = mainCamera.GetUniversalAdditionalCameraData();
        cameraData.cameraStack.Add(cam);
    }

    private void DamageScreenshake(float healthDeltaRatio)
    {
        if (healthDeltaRatio > 0) return;

        noise.FrequencyGain += -healthDeltaRatio;
        noise.FrequencyGain = Mathf.Clamp(noise.FrequencyGain, minShakeValue, maxShakeValue);
    }

}
