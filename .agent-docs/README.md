# Agent Documentation Index

> AI-optimized documentation for Megabonk Together mod. Start here for codebase navigation.

## Quick Reference

| Document | Purpose | Lines |
|----------|---------|-------|
| [architecture.md](./architecture.md) | System overview, three-tier architecture | ~230 |
| [codebase-reference.md](./codebase-reference.md) | Directory structure, key files | ~300 |
| [build-run-instructions.md](./build-run-instructions.md) | Build, install, run | ~360 |
| [networking.md](./networking.md) | Network protocol, transports | ~350 |
| [game-sync-systems.md](./game-sync-systems.md) | How each game system syncs | ~420 |
| [bepinex-harmony-modding.md](./bepinex-harmony-modding.md) | Modding framework reference | ~820 |
| [implementation-notes.md](./implementation-notes.md) | Active work, issues, optimizations | ~250 |
| [server-deployment.md](./server-deployment.md) | Server setup and deployment | ~410 |
| **message-types.md** | Network message reference | NEW |
| **manager-services.md** | Service layer reference | NEW |
| **testing.md** | Testing approach and checklists | NEW |

---

## Project Summary

| Attribute | Value |
|-----------|-------|
| **Type** | BepInEx IL2CPP Unity Mod + Matchmaking Server |
| **Framework** | .NET 6.0 |
| **Game** | Megabonk 1.0.49 |
| **Max Players** | 6 |
| **Architecture** | Host-Client (P2P with Relay fallback) |
| **Version** | 2.0.2 |

## Solution Structure

```
src/
├── common/     # Shared library (messages, models)
├── plugin/     # BepInEx game mod
├── server/     # Matchmaking/relay server
└── tests/      # Unit tests
```

---

## Key Entry Points

| Component | File | Key Lines |
|-----------|------|-----------|
| Plugin Entry | `src/plugin/Plugin.cs` | 44-46, 105-128, 130-153 |
| Main Sync Service | `src/plugin/Services/SynchronizationService.cs` | State machine ~2000+ |
| UDP Service | `src/plugin/Services/UdpClientService.cs` | ~800+ |
| Server Entry | `src/server/Program.cs` | - |
| NAT/Relay Server | `src/server/Services/RendezVousServer.cs` | 69+ |
| Message Registry | `src/common/Messages/GameNetworkMessages/GameNetworkMessage.cs` | Union IDs |

---

## Common Tasks

### Adding a New Network Message

1. Create message class in `src/common/Messages/GameNetworkMessages/`
2. Add `[MemoryPackable]` attribute
3. Register union in `GameNetworkMessage.cs` (next available ID)
4. Add handler in `SynchronizationService.cs` via `EventManager.Subscribe<T>()`
5. Send via `UdpClientService.SendToAllClients()`

**Reference:** [message-types.md](./message-types.md)

### Adding a New Harmony Patch

1. Create patch file in `src/plugin/Patches/{domain}/`
2. Use `[HarmonyPatch(typeof(TargetClass))]`
3. Always check `HasNetplaySessionStarted()` before processing
4. Reference [bepinex-harmony-modding.md](./bepinex-harmony-modding.md) for patterns

### Debugging Network Issues

1. Check `BepInEx/LogOutput.log` for client errors
2. Check server logs for relay/NAT issues
3. Enable debug logging in `appsettings.json`
4. Reference [networking.md](./networking.md) for protocol details

### Running Tests

```bash
cd src/tests && dotnet test
```

**Reference:** [testing.md](./testing.md)

---

## Critical Constraints

| Constraint | Reason |
|------------|--------|
| **Never change MemoryPackUnion IDs** | Breaks network compatibility |
| **Register all MonoBehaviours** | IL2CPP requires `ClassInjector.RegisterTypeInIl2Cpp<T>()` |
| **Max C# 10 features** | Windows build compatibility |
| **Host authority** | Only host spawns enemies/projectiles |
| **Binary compatibility** | `MegabonkTogether.Common` must match across versions |

