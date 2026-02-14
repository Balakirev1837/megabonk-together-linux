# Pickup and Chest Synchronization Analysis Plan

## 1. Codebase Analysis (Completed)

### Pickup Logic
*   **Manager:** `PickupManagerService` (Server-side ID generation, Client-side mapping).
*   **Patch:** `PickupPatches`
    *   `ApplyPickup_Prefix`:
        *   Prevents local application if `ownerId` is remote (except Time/Magnet).
        *   Allows local application if `ownerId` is local or null.
    *   `ApplyPickup_Postfix`:
        *   Sends `OnPickupApplied` to server if local.
        *   Despawns pickup if prefix blocked application.
    *   `StartFollowingPlayer_Prefix`:
        *   Prevents following if `ownerId` is set (already claimed).
        *   Sends `OnWantToStartFollowingPickup` to server if unclaimed.
*   **Sync Service:**
    *   `OnPickupSpawned` / `OnReceivedSpawnedPickup`: Spawns pickup on clients.
    *   `OnWantToStartFollowingPickup` / `HandleWantToStartFollowingPickup`:
        *   **Race Condition Check:** Checks if `ownerId` is already set. If so, tells client to update target (rejected claim).
        *   If free, sets `ownerId` and broadcasts `PickupFollowingPlayer`.
    *   `OnPickupApplied` / `OnReceivedPickupApplied`:
        *   Despawns pickup.
        *   Applies effect remotely (e.g., `StatusEffectPickup`).

### Chest Logic
*   **Manager:** `ChestManagerService` (ID mapping).
*   **Patch:** `OpenChestPatches`
    *   `OnTriggerStay_Postfix`: Sends `OnChestOpened` if local player is opening.
    *   `ItemInventoryPatches`: Sends `OnItemAdded` when `AddItem` is called locally.
*   **Patch:** `ChestWindowUiPatches`
    *   Handles UI invulnerability frames.
    *   Does **not** appear to block the UI itself based on ownership.
*   **Sync Service:**
    *   `OnSpawnedChest` / `OnReceivedSpawnedChest`: Spawns chest visuals.
    *   `OnChestOpened` / `OnReceivedChestOpened`: Destroys chest object.

### Critical Race Condition: Duplicate Chest Rewards
*   **Scenario:** Player A and Player B touch a chest simultaneously (within RTT).
*   **Flow:**
    1.  Both clients trigger `OpenChest.OnTriggerStay`.
    2.  Both clients independently grant items (`ItemInventory.AddItem`).
    3.  Both clients independently send `ItemAdded` and `ChestOpened`.
    4.  **Result:** Double rewards, double chest open messages.
*   **Root Cause:** Chest opening and reward generation are purely local and optimistic.

## 2. Optimization Opportunities

### Pickups
*   **Batch Spawning:** `PickupManagerService` sends individual packets for XP. This scales poorly with hordes.
*   **Deterministic Drops:** Enemies could drop seeded XP, allowing clients to predict spawn without network traffic.

## 3. Actionable Steps (Beads Tasks)

- [ ] **Verify "Time" & "Magnet" Handling:** Check if `OnReceivedPickupApplied` applies `Time`/`Magnet` again if `ApplyPickup_Prefix` already allowed it.
- [ ] **Implement Chest Claiming System:**
    *   **Phase 1 (Request):** Hook `OpenChest.OnTriggerStay` (Prefix). Return `false` (block interaction) and send `RequestChestOpen(chestId)` to server.
    *   **Phase 2 (Grant):** Server checks if chest is unclaimed. If yes, sends `GrantChestOpen(chestId)` to requester. If no, sends `DenyChestOpen` (or simply destroys it).
    *   **Phase 3 (Open):** Client receives `GrantChestOpen`. Calls original `OpenChest` logic to show UI and grant rewards.
- [ ] **Chest Interaction Lock:** Add a visual indicator or internal state to prevent repeated interaction attempts while waiting for server response.
- [ ] **XP Spawning Optimization:** Investigate implementing a `BatchSpawnPickup` message for XP orbs to reduce packet overhead.

## 4. Next Phase: Implementation
*   Create a "Chest Interaction Lock" patch.
*   Implement `RequestChestOpen` / `GrantChestOpen` messages.
