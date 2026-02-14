# Optimization Plan

## 1. Enemy Delta Compression Analysis (`EnemyManagerService.cs`)

### Current Implementation
*   **Method:** `GetAllEnemiesDeltaAndUpdate()`
*   **Logic:**
    1.  Converts all `spawnedEnemies` to `EnemyModel`.
    2.  Compares current models against `previousSpawnedEnemiesDelta`.
    3.  **Delta Check (`HasDelta`):**
        *   Position diff > `0.1f`
        *   Yaw diff > `5.0f`
        *   HP diff > `1` (uint)
    4.  If *any* condition matches, the enemy is added to the "delta" list.
    5.  `previousSpawnedEnemiesDelta` is updated with the *full* current state.

### Identified Weaknesses
1.  **Full List Iteration:** It iterates through *all* enemies every tick to check for deltas. For 500+ enemies, this `ToModel()` conversion and comparison is CPU intensive.
2.  **Redundant Data:** If an enemy moves, we send its *entire* `EnemyModel` (Id, Position, Yaw, Hp, TargetId).
    *   *Optimization:* We could split updates into `EnemyPositionUpdate` (frequent) and `EnemyStateUpdate` (infrequent).
3.  **Threshold Sensitivity:** `0.1f` for position might be too sensitive for a "horde" shooter. Increasing this slightly (e.g., `0.5f`) for non-boss enemies could reduce packet size.

## 2. Player Updates Analysis (`PlayerManagerService.cs`)

### Current Implementation
*   `GetLocalPlayerUpdate()` generates a full `PlayerUpdate` struct every frame.
*   Includes: `Position`, `Rotation`, `HP`, `Shield`, `Inventory` (list of items!), `AnimatorState`.

### Identified Weaknesses
1.  **Inventory Resending:** sending the full `Inventory` list every frame/tick is extremely wasteful. Inventory changes rarely.
    *   *Fix:* Only send `Inventory` field when it changes (Dirty Flag) or use a separate message `PlayerInventoryUpdate`.
2.  **Stats Resending:** `MaxHp`, `MaxShield` change rarely.

## 3. Proposed Optimizations

### Phase 1: Dirty Flags (CPU & Bandwidth)
*   **Enemies:** Instead of polling all enemies for deltas, have `Enemy` components set a `isDirty` flag when they move/take damage. `EnemyManagerService` only collects dirty enemies.
*   **Players:** Split `PlayerUpdate` into:
    *   `PlayerMovementUpdate` (Pos, Rot, Anim) - High Frequency.
    *   `PlayerStatsUpdate` (HP, Inventory) - Low Frequency / Event-based.

### Phase 2: Structural Changes
*   **Inventory Sync:** Remove `Inventory` from `PlayerUpdate`. Rely on `ItemAdded`/`ItemRemoved` events (which already exist!) to keep state in sync. Add a `FullInventorySync` only for join/re-sync.

## 4. Immediate Action Plan
1.  **Optimize `PlayerUpdate`:** Remove `Inventory` from the frequent update loop if possible, or hash it to check for changes before sending.
2.  **Tune Enemy Thresholds:** Increase `POSITION_TRESHOLD` to `0.25f`.