---

## Service Layer Quick Reference

| Service | Purpose |
|---------|---------|
| `SynchronizationService` | State machine, message routing |
| `UdpClientService` | UDP networking, NAT punch |
| `PlayerManagerService` | Player tracking, NetPlayer spawn |
| `EnemyManagerService` | Enemy ID mapping, delta compression |
| `ProjectileManagerService` | Projectile tracking |
| `PickupManagerService` | Pickup/orb tracking |
| `ChestManagerService` | Chest claim protocol |
| `GameBalanceService` | Multiplayer difficulty scaling |

**Full Reference:** [manager-services.md](./manager-services.md)

---

## Network Message Categories

| Category | Examples |
|----------|----------|
| **Player** | `PlayerUpdate`, `PlayerDied`, `PlayerRespawned` |
| **Enemy** | `SpawnedEnemy`, `EnemyDied`, `RetargetedEnemies` |
| **Projectile** | `SpawnedProjectile`, `ProjectilesUpdate`, `ProjectileDone` |
| **Pickup** | `SpawnedPickup`, `PickupApplied`, `PickupFollowingPlayer` |
| **Chest** | `RequestChestOpen`, `GrantChestOpen`, `ChestOpened` |
| **Shrines** | `StartingChargingShrine`, `StoppingChargingShrine` |
| **Inventory** | `ItemAdded`, `ItemRemoved`, `WeaponAdded` |

**Full Reference:** [message-types.md](./message-types.md)

---

## Platform Support

| Platform | Status | Notes |
|----------|--------|-------|
| Windows | ✅ Primary | Official target |
| Linux (Proton) | ✅ Supported | Proton 9.0+ required |
| Linux (Native) | ⚠️ Unstable | BepInEx 6 IL2CPP issues |
| macOS | ❓ Unknown | Not tested |

---

## External Documentation (Root Directory)

| File | Audience | Purpose |
|------|----------|---------|
| `README.md` | Users | Installation, gameplay |
| `NETPLAY_CHANGES.md` | Users | Gameplay changes |
| `IMPLEMENTATION_ROADMAP.md` | Developers | Planned features |
| `AGENTS.md` | AI Agents | Beads workflow |

### Historical/Reference (Consolidated into .agent-docs)

| File | Status | Info Now In |
|------|--------|-------------|
| `TECHNICAL_NOTES.md` | Consolidated | architecture.md, implementation-notes.md |
| `SYNC_ANALYSIS.md` | Consolidated | implementation-notes.md |
| `OPTIMIZATION_PLAN.md` | Consolidated | implementation-notes.md |
| `PICKUP_CHEST_SYNC_PLAN.md` | Implemented | game-sync-systems.md |
| `LINUX_PORTING_NOTES.md` | Historical | build-run-instructions.md |
| `PROTON_SETUP.md` | Consolidated | build-run-instructions.md |
| `DEVELOPMENT_WORKFLOW.md` | Consolidated | build-run-instructions.md |
| `THUNDERSTORE_BUILD.md` | Consolidated | build-run-instructions.md |

---

## Quick Links by Task

| I want to... | Read this |
|--------------|-----------|
| Understand the architecture | [architecture.md](./architecture.md) |
| Build and run the mod | [build-run-instructions.md](./build-run-instructions.md) |
| Understand network protocol | [networking.md](./networking.md) |
| Add a new network message | [message-types.md](./message-types.md) |
| Understand a specific service | [manager-services.md](./manager-services.md) |
| Fix a sync issue | [game-sync-systems.md](./game-sync-systems.md) |
| Write a Harmony patch | [bepinex-harmony-modding.md](./bepinex-harmony-modding.md) |
| Find known issues | [implementation-notes.md](./implementation-notes.md) |
| Deploy the server | [server-deployment.md](./server-deployment.md) |
| Test the mod | [testing.md](./testing.md) |
| Find a specific file | [codebase-reference.md](./codebase-reference.md) |
