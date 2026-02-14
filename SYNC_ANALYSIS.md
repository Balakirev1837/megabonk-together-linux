# Synchronization Analysis & Network Optimizations

## 1. Player Respawn Logic Analysis

### Current Flow
1.  **Trigger:** `InteractableReviver` (Ghost Boss death) calls `SynchronizationService.OnRespawn` (Host only).
2.  **Host Logic (`OnRespawn`):**
    *   Updates local `PlayerManager` HP for the respawned player.
    *   Calls `netplayer.Respawn()` (if remote) or resets local player state (if host).
    *   Sends `PlayerRespawned` message to **ALL** clients.
3.  **Client Logic (`OnReceivedPlayerRespawned`):**
    *   Receives `PlayerRespawned` message.
    *   Calls `netplayer.Respawn()` (if remote) or resets local player state (if self).

### Identified Issues
1.  **HP Desync Window:**
    *   On clients, `OnReceivedPlayerRespawned` for a *remote* player (i.e., not the client itself) **does not** update the `PlayerManager` HP.
    *   The client must wait for the next `PlayerUpdate` or `LobbyUpdates` from the Host to see the non-zero HP. This could cause UI flickering or logic errors (e.g., checks for `HP > 0`).
    *   **Fix:** Explicitly set `player.Hp = player.MaxHp` in `OnReceivedPlayerRespawned` for the `NetPlayer` case.

2.  **Invulnerability (I-Frames):**
    *   No explicit I-frames are granted on respawn. A player spawning into a swarm might instantly take damage.
    *   **Recommendation:** Apply a temporary `StatusEffect` or flag for I-frames upon respawn.

## 2. Network Optimizations

### Update Frequency
*   `UdpClientService.Update` is called in `Plugin.Update` (Unity Frame Loop).
*   **Issue:** Sending packets every frame (60+ Hz) is potentially excessive for `LobbyUpdates` (full enemy/projectile state).
*   **Current Checks:**
    *   `SendLobbyUpdate` (Host) sends `LobbyUpdates` message.
    *   `SendToHost` (Client) sends `PlayerUpdate` message.
*   **Recommendation:** Throttle updates (e.g., 20Hz or 30Hz) instead of tying to Frame Rate. Use a timer accumulator.

### Data Quantization
*   Projectiles and Players use `Quantizer`.
*   **Observation:** Check `Quantizer` precision. If too high, bandwidth is wasted. If too low, jitter occurs.

## 3. Implementation Plan
1.  **Fix HP Sync:** Modify `SynchronizationService.cs` to update HP in `OnReceivedPlayerRespawned`.
2.  **Throttle Updates:** Implement a `tickRate` in `UdpClientService` to limit send rate (e.g., `1.0f / 30.0f`).
