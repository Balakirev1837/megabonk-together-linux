# Game Synchronization Systems

> Agent-first documentation. Detailed reference for each synchronized game system.

## Overview

Megabonk Together synchronizes most in-game mechanics using a **host-authoritative** model. The host is the source of truth for all world state.

### Synchronization Principles

1. **Host Authority**: Host controls all spawns, deaths, and world events
2. **Client Prediction**: Clients can predict local effects, validated by host
3. **State Snapshots**: Full world state sent periodically via `LobbyUpdates`
4. **Delta Updates**: Incremental updates sent per-frame for positions

## Player Synchronization

### Local Player Capture

**File:** `src/plugin/Services/UdpClientService.cs`

```csharp
// Captured every frame
var playerUpdate = new PlayerUpdate
{
    ConnectionId = localPlayer.ConnectionId,
    Position = Quantizer.Quantize(transform.position),
    Rotation = Quantizer.Quantize(transform.rotation),
    Hp = playerHealth.hp,
    MaxHp = playerHealth.maxHp,
    Shield = playerHealth.shield,
    MaxShield = playerHealth.maxShield,
    AnimatorState = player.GetAnimatorState()
};
```

### Remote Player Representation

**File:** `src/plugin/Scripts/NetPlayer/NetPlayer.cs`

Remote players are instantiated as `NetPlayer` GameObjects:

```
NetPlayer GameObject
├── Model (cloned character prefab)
│   ├── SkinnedMeshRenderer
│   ├── Hat attachment point
│   └── Weapon visuals
├── PlayerInterpolator (smooths position)
├── Nameplate (World Space UI)
└── Minimap Icon
```

### Player State Flow

```
Local Player
     │
     ▼ UpdateLocalPlayer() [UdpClientService]
     │
PlayerUpdate message
     │
     ▼ SendToHost() or Broadcast
     │
Remote NetPlayer
     │
     ▼ AddUpdate() [NetPlayer]
     │
PlayerInterpolator
     │
     ▼ Interpolated position in Update()
```

### Player Components

| Component | Purpose |
|-----------|---------|
| `PlayerInterpolator` | Snapshot interpolation for smooth movement |
| `CustomInventoryHud` | Remote inventory display |
| `DisplayBar` | Health/shield bars |
| `NetPlayerCard` | HUD player card |

## Enemy Synchronization

### Enemy Spawning (Host Only)

**File:** `src/plugin/Patches/Enemies/EnemyManager.cs`

```csharp
[HarmonyPostfix]
[HarmonyPatch(nameof(EnemyManager.SpawnEnemy))]
public static void SpawnEnemy_Postfix(Enemy __result, ...)
{
    if (!synchronizationService.HasNetplaySessionStarted()) return;
    if (!udpClientService.IsHost()) return;
    
    synchronizationService.OnSpawnedEnemy(__result, ...);
}
```

### SpawnedEnemy Message

```csharp
new SpawnedEnemy
{
    Id = netplayId,
    TargetId = targetPlayerId,
    Name = (int)enemyType,
    Position = position.ToNumericsVector3(),
    Hp = enemy.hp,
    Flag = (int)enemyFlag,
    CanBeElite = isElite,
    Wave = waveNumber
}
```

### Enemy Tracking

**File:** `src/plugin/Services/EnemyManagerService.cs`

```csharp
// Maps netplay IDs to Enemy instances
ConcurrentDictionary<uint, Enemy> spawnedEnemies;

// Add new enemy
uint id = AddSpawnedEnemy(enemy);

// Retrieve enemy
Enemy enemy = GetSpawnedEnemy(netplayId);
```

### Enemy Death

**File:** `src/plugin/Patches/Enemies/Enemy.cs`

```csharp
[HarmonyPostfix]
[HarmonyPatch(nameof(Enemy.EnemyDied))]
public static void EnemyDied_Postfix(Enemy __instance, DamageContainer dc)
{
    uint? ownerId = DynamicData.For(dc).Get<uint?>("ownerId");
    synchronizationService.OnEnemyDied(__instance, ownerId);
}
```

