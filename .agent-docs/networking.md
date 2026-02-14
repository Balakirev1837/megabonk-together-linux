# Networking Architecture

> Agent-first documentation. Detailed networking protocol and implementation reference.

## Transport Overview

Megabonk Together uses a **dual transport** architecture:

```
┌─────────────────────────────────────────────────────────────────┐
│                        CLIENT PLUGIN                            │
├─────────────────────────────────────────────────────────────────┤
│  WebSocket Client        │        UDP Client (LiteNetLib)       │
│  (WebsocketClientService)│        (UdpClientService)            │
│                          │                                       │
│  • Matchmaking           │  • Real-time game state              │
│  • Lobby management      │  • NAT punchthrough                  │
│  • Room codes            │  • P2P or Relay                      │
│  • Game statistics       │  • Snapshot interpolation           │
└──────────────────────────┴───────────────────────────────────────┘
           │                            │
           ▼                            ▼
    ┌─────────────┐            ┌──────────────┐
    │  WebSocket  │            │  UDP Server  │
    │  Server     │            │  (Relay)     │
    │  (TCP/TLS)  │            │  Port 5678   │
    │  Port 5432  │            └──────────────┘
    └─────────────┘
```

## WebSocket Protocol (Matchmaking)

### Endpoints

| Endpoint | Purpose |
|----------|---------|
| `/ws?random` | Quickplay matchmaking |
| `/ws?friendlies` | Private lobby (room code) |

### Connection Flow

```
1. Client connects to /ws?random or /ws?friendlies
2. Server sends MatchInfo with lobby assignment
3. Server sends NAT punch info for host
4. Clients establish UDP connection
5. Server sends GameStarting
6. Clients respond GameStartingResponse
7. Game starts via UDP
8. On game end, client sends RunStatistics
```

### WebSocket Message Types

**File:** `src/common/Messages/WsMessages/`

| Message | Direction | Fields |
|---------|-----------|--------|
| `MatchInfo` | S→C | `ConnectionId`, `IsHost`, `RoomCode`, `HostInternalEP`, `HostExternalEP` |
| `GameStarting` | S→C | `StartTime` |
| `GameStartingResponse` | C→S | `ConnectionId`, `PlayerName`, `Character` |
| `RunStatistics` | C→S | `Kills`, `Money`, `Time`, `Level` |
| `ClientDisconnected` | S→C | `ConnectionId` |
| `HostDisconnected` | S→C | (empty) |

## UDP Protocol (Game Traffic)

### Service: UdpClientService

**File:** `src/plugin/Services/UdpClientService.cs`

### NAT Punchthrough

LiteNetLib's built-in NAT punchthrough via the matchmaking server:

```
┌────────┐                 ┌────────┐                 ┌────────┐
│ Client │                 │ Server │                 │  Host  │
└───┬────┘                 └───┬────┘                 └───┬────┘
    │  NAT Intro Request       │                          │
    │  token: "client|host|id" │                          │
    │─────────────────────────►│                          │
    │                          │  NAT Intro Request       │
    │                          │  token: "host|client|id" │
    │                          │─────────────────────────►│
    │                          │                          │
    │◄─────────────────────────┼──────────────────────────►
    │       Bidirectional NAT Introduction                 │
    │                                                       │
    │◄────────────────────────────────────────────────────►│
    │                   Direct P2P Connection               │
```

### Token Format

```
{role}|{hostId}|{peerId}[|force_relay]

role: "host" or "client"
hostId: Host's connection ID (uint)
peerId: Peer's connection ID (uint)
force_relay: Optional, "force_relay" to skip P2P
```

### Relay Mode

When P2P fails (IPv6, symmetric NAT), relay mode is used:

```
┌────────┐     ┌────────┐     ┌────────┐
│ Client │────►│ Server │────►│  Host  │
└────────┘     └────────┘     └────────┘
```

**Trigger Conditions:**
- Any IPv6 address detected
- `force_relay` flag in token
- P2P connection timeout

**Relay Protocol:**

1. Server sends `USE_RELAY` to both parties
2. Clients connect to server via UDP
3. Clients send `RELAY_BIND` to register
4. All traffic wrapped in `RelayEnvelope`

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

## Serialization (MemoryPack)

All network messages use **MemoryPack** for zero-allocation binary serialization.

### Message Interface

**File:** `src/common/Messages/GameNetworkMessages/GameNetworkMessage.cs`

```csharp
[MemoryPackable]
[MemoryPackUnion(0, typeof(LobbyUpdates))]
[MemoryPackUnion(1, typeof(ClientInGameReady))]
// ... 66 total union types
public partial interface IGameNetworkMessage { }
```

### Serialization Example

```csharp
// Serialize
byte[] data = MemoryPackSerializer.Serialize<IGameNetworkMessage>(message);

// Deserialize
var message = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(data);
```

### Union IDs (Critical - Do Not Change)

| ID | Message Type |
|----|--------------|
| 0 | LobbyUpdates |
| 1 | ClientInGameReady |
| 2 | SpawnedObject |
| 3 | PlayerUpdate |
| 4 | SpawnedEnemy |
| 5 | SelectedCharacter |
| 6 | EnemyDied |
| 7 | SpawnedProjectile |
| ... | ... |

