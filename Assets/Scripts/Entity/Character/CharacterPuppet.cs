using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static Unity.Netcode.Components.NetworkTransform;

public class CharacterPuppet : MonoBehaviour
{
    private GameObject characterTypeObj;
    private CharacterType characterTypeData;

    public Rigidbody rb;
    public CapsuleCollider characterCollider;

    Vector3 lastReceivedPosition;

    private float smoothThreshold = 0.1f;
    private float smoothAmount = 100f;
    private float snapThreshold = 10f;

    bool isInitialized = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.sleepThreshold = 0.0f;
    }

    private void FixedUpdate()
    {
        if (!isInitialized) return;

        // Smoothly interpolate towards the last received position from the authoritative player, if it's not too far away
        float syncDistance = Vector3.Distance(rb.position, lastReceivedPosition);
        if (syncDistance > smoothThreshold)
        {
            rb.position = Vector3.Lerp(rb.position, lastReceivedPosition, Time.fixedDeltaTime * smoothAmount * syncDistance / snapThreshold);
        }
    }

    private void OnDrawGizmos()
    {
        // Draw Debug Gizmo showing a capsule of the authoritative player's collider for testing and debugging purposes
        if (isInitialized && lastReceivedPosition != Vector3.zero)
        {
            float syncDistance = Vector3.Distance(rb.position, lastReceivedPosition);
            Gizmos.color = syncDistance > snapThreshold ? Color.red : syncDistance > snapThreshold/2f ? Color.yellow : Color.green;
            Gizmos.DrawWireCube(lastReceivedPosition, characterCollider.bounds.size);
        }    
    }

    public void Initialize(Character character)
    {
        // Hide all visuals on authoritative character object
        foreach (Renderer r in character.localCharacterType.gameObject.GetComponentsInChildren<Renderer>())
        {
            r.enabled = false;
        }

        // Disable audio sources
        foreach (AudioSource a in character.localCharacterType.gameObject.GetComponentsInChildren<AudioSource>())
        {
            a.enabled = false;
        }

        // Disable the collider on the authoritative character object so it doesn't interfere with the puppet's collider
        character.localCharacterType.characterCollider.enabled = false;

        uint prefabIdHash = character.localCharacterType.NetworkObject.PrefabIdHash;
        GameObject prefabObj = NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs.First(p => p.SourcePrefabGlobalObjectIdHash == prefabIdHash).Prefab;

        // Set the local character's transform and, and rigidbody references to the puppet's so that the rest of the
        character.localRb = rb;
        character.characterMovement.UpdateCharacterData(null, character.localRb);

        CharacterType characterTypeData = SetCharacterType(prefabObj);
        character.GetComponent<CharacterNetworkTransform>().onNewLocalTransformState.AddListener(OnNewLocalTransformState);
        isInitialized = true;

        character.OnCharacterTypeObjectSpawned(characterTypeData);
    }

    private CharacterType SetCharacterType(GameObject characterTypePrefabObj)
    {
        if (characterTypeObj != null) Destroy(characterTypeObj);
        characterTypeObj = Instantiate(characterTypePrefabObj, transform.position, transform.rotation, transform);
        characterTypeData = characterTypeObj.GetComponent<CharacterType>();
        characterCollider = characterTypeData.characterCollider;
        rb.mass = characterTypeData.mass;

        return characterTypeData;
    }

    private void OnNewLocalTransformState(NetworkTransformState newState)
    {
        if (!newState.HasPositionChange) return;

        Vector3 newPosition = newState.GetPosition();
        float syncDistance = Vector3.Distance(transform.position, newPosition);

        // Desync is either too big or you are teleporting, snap to the new position immediately
        if (syncDistance > snapThreshold || newState.IsTeleportingNextFrame)
        {
            rb.isKinematic = true;
            characterCollider.enabled = false;

            rb.position = newPosition;
            rb.PublishTransform();

            characterCollider.enabled = true;
            rb.isKinematic = false;
        }
        lastReceivedPosition = newPosition;
    }
}
