# Network Message Types Reference

> Agent-first documentation. Complete reference for all network message types.

## Message Interfaces

### UDP Messages (`IGameNetworkMessage`)

**File:** `src/common/Messages/GameNetworkMessages/GameNetworkMessage.cs`

```csharp
[MemoryPackable]
[MemoryPackUnion(0, typeof(LobbyUpdates))]
[MemoryPackUnion(1, typeof(ClientInGameReady))]
// ... 66 total union types
public partial interface IGameNetworkMessage { }
```

**Critical:** Never change existing union IDs - breaks network compatibility.

### WebSocket Messages (`IWsMessage`)

**File:** `src/common/Messages/WsMessages/IWsMessage.cs`

```csharp
[MemoryPackable]
[MemoryPackUnion(0u, typeof(MatchInfo))]
// ...
public partial interface IWsMessage { }
```

---

## UDP Messages by Category

### Player

| Message | ID | Direction | Fields | Purpose |
|---------|----|-----------|--------|---------|
| `PlayerUpdate` | 3 | C→H, H→All | ConnectionId, Position, Rotation, Hp, MaxHp, Shield, MaxShield, Inventory, AnimatorState | Full player state |
| `PlayerDied` | - | H→All | ConnectionId | Player death notification |
| `PlayerRespawned` | - | H→All | PlayerUpdate | Player respawn with state |
| `PlayerDisconnected` | - | H→All | ConnectionId | Player left game |
| `SelectedCharacter` | 5 | C→H | ConnectionId, Character | Character selection |
| `HatChanged` | - | C→H, H→All | ConnectionId, Hat | Hat cosmetic change |
| `Introduced` | - | C→H | ConnectionId, PlayerName, Character | New player joining |

### Enemy

| Message | Direction | Fields | Purpose |
|---------|-----------|--------|---------|
| `SpawnedEnemy` | H→All | Id, TargetId, Name, Position, Hp, Flag, CanBeElite, Wave | Enemy spawn |
| `EnemyDied` | H→All | Id, OwnerId | Enemy killed by player |
| `EnemyDamaged` | H→All | Id, Damage | Damage notification |
| `EnemyExploder` | H→All | Id | Exploding enemy |
| `RetargetedEnemies` | H→All | EnemyIds[], TargetId | Bulk retarget |
| `SpawnedEnemySpecialAttack` | H→All | EnemyId, AttackId | Special attack spawn |

### Projectile (Weapon-Specific)

| Message | Weapon | Special Fields |
|---------|--------|----------------|
| `SpawnedProjectile` | Default | Position, Rotation, Id |
| `SpawnedRevolverProjectile` | Revolver | MuzzlePosition |
| `SpawnedShotgunProjectile` | Shotgun | MuzzlePosition |
| `SpawnedSniperProjectile` | Sniper | MuzzlePosition |
| `SpawnedAxeProjectile` | Axe | StartPosition, DesiredPosition |
| `SpawnedBlackHoleProjectile` | Black Hole | StartPosition, DesiredPosition, Scale |
| `SpawnedRocketProjectile` | Rocket | Position, Rotation |
| `SpawnedHeroSwordProjectile` | Hero Sword | MovingProjectilePosition |
| `SpawnedCringeSwordProjectile` | Cringe Sword | MovingProjectilePosition |
| `SpawnedDexecutionerProjectile` | Dexecutioner | Direction, ExecutionChance |
| `SpawnedFireFieldProjectile` | Flamewalker | ExpirationTime |
| `ProjectilesUpdate` | All | Projectiles[] (delta updates) |
| `ProjectileDone` | All | Id | Projectile destroyed |

### Pickup

| Message | Direction | Fields | Purpose |
|---------|-----------|--------|---------|
| `SpawnedPickup` | H→All | Id, Name, Position | Generic pickup spawn |
| `SpawnedPickupOrb` | H→All | Id, Position | XP orb spawn |
| `PickupApplied` | C→H, H→All | Id, OwnerId | Pickup collected |
| `PickupFollowingPlayer` | H→All | Id, OwnerId | Pickup moving to player |
| `WantToStartFollowingPickup` | C→H | Id, OwnerId | Request to claim pickup |

### Chest

| Message | Direction | Fields | Purpose |
|---------|-----------|--------|---------|
| `SpawnedChest` | H→All | Id, Position | Chest spawn |
| `ChestOpened` | H→All | Id, OwnerId | Chest opened notification |
| `RequestChestOpen` | C→H | ChestId, PlayerId | Request to open chest |
| `GrantChestOpen` | H→C | ChestId, Granted | Grant/deny chest open |

### Interactables

| Message | Purpose |
|---------|---------|
| `InteractableUsed` | Generic interactable usage |
| `SpawnedReviver` | Reviver coffin spawn |
| `InteractableCharacterFightEnemySpawned` | Fight enemy spawn (Shady Guy, etc.) |

### Shrines/Charging

| Message | Type |
|---------|------|
| `StartingChargingShrine` | Standard shrine |
| `StoppingChargingShrine` | Standard shrine |
| `StartingChargingPylon` | Pylon |
| `StoppingChargingPylon` | Pylon |
| `StartingChargingLamp` | Boss lamp |
| `StoppingChargingLamp` | Boss lamp |

