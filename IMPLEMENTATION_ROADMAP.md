# Implementation Roadmap

This roadmap outlines the plan for network performance optimizations and synchronization reliability.

## 1. Chest & Pickup Synchronization
**Goal:** Prevent duplicate rewards and race conditions when interacting with chests.

### Tasks
- [x] Implement Chest Claiming Messages (`RequestChestOpen`, `GrantChestOpen`)
- [x] Implement Server-Side Chest Claim Logic in `ChestManagerService`
- [x] Implement Client-Side Chest Claim Logic in `SynchronizationService`
- [x] Add Chest Interaction Lock in `OpenChestPatches`

## 2. Player Position & Stats Optimization
**Goal:** Reduce bandwidth by quantizing data and splitting high-frequency movement from low-frequency stats.

### Tasks
- [x] Use Quantized Position for Players (Task `zzz`)
- [ ] Create `PlayerMovementUpdate` and `PlayerStatsUpdate` Messages (Task `9tn`)
- [ ] Refactor `UdpClientService` for Split Updates (Task `eyl`)
- [ ] Implement Dirty Flags for Player Stats in `PlayerManagerService` (Task `wgw`)

## 3. Enemy & Projectile Optimization
**Goal:** Ensure updates are efficient and use minimal bandwidth via O(1) delta compression.

### Tasks
- [x] Implement Projectile Delta Compression (Task `sla`)
- [x] Implement Boss Orb Delta Compression (Task `3yg`)
- [ ] Enforce Host-Only Target Switching in `TargetSwitcher.cs` (Task `b4s`)
- [ ] Optimize Target Switch Frequency constants (Task `ddv`)

## 4. Synchronization Reliability
**Goal:** Fix desync windows and ensure consistent game state across all clients.

### Tasks
- [ ] Fix Shared Traits/Stats bleeding (Task `83j`)
    - 1. Identify all locations using `PeakNetplayerPositionRequest` side-channel.
    - 2. Implement a scoped `StatContext` to correctly map `GetStat` calls to the specific player instance.
- [ ] Fix Post-Death Pickup Range Desync (Task `92k`)
    - 1. Investigate `Pickup.StartFollowingPlayer` behavior when `NetPlayer` is dead or respawning.
    - 2. Ensure `Magnet` and `PickupRange` stats are properly reconciled after respawn.
- [ ] Reconcile Player HP on Respawn (Task `8ym`)
    - 1. Update `SynchronizationService.OnReceivedPlayerRespawned` to set `player.Hp = player.MaxHp`.
    - 2. Ensure remote players are visible and active immediately after respawn message.
- [ ] Research Deterministic XP Drops (Task `759`)
    - 1. Investigate seeded random for XP drops.
    - 2. Implement local prediction of XP orb spawns on clients.
- [ ] Implement Reliable Enemy Death (Task `77d`)
    - 1. Add `RecentDeaths` (HashSet of IDs) to `LobbyUpdates` snapshot.
    - 2. Host: Buffer enemy IDs in `pendingSyncDeaths` for 3 seconds after death.
    - 3. Client: In `OnLobbyUpdate`, purge local enemies matching `RecentDeaths` that are still alive locally.
- [ ] Synchronize Enemy Attack Animations (Task `kf2`)
    - 1. Create `EnemyAnimationUpdate` message.
    - 2. Hook `StartAttack` / `StopAttack` on Host and broadcast to clients.

## 5. Player Position & Interpolation
**Goal:** Smooth out player movement and prevent jitter/rubber-banding.

### Tasks
- [ ] Implement Jitter Buffer Tuning (Task `hxa`)
    - 1. Calculate dynamic offset in `PlayerInterpolator` based on RTT.
    - 2. Implement adaptive buffer to handle packet arrival variance.
- [ ] Add Server-Side Speed Validation (Task `975`)
    - 1. Implement max speed checks on Host.
    - 2. Correct client position (snap-back) if validation fails.

## 6. Network Monitor
**Goal:** Gather deep-level telemetry and packet logs for data-driven debugging.

### Tasks
- [ ] Design `INetworkMonitor` Service (Task `s9w`)
- [ ] Implement UDP Packet Hooks in `UdpClientService` (Task `ipj`)
- [ ] Implement WebSocket Packet Hooks in `WebsocketClientService` (Task `d1i`)
- [ ] Add Bandwidth and Latency Telemetry (Task `lut`)
- [ ] Implement Structured Log Exporter (JSONL) (Task `ix7`)

## 8. Architectural Dark Spots
**Goal:** Address technical debt, fragile logic, and noted codebase "shady" spots.

### Tasks
- [ ] `bd create "Fix Spawn Object Infinite Retry" --description "Modify 'SynchronizationService.NewObjectToSpawnRoutine' to implement a retry limit (e.g., 5 attempts) before moving unspawnable objects to a dead-letter log. This prevents potential infinite loops and log spam."`
- [ ] `bd create "Fix 'ShadyGuy' and 'Microwave' Sync" --description "Investigate why certain interactables (ShadyGuy, Microwave) fail on clients. Refactor their synchronization to match 'InteractableCharacterFight' logic if applicable."`
- [ ] `bd create "Ensure Thread-Safe ID Generation" --description "Audit and refactor 'currentPickupId', 'currentObjectId', and 'currentEnemyId' in ManagerServices to use 'Interlocked.Increment' or 'lock' to prevent race conditions during high-frequency spawning."`
- [ ] `bd create "Unify Charging Pylon/Lamp Logic" --description "Refactor 'OnStartingToChargingLamp' and 'OnStartingToChargingPylon' into a single, generic 'ChargingInteractable' service to reduce duplication and maintenance complexity."`
- [ ] `bd create "Implement Network Handshake Key Validation" --description "Update 'UdpClientService' to validate connection request keys. Currently, it accepts any connection without checking the handshake key, which is a security/robustness gap."`
