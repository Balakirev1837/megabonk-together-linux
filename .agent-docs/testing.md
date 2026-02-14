# Testing Guide

> Agent-first documentation. Testing approach, patterns, and verification steps.

## Testing Overview

Megabonk Together uses a layered testing approach:

| Layer | Type | Location |
|-------|------|----------|
| Unit | Serialization, quantization | `src/tests/` |
| Integration | Service interactions | Manual (game runtime) |
| End-to-End | Cross-play verification | Manual (multi-client) |

---

## Unit Tests

### Location

```
src/tests/
├── MessageParsingTests.cs   # MemoryPack serialization
├── QuantizationTests.cs     # Vector compression precision
└── ...
```

### Running Tests

```bash
cd src/tests
dotnet test
```

### Test Categories

#### Serialization Tests

Verify MemoryPack round-trips correctly:

```csharp
[Test]
public void PlayerUpdate_SerializeDeserialize_RoundTrip()
{
    var original = new PlayerUpdate
    {
        ConnectionId = 123,
        Position = new QuantizedVector3(100, 200, 300),
        Hp = 50,
        MaxHp = 100
    };
    
    byte[] data = MemoryPackSerializer.Serialize<PlayerUpdate>(original);
    var result = MemoryPackSerializer.Deserialize<PlayerUpdate>(data);
    
    Assert.AreEqual(original.ConnectionId, result.ConnectionId);
    Assert.AreEqual(original.Position.X, result.Position.X);
}
```

#### Quantization Tests

Verify precision within acceptable bounds:

```csharp
[Test]
public void QuantizeVector3_WithinPrecision()
{
    var original = new Vector3(1.234f, 5.678f, 9.012f);
    
    var quantized = Quantizer.Quantize(original);
    var dequantized = Quantizer.Dequantize(quantized);
    
    var diff = Vector3.Distance(original, dequantized);
    Assert.Less(diff, 0.01f);  // Within 1cm precision
}
```

---

## Integration Testing (Runtime)

### Prerequisites

- Game installed with BepInEx
- Plugin built and deployed
- Local server running (optional)

### Test Scenarios

#### 1. Plugin Load Test

```bash
# Start game, check for errors
tail -f BepInEx/LogOutput.log | grep -E "(ERROR|Exception)"
```

**Expected:** No errors during plugin load

#### 2. Menu Button Test

1. Launch game to main menu
2. Verify "Together!" button appears
3. Click button, verify menu opens

#### 3. Connection Test (Local Server)

```bash
# Terminal 1: Start server
cd src/server && dotnet run

# Terminal 2: Update config
echo '[Network]
ServerUrl = ws://127.0.0.1:5000/ws' > BepInEx/config/MegabonkTogether.cfg

# Terminal 3: Monitor logs
tail -f BepInEx/LogOutput.log
```

1. Launch game
2. Open Together menu
3. Select "Random" queue
4. Verify connection in logs

---

## End-to-End Testing (Multi-Client)

### Single Machine Test

Requires two game instances:

1. **Copy game folder** to second location
2. **Install BepInEx + Plugin** in both
3. **Start local server**
4. **Launch both games**
5. **Join same queue**

### Cross-Play Verification Checklist

| Test | Steps | Expected |
|------|-------|----------|
| **Join** | Player B joins Player A's game | B spawns in A's world |
| **Movement** | A moves around | B sees A move smoothly |
| **Enemy Sync** | Enemies spawn | Both see same enemies |
| **Combat** | A kills enemy | Enemy dies for both |
| **Items** | A picks up item | Both see pickup disappear |
| **Chest** | A opens chest | Only A gets reward |
| **Portal** | A enters portal | Both transition to next level |

### Latency Testing

Test with artificial latency:

```bash
# Linux: Add 100ms latency
sudo tc qdisc add dev eth0 root netem delay 100ms

# Remove latency
sudo tc qdisc del dev eth0 root
```

### Desync Detection

Watch for desync indicators:

1. **Enemy position mismatch** - Enemy jumps/glitches
2. **Ghost enemies** - Enemy visible but not really there
3. **Item duplication** - Both players pick up same item
4. **Health desync** - HP bar doesn't match damage

---

## Debug Logging

### Enable Verbose Logging

**Client:** `BepInEx/config/BepInEx.cfg`
```ini
[Logging]
LogLevel = Debug
```

**Server:** `src/server/appsettings.json`
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

### Log Analysis Commands

```bash
# Count errors
grep -c "ERROR" BepInEx/LogOutput.log

# Find exceptions
grep "Exception" BepInEx/LogOutput.log

# Network messages
grep -E "(Sending|Received)" BepInEx/LogOutput.log

# Filter by service
grep "SynchronizationService" BepInEx/LogOutput.log
```

---

## Performance Testing

### Metrics to Monitor

| Metric | Target | How to Check |
|--------|--------|--------------|
| FPS | > 60 | Unity stats |
| Memory | Stable | Process monitor |
| Bandwidth | < 100 KB/s | Network monitor |
| Latency | < 100ms RTT | Ping to server |

### Stress Test Scenarios

1. **Max Players (6)** - Full lobby
2. **Enemy Hordes (500+)** - Swarm event
3. **Extended Play (30+ min)** - Memory leak check
4. **Rapid Join/Leave** - Connection handling

---

## Regression Test Checklist

Before releasing, verify:

- [ ] Plugin loads without errors
- [ ] Menu button visible and functional
- [ ] Can join random queue
- [ ] Can create/join friendlies
- [ ] Player movement synchronized
- [ ] Enemy spawning synchronized
- [ ] Enemy death synchronized
- [ | Projectiles synchronized
- [ ] Pickups synchronized
- [ ] Chest claim protocol works
- [ ] Shrine charging works
- [ ] Boss fight synchronized
- [ ] Portal transitions work
- [ ] Player death/respawn works
- [ ] Disconnection handled gracefully
- [ ] Cross-play with Windows works

---

## Known Issues to Test For

### High Priority

| Issue | Test | Fix Status |
|-------|------|------------|
| Chest duplicate rewards | Two players open same chest | Fixed (claim protocol) |
| Ghost item crash | Collect ghost item | Fixed (v2.0.2) |
| HP desync on respawn | Player respawns | Partial |

### Medium Priority

| Issue | Test | Fix Status |
|-------|------|------------|
| Enemy attack desync | Watch enemy attacks | Open |
| XP drop desync | Kill enemies, watch XP | Open |
| I-frames on respawn | Player spawns in swarm | Open |

---

## Automated Testing (Future)

### Potential Additions

1. **CI/CD Tests** - Run unit tests on PR
2. **Mock Network Tests** - Simulate client/server without game
3. **Snapshot Comparison** - Verify LobbyUpdates consistency
4. **Performance Benchmarks** - Track serialization speed

### Test Infrastructure

```yaml
# .github/workflows/test.yml
name: Tests
on: [push, pull_request]
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      - run: dotnet test src/tests
```