### Enemy Interpolation

**File:** `src/plugin/Scripts/Snapshot/EnemyInterpolator.cs`

```
Host sends position → Client receives → Add to buffer → Interpolate between snapshots
```

## Projectile Synchronization

### Projectile Types

Each weapon has specific projectile handling:

| Weapon | Message Type | Special Data |
|--------|--------------|--------------|
| Revolver | `SpawnedRevolverProjectile` | Muzzle position |
| Shotgun | `SpawnedShotgunProjectile` | Muzzle position |
| Sniper | `SpawnedSniperProjectile` | Muzzle position |
| Axe | `SpawnedAxeProjectile` | Start/desired position |
| Black Hole | `SpawnedBlackHoleProjectile` | Start/desired position, scale |
| Rocket | `SpawnedRocketProjectile` | Rocket position/rotation |
| Hero Sword | `SpawnedHeroSwordProjectile` | Moving projectile position |
| Cringe Sword | `SpawnedCringeSwordProjectile` | Moving projectile position |
| Dexecutioner | `SpawnedDexecutionerProjectile` | Attack direction, execution chance |
| Flamewalker | `SpawnedFireFieldProjectile` | Expiration time |
| Default | `SpawnedProjectile` | Position, rotation |

### Projectile Patches

**File:** `src/plugin/Patches/Projectiles/ProjectileBase.cs`

```csharp
[HarmonyPostfix]
[HarmonyPatch(nameof(ProjectileBase.Start))]
public static void Start_Postfix(ProjectileBase __instance)
{
    if (!synchronizationService.HasNetplaySessionStarted()) return;
    if (udpClientService.IsHost())
    {
        synchronizationService.OnSpawnedProjectile(__instance);
    }
}
```

### Projectile Tracking

**File:** `src/plugin/Services/ProjectileManagerService.cs`

```csharp
ConcurrentDictionary<uint, ProjectileBase> spawnedProjectiles;
List<ProjectileSnapshot> projectileSnapshots;
```

## Pickup Synchronization

### Pickup Types

- **XP Orbs**: `SpawnedPickupOrb`
- **Gold**: `SpawnedPickup`
- **Powerups**: `SpawnedPickup`

### Pickup Flow

```
1. Pickup spawns (server-side logic)
2. OnPickupSpawned → Send SpawnedPickup to all
3. Pickup follows player → Send PickupFollowingPlayer
4. Player collects → Send PickupApplied
```

### Pickup Ownership

```csharp
// Set ownership when spawning
DynamicData.For(pickup).Set("ownerId", playerConnectionId);
```

## Chest Synchronization

### Chest Claim Protocol

Prevents race conditions when multiple players interact with same chest:

```
Client A                     Host                     Client B
   │                          │                          │
   │ RequestChestOpen ───────►│                          │
   │                          │                          │
   │                          │◄────── RequestChestOpen ─│
   │                          │                          │
   │◄──── GrantChestOpen ─────│                          │
   │       (granted=true)     │                          │
   │                          │                          │
   │                          │──── GrantChestOpen ─────►│
   │                          │       (granted=false)    │
```

### Chest Manager Service

**File:** `src/plugin/Services/ChestManagerService.cs`

```csharp
public bool TryClaimChest(uint chestId, uint playerId)
{
    // Only first claim succeeds
    return claimedChests.TryAdd(chestId, playerId);
}

public bool IsChestGranted(uint chestId, uint playerId)
{
    return claimedChests.TryGetValue(chestId, out var owner) 
        && owner == playerId;
}
```

## Interactables Synchronization

### Shrine Charging

| Shrine Type | Messages |
|-------------|----------|
| Standard | `StartingChargingShrine`, `StoppingChargingShrine` |
| Pylon | `StartingChargingPylon`, `StoppingChargingPylon` |
| Lamp (Boss) | `StartingChargingLamp`, `StoppingChargingLamp` |

