using Unity.Netcode.Components;
using UnityEngine;
using static Unity.Netcode.Components.NetworkTransform;

public class CharacterPuppet : MonoBehaviour
{
    private GameObject characterTypeObj;
    private CharacterType characterTypeData;

    public Transform cameraLookAtTarget;
    public Transform weaponMountPoint;
    public Transform throwableMountPoint;
    public Animator characterAnimator;
    public AudioSource hoverAudioSource;
    public AudioSource windAudioSource;
    public Rigidbody rb;
    public CapsuleCollider characterCollider;
    private Character character;

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
        if (character != null)
        {
            // Smoothly interpolate towards the last received position from the authoritative player, if it's not too far away
            float syncDistance = Vector3.Distance(rb.position, lastReceivedPosition);
            if (syncDistance > smoothThreshold)
            {
                rb.position = Vector3.Lerp(rb.position, lastReceivedPosition, Time.fixedDeltaTime * smoothAmount * syncDistance / snapThreshold);
            }
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
        Debug.Log("Initializing CharacterPuppet for character: " + character.name);
        this.character = character;
        CharacterType characterTypeData = SetCharacterType(character.characterTypePrefabObj);
        character.GetComponent<CharacterNetworkTransform>().onNewLocalTransformState.AddListener(OnNewLocalTransformState);
        isInitialized = true;

        character.OnCharacterTypeObjectSpawned(characterTypeData);
        Debug.Log("CharacterPuppet initialization complete for character: " + character.name);
    }

    private CharacterType SetCharacterType(GameObject characterTypePrefabObj)
    {
        if (characterTypeObj != null) Destroy(characterTypeObj);
        characterTypeObj = Instantiate(characterTypePrefabObj, transform.position, transform.rotation, transform);
        characterTypeData = characterTypeObj.GetComponent<CharacterType>();
        characterCollider = characterTypeData.characterCollider;
        characterAnimator = characterTypeData.characterAnimator;
        cameraLookAtTarget = characterTypeData.cameraLookAtTarget;
        weaponMountPoint = characterTypeData.weaponMountPoint;
        throwableMountPoint = characterTypeData.throwableMountPoint;
        hoverAudioSource = characterTypeData.hoverAudioSource;
        windAudioSource = characterTypeData.windAudioSource;
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
