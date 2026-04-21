using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Character))]
[RequireComponent(typeof(Identification))]
public class CharacterState : State
{
    private Character character;
    private CharacterInputs characterInputs;
    [SerializeField] private AudioSource respawnAudioSource;

    private void Awake()
    {
        base.Initialize(entity);
        
        character = GetComponent<Character>();
        characterInputs = character.characterInputs;
    }

    protected override void OnDie()
    {
        if (!IsServer) return;

        characterInputs.SetCharacterControlsRpc(false);
        character.localRb.isKinematic = true;
        character.localCharacterType.characterCollider.enabled = false;
        OnDieRpc(character.localRb.linearVelocity);
    }
    [Rpc(SendTo.Everyone)]
    private void OnDieRpc(Vector3 inheritedVelocity)
    {
        if (IsOwner)
        {
            character.localRb.isKinematic = true;
            character.localCharacterType.characterCollider.enabled = false;
        }
        character.localCharacterType.OnDie(inheritedVelocity);
    }

    protected override void OnRespawn()
    {
        if (!IsServer) return;

        Transform respawnPoint = LevelManager.Instance.GetSpawnPoint(entity.identification.FetchTeamId());

        if (respawnPoint) character.Teleport(respawnPoint.position, respawnPoint.rotation);
        else character.Teleport(Vector3.zero, Quaternion.identity);

        character.localCharacterType.characterCollider.enabled = true;
        character.localRb.isKinematic = false;
        character.characterLoadout.Deinitialize();
        character.characterLoadout.Initialize();

        OnRespawnRpc();
        characterInputs.SetCharacterControlsRpc(true);
    }
    [Rpc(SendTo.Everyone)]
    private void OnRespawnRpc()
    {
        if (IsOwner)
        {
            character.localRb.isKinematic = false;
            character.localCharacterType.characterCollider.enabled = true;
            respawnAudioSource.Play();
        }
        character.localCharacterType.OnRespawn();
    }
}
