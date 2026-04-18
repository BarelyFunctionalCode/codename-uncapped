using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Events;

public class CharacterNetworkTransform : NetworkTransform
{
    public UnityEvent<NetworkTransformState> onNewLocalTransformState = new();

    protected override void OnNetworkTransformStateUpdated(ref NetworkTransformState oldState, ref NetworkTransformState newState)
    {
        if (NetworkObject.IsOwner) onNewLocalTransformState.Invoke(newState);
        base.OnNetworkTransformStateUpdated(ref oldState, ref newState);
    }
}
