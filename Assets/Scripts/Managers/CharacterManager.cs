using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class CharacterManager : NetworkBehaviour
{
    public static CharacterManager Instance { get; private set; } = null;
    [SerializeField] private GameObject defaultPlayerCharacterTypePrefabObj;
    [SerializeField] private GameObject characterPrefabObj;

    private NetworkList<NetworkBehaviourReference> _characters = new();
    public List<Character> characters = new();
    public UnityEvent<ulong> OnCharacterAdded = new();
    public UnityEvent<ulong> OnCharacterRemoved = new();
    public UnityEvent<NetworkBehaviourReference> OnCharacterChangedTeam = new();


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _characters.OnListChanged += OnCharacterListChanged;
        foreach (NetworkBehaviourReference characterRef in _characters) SyncCharacter(characterRef);
    }

    public override void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        OnCharacterChangedTeam.RemoveAllListeners();
        _characters.OnListChanged -= OnCharacterListChanged;
        characters.Clear();

        base.OnDestroy();
    }

    private void OnCharacterListChanged(NetworkListEvent<NetworkBehaviourReference> changeEvent)
    {
        if (changeEvent.Type == NetworkListEvent<NetworkBehaviourReference>.EventType.Add)
        {
            SyncCharacter(changeEvent.Value);
        }
        else if (changeEvent.Type == NetworkListEvent<NetworkBehaviourReference>.EventType.Clear)
        {
            characters.Clear();
        }
    }
    private void SyncCharacter(NetworkBehaviourReference characterRef)
    {
        characterRef.TryGet(out Character character);
        if (character != null && !characters.Contains(character))
        {
            characters.Add(character);
            OnCharacterChangedTeam.Invoke(characterRef);
        }
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
            _characters.Add(newCharacter);
            _characters.SetDirty(true);
        }
    }
    [Rpc(SendTo.Server)]
    public void CompletedPlayerInitializationRpc(ulong characterId)
    {
        OnCharacterAdded.Invoke(characterId);
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

    public void RegisterAI(AI newAI)
    {
        if (!IsServer) return;

        GameObject newCharacterObj = SpawnManager.Instance.Spawn(
            characterPrefabObj,
            null
        );
        Character newCharacter = newCharacterObj.GetComponent<Character>();
        ulong characterId = newCharacter.NetworkObjectId + 1;
        newAI.Add(newCharacter, characterId);
        characters.Add(newCharacter);
        _characters.Add(newCharacter);
        _characters.SetDirty(true);
        newCharacter.Initialize(defaultPlayerCharacterTypePrefabObj, characterId, false);
    }
    public void CompletedAIInitialization(ulong characterId)
    {
        OnCharacterAdded.Invoke(characterId);
    }

    public bool IsLocalPlayerCharacter(NetworkObjectReference objRef)
    {
        if (!objRef.TryGet(out NetworkObject netObj)) return false;
        if (!netObj.TryGetComponent(out Character character)) return false;
        return IsLocalPlayerCharacter(character);
    }

    public bool IsLocalPlayerCharacter(Character character)
    {
        return character != null &&
                character.IsPlayerCharacter &&
                Player.Instance.Character.identification.FetchEntityId() == character.identification.FetchEntityId();
    }

    public Character GetCharacterByClientId(ulong clientId)
    {
        return characters.Find(c => c.OwnerClientId == clientId);
    }

    public Character GetCharacterByEntityId(ulong entityId)
    {
        return characters.Find(c => c.identification.FetchEntityId() == entityId);
    }

    public List<NetworkBehaviourReference> GetCharactersByTeamId(int teamId)
    {
        List<NetworkBehaviourReference> charactersOnTeam = new();
        foreach (Character character in characters)
        {
            if (character.identification.FetchTeamId() == teamId)
            {
                charactersOnTeam.Add(new NetworkBehaviourReference(character));
            }
        }
        return charactersOnTeam;
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
        _characters.Clear();
        _characters.SetDirty(true);
    }
}