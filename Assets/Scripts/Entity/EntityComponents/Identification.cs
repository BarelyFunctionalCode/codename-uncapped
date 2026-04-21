using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Identification : EntityComponent
{
    [SerializeField] protected NetworkVariable<ulong> _entityId = new(0);
    [SerializeField] protected NetworkVariable<FixedString32Bytes> _entityName = new("");
    [SerializeField] protected NetworkVariable<int> _teamId = new(-1);

    public ulong EntityId => _entityId.Value;
    public string EntityName => _entityName.Value.ToString();
    public int TeamId { get { return _teamId.Value; } set { _teamId.Value = value; } }


    public override void Initialize(Entity entity)
    {
        base.Initialize(entity);

        _teamId.OnValueChanged += OnTeamIdChanged;

        if (!IsServer) return;
        if (_entityId.Value == 0)
            _entityId.Value = NetworkObjectId + 5000; // Arbitrary offset to avoid conflicts with player IDs, which are based on client IDs
    }

    public override void Deinitialize()
    {
        base.Deinitialize();

        _teamId.OnValueChanged -= OnTeamIdChanged;
    }

    public ulong FetchEntityId() => EntityId;
    public string FetchEntityName() => EntityName;
    public int FetchTeamId() => TeamId;
    public void SetEntityName(string s) => _entityName.Value = s;
    public void SetEntityId(ulong id) => _entityId.Value = id;
    public void SetTeamId(int id) => _teamId.Value = id;

    private void OnTeamIdChanged(int oldTeamId, int newTeamId)
    {
        CharacterManager.Instance.OnCharacterChangedTeam.Invoke(new NetworkBehaviourReference(this));
    }
}
