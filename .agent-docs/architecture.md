# Megabonk Together - Architecture Overview

> Agent-first documentation for AI consumption. Provides high-level understanding of the mod's architecture.

## Project Summary

**Megabonk Together** is a multiplayer networking mod for the Unity game "Megabonk". It adds online co-op support for up to 6 players via a host-client architecture with a dedicated matchmaking server.

| Attribute | Value |
|-----------|-------|
| **Type** | BepInEx IL2CPP Unity Mod + Matchmaking Server |
| **Target Framework** | .NET 6.0 |
| **Game Version** | Megabonk 1.0.49 |
| **Max Players** | 6 |
| **Architecture** | Host-Client (P2P with Relay fallback) |
| **Current Version** | 2.0.2 |

## Solution Structure

```
MegabonkTogether.sln
├── src/common/     # Shared code (messages, models, serialization)
├── src/plugin/     # BepInEx game client mod
├── src/server/     # ASP.NET Core matchmaking/relay server
└── src/tests/      # Unit tests
```

### Project Dependencies

```
src/plugin  ──────►  src/common
src/server  ──────►  src/common
src/tests  ───────►  src/common
```

## Three-Tier Architecture

### 1. Plugin (Game Client Mod)

**Path:** `src/plugin/`  
**Output:** `MegabonkTogether.dll`  
**Runtime:** BepInEx 6 IL2CPP (Unity Mod Loader)

The plugin is injected into the game process and hooks into game logic via Harmony patches.

**Key Responsibilities:**
- Intercept game events via Harmony patches
- Manage network connections (UDP/WebSocket)
- Synchronize game state with other players
- Render remote player representations (`NetPlayer`)
- Handle UI elements (menu tabs, modals, notifications)

**Entry Point:** `src/plugin/Plugin.cs:44-46`
```csharp
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
```

### 2. Common (Shared Library)

**Path:** `src/common/`  
**Output:** `MegabonkTogether.Common.dll`  
**Shared between:** Plugin, Server, Tests

Contains all network message definitions and data models used for communication.

**Key Components:**
- `IGameNetworkMessage` - UDP message interface (66 message types)
- `IWsMessage` - WebSocket message interface
- Models: `Player`, `EnemyModel`, `Projectile`, `PickupModel`, etc.
- Quantization utilities for bandwidth optimization

### 3. Server (Matchmaking/Relay)

**Path:** `src/server/`  
**Output:** `MegabonkTogether.Server.dll`  
**Runtime:** ASP.NET Core 6.0 (Docker-ready)

Standalone server for lobby management and relay traffic.

**Key Responsibilities:**
- WebSocket endpoint for matchmaking (`/ws?random`, `/ws?friendlies`)
- NAT punchthrough facilitation
- Relay mode for failed P2P connections
- Room code generation for private lobbies

**Entry Point:** `src/server/Program.cs`
**Default Ports:** HTTP 5432, UDP 5678

## Network Architecture

### Dual Transport System

| Transport | Protocol | Purpose | Service |
|-----------|----------|---------|---------|
| WebSocket | TCP/TLS | Matchmaking, lobby, statistics | `WebsocketClientService` |
| UDP | LiteNetLib | Real-time game sync | `UdpClientService` |

### Connection Flow

```
┌─────────────┐     WebSocket      ┌──────────────────┐
│   Plugin    │ ──────────────────►│  Matchmaking     │
│  (Client)   │   Matchmaking      │     Server       │
└─────────────┘                    └──────────────────┘
       │                                    │
       │ UDP (LiteNetLib)                   │ NAT Punch Info
       │                                    │
       ▼                                    ▼
┌─────────────┐    P2P / Relay    ┌──────────────────┐
│   Plugin    │ ◄────────────────►│     Plugin      │
│   (Host)    │   Game Traffic    │   (Client)      │
└─────────────┘                   └──────────────────┘
```

### Relay Fallback

When NAT punchthrough fails (IPv6, symmetric NAT), traffic routes through server:

```
Client A ──► Server ──► Client B
         (RelayEnvelope wrapping)
```

## Game State Synchronization

### Host Authority Model

- **Host** is the authoritative source for all game state
- **Clients** send input/state updates to host
- Host broadcasts world state to all clients

### Synchronized Systems

| System | Direction | Frequency |
|--------|-----------|-----------|
| Player Position/Animation | Client→Host, Host→All | Per frame |
| Enemy Spawning/Movement/Death | Host→All | On event |
| Projectiles | Host→All | On spawn + updates |
| Pickups/XP | Host→All | On spawn/collect |
| Chests | Host→All (with claim protocol) | On open |
| Shrines/Pylons/Lamps | Host→All | On interaction |
| Boss Orbs | Host→All | Per frame updates |

### State Machine (SynchronizationService)

```
None ──► Loading ──► Ready ──► Started ──► LoadingNextLevel ──► Endgame ──► GameOver
                         │                      │
                         └──── Game Loop ───────┘
```

## Key Design Patterns

### Dependency Injection

All services use DI via `Microsoft.Extensions.Hosting`:

**File:** `src/plugin/Plugin.cs:130-153`
```csharp
builder.ConfigureServices(services =>
{
    services.AddSingleton<IUdpClientService, UdpClientService>();
    services.AddSingleton<ISynchronizationService, SynchronizationService>();
    // ... more services
});
```

### Event-Driven Architecture

`EventManager` provides pub/sub for network message handling:

**File:** `src/plugin/Services/EventManager.cs`
```csharp
EventManager.SubscribePlayerUpdatesEvents(OnPlayerUpdate);
EventManager.SubscribeSpawnedEnemyEvents(OnReceivedSpawnedEnemy);
```

### Snapshot Interpolation

Remote entities use interpolators for smooth movement:

- `PlayerInterpolator` - Player movement
- `EnemyInterpolator` - Enemy movement
- `ProjectileInterpolator` - Projectile trajectories
- `BossOrbInterpolator` - Final boss orbs

## Critical Constraints

### Binary Compatibility

**WARNING:** The `MegabonkTogether.Common` project defines network message schemas.

- **DO NOT** change existing `MemoryPackUnion` indices
- **DO NOT** reorder fields in message classes
- **DO NOT** use C# features newer than C# 10 without updating Windows build

**File:** `src/common/Messages/GameNetworkMessages/GameNetworkMessage.cs`

### IL2CPP Registration

All custom `MonoBehaviour` classes must be registered:

**File:** `src/plugin/Plugin.cs:105-128`
```csharp
ClassInjector.RegisterTypeInIl2Cpp<NetPlayer>();
ClassInjector.RegisterTypeInIl2Cpp<PlayerInterpolator>();
// ... etc
```

## Platform Support

| Platform | Status | Notes |
|----------|--------|-------|
| Windows | Primary | Official target platform |
| Linux (Proton) | Supported | Proton 9.0+ required |
| Linux (Native) | Unstable | BepInEx 6 IL2CPP issues with newer kernels |
| macOS | Unknown | Not tested |

## Related Documentation

- [BepInEx/Harmony Modding Reference](./bepinex-harmony-modding.md)
- [Codebase Reference](./codebase-reference.md)
- [Networking Details](./networking.md)
- [Build & Run Instructions](./build-run-instructions.md)
