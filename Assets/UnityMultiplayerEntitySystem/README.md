Work in progress, trying to untangle all of the generic networking logic from a game project to live as its own package.

Certain scripts may be broken due to dependencies that were not copied from the actual game I am working on.


## GameManager

- Non-Network-Behaviour
- Manages your local client
- Manages lobby if you are the lobby host
- Currently keeps track of data related to the currently selected heist, may get moved to its own manager script. Heist data only accessible on the Host side.

## PlayerManager

- Network-Behaviour
- Manages lobby players and their data

### Notes

- See `PlayerDataUpdateType` enum in `PlayerManager.cs` to see types of updates that are tracked.
- Any variable that needs to be tracked in the `PlayerManager` needs to be added to the `Player.cs` `NetworkSerialize` function, or it's child components (Like `PlayerStats.NetworkSerialize`).
- To see an example of the `BroadcastUpdate` function being used, look at the Property definitions in `PlayerStats.cs`

### Useful Functions

- `Player? GetPlayerData(ulong clientId)` | Given a client ID, returns that `Player` class instance.
- `List<Player> GetPlayerList()` | Returns a list of all the players in the lobby.
- `void ClientAddListener(UnityAction<PlayerDataUpdateType, ulong, Player> listener)` | When a variable that is tracked by the `PlayerManager` is updated on a player, the player's data and "type of data that was updated" is sent to the given event listener function. Useful for updating UI that needs to be synced up with player data.
- `void BroadcastUpdate<T>(ulong playerClientId, PlayerDataUpdateType updateType, ref T currentValue, T newValue)` | Used to tell the `PlayerManager` that a variable on a player has been updated, which then broadcasts the update to all event listeners.

## SuscpicionManager

- Network-Behaviour
- Manages player/entity detection.
- Attached to anything that is meant to "see" or "hear".

### Notes

- This is a prefab, and you should use the prefab for attaching to objects.
- Use the `chainedManager` variable to directly link multiple managers together (e.g. a SecurityCamera being linked to a SecurityStation, forwarding any Alerts to the station).

### Useful Functions

- `void Update*` | Various functions used to tweak the defection parameters to meet the needs of the Entity it's attached to.
- `Alert(Vector3? position = null, float cooldownMultiplier = 1.0f)` | Provide a position that the `SuspicionManager` will react to. Can be used to override the normal detection methods (See `Radio.cs` for example).