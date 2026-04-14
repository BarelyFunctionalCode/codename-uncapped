using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Identification : EntityComponent
{
    [SerializeField] protected NetworkVariable<ulong> _entityId = new(0);
    [SerializeField] protected NetworkVariable<FixedString32Bytes> _entityName = new("");
    [SerializeField] protected NetworkVariable<uint> _teamId = new(0);

    public ulong EntityId => _entityId.Value;
    public string EntityName => _entityName.Value.ToString();
    public uint TeamId { get { return _teamId.Value; } set { _teamId.Value = value; } }


    public override void Initialize(Entity entity)
    {
        base.Initialize(entity);

        if (!IsServer) return;
        if (_entityId.Value == 0)
            _entityId.Value = NetworkObjectId + 5000; // Arbitrary offset to avoid conflicts with player IDs, which are based on client IDs
    }

    public ulong FetchEntityId() => EntityId;
    public string FetchEntityName() => EntityName;
    public uint FetchTeamId() => TeamId;
    public void SetEntityName(string s) => _entityName.Value = s;
    public void SetEntityId(ulong id) => _entityId.Value = id;
    public void SetTeamId(uint id) => _teamId.Value = id;
}
