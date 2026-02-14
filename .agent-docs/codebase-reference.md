# Codebase Reference

> Agent-first documentation. Granular file and directory reference for navigating the codebase.

## Directory Structure

```
src/
├── common/                          # Shared library (plugin ↔ server)
│   ├── Extensions/
│   │   └── WebSocketsExtensions.cs  # WebSocket helper methods
│   ├── Messages/
│   │   ├── GameNetworkMessages/     # UDP message types (66 total)
│   │   │   ├── GameNetworkMessage.cs    # Interface + union definitions
│   │   │   ├── PlayerUpdate.cs           # Player state broadcast
│   │   │   ├── LobbyUpdates.cs           # Full world state snapshot
│   │   │   ├── SpawnedEnemy.cs           # Enemy spawn notification
│   │   │   ├── EnemyDied.cs              # Enemy death notification
│   │   │   ├── SpawnedProjectile.cs      # Projectile spawn (base + variants)
│   │   │   ├── ItemAdded.cs              # Inventory item added
│   │   │   └── ... (60+ more message types)
│   │   ├── WsMessages/              # WebSocket message types
│   │   │   ├── IWsMessage.cs            # Interface
│   │   │   ├── MatchInfo.cs             # Lobby match information
│   │   │   ├── GameStarting.cs          # Game start notification
│   │   │   └── ...
│   │   └── RelayEnveloppe.cs        # Relay packet wrapper
│   └── Models/
│       ├── Player.cs                # Player data model
│       ├── EnemyModel.cs            # Enemy state model
│       ├── Projectile.cs            # Projectile state model
│       ├── QuantizedVector3.cs      # Compressed vector
│       └── ...
│
├── plugin/                          # Game client mod
│   ├── Plugin.cs                    # ★ Entry point
│   ├── Configuration/
│   │   └── ModConfig.cs             # BepInEx config bindings
│   ├── Services/                    # ★ Core services (DI)
│   │   ├── SynchronizationService.cs    # ★ Main sync orchestrator
│   │   ├── UdpClientService.cs          # UDP network handling
│   │   ├── WebsocketClientService.cs    # Matchmaking connection
│   │   ├── PlayerManagerService.cs      # Player tracking
│   │   ├── EnemyManagerService.cs       # Enemy tracking
│   │   ├── ProjectileManagerService.cs  # Projectile tracking
│   │   ├── PickupManagerService.cs      # Pickup/orb tracking
│   │   ├── ChestManagerService.cs       # Chest claim protocol
│   │   ├── GameBalanceService.cs        # Multiplayer balance adjustments
│   │   └── ...
│   ├── Patches/                     # ★ Harmony patches by domain
│   │   ├── Player/
│   │   │   ├── MyPlayer.cs              # Player controller patches
│   │   │   └── PlayerRenderer.cs        # Player rendering patches
│   │   ├── PlayerHealth.cs              # Health/damage patches
│   │   ├── PlayerMovement.cs            # Movement patches
│   │   ├── Enemies/
│   │   │   ├── Enemy.cs                 # ★ Core enemy patches
│   │   │   ├── EnemyManager.cs          # Enemy spawning patches
│   │   │   ├── EnemyMovementRb.cs       # Enemy movement patches
│   │   │   └── EnemyStats.cs            # Enemy stat patches
│   │   ├── Projectiles/
│   │   │   ├── ProjectileBase.cs        # Base projectile patches
│   │   │   ├── Rocket.cs                # Rocket-specific patches
│   │   │   ├── ProjectileAxe.cs         # Axe-specific patches
│   │   │   └── ... (weapon-specific)
│   │   ├── Inventories/
│   │   │   ├── PlayerInventory.cs       # Player inventory patches
│   │   │   ├── WeaponInventory.cs       # Weapon inventory patches
│   │   │   └── ItemInventory.cs         # Item inventory patches
│   │   ├── Interactables/
│   │   │   ├── InteractableCoffin.cs    # Reviver coffin patches
│   │   │   ├── InteractableShadyGuy.cs  # Shady guy shop patches
│   │   │   └── ...
│   │   ├── BossOrb/
│   │   │   ├── BossOrbBleed.cs          # Bleed orb patches
│   │   │   ├── BossOrbFollowing.cs      # Following orb patches
│   │   │   └── BossOrbShooty.cs         # Shooting orb patches
│   │   ├── MapGeneration/
│   │   │   ├── MapGenerationController.cs
│   │   │   ├── MapGenerator.cs
│   │   │   └── ...
│   │   ├── Items/
│   │   │   └── ItemGhost.cs             # Ghost item fix
│   │   ├── ConstantAttacks/             # Aura/dragonsbreath/etc
│   │   ├── SpecialAttack/               # Enemy special attacks
│   │   ├── Summoner/                    # Enemy summoner patches
│   │   ├── Unity/                       # Unity engine patches
│   │   │   ├── UnityComponent.cs
│   │   │   ├── UnityObject.cs
│   │   │   └── UnityLocalizedString.cs
│   │   └── ... (40+ more patch files)
│   ├── Scripts/                     # Custom Unity components
│   │   ├── NetPlayer/
│   │   │   ├── NetPlayer.cs             # ★ Remote player representation
│   │   │   ├── NetPlayerCard.cs         # HUD player card
│   │   │   ├── CustomInventoryHud.cs    # Inventory HUD
│   │   │   └── DisplayBar.cs            # Health/shield bars
│   │   ├── Snapshot/
│   │   │   ├── Snapshot.cs              # Snapshot data structure
│   │   │   ├── PlayerInterpolator.cs    # Player interpolation
│   │   │   ├── EnemyInterpolator.cs     # Enemy interpolation
│   │   │   ├── ProjectileInterpolator.cs
│   │   │   └── ...
│   │   ├── Modal/
│   │   │   ├── ModalBase.cs             # Base modal class
│   │   │   ├── NetworkMenuTab.cs        # Network menu UI
│   │   │   ├── LoadingModal.cs          # Loading overlay
│   │   │   └── ...
│   │   ├── Button/
│   │   │   ├── PlayTogetherButton.cs    # Main menu button
│   │   │   └── CustomButton.cs
│   │   ├── CameraSwitcher.cs            # Death cam switching
│   │   ├── NetworkHandler.cs            # Network state machine
│   │   ├── MainThreadDispatcher.cs      # Unity main thread dispatch
│   │   └── ...
│   ├── Helpers/
│   │   ├── Helper.cs                    # ★ General utilities
│   │   ├── Quantizer.cs                 # Vector quantization
│   │   ├── PoolHelper.cs                # Object pooling
│   │   ├── DistanceThrottler.cs         # Update rate limiting
│   │   └── ...
│   └── Extensions/
│       ├── MyPlayer.cs                  # Player extension methods
│       ├── Enemy.cs                     # Enemy extension methods
│       ├── Vectors3.cs                  # Vector conversion
│       └── ...
│
├── server/                          # Matchmaking server
│   ├── Program.cs                   # ★ Entry point
│   ├── Services/
│   │   ├── WebSocketHandler.cs      # WebSocket endpoint
│   │   ├── RendezVousServer.cs      # ★ UDP NAT punch/relay
│   │   └── MetricsService.cs        # OpenTelemetry metrics
│   ├── Models/
│   │   ├── Lobby.cs                 # Lobby state
│   │   └── ClientInfo.cs            # Client connection info
│   ├── Pools/
│   │   ├── ConnectionIdPool.cs      # Connection ID allocation
│   │   └── RoomCodePool.cs          # Room code generation
│   ├── ConfigOptions.cs             # Server configuration
│   ├── appsettings.json             # Production settings
│   └── appsettings.Production.json
│
└── tests/                           # Unit tests
    ├── MessageParsingTests.cs       # Serialization tests
    └── QuantizationTests.cs         # Quantizer precision tests
```