### Boss

| Message | Purpose |
|---------|---------|
| `FinalBossOrbSpawned` | Boss orb spawn |
| `FinalBossOrbDestroyed` | Boss orb destroyed |

### World/Level

| Message | Purpose |
|---------|---------|
| `SpawnedObject` | Generic object spawn |
| `SpawnedObjectInCrypt` | Crypt object spawn |
| `RunStarted` | Game run started |
| `GameOver` | Game ended |
| `LobbyUpdates` | Full world state snapshot |
| `ClientInGameReady` | Client ready confirmation |

### Weather

| Message | Purpose |
|---------|---------|
| `StormStarted` | Desert storm start |
| `StormStopped` | Desert storm end |
| `TornadoesSpawned` | Tornado spawn |
| `TumbleWeedSpawned` | Tumbleweed spawn |
| `TumbleWeedsUpdate` | Tumbleweed positions |
| `TumbleWeedDespawned` | Tumbleweed destroyed |

### Inventory

| Message | Purpose |
|---------|---------|
| `ItemAdded` | Item added to inventory |
| `ItemRemoved` | Item removed from inventory |
| `WeaponAdded` | Weapon added |
| `WeaponToggled` | Weapon enabled/disabled |
| `TomeAdded` | Tome added |

### Game Events

| Message | Purpose |
|---------|---------|
| `TimerStarted` | Timer started |
| `StartedSwarmEvent` | Swarm event started |
| `LightningStrike` | Lightning strike |

---

## WebSocket Messages

### Server → Client

| Message | Purpose |
|---------|---------|
| `MatchInfo` | Lobby assignment with host endpoint |
| `GameStarting` | Game about to start |
| `ServerConnectionStatus` | Connection state update |
| `ClientDisconnected` | Another player left |
| `HostDisconnected` | Host left |

**MatchInfo Fields:**
```csharp
uint ConnectionId     // This client's ID
bool IsHost           // Is this client the host?
string RoomCode       // Room code for friendlies
string HostInternalEP // Host internal endpoint
string HostExternalEP // Host external endpoint
```

### Client → Server

| Message | Purpose |
|---------|---------|
| `GameStartingResponse` | Ready confirmation |
| `RunStatistics` | End-game stats |

**RunStatistics Fields:**
```csharp
int Kills
int Money
float Time
int Level
```

---

## Relay Protocol

### RelayEnvelope

**File:** `src/common/Messages/RelayEnveloppe.cs`

```csharp
[MemoryPackable]
public partial class RelayEnvelope
{
    public bool HaveTarget { get; set; }
    public uint TargetConnectionId { get; set; }
    public uint[] ToFilters { get; set; }  // Exclude from broadcast
    public byte[] Payload { get; set; }
}
```

### Relay Signals

| Signal | Direction | Purpose |
|--------|-----------|---------|
| `RELAY_BIND` | C→S | Client ready for relay traffic |
| `USE_RELAY` | S→C | Switch to relay mode |

---

## LobbyUpdates (Full Snapshot)

**File:** `src/common/Messages/GameNetworkMessages/LobbyUpdates.cs`

Most important message - contains full world state:

```csharp
[MemoryPackable]
public partial class LobbyUpdates : IGameNetworkMessage
{
    public uint[] ConnectionIds { get; set; }
    public uint[] EnemyIds { get; set; }
    public QuantizedVector3[] EnemyPositions { get; set; }
    public float[] EnemyYaws { get; set; }
    public uint[] EnemyTargetIds { get; set; }
    public uint[] EnemyHps { get; set; }
    public List<ProjectileSnapshot> Projectiles { get; set; }
    public List<BossOrbSnapshot> BossOrbs { get; set; }
    // ...
}
```

---

## Adding a New Message

1. **Create message class** in `src/common/Messages/GameNetworkMessages/`:
```csharp
[MemoryPackable]
public partial class MyNewMessage : IGameNetworkMessage
{
    public uint Id { get; set; }
    public string Data { get; set; }
}
```

2. **Register union** in `GameNetworkMessage.cs`:
```csharp
[MemoryPackUnion(67, typeof(MyNewMessage))]  // Use next available ID
```

3. **Add handler** in `SynchronizationService.cs`:
```csharp
EventManager.Subscribe<MyNewMessage>(OnReceivedMyNewMessage);

private void OnReceivedMyNewMessage(MyNewMessage msg)
{
    // Handle message
}
```

4. **Send message** via `UdpClientService`:
```csharp
udpClientService.SendToAllClients(new MyNewMessage { ... });
```

---

## Delivery Methods

| Method | Reliability | Order | Use Case |
|--------|-------------|-------|----------|
| `ReliableUnordered` | Guaranteed | No | State changes, spawns |
| `ReliableOrdered` | Guaranteed | Yes | Important events, chat |
| `Unreliable` | Best effort | No | Position updates |

```csharp
udpClientService.SendToAllClients(message, DeliveryMethod.ReliableUnordered);
udpClientService.SendToHost(message, DeliveryMethod.Unreliable);
```
