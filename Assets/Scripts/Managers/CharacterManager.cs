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
    public UnityEvent<NetworkBehaviourReference> OnCharacterChangedTeam = new();


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _characters.OnListChanged += OnCharacterListChanged;
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
            changeEvent.Value.TryGet(out Character character);
            if (character != null && !characters.Contains(character))
            {
                characters.Add(character);
                OnCharacterChangedTeam.Invoke(changeEvent.Value);
            }
        }
        else if (changeEvent.Type == NetworkListEvent<NetworkBehaviourReference>.EventType.Remove)
        {
            changeEvent.Value.TryGet(out Character character);
            if (character != null)
            {
                characters.Remove(character);
            }
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
        }
    }
    [Rpc(SendTo.Server)]
    public void CompletedPlayerInitializationRpc(ulong clientId)
    {
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
    }
}