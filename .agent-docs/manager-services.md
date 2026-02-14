# Manager Services Reference

> Agent-first documentation. Complete reference for all manager services in the plugin.

## Service Layer Overview

All services are singletons registered via DI in `Plugin.cs`:

**File:** `src/plugin/Plugin.cs:130-153`

```csharp
builder.ConfigureServices(services =>
{
    services.AddSingleton<IUdpClientService, UdpClientService>();
    services.AddSingleton<ISynchronizationService, SynchronizationService>();
    // ...
});
```

### Accessing Services

```csharp
// From any plugin code
var sync = Plugin.Services.GetRequiredService<ISynchronizationService>();
var playerMgr = Plugin.Services.GetRequiredService<IPlayerManagerService>();
```

---

## Core Networking Services

### SynchronizationService

**Interface:** `ISynchronizationService`  
**File:** `src/plugin/Services/SynchronizationService.cs`  
**Lines:** ~2000+

**Purpose:** Main sync orchestrator, state machine, message routing

**Key Methods:**
| Method | Purpose |
|--------|---------|
| `HasNetplaySessionStarted()` | Check if in active netplay |
| `OnSpawnedEnemy()` | Handle enemy spawn (host) |
| `OnEnemyDied()` | Handle enemy death |
| `OnPlayerDied()` | Handle player death |
| `OnRespawn()` | Handle player respawn |

**State Machine:**
```
None → Loading → Ready → Started → LoadingNextLevel → Endgame → GameOver
```

**State Transitions:**
- `GameEvent.Loading` → `State.Loading`
- `GameEvent.Ready` → `State.Ready`
- `GameEvent.Start` → `State.Started` (if Ready and LobbyReady)
- `GameEvent.PortalOpened` → `State.LoadingNextLevel`
- `GameEvent.FinalPortalOpened` → `State.Endgame`
- `GameEvent.GameOver` → `State.GameOver`

---

### UdpClientService

**Interface:** `IUdpClientService`  
**File:** `src/plugin/Services/UdpClientService.cs`  
**Lines:** ~800+

**Purpose:** UDP networking, NAT punchthrough, relay handling

**Key Methods:**
| Method | Purpose |
|--------|---------|
| `IsHost()` | Check if this client is host |
| `SendToAllClients()` | Broadcast to all clients |
| `SendToHost()` | Send to host only |
| `UpdateLocalPlayer()` | Capture local player state |
| `StartClient()` | Begin UDP connection |
| `StartHost()` | Begin hosting |

**Events:**
- `OnPeerConnected` - New peer joined
- `OnPeerDisconnected` - Peer left

---

### WebsocketClientService

**Interface:** `IWebsocketClientService`  
**File:** `src/plugin/Services/WebsocketClientService.cs`

**Purpose:** Matchmaking server connection

**Key Methods:**
| Method | Purpose |
|--------|---------|
| `ConnectRandom()` | Join quickplay queue |
| `ConnectFriendlies()` | Join/create private lobby |
| `Disconnect()` | Leave matchmaking |

---

## Entity Manager Services

### PlayerManagerService

**Interface:** `IPlayerManagerService`  
**File:** `src/plugin/Services/PlayerManagerService.cs`  
**Lines:** ~400+

**Purpose:** Player tracking, NetPlayer spawning

**Key Data Structures:**
```csharp
ConcurrentDictionary<uint, NetPlayer> spawnedPlayers;  // netplayId → NetPlayer
ConcurrentDictionary<PlayerHealth, uint> playerHealths; // PlayerHealth → netplayId
```

**Key Methods:**
| Method | Purpose |
|--------|---------|
| `GetNetPlayer(netplayId)` | Get remote player by ID |
| `IsRemotePlayerHealth()` | Check if health belongs to remote |
| `GetLocalPlayerUpdate()` | Build PlayerUpdate for local |
| `PeakNetplayerPositionRequest()` | Get pending position request |
| `SpawnNetPlayer()` | Create remote player GameObject |

---

### EnemyManagerService

**Interface:** `IEnemyManagerService`  
**File:** `src/plugin/Services/EnemyManagerService.cs`

**Purpose:** Enemy ID mapping, delta compression

**Key Data Structures:**
```csharp
ConcurrentDictionary<uint, Enemy> spawnedEnemies;          // netplayId → Enemy
ConcurrentDictionary<Enemy, uint> spawnedEnemiesByEnemy;   // Enemy → netplayId
Dictionary<uint, EnemyModel> previousSpawnedEnemiesDelta;  // For delta checks
```

**Key Methods:**
| Method | Purpose |
|--------|---------|
| `AddSpawnedEnemy()` | Register new enemy, return ID |
| `GetSpawnedEnemy(id)` | Get enemy by netplay ID |
| `GetSpawnedEnemyId(enemy)` | Get ID for enemy |
| `GetAllEnemiesDeltaAndUpdate()` | Get changed enemies only |

**Delta Thresholds:**
| Check | Threshold |
|-------|-----------|
| Position diff | 0.1f |
| Yaw diff | 5.0f |
| HP diff | 1 |

---

### ProjectileManagerService

**Interface:** `IProjectileManagerService`  
**File:** `src/plugin/Services/ProjectileManagerService.cs`

**Purpose:** Projectile tracking, snapshots

**Key Data Structures:**
```csharp
ConcurrentDictionary<uint, ProjectileBase> spawnedProjectiles;
List<ProjectileSnapshot> projectileSnapshots;
Dictionary<uint, ProjectileSnapshot> previousProjectilesDelta;
```

