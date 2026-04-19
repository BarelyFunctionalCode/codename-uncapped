using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CharacterManager : NetworkBehaviour
{
    public static CharacterManager Instance { get; private set; } = null;
    [SerializeField] private GameObject defaultPlayerCharacterTypePrefabObj;
    [SerializeField] private GameObject characterPrefabObj;

    public List<Character> characters = new();


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnDestroy()
    {
        ClearCharacters();
        base.OnDestroy();
        if (Instance == this) Instance = null;
    }

    public void RegisterLocalClient(ulong clientId)
    {
        if (!IsServer) return;
        GetPlayerIdRpc(clientId);
    }
    [Rpc(SendTo.Everyone)]
    private void GetPlayerIdRpc(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            ulong playerId = Player.Instance.playerId;
            RegisterPlayerRpc(playerId, clientId);
        }
    }
    [Rpc(SendTo.Server)]
    private void RegisterPlayerRpc(ulong playerId, ulong clientId)
    {        
        Character localPlayerCharacter = characters.Find(c => c.identification.FetchEntityId() == playerId);
        if (localPlayerCharacter != null)
        {
            localPlayerCharacter.NetworkObject.ChangeOwnership(clientId);
            localPlayerCharacter.Initialize(defaultPlayerCharacterTypePrefabObj, playerId);
        }
        else
        {
            GameObject newCharacterObj = SpawnManager.Instance.Spawn(
                characterPrefabObj,
                false,
                Vector3.zero,
                Quaternion.identity,
                null,
                clientId
            );
            Character newCharacter = newCharacterObj.GetComponent<Character>();
            newCharacter.Initialize(defaultPlayerCharacterTypePrefabObj, playerId);
            characters.Add(newCharacter);
        }
        GameManager.Instance.OnClientConnectedEvent.Invoke(clientId);
    }

    public void HandlePlayerDisconnect(ulong clientId)
    {
        if (!IsServer) return;
        Character character = characters.Find(c => c.OwnerClientId == clientId);
        if (character != null && character.NetworkObject.IsSpawned)
        {
            character.NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);
        }
        GameManager.Instance.OnClientDisconnectedEvent.Invoke(clientId);
    }

    public Character GetCharacterByClientId(ulong clientId)
    {
        return characters.Find(c => c.OwnerClientId == clientId);
    }

    public Character GetCharacterByEntityId(ulong entityId)
    {
        return characters.Find(c => c.identification.FetchEntityId() == entityId);
    }

    public void ClearCharacters()
    {
        foreach (var character in characters)
        {
            if (character != null && character.NetworkObject.IsSpawned)
            {
                Destroy(character.gameObject);
            }
        }
        characters.Clear();
    }
}