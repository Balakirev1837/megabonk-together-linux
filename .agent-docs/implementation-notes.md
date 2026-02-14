# Implementation Notes

> Agent-first documentation. Active work, known issues, optimizations, and gotchas.

## Active Work Items

See [IMPLEMENTATION_ROADMAP.md](../IMPLEMENTATION_ROADMAP.md) for full task breakdown.

### In Progress

| Task | ID | Status |
|------|-----|--------|
| Split PlayerUpdate messages | `9tn`, `eyl`, `wgw` | Pending |
| Target switch optimization | `b4s`, `ddv` | Pending |
| Jitter buffer tuning | `hxa` | Pending |
| Network monitor | `s9w` | Pending |

### Recently Completed

| Task | Status |
|------|--------|
| Chest claim protocol | ✅ Done |
| Projectile delta compression | ✅ Done |
| Boss orb delta compression | ✅ Done |
| Ghost item crash fix | ✅ Done (v2.0.2) |

---

## Known Issues

### Race Conditions

#### Chest Duplicate Rewards (FIXED)

**Problem:** Two players opening same chest simultaneously → double rewards.

**Solution:**
- `RequestChestOpen` / `GrantChestOpen` claim protocol
- `ChestManagerService.TryClaimChest()` atomic claims
- UI lock while waiting for server response

**Files:**
- `src/plugin/Services/ChestManagerService.cs`
- `src/common/Messages/GameNetworkMessages/RequestChestOpen.cs`

### Desync Windows

#### HP Desync on Respawn

**Problem:** Client HP not updated when remote player respawns.

**Location:** `src/plugin/Services/SynchronizationService.cs:OnReceivedPlayerRespawned`

**Fix Required:**
```csharp
if (netPlayer != null)
{
    netPlayer.Inventory.playerHealth.hp = (int)playerUpdate.MaxHp;
}
```

**Task:** `8ym`

#### Missing I-Frames on Respawn

**Problem:** No invulnerability frames when respawning.

**Solution:** Apply temporary `StatusEffect` for I-frames.

#### Shared Stats Bleeding

**Problem:** `PeakNetplayerPositionRequest` side-channel causes stat context confusion.

**Task:** `83j`

#### Post-Death Pickup Range

**Problem:** `Pickup.StartFollowingPlayer` issues when `NetPlayer` is dead/respawning.

**Task:** `92k`

---

## Optimization Analysis

### Player Update Optimization

**Current Issue:** `PlayerUpdate` sent every frame includes full inventory.

**Location:** `src/plugin/Services/PlayerManagerService.cs`

**Proposed Split:**

| Message | Fields | Frequency |
|---------|--------|-----------|
| `PlayerMovementUpdate` | Position, Rotation, AnimatorState | Per frame |
| `PlayerStatsUpdate` | HP, Shield, MaxHp, MaxShield | On change |

**Action Items:**
- [ ] Create `PlayerMovementUpdate` message
- [ ] Create `PlayerStatsUpdate` message  
- [ ] Implement dirty flags for stats
- [ ] Remove `Inventory` from frequent updates

**Note:** Inventory already synced via `ItemAdded`/`ItemRemoved` - redundant in `PlayerUpdate`.

### Enemy Delta Compression

**Current:** `src/plugin/Services/EnemyManagerService.cs:GetAllEnemiesDeltaAndUpdate()`

**Thresholds:**
| Check | Threshold |
|-------|-----------|
| Position diff | 0.1f |
| Yaw diff | 5.0f |
| HP diff | 1 |

**Cost:** For 500 enemies, iterates all every tick with `ToModel()` conversion.

**Optimization Ideas:**
1. **Dirty flags** - Enemy sets `isDirty` on move/damage
2. **Increase threshold** - 0.25f for non-boss
3. **Split updates** - `EnemyPositionUpdate` + `EnemyStateUpdate`

### Pickup Batch Spawning

**Issue:** XP orbs spawn individually, each with a network message.

**Location:** `src/plugin/Services/PickupManagerService.cs`

**Proposed:**
- `BatchSpawnPickup` message for XP orbs
- **Alternative:** Deterministic drops with seeded random

### Update Frequency

**Current:** Updates sent every frame (60+ Hz potential)

**Recommended:** 20-30 Hz for `LobbyUpdates`