**Key Methods:**
| Method | Purpose |
|--------|---------|
| `AddSpawnedProjectile()` | Register projectile |
| `RemoveProjectile(id)` | Remove projectile |
| `GetProjectileSnapshots()` | Get all snapshots |
| `GetProjectilesDelta()` | Get changed projectiles |

---

### PickupManagerService

**Interface:** `IPickupManagerService`  
**File:** `src/plugin/Services/PickupManagerService.cs`

**Purpose:** Pickup/orb tracking, ownership

**Key Data Structures:**
```csharp
ConcurrentDictionary<uint, Pickup> spawnedPickups;        // Server-side ID map
ConcurrentDictionary<uint, Pickup> clientSpawnedPickups;  // Client-side map
uint currentPickupId;  // ID generator (needs thread-safety)
```

**Key Methods:**
| Method | Purpose |
|--------|---------|
| `AddSpawnedPickup()` | Register pickup, return ID |
| `GetPickup(id)` | Get pickup by ID |
| `SetOwner(id, ownerId)` | Set pickup ownership |

---

### ChestManagerService

**Interface:** `IChestManagerService`  
**File:** `src/plugin/Services/ChestManagerService.cs`

**Purpose:** Chest claim protocol

**Key Data Structures:**
```csharp
ConcurrentDictionary<uint, Chest> spawnedChests;          // netplayId → Chest
ConcurrentDictionary<uint, uint> claimedChests;           // chestId → playerId
ConcurrentDictionary<uint, bool> pendingChestClaims;      // chestId → pending
```

**Key Methods:**
| Method | Purpose |
|--------|---------|
| `TryClaimChest(chestId, playerId)` | Atomic chest claim |
| `IsChestGranted(chestId, playerId)` | Check if claim granted |
| `SetPending(chestId, pending)` | Mark claim pending |

---

### FinalBossOrbManagerService

**Interface:** `IFinalBossOrbManagerService`  
**File:** `src/plugin/Services/FinalBossOrbManagerService.cs`

**Purpose:** Boss orb tracking

**Key Data Structures:**
```csharp
ConcurrentDictionary<uint, Orb> spawnedBossOrbs;
Dictionary<uint, BossOrbSnapshot> previousBossOrbsDelta;
```

---

### SpawnedObjectManagerService

**Interface:** `ISpawnedObjectManagerService`  
**File:** `src/plugin/Services/SpawnedObjectManagerService.cs`

**Purpose:** Generic object tracking (interactables, etc.)

---

## Game Logic Services

### GameBalanceService

**Interface:** `IGameBalanceService`  
**File:** `src/plugin/Services/GameBalanceService.cs`

**Purpose:** Multiplayer difficulty scaling

**Scaling Table:**
| Setting | Solo | 2-4 Players | 5-6 Players |
|---------|------|-------------|-------------|
| Credit Timer | 1x | +50-100% | +150% |
| Free Chest Rate | 1x | +50-100% | +150% |
| XP Multiplier | 1x | 2x | 3x |
| Boss Lamp Charge | 3s | 6s | 12s |
| Max Enemies | 400 | 500 | 600 |

**Key Methods:**
| Method | Purpose |
|--------|---------|
| `GetCreditTimerMultiplier()` | Enemy spawn rate |
| `GetXpMultiplier()` | XP reward scaling |
| `GetMaxEnemies()` | Enemy cap |

---

### AutoUpdaterService

**File:** `src/plugin/Services/AutoUpdaterService.cs`

**Purpose:** Check for mod updates

**Disabled on:** Linux native builds

---

## Server Services

### WebSocketHandler

**File:** `src/server/Services/WebSocketHandler.cs`

**Purpose:** Matchmaking WebSocket endpoint

**Endpoints:**
- `/ws?random` - Quickplay
- `/ws?friendlies` - Private lobbies

---

### RendezVousServer

**File:** `src/server/Services/RendezVousServer.cs`  
**Lines:** ~770

**Purpose:** NAT punchthrough, relay management

**Key Structures:**
```csharp
public class RelaySession
{
    public uint HostConnectionId;
    public RelayPeer Host;
    public ConcurrentDictionary<uint, RelayPeer> Clients;
    public ConcurrentQueue<PendingRelayMessage> PendingToHost;
}
```

**Cleanup Intervals:**
| Resource | Retention |
|----------|-----------|
| Registered Hosts | 1 minute |
| Pending Clients | 45 seconds |
| Processed Pairs | 45 seconds |
| Pending Relay | 30 seconds |

---

## Service Dependencies

```
SynchronizationService
    ├── IUdpClientService
    ├── IWebsocketClientService
    ├── IPlayerManagerService
    ├── IEnemyManagerService
    ├── IProjectileManagerService
    ├── IPickupManagerService
    ├── IChestManagerService
    ├── IFinalBossOrbManagerService
    ├── ISpawnedObjectManagerService
    └── IGameBalanceService
```

---

## Common Patterns

### Check Netplay Before Processing

```csharp
[HarmonyPostfix]
public static void SomePatch(...)
{
    if (!synchronizationService.HasNetplaySessionStarted()) return;
    // Process patch
}
```

### Check Host vs Client

```csharp
if (udpClientService.IsHost())
{
    // Host-only logic
}
else
{
    // Client-only logic
}
```

### Get Service from Patch

```csharp
// At class level
private static readonly Lazy<ISynchronizationService> syncService = 
    new(() => Plugin.Services.GetRequiredService<ISynchronizationService>());

// Usage
var sync = syncService.Value;
```
