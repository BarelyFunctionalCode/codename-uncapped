using Unity.Netcode;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

public class Showcase : MonoBehaviour
{
    public UnityEvent OnRotationCompleteEvent;
    [SerializeField] private Transform rotatingPlatform;
    [SerializeField] private CinemachinePositionComposer positionComposer;
    private float originalCameraDistance;

    private float rotationSpeed = 20f;
    private float lastRotationAngle = 0f;

    private void Awake()
    {
        if (positionComposer != null)
        {
            originalCameraDistance = positionComposer.CameraDistance;
        }
    }

    private void Update()
    {
        if (rotatingPlatform != null)
        {
            rotatingPlatform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            lastRotationAngle += rotationSpeed * Time.deltaTime;

            if (lastRotationAngle >= 360f)
            {
                lastRotationAngle = 0f;
                OnRotationCompleteEvent?.Invoke();
            }
        }
    }

    public void AddObject(GameObject subjectObj, float additionalCameraDistance = 0f, bool alreadySpawned = false)
    {
        if (positionComposer != null)
        {
            positionComposer.CameraDistance = originalCameraDistance + additionalCameraDistance;
        }
        if (!alreadySpawned)
        {
            subjectObj = Instantiate(subjectObj);
            NetworkObject networkObject = subjectObj.transform.GetComponentInChildren<NetworkObject>();
            if (networkObject != null) DestroyImmediate(networkObject);
        }
        if (rotatingPlatform != null && subjectObj != null)
        {
            subjectObj.layer = LayerMask.NameToLayer("Showcase");
            foreach (Transform trans in subjectObj.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                trans.gameObject.layer = LayerMask.NameToLayer("Showcase");
            }
            subjectObj.transform.parent = rotatingPlatform;
            subjectObj.transform.localPosition = Vector3.zero;
            subjectObj.transform.localRotation = Quaternion.identity;
        }
    }

    public void Clear()
    {
        if (rotatingPlatform != null)
        {
            foreach (Transform child in rotatingPlatform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
