using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private CinemachineCamera cam;
    private CinemachineBasicMultiChannelPerlin noise;

    private float minShakeValue = 0;
    private float maxShakeValue = 5;

    public bool IsEnabled => cam.Priority.Value > 0;

    void Awake()
    {
        noise = cam.GetComponent<CinemachineBasicMultiChannelPerlin>();
        Player.Instance.Character.health.onHealthChanged.AddListener(DamageScreenshake);
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
}
