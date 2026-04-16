using Unity.Netcode;
using UnityEngine;

public class PowerCore : Entity, IIdentifiable
{
    private bool youAreHoldingIt = false;

    public sealed override void OnNetworkObjectParentChanged(NetworkObject networkObject = null)
    {
        base.OnNetworkObjectParentChanged(networkObject);

        if (networkObject != null && networkObject.IsLocalPlayer) youAreHoldingIt = true;
        else youAreHoldingIt = false;
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
