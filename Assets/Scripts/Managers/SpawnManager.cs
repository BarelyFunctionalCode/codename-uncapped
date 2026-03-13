using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public static class SpawnManager
{
    public static UnityEvent<GameObject> objectSpawnedEvent = new(); // TODO: THIS DOES NOT REACH CLIENTS!

    public static GameObject Spawn(GameObject objectPrefab, Transform parent)
    {
        GameObject spawnedObject = Object.Instantiate(objectPrefab, Vector3.zero, Quaternion.identity, parent);
        if (spawnedObject.TryGetComponent(out NetworkObject networkObject))
        {
            networkObject.Spawn(false);
            networkObject.TrySetParent(parent?.GetComponentInParent<NetworkObject>());
        }
        objectSpawnedEvent.Invoke(spawnedObject);
        return spawnedObject;
    }

    public static GameObject Spawn(GameObject objectPrefab, bool destroyWithScene = false, Vector3 spawnPosition = default, Quaternion spawnRotation = default, Transform parent = null, ulong ownerClientId = ulong.MaxValue)
    {
        GameObject spawnedObject = Object.Instantiate(objectPrefab, spawnPosition, spawnRotation, parent);
        if (spawnedObject.TryGetComponent(out NetworkObject networkObject))
        {
            networkObject.Spawn(destroyWithScene);
            networkObject.TrySetParent(parent?.GetComponentInParent<NetworkObject>());
            if (ownerClientId != ulong.MaxValue) networkObject.ChangeOwnership(ownerClientId);
        }
        objectSpawnedEvent.Invoke(spawnedObject);
        return spawnedObject;
    }
}