## Key Files Quick Reference

| File | Purpose | Lines |
|------|---------|-------|
| `src/plugin/Plugin.cs` | Entry point, DI setup, IL2CPP registration | ~513 |
| `src/plugin/Services/SynchronizationService.cs` | Main sync orchestrator, state machine | ~2000+ |
| `src/plugin/Services/UdpClientService.cs` | UDP send/receive, NAT punch | ~800+ |
| `src/plugin/Services/PlayerManagerService.cs` | Player tracking, spawning | ~400+ |
| `src/plugin/Scripts/NetPlayer/NetPlayer.cs` | Remote player GameObject | ~600+ |
| `src/plugin/Patches/Enemies/Enemy.cs` | Enemy patches | ~400+ |
| `src/server/Services/RendezVousServer.cs` | NAT punch/relay server | ~770 |
| `src/server/Services/WebSocketHandler.cs` | Matchmaking WS handler | ~500+ |
| `src/common/Messages/GameNetworkMessages/GameNetworkMessage.cs` | Message union definitions | ~77 |

## Service Layer

### Core Services (Singletons)

| Service | Interface | Responsibility |
|---------|-----------|----------------|
| `SynchronizationService` | `ISynchronizationService` | State machine, message routing, game sync |
| `UdpClientService` | `IUdpClientService` | UDP networking, NAT punch, relay |
| `WebsocketClientService` | `IWebsocketClientService` | Matchmaking connection |
| `PlayerManagerService` | `IPlayerManagerService` | Player tracking, NetPlayer spawning |
| `EnemyManagerService` | `IEnemyManagerService` | Enemy ID mapping, tracking |
| `ProjectileManagerService` | `IProjectileManagerService` | Projectile tracking |
| `PickupManagerService` | `IPickupManagerService` | Pickup/orb tracking |
| `ChestManagerService` | `IChestManagerService` | Chest claim protocol |
| `GameBalanceService` | `IGameBalanceService` | Multiplayer difficulty scaling |
| `SpawnedObjectManagerService` | `ISpawnedObjectManagerService` | Generic object tracking |
| `FinalBossOrbManagerService` | `IFinalBossOrbManagerService` | Final boss orb tracking |

### Service Access Pattern

