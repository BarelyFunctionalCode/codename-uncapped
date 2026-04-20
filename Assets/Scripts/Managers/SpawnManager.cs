using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager Instance { get; private set; } = null;
    public UnityEvent<GameObject> objectSpawnedEvent = new();

    private NetworkList<NetworkObjectReference> spawnedObjects = new();
    private float retryInterval = 0.5f;
    private float retryTimer = 0f;
    private List<NetworkObjectReference> retryList = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnDestroy()
    {
        if (Instance != this) return;
        objectSpawnedEvent.RemoveAllListeners();
        spawnedObjects.Clear();
        Instance = null;

        base.OnDestroy();
    }

    private void Update()
    {
        if (IsServer)
        {
            for (int i = spawnedObjects.Count - 1; i >= 0; i--)
            {
                if (!spawnedObjects[i].TryGet(out NetworkObject networkObject) || networkObject == null)
                {
                    spawnedObjects.RemoveAt(i);
                }
            }
        }

        if (retryList.Count > 0)
        {
            retryTimer += Time.deltaTime;
            if (retryTimer >= retryInterval)
            {
                retryTimer = 0f;
                for (int i = retryList.Count - 1; i >= 0; i--)
                {
                    if (retryList[i].TryGet(out NetworkObject networkObject) && networkObject != null && networkObject.IsSpawned)
                    {
                        objectSpawnedEvent.Invoke(networkObject.gameObject);
                        retryList.RemoveAt(i);
                    }
                }
            }
        }
    }

    public void Subscribe(UnityAction<GameObject> listener)
    {
        objectSpawnedEvent.AddListener(listener);
        NewSubscriptionDump();
    }
    public void Unsubscribe(UnityAction<GameObject> listener) => objectSpawnedEvent.RemoveListener(listener);

    public GameObject Spawn(GameObject objectPrefab, Transform parent)
    {
        if (!IsServer) return null;
        GameObject spawnedObject = Instantiate(objectPrefab, Vector3.zero, Quaternion.identity, parent);
        if (spawnedObject.TryGetComponent(out NetworkObject networkObject))
        {
            networkObject.Spawn(false);
            networkObject.TrySetParent(parent?.GetComponentInParent<NetworkObject>());
            spawnedObjects.Add(networkObject);
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
            spawnedObjects.Add(networkObject);
            BroadcastNotificationRpc(networkObject);
        }
        return spawnedObject;
    }

    public void RegisterSpawnedObject(NetworkObject networkObject)
    {
        if (!IsServer) return;
        if (networkObject == null) return;
        if (spawnedObjects.Contains(new NetworkObjectReference(networkObject))) return;
        spawnedObjects.Add(networkObject);
        BroadcastNotificationRpc(networkObject);
    }

    private void NewSubscriptionDump()
    {
        foreach (var objRef in spawnedObjects)
        {
            if (!objRef.TryGet(out NetworkObject networkObject) || networkObject == null || !networkObject.IsSpawned)
            {
                retryList.Add(objRef);
                return;
            }
            objectSpawnedEvent.Invoke(networkObject.gameObject);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void BroadcastNotificationRpc(NetworkObjectReference obj)
    {
        if (!obj.TryGet(out NetworkObject networkObject) || networkObject == null || !networkObject.IsSpawned)
        {
            retryList.Add(obj);
            return;
        }
        objectSpawnedEvent.Invoke(networkObject.gameObject);
    }
}