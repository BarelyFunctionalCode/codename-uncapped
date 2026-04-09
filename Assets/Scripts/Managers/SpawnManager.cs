using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager Instance { get; private set; } = null;
    public UnityEvent<GameObject> objectSpawnedEvent = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public GameObject Spawn(GameObject objectPrefab, Transform parent)
    {
        if (!IsServer) return null;
        GameObject spawnedObject = Instantiate(objectPrefab, Vector3.zero, Quaternion.identity, parent);
        if (spawnedObject.TryGetComponent(out NetworkObject networkObject))
        {
            networkObject.Spawn(false);
            networkObject.TrySetParent(parent?.GetComponentInParent<NetworkObject>());
            BroadcastNotificationRpc(networkObject);
        }
        return spawnedObject;
    }

    public GameObject Spawn(GameObject objectPrefab, bool destroyWithScene = false, Vector3 spawnPosition = default, Quaternion spawnRotation = default, Transform parent = null, ulong ownerClientId = ulong.MaxValue)
    {
        if (!IsServer) return null;
        GameObject spawnedObject = Instantiate(objectPrefab, spawnPosition, spawnRotation, parent);
        if (spawnedObject.TryGetComponent(out NetworkObject networkObject))
        {
            networkObject.Spawn(destroyWithScene);
            networkObject.TrySetParent(parent?.GetComponentInParent<NetworkObject>());
            if (ownerClientId != ulong.MaxValue) networkObject.ChangeOwnership(ownerClientId);
            BroadcastNotificationRpc(networkObject);
        }
        return spawnedObject;
    }

    [Rpc(SendTo.Everyone)]
    private void BroadcastNotificationRpc(NetworkObjectReference obj)
    {
        objectSpawnedEvent.Invoke(obj.TryGet(out NetworkObject networkObject) ? networkObject.gameObject : null);
    }
}