**WARNING:** Changing union IDs breaks network compatibility with existing clients.

## Quantization

Vectors are quantized to reduce bandwidth:

**File:** `src/plugin/Helpers/Quantizer.cs`

```csharp
// Quantize (compress)
QuantizedVector3 quantized = Quantizer.Quantize(position);

// Dequantize (decompress)
Vector3 original = Quantizer.Dequantize(quantized);
```

### Quantized Types

| Type | Fields | Size |
|------|--------|------|
| `QuantizedVector3` | X, Y, Z (int) | 12 bytes |
| `QuantizedVector4` | X, Y, Z, W (int) | 16 bytes |
| `QuantizedVector2` | X, Y (int) | 8 bytes |
| `QuantizedRotation` | Euler angles (int) | 12 bytes |

## Message Flow Examples

### Player Join

```
1. [WS] Client → Server: Connect to /ws?random
2. [WS] Server → Client: MatchInfo { IsHost: false, HostEP }
3. [UDP] Client → Server: NAT Introduction Request
4. [UDP] Server ↔ Client/Host: NAT Punch
5. [UDP] Client → Host: Introduced { ConnectionId, PlayerName }
6. [UDP] Host → Client: LobbyUpdates { Players }
```

### Enemy Spawn (Host Only)

```
1. Game spawns enemy (via EnemyManager)
2. [Patch] EnemyManagerPatches.OnSpawnedEnemy
3. [Service] SynchronizationService.OnSpawnedEnemy
4. [Service] UdpClientService.SendToAllClients(SpawnedEnemy)
5. [Network] Broadcast to all clients
6. [Client] SynchronizationService.OnReceivedSpawnedEnemy
7. [Game] EnemyManager.Instance.SpawnEnemy(...)
```

### Player Death

```
1. [Patch] PlayerHealthPatches.PlayerDied_Postfix
2. [Service] SynchronizationService.OnPlayerDied
3. [UDP] Host → All: PlayerDied { ConnectionId }
4. [Client] OnReceivedPlayerDied
5. [UI] Show death screen / switch camera
```

### Chest Open (Claim Protocol)

```
1. [Client] Player interacts with chest
2. [UDP] Client → Host: RequestChestOpen { ChestId, PlayerId }
3. [Host] ChestManagerService.TryClaimChest
4. [UDP] Host → Client: GrantChestOpen { ChestId, Granted }
5. [UDP] Host → All: ChestOpened { ChestId, PlayerId }
```

## Delivery Methods

| Method | Use Case | Reliability |
|--------|----------|-------------|
| `ReliableUnordered` | State changes, spawns | Guaranteed, no order |
| `ReliableOrdered` | Chat, important events | Guaranteed, ordered |
| `Unreliable` | Position updates | Best effort |

**Usage in UdpClientService:**
```csharp
udpClientService.SendToAllClients(message, DeliveryMethod.ReliableUnordered);
udpClientService.SendToHost(message, DeliveryMethod.ReliableOrdered);
```

## Network State Machine

### SynchronizationService States

```csharp
public enum State
{
    None,              // Not connected
    Loading,           // Loading map
    Ready,             // In lobby, ready check
    Started,           // Game in progress
    LoadingNextLevel,  // Portal opened
    Endgame,           // Final level
    GameOver           // Game ended
}
```

### State Transitions

```
GameEvent.Loading         → State.Loading
GameEvent.Ready           → State.Ready
GameEvent.Start           → State.Started (if Ready and LobbyReady)
GameEvent.PortalOpened    → State.LoadingNextLevel
GameEvent.FinalPortalOpened → State.Endgame
GameEvent.GameOver        → State.GameOver
```

## Server Implementation

### RendezVousServer

**File:** `src/server/Services/RendezVousServer.cs`

Key responsibilities:
- UDP listener on port 5678
- NAT punch module
- Relay session management
- Stale entry cleanup

### Relay Session Structure

```csharp
public class RelaySession
{
    public uint HostConnectionId;
    public RelayPeer Host;
    public ConcurrentDictionary<uint, RelayPeer> Clients;
    public ConcurrentQueue<PendingRelayMessage> PendingToHost;
}
```

### Cleanup Intervals

| Resource | Retention Time |
|----------|----------------|
| Registered Hosts | 1 minute |
| Pending Clients | 45 seconds |
| Processed Pairs | 45 seconds |
| Pending Relay | 30 seconds |

## Ports Summary

| Service | Protocol | Port | Purpose |
|---------|----------|------|---------|
| Matchmaking | WebSocket | 5432 | Lobby management |
| UDP Server | UDP | 5678 | NAT punch, relay |
| Game Traffic | UDP | 27015+ | P2P game sync |

## Debugging

### Log Network Messages

In `UdpClientService`:
```csharp
Log.LogDebug($"Sending {message.GetType().Name} to {peer.EndPoint}");
```

### Monitor in Real-time

```bash
# Linux
tail -f ~/.local/share/Steam/steamapps/common/Megabonk/BepInEx/LogOutput.log | grep -E "(UDP|WebSocket|Relay)"
```

### Check Relay Status

Server logs show relay mode activation:
```
Using RELAY mode to connect CLIENT {id} ({ep}) TO HOST {id} ({ep})
```