```csharp
// From any plugin code
var sync = Plugin.Services.GetRequiredService<ISynchronizationService>();
var playerMgr = Plugin.Services.GetRequiredService<IPlayerManagerService>();
```

## Network Message Types

### UDP Messages (IGameNetworkMessage)

**File:** `src/common/Messages/GameNetworkMessages/GameNetworkMessage.cs`

| Category | Messages |
|----------|----------|
| **Player** | `PlayerUpdate`, `PlayerDied`, `PlayerRespawned`, `PlayerDisconnected`, `SelectedCharacter`, `HatChanged` |
| **Enemy** | `SpawnedEnemy`, `EnemyDied`, `EnemyDamaged`, `EnemyExploder`, `RetargetedEnemies`, `SpawnedEnemySpecialAttack` |
| **Projectile** | `SpawnedProjectile`, `ProjectilesUpdate`, `ProjectileDone` (+ weapon variants) |
| **Pickup** | `SpawnedPickup`, `SpawnedPickupOrb`, `PickupApplied`, `PickupFollowingPlayer`, `WantToStartFollowingPickup` |
| **Chest** | `SpawnedChest`, `ChestOpened`, `RequestChestOpen`, `GrantChestOpen` |
| **Interactables** | `InteractableUsed`, `SpawnedReviver`, `InteractableCharacterFightEnemySpawned` |
| **Shrines** | `StartingChargingShrine`, `StoppingChargingShrine`, `StartingChargingPylon`, `StoppingChargingPylon`, `StartingChargingLamp`, `StoppingChargingLamp` |
| **Boss** | `FinalBossOrbSpawned`, `FinalBossOrbDestroyed` |
| **World** | `SpawnedObject`, `SpawnedObjectInCrypt`, `RunStarted`, `GameOver`, `LobbyUpdates` |
| **Weather** | `StormStarted`, `StormStopped`, `TornadoesSpawned`, `TumbleWeedSpawned`, `TumbleWeedsUpdate`, `TumbleWeedDespawned` |
| **Inventory** | `ItemAdded`, `ItemRemoved`, `WeaponAdded`, `WeaponToggled`, `TomeAdded` |
| **Game** | `ClientInGameReady`, `Introduced`, `TimerStarted`, `StartedSwarmEvent`, `LightningStrike` |

### WebSocket Messages (IWsMessage)

| Message | Direction | Purpose |
|---------|-----------|---------|
| `MatchInfo` | Server→Client | Lobby assignment |
| `GameStarting` | Server→Client | Game about to start |
| `GameStartingResponse` | Client→Server | Ready confirmation |
| `RunStatistics` | Client→Server | End-game stats |
| `ServerConnectionStatus` | Server→Client | Connection state |
| `ClientDisconnected` | Server→Client | Player left |
| `HostDisconnected` | Server→Client | Host left |

## Configuration

### Plugin Config (BepInEx)

**File:** `BepInEx/config/MegabonkTogether.cfg`

| Section | Key | Default | Description |
|---------|-----|---------|-------------|
| Network | ServerUrl | Production URL | Matchmaking server WebSocket URL |
| Player | PlayerName | "Player" | Display name |
| Updates | CheckForUpdates | true | Auto-update check |
| Gameplay | AllowSave | false | Allow saves in netplay |

**Code:** `src/plugin/Configuration/ModConfig.cs`

### Server Config

**File:** `src/server/appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "MegabonkTogether.Server.Services.WebSocketHandler": "Information",
      "MegabonkTogether.Server.Services.RendezVousServer": "Information"
    }
  }
}
```

## Common Extension Methods

### Vector Conversion

**File:** `src/plugin/Extensions/Vectors3.cs`

```csharp
// Unity → Numerics
Vector3 unityPos = transform.position;
var netPos = unityPos.ToNumericsVector3();

// Numerics → Unity
var unityPos = netPos.ToUnityVector3();
```

### Player Extensions

**File:** `src/plugin/Extensions/MyPlayer.cs`

```csharp
// Get network-relevant player state
var animatorState = player.GetAnimatorState();
var quantizedPos = Quantizer.Quantize(player.transform.position);
```

## Project Files

| File | Purpose |
|------|---------|
| `MegabonkTogether.sln` | Visual Studio solution |
| `src/plugin/MegabonkTogether.Plugin.csproj` | Plugin project (BepInEx packages) |
| `src/common/MegabonkTogether.Common.csproj` | Shared library (MemoryPack) |
| `src/server/MegabonkTogether.Server.csproj` | Server project (ASP.NET Core) |
| `Directory.Build.props` | Local path overrides (MegabonkPath) |

## Build Artifacts

After building, output goes to:

```
# Plugin
src/plugin/bin/Debug/net6.0/MegabonkTogether.dll
src/plugin/bin/Debug/net6.0/MegabonkTogether.Common.dll

# Server
src/server/bin/Debug/net6.0/MegabonkTogether.Server.dll
```

Auto-deploy (when MegabonkPath configured):
```
{MegabonkPath}/BepInEx/plugins/MegabonkTogether/
```