**Implementation:**
```csharp
private float tickRate = 1.0f / 30.0f;
private float timeSinceLastUpdate = 0f;

void Update()
{
    timeSinceLastUpdate += Time.deltaTime;
    if (timeSinceLastUpdate >= tickRate)
    {
        SendLobbyUpdate();
        timeSinceLastUpdate = 0f;
    }
}
```

---

## Synchronization Gaps

### Enemy Attack Animations

**Status:** Not synchronized  
**Effect:** Clients see attacks at different times  
**Solution:** `EnemyAnimationUpdate` message  
**Task:** `kf2`

### Enemy Death Reliability

**Problem:** Clients may have "ghost" enemies that died on host.

**Proposed Fix:**
1. Add `RecentDeaths` HashSet to `LobbyUpdates`
2. Host buffers dead enemy IDs for 3 seconds
3. Client purges local enemies matching `RecentDeaths`

**Task:** `77d`

### XP Drop Determinism

**Problem:** XP drops are non-deterministic.

**Proposed:** Seeded random for XP orb spawns, local prediction.

**Task:** `759`

---

## Bandwidth Optimization Status

| Optimization | Status | Impact |
|--------------|--------|--------|
| Vector quantization | ✅ Implemented | ~50% reduction |
| Delta compression | ✅ Implemented | Variable |
| Dirty flags | 📋 Planned | CPU + bandwidth |
| Bit-packing booleans | 📋 Planned | Minor |
| Partial model updates | 🔬 Research | Unknown |

---

## Architectural Issues (Dark Spots)

### Infinite Retry in Spawn Routine

**Location:** `SynchronizationService.NewObjectToSpawnRoutine`

**Problem:** Infinite retry loop if object can't spawn.

**Fix:** Add retry limit (5 attempts) + dead-letter log.

### ShadyGuy/Microwave Sync Issues

**Problem:** Certain interactables fail on clients.

**Fix:** Refactor to match `InteractableCharacterFight` pattern.

### Thread-Unsafe ID Generation

**Locations:**
- `currentPickupId`
- `currentObjectId`
- `currentEnemyId`

**Fix:** Use `Interlocked.Increment` or `lock`.

### Duplicate Charging Logic

**Locations:**
- `OnStartingToChargingLamp`
- `OnStartingToChargingPylon`

**Fix:** Unify into generic `ChargingInteractable` service.

### Missing Handshake Validation

**Location:** `UdpClientService`

**Problem:** Accepts connections without validating handshake key.

**Fix:** Add key validation.

---

## Platform-Specific Issues

### Linux Native (BepInEx 6 IL2CPP)

**Issue:** `PAL_SEHException` crashes on newer kernels (Fedora 42)  
**Cause:** CoreCLR/GLIBC/CET conflicts  
**Solution:** Use Proton 9.0+

### Proton Requirements

**Critical:** `WINEDLLOVERRIDES="winhttp=n,b" %command%` in Steam launch options.

---

## Game Balance Parameters

**File:** `src/plugin/Services/GameBalanceService.cs`

| Setting | Solo | 2-4 Players | 5-6 Players |
|---------|------|-------------|-------------|
| Credit Timer | 1x | +50-100% | +150% |
| Free Chest Rate | 1x | +50-100% | +150% |
| XP Multiplier | 1x | 2x | 3x |
| Boss Lamp Charge | 3s | 6s | 12s |
| Max Enemies | 400 | 500 | 600 |

---

## Debug Checklist

### Desync Investigation

1. Check `LobbyUpdates` frequency in logs
2. Verify enemy IDs match between host/client
3. Check `OnReceivedSpawnedEnemy` for spawn failures
4. Monitor `RecentDeaths` (when implemented)

### Connection Issues

1. Check NAT type (symmetric = relay required)
2. Verify server URL in config
3. Check firewall (ports 5432, 5678)
4. Look for `USE_RELAY` in server logs

### Performance Issues

1. Check enemy count (>500 may need optimization)
2. Monitor bandwidth usage
3. Check for GC spikes (MemoryPack reduces allocations)
4. Verify delta compression is working

---

## Related Documentation

- [Networking](./networking.md) - Protocol reference
- [Game Sync Systems](./game-sync-systems.md) - How each system syncs
- [Implementation Roadmap](../IMPLEMENTATION_ROADMAP.md) - Full task list
