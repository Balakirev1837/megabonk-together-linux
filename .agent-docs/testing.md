# Testing Guide

> Agent-first documentation. Run commands, test inventory, and patterns.

## Quick Reference

```bash
# Run all tests
dotnet test src/tests

# Run specific test class
dotnet test src/tests --filter "FullyQualifiedName~MessageParsingTests"

# Run specific test
dotnet test src/tests --filter "FullyQualifiedName~RequestChestOpen_ShouldRoundTripCorrectly"

# Run with verbose output
dotnet test src/tests -v n

# Run without build (faster if already built)
dotnet test src/tests --no-build
```

---

## Test Suite Overview

| File | Tests | Purpose |
|------|-------|---------|
| `MessageParsingTests.cs` | 60+ | MemoryPack serialization round-trips |
| `ProtocolCoverageTests.cs` | 4+ | Union registration, unique codes, coverage |
| `QuantizationTests.cs` | 6 | Vector compression precision |

**Framework:** xUnit v2.9.3
**Assertions:** FluentAssertions v6.12.0
**Mocks:** NSubstitute v5.1.0
**Serialization:** MemoryPack v1.21.4

---

## When to Run Tests

| Trigger | Command |
|---------|---------|
| Before commit | `dotnet test src/tests` |
| After changing messages | `dotnet test src/tests --filter "MessageParsing"` |
| After changing models | `dotnet test src/tests --filter "ProtocolCoverage"` |
| After changing quantization | `dotnet test src/tests --filter "Quantization"` |
| CI/CD | `dotnet test src/tests --logger "trx" --results-directory ./test-results` |

---

## Test Files

### MessageParsingTests.cs

**Location:** `src/tests/MessageParsingTests.cs`
**Focus:** Serialization round-trips for all `IGameNetworkMessage` implementations

**Pattern:**
```csharp
[Fact]
public void MessageType_ShouldRoundTripCorrectly()
{
    var original = new MessageType { Field = value };
    var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
    var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
    parsed.Should().BeOfType<MessageType>();
    var typed = (MessageType)parsed!;
    typed.Field.Should().Be(value);
}
```

**Message Types Tested:**

| Category | Types |
|----------|-------|
| Player | `PlayerUpdate`, `PlayerDied`, `PlayerRespawned`, `Introduced`, `PlayerDisconnected`, `SelectedCharacter` |
| Enemy | `SpawnedEnemy`, `EnemyDied`, `EnemyDamaged`, `EnemyExploder`, `SpawnedEnemySpecialAttack`, `RetargetedEnemies` |
| Projectile | `SpawnedProjectile`, `ProjectileDone`, `SpawnedAxeProjectile`, `SpawnedBlackHoleProjectile`, `SpawnedRocketProjectile`, `SpawnedShotgunProjectile`, `SpawnedDexecutionerProjectile`, `SpawnedFireFieldProjectile`, `SpawnedCringeSwordProjectile`, `SpawnedHeroSwordProjectile`, `SpawnedRevolverProjectile`, `SpawnedSniperProjectile`, `ProjectilesUpdate` |
| Pickup | `SpawnedPickupOrb`, `SpawnedPickup`, `PickupApplied`, `PickupFollowingPlayer`, `WantToStartFollowingPickup` |
| Chest | `SpawnedChest`, `ChestOpened`, `RequestChestOpen`, `GrantChestOpen` |
| Weapon/Item | `WeaponAdded`, `WeaponToggled`, `TomeAdded`, `ItemAdded`, `ItemRemoved` |
| Interactable | `InteractableUsed`, `StartingChargingShrine`, `StoppingChargingShrine`, `StartingChargingPylon`, `StoppingChargingPylon`, `StartingChargingLamp`, `StoppingChargingLamp`, `SpawnedObjectInCrypt`, `InteractableCharacterFightEnemySpawned` |
| World/Event | `SpawnedObject`, `LobbyUpdates`, `RunStarted`, `GameOver`, `StartedSwarmEvent`, `StormStarted`, `StormStopped`, `TornadoesSpawned`, `TumbleWeedSpawned`, `TumbleWeedsUpdate`, `TumbleWeedDespawned`, `TimerStarted` |
| Boss | `FinalBossOrbSpawned`, `FinalBossOrbDestroyed`, `LightningStrike` |
| Lobby | `ClientInGameReady`, `HatChanged`, `SpawnedReviver` |