### Shrine Multi-Player Tracking

```csharp
// Track who is charging each shrine
ConcurrentDictionary<uint, ICollection<uint>> shrineChargingPlayers;

// Shrine ready when all players contributed
if (shrineChargingPlayers[shrineId].Count >= requiredPlayers)
{
    // Activate shrine
}
```

### Other Interactables

| Interactable | Sync Method |
|--------------|-------------|
| Shady Guy | `SpawnedObject` + `InteractableUsed` |
| Microwave | `SpawnedObject` + `InteractableUsed` |
| Desert Grave | `SpawnedObject` + linked enemy spawn |
| Skeleton King | `SpawnedObject` + linked enemy spawn |
| Reviver (Coffin) | `SpawnedReviver` + miniboss enemies |

## Boss Synchronization

### Final Boss Orbs

**File:** `src/plugin/Services/FinalBossOrbManagerService.cs`

```csharp
// Track all boss orbs
ConcurrentDictionary<uint, Orb> spawnedBossOrbs;

// Delta updates for positions
LobbyUpdates.BossOrbs = GetBossOrbSnapshots();
```

### Boss Orb Types

- **Bleed Orb**: `BossOrbBleed`
- **Following Orb**: `BossOrbFollowing`
- **Shooty Orb**: `BossOrbShooty`

### Interpolation

**File:** `src/plugin/Scripts/Snapshot/BossOrbInterpolator.cs`

Same snapshot interpolation pattern as enemies/players.

## Inventory Synchronization

### Weapons

```csharp
// Weapon added
OnWeaponAdded(weaponInventory, weaponData, upgrades)
→ Send WeaponAdded message

// Weapon toggled
OnWeaponToggled(weaponInventory, eWeapon, enable)
→ Send WeaponToggled message
```

### Items

```csharp
// Item added
OnItemAdded(EItem item)
→ Send ItemAdded message

// Item removed
OnItemRemoved(EItem item)
→ Send ItemRemoved message
```

### Tomes

```csharp
OnTomeAdded(tomeInventory, tomeData, upgrades, rarity)
→ Send TomeAdded message
```

## World Events

### Storm (Desert)

| Event | Message |
|-------|---------|
| Storm Start | `StormStarted` |
| Storm End | `StormStopped` |
| Tornadoes Spawn | `TornadoesSpawned` |

### Swarm Events

```csharp
OnSwarmEvent(timelineEvent)
→ Send StartedSwarmEvent
```

### Tumbleweeds

| Event | Message |
|-------|---------|
| Spawn | `TumbleWeedSpawned` |
| Update | `TumbleWeedsUpdate` |
| Despawn | `TumbleWeedDespawned` |

## Game Balance (Multiplayer)

**File:** `src/plugin/Services/GameBalanceService.cs`

Adjusts difficulty based on player count:

| Setting | 2 Players | 4 Players | 6 Players |
|---------|-----------|-----------|-----------|
| Credit Timer (enemy spawn) | +50% | +100% | +150% |
| Free Chest Rate | +50% | +100% | +150% |
| XP Multiplier | 2x | 2x | 3x |
| Boss Lamp Charge Time | 2x | 3x | 4x |
| Max Enemies | 500 | 500 | 600 |

## Player Respawn (Reviver)

### Reviver System

When a player dies:
1. `InteractableCoffin` spawns at death location
2. Host sends `SpawnedReviver`
3. Miniboss enemies spawn from coffin
4. Other players defeat miniboss
5. Dead player respawns via `PlayerRespawned`

**File:** `src/plugin/Scripts/Interactables/InteractableReviver.cs`

## Disable in Netplay

The following are disabled during netplay:

| Feature | Reason |
|---------|--------|
| Pause | Game continues for others |
| Saving | Would corrupt with multiple players |
| Steam Achievements | Not meant for multiplayer |
| Leaderboards | Would result in bans |

**File:** `src/plugin/Patches/SaveManager.cs`, `src/plugin/Patches/SteamAchievementsManager.cs`
