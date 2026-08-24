using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(PowerCorePickup))]
public class PowerCore : Entity, IIdentifiable
{
    [SerializeField] CoreHolder coreHolder;

    public int TeamId => coreHolder != null ? (int)coreHolder.teamId : -1;
    public bool IsAtBase { get; private set; } = true;
    private bool youAreHoldingIt = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer) return;
        if (SpawnManager.Instance) SpawnManager.Instance.RegisterSpawnedObject(NetworkObject);
    }

    public sealed override void OnNetworkObjectParentChanged(NetworkObject networkObject = null)
    {
        base.OnNetworkObjectParentChanged(networkObject);

        if (networkObject != null && networkObject.TryGetComponent(out CoreHolder _)) IsAtBase = true;
        else IsAtBase = false;

        if (networkObject != null && CharacterManager.Instance.IsLocalPlayerCharacter(networkObject)) youAreHoldingIt = true;
        else youAreHoldingIt = false;
    }

    public void ResetToBase()
    {   
        if (!IsServer) return;

        PowerCorePickup pickup = GetComponent<PowerCorePickup>();
        pickup.CanBePickedUp.Value = false;
        if (pickup.isPickedUp) pickup.pickerUpper.TryPutDownRpc(Vector3.up);

        if (coreHolder != null)
        {
            pickup.Rb.isKinematic = true;
            pickup.Rb.interpolation = RigidbodyInterpolation.None;
            transform.position = coreHolder.transform.position;
            transform.rotation = coreHolder.transform.rotation;
            NetworkObject.TrySetParent(coreHolder.NetworkObject);
            pickup.lastHeldByIdentification = null;
        }
        pickup.CanBePickedUp.Value = true;
    }

    public IdentifierData GetIdentifierData()
    {
        if (!baseEntityInitialized) return default;

        return new IdentifierData
        {
            color = Color.yellow,
            topText = "Power Core",
            bottomText = "",
            isActive = !youAreHoldingIt,
            targetTransform = transform,
            isAlwaysVisible = true
        };
    }
}
