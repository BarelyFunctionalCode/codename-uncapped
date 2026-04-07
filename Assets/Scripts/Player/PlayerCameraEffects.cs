using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class PlayerCameraEffects : MonoBehaviour
{
    [SerializeField] private CinemachineCamera cam;

    private CinemachineBasicMultiChannelPerlin noise;

    private float minShakeValue = 0;
    private float maxShakeValue = 5;

    void Awake()
    {
        noise = cam.GetComponent<CinemachineBasicMultiChannelPerlin>();

        NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<Health>().onHealthChanged.AddListener(DamageScreenshake);
    }

    // Update is called once per frame
    void Update()
    {
        noise.FrequencyGain = Mathf.Lerp(noise.FrequencyGain, minShakeValue, Time.deltaTime * 5);
        if (noise.FrequencyGain < 0.01f) noise.FrequencyGain = 0;
    }

    private void DamageScreenshake(float healthDeltaRatio)
    {
        if (healthDeltaRatio > 0) return;

        noise.FrequencyGain += -healthDeltaRatio;
        noise.FrequencyGain = Mathf.Clamp(noise.FrequencyGain, minShakeValue, maxShakeValue);
    }

}
