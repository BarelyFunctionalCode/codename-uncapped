using UnityEngine;

public class ImpactFrameEffectController : MonoBehaviour
{
    [SerializeField] private RenderTexture targetOverlay;

    private float duration = 0.2f;
    private float durationTimer = 0f;
    private float minRadius = 100f;
    private float maxRadius = 600f;

    private bool isActive = false;


    private void Start()
    {
        Shader.SetGlobalFloat("_ImpactFrameStartTime", -1000);
    }

    private void Update()
    {
        if (!isActive) return;

        durationTimer += Time.deltaTime;
        Shader.SetGlobalTexture("_ImpactFrameTargetOverlay", targetOverlay);
        if (durationTimer >= duration)
        {
            isActive = false;
        }
    }

    private void OnDestroy()
    {
        Shader.SetGlobalFloat("_ImpactFrameStartTime", -1000);
    }

    public void Trigger(Transform targetTransform)
    {

        Vector3 targetPosition = targetTransform.position;
        float targetDistance = Vector3.Distance(Camera.main.transform.position, targetPosition);
        Vector3 targetScreenPosition = Camera.main.WorldToScreenPoint(targetPosition);

        float radius = Mathf.Clamp(maxRadius - targetDistance, minRadius, maxRadius);

        Shader.SetGlobalFloat("_ImpactFrameDuration", duration);
        Shader.SetGlobalFloat("_ImpactFrameStartTime", Time.time);
        Shader.SetGlobalVector("_ImpactFramePOI", (Vector2)targetScreenPosition);
        Shader.SetGlobalFloat("_ImpactFrameRadius", radius);
        isActive = true;
        durationTimer = 0f;
    }
}