---

### ProtocolCoverageTests.cs

**Location:** `src/tests/ProtocolCoverageTests.cs`
**Focus:** Protocol integrity and MemoryPack union registration

| Test | Purpose |
|------|---------|
| `AllMessageTypes_ShouldBeRegisteredInUnion` | Every `IGameNetworkMessage` has MemoryPackUnion attribute |
| `AllMessageTypes_ShouldRoundTripWithPopulatedData` | All message types serialize/deserialize with auto-populated data |
| `AllMessageTypes_ShouldHaveUniqueUnionCodes` | No duplicate union tags |
| `MessageType_ShouldRoundTrip` (Theory) | Per-type round-trip via MemberData |

**TestDataBuilder:**

Auto-generates test data for any type:
- Primitives: `int=42`, `float=3.14f`, `bool=true`, `string="TestData"`
- Vectors: `QuantizedVector3`, `QuantizedVector2`, `Vector3`, `Quaternion`
- Collections: `List<T>` with one populated item
- Models: `EnemyModel`, `Player`, `Projectile`, etc.

---

### QuantizationTests.cs

**Location:** `src/tests/QuantizationTests.cs`
**Focus:** Precision bounds for vector compression

| Test | Input | Expected Precision |
|------|-------|-------------------|
| `Yaw_ShouldRoundTripWithAcceptablePrecision` | `[0, 180, 359.9, -10, 720]` | ±0.01° |
| `Position_ShouldRoundTripWithAcceptablePrecision` | `[0, 100, -250, 499]` | ±0.05 units |
| `Quantize_ShouldBeClampedByRange` | `[-500, 500]` | Exact boundary match |

**World Bounds:**
- Min: -500
- Max: 500
- Range: 1000
- Precision: ~0.03 units (1000/32767)

---

## Test Patterns

### Round-Trip Pattern
```csharp
// Standard serialization test
var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
parsed.Should().BeOfType<ExpectedType>();
```

### Theory Pattern (Parameterized)
```csharp
[Theory]
[InlineData(0f)]
[InlineData(180f)]
public void Yaw_ShouldRoundTrip(float input) { ... }
```

### MemberData Pattern (Dynamic)
```csharp
[Theory]
[MemberData(nameof(GetAllMessageTypes))]
public void MessageType_ShouldRoundTrip(Type messageType) { ... }

public static IEnumerable<object[]> GetAllMessageTypes() => 
    CommonAssembly.GetTypes()
        .Where(t => typeof(IGameNetworkMessage).IsAssignableFrom(t))
        .Select(t => new object[] { t });
```

---

## Adding New Tests

### For New Message Type

1. Add round-trip test in `MessageParsingTests.cs`:
```csharp
[Fact]
public void NewMessageType_ShouldRoundTripCorrectly()
{
    var original = new NewMessageType { Field = value };
    var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
    var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
    parsed.Should().BeOfType<NewMessageType>();
    // Assert field values
}
```

2. ProtocolCoverageTests auto-detects new types via reflection

### For New Model

Add to `TestDataBuilder` in `ProtocolCoverageTests.cs`:
```csharp
if (type == typeof(NewModel))
{
    return new NewModel { Property = defaultValue };
}
```

---

## Common Failures

| Error | Cause | Fix |
|-------|-------|-----|
| `NullReferenceException` in round-trip | Missing MemoryPackUnion attribute | Add `[MemoryPackUnion(tag, typeof(Type))]` to `IGameNetworkMessage` |
| Duplicate tag | Two messages share union code | Assign unique tag |
| Precision failure | Quantization out of range | Check world bounds |
| Type mismatch after deserialize | Wrong union tag order | Verify tag mapping |

---

## CI Integration

```yaml
# .github/workflows/test.yml
- name: Run Tests
  run: dotnet test src/tests --logger "trx" --results-directory ./test-results
```

---

## Related Files

- `src/tests/MegabonkTogether.Tests.csproj` - Project configuration
- `src/common/MegabonkTogether.Common/Messages/IGameNetworkMessage.cs` - Union definitions
- `src/common/MegabonkTogether.Common/Models/` - Data models
