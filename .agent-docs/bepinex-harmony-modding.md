# BepInEx and Unity Harmony Runtime Modding Reference

> Agent-first documentation for the MegabonkTogether modding project. This document provides patterns, conventions, and code examples for working with BepInEx IL2CPP and Harmony patching.

## Table of Contents

1. [BepInEx Overview](#1-bepinex-overview)
2. [Harmony Patching Fundamentals](#2-harmony-patching-fundamentals)
3. [Common Patching Patterns](#3-common-patching-patterns)
4. [Advanced Harmony Techniques](#4-advanced-harmony-techniques)
5. [Unity-Specific Considerations](#5-unity-specific-considerations)
6. [Debugging and Troubleshooting](#6-debugging-and-troubleshooting)
7. [Code Examples](#7-code-examples)

---

## 1. BepInEx Overview

### What is BepInEx?

BepInEx is a plugin framework for Unity games that enables runtime code modification through Harmony patching. For IL2CPP games (like Megabonk), it uses Il2CppInterop to bridge between managed C# code and the IL2CPP runtime.

### Directory Structure

```
BepInEx/
├── config/           # Configuration files (.cfg)
├── plugins/          # Runtime plugins (.dll)
├── patchers/         # Pre-load patchers (run before game loads)
├── interop/          # Il2CppInterop generated assemblies
├── unity-libs/       # Unity managed assemblies
└── LogOutput.log     # Log file
```

### Plugin Base Class and Attributes

**File Reference:** `src/plugin/Plugin.cs:44-45`

```csharp
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    public override void Load()
    {
        // Plugin initialization
    }
}
```

**Key Attributes:**

| Attribute | Purpose |
|-----------|---------|
| `[BepInPlugin]` | Declares the plugin with GUID, name, version |
| `[BepInDependency]` | Declares dependency on another plugin |
| `[BepInProcess]` | Specifies which process to load in |

### Configuration System

**File Reference:** `src/plugin/Configuration/ModConfig.cs:23-64`

```csharp
public static class ModConfig
{
    public static ConfigEntry<string> PlayerName { get; private set; }
    
    public static void Initialize(ConfigFile config)
    {
        PlayerName = config.Bind(
            "Player",                    // Section
            "PlayerName",                // Key
            "Player",                    // Default value
            "Your display name shown to other players"  // Description
        );
    }
    
    public static void Save()
    {
        configFile?.Save();
    }
}
```

**Accessing config values:**
```csharp
string name = ModConfig.PlayerName.Value;
```

### Logging

**File Reference:** `src/plugin/Plugin.cs:66,99-100`

```csharp
internal static new ManualLogSource Log;

public override void Load()
{
    Log = base.Log;
    Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    Log.LogWarning("Character icons already added");
    Log.LogError($"Auto-update check failed: {ex.Message}");
    Log.LogDebug("Debug information");  // Only shown in debug builds
}
```

### IL2CPP-Specific Considerations

**ClassInjector for Custom Components**

**File Reference:** `src/plugin/Plugin.cs:105-128`

```csharp
// Register custom MonoBehaviour types so Unity can instantiate them
ClassInjector.RegisterTypeInIl2Cpp<NetPlayer>();
ClassInjector.RegisterTypeInIl2Cpp<CoroutineRunner>();
ClassInjector.RegisterTypeInIl2Cpp<MainThreadDispatcher>();
```

**CRITICAL:** Any custom class inheriting from `MonoBehaviour` must be registered via `ClassInjector.RegisterTypeInIl2Cpp<T>()` before use.

---

## 2. Harmony Patching Fundamentals

### What is Harmony?

Harmony is a library for patching, replacing, and decorating .NET methods at runtime. It works by manipulating IL code during JIT compilation.

### Patch Types

| Patch Type | When It Runs | Return Value | Use Case |
|------------|--------------|--------------|----------|
| **Prefix** | Before original | `bool` (false = skip original) | Validation, early returns, skipping original |
| **Postfix** | After original | `void` | Side effects, modifying results |
| **Transpiler** | During IL compilation | `IEnumerable<CodeInstruction>` | IL-level modification |
| **Finalizer** | After exception handling | `void` | Exception handling, cleanup |

### Patch Attributes and Method Signatures

**File Reference:** `src/plugin/Patches/PlayerHealth.cs:9-10`

```csharp
[HarmonyPatch(typeof(PlayerHealth))]
internal static class PlayerHealthPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(PlayerHealth.Tick))]
    public static bool Tick_Prefix(PlayerHealth __instance)
    {
        // Patch logic
        return true;  // true = run original, false = skip
    }
}
```

### Special Parameter Names

| Name | Type | Description |
|------|------|-------------|
| `__instance` | ClassType | Reference to the instance (for instance methods) |
| `__result` | ReturnType (ref) | Return value (Postfix only, must be `ref`) |
| `__state` | Any (ref) | Passes data from Prefix to Postfix |
| `__0`, `__1`, ... | ParamType | Original method parameters by position |
| `___fieldName` | FieldType | Access private instance fields (three underscores) |

### Accessing Original Method Parameters

**Named parameters (same names as original):**
```csharp
[HarmonyPrefix]
[HarmonyPatch(nameof(Enemy.Damage))]
public static bool Damage_Prefix(Enemy __instance, DamageContainer damageContainer)
{
    // damageContainer is the original parameter
}
```

**Positional parameters:**
```csharp
[HarmonyPrefix]
[HarmonyPatch(nameof(SomeMethod))]
public static bool SomeMethod_Prefix(int __0, string __1)  // __0 = first param, __1 = second
```

### Method Targeting

**By name (simplest):**
```csharp
[HarmonyPatch(nameof(PlayerHealth.PlayerDied))]
```

**By parameter types (overload resolution):**
```csharp
[HarmonyPatch(nameof(Enemy.EnemyDied), typeof(DamageContainer))]
[HarmonyPatch(nameof(Enemy.EnemyDied), new System.Type[0])]  // Parameterless overload
```

**Full type specification:**
```csharp
[HarmonyPatch(nameof(EnemyManager.SpawnEnemy), 
    [typeof(EnemyData), typeof(Vector3), typeof(int), typeof(bool), typeof(EEnemyFlag), typeof(bool), typeof(float)])]
```

---

## 3. Common Patching Patterns

### Replacing Method Logic Entirely

**File Reference:** `src/plugin/Patches/Enemies/Enemy.cs:258-270`

Return `false` from Prefix to skip the original method entirely:

```csharp
[HarmonyPrefix]
[HarmonyPatch(nameof(Enemy.IsRunningFromPlayer))]
public static bool IsRunningFromPlayer_Prefix(ref bool __result)
{
    if (!synchronizationService.HasNetplaySessionStarted())
    {
        return true;  // Run original
    }
    
    __result = false;  // Set the return value
    return false;      // Skip original method
}
```

### Conditional Execution / Skipping Original

**File Reference:** `src/plugin/Patches/PlayerHealth.cs:33-49`

```csharp
[HarmonyPrefix]
[HarmonyPatch(nameof(PlayerHealth.Tick))]
public static bool Tick_Prefix(PlayerHealth __instance)
{
    if (!synchronizationService.HasNetplaySessionStarted())
    {
        return true;  // Run original when not in netplay
    }
    
    var isRemotePlayer = playerManagerService.IsRemotePlayerHealth(__instance);
    if (isRemotePlayer)
    {
        return false;  // Skip original for remote players
    }
    
    return true;  // Run original for local player
}
```

### Modifying Return Values (Postfix with __result)

**File Reference:** `src/plugin/Patches/GameManager.cs:17-38`

```csharp
[HarmonyPrefix]
[HarmonyPatch(nameof(GameManager.GetPlayerInventory))]
public static bool GetPlayerInventory_Prefix(ref PlayerInventory __result)
{
    if (!synchronizationService.HasNetplaySessionStarted())
    {
        return true;
    }
    
    var netPlayer = playerManagerService.GetNetPlayerByNetplayId(peaked.Value);
    if (netPlayer != null)
    {
        __result = netPlayer.Inventory;
        return false;  // Return modified result, skip original
    }
    return true;
}
```

**Postfix modification:**
```csharp
[HarmonyPostfix]
[HarmonyPatch(nameof(EnemyManager.GetNumMaxEnemies))]
public static void GetNumMaxEnemies(ref int __result)
{
    if (!synchronizationService.HasNetplaySessionStarted()) return;
    __result = 1000;  // Override the return value
}
```

### Accessing Private Members

**Using DynamicData (MonoMod.Utils):**

**File Reference:** `src/plugin/Patches/Pickup.cs:36-37`

```csharp
var dynPickup = DynamicData.For(__instance);
var ownerId = dynPickup.Get<uint?>("ownerId");
dynPickup.Set("targetId", host.ConnectionId);
dynPickup.Data.Clear();
```

**Using AccessTools (HarmonyLib):**

**File Reference:** `src/plugin/Helpers/Helper.cs:30-31`

```csharp
// Get declared methods (includes private)
var methods = AccessTools.GetDeclaredMethods(typeof(UnityEngine.Object))
    .FirstOrDefault(m => m.Name == "FindObjectsByType");

// Get property getter/setter
var getter = AccessTools.PropertyGetter(typeof(Renderer), "sharedMaterials");
var setter = AccessTools.PropertySetter(typeof(Renderer), "sharedMaterials");

// Get constructor
var ctor = AccessTools.Constructor(someType, [typeof(int)]);

// Get declared constructors
var ctors = AccessTools.GetDeclaredConstructors(objectPoolType);
```

### Passing Data Between Prefix and Postfix (__state)

**File Reference:** `src/plugin/Patches/Pickup.cs:22-59`

```csharp
[HarmonyPrefix]
[HarmonyPatch(nameof(Pickup.ApplyPickup))]
public static bool ApplyPickup_Prefix(Pickup __instance, ref bool? __state)
{
    __state = true;  // Default: apply pickup
    
    var isRemote = playerManagerService.IsRemoteConnectionId(ownerId.Value);
    if (isRemote)
    {
        __state = false;  // Mark as skipped
        return false;
    }
    
    return true;
}

[HarmonyPostfix]
[HarmonyPatch(nameof(Pickup.ApplyPickup))]
public static void ApplyPickup_Postfix(Pickup __instance, bool? __state)
{
    if (__state.HasValue && !__state.Value)
    {
        // Prefix marked this as skipped - cleanup
        DynamicData.For(__instance).Data.Clear();
        return;
    }
}
```

### Patching Property Getters/Setters

**File Reference:** `src/plugin/Patches/Unity/UnityComponent.cs:18-20`

```csharp
[HarmonyPatch(typeof(Transform))]
internal static class TransformPatches
{
    [HarmonyPrefix]
    [HarmonyPatch("get_position")]  // Property getter
    public static bool get_position_Prefix(Transform __instance, ref Vector3 __result)
    {
        __result = someOtherPosition;
        return false;
    }
}
```

---

## 4. Advanced Harmony Techniques

### Transpilers

Transpilers modify IL code directly. Not used in this codebase but here's the pattern:

```csharp
[HarmonyTranspiler]
[HarmonyPatch(nameof(SomeMethod))]
public static IEnumerable<CodeInstruction> SomeMethod_Transpiler(
    IEnumerable<CodeInstruction> instructions, 
    ILGenerator generator)
{
    var matcher = new CodeMatcher(instructions, generator);
    
    // Find and replace IL instructions
    matcher.MatchForward(false, 
        new CodeMatch(OpCodes.Call, typeof(SomeClass).GetMethod("SomeMethod")))
           .Set(OpCodes.Call, typeof(MyPatch).GetMethod("MyReplacement"));
    
    return matcher.InstructionEnumeration();
}
```

### Patch Priorities

Control patch execution order when multiple mods patch the same method:

```csharp
[HarmonyPrefix]
[HarmonyPriority(Priority.First)]  // Run before other prefixes
public static void MyPrefix() { }

[HarmonyPostfix]
[HarmonyPriority(Priority.Last)]   // Run after other postfixes
public static void MyPostfix() { }
```

### Reverse Patches

Create a method that calls the original implementation:

```csharp
[HarmonyReversePatch]
[HarmonyPatch(typeof(TargetClass), nameof(TargetClass.TargetMethod))]
public static void TargetMethod_ReversePatch(object instance)
{
    // This body is ignored - it's a placeholder
    throw new NotImplementedException();
}

// Usage: calls original TargetClass.TargetMethod
TargetMethod_ReversePatch(instance);
```

---

## 5. Unity-Specific Considerations

### MonoBehaviour Lifecycle Methods

**File Reference:** `src/plugin/Scripts/NetPlayer/NetPlayer.cs:59-104`

```csharp
public class NetPlayer : MonoBehaviour
{
    protected void Awake()   // Called when component is created
    {
        interpolator = gameObject.AddComponent<PlayerInterpolator>();
    }
    
    protected void Start()   // Called after Awake, before first Update
    {
        // Initialization that depends on other components
    }
    
    protected void Update()  // Called every frame
    {
        UpdateNameplateText();
    }
    
    public void FixedUpdate()  // Called at fixed intervals (physics)
    {
        inventory.PhysicsTick();
    }
    
    private void OnDestroy()  // Called when component is destroyed
    {
        Destroy();
    }
}
```

### GameObject and Component Manipulation

**File Reference:** `src/plugin/Plugin.cs:197-215`

```csharp
// Create new GameObject
var go = new GameObject("MainThreadDispatcher");

// Prevent destruction on scene load
GameObject.DontDestroyOnLoad(go);

// Add component
go.AddComponent<MainThreadDispatcher>();

// Find existing objects
var gameObject = Il2CppFindHelper.FindAllGameObjects()
    .FirstOrDefault(go => go.GetComponent<AchievementPopup>() != null);
```

### Instantiating Prefabs

**File Reference:** `src/plugin/Scripts/NetPlayer/NetPlayer.cs:186-187,352`

```csharp
var characterData = DataManager.Instance.GetCharacterData(eCharacter);
this.Model = GameObject.Instantiate(characterData.prefab);

var attack = GameObject.Instantiate(weapon.weaponData.attack);
```

### Unity's Serialization and Inspector

IL2CPP games have Unity's serialization handled differently. Use `DynamicData` to access serialized fields at runtime.

### DontDestroyOnLoad Pattern

**File Reference:** `src/plugin/Plugin.cs:197-215`

For objects that should persist across scene changes:

```csharp
var go = new GameObject("NetworkHandler");
GameObject.DontDestroyOnLoad(go);
NetworkHandler = goNetworkHandler.AddComponent<NetworkHandler>();
```

### IL2CPP Array Handling

**File Reference:** `src/plugin/Helpers/Helper.cs:43-44,136-140`

```csharp
// IL2CPP arrays are Il2CppArrayBase<T>
if (result is Il2CppArrayBase<GameObject> array)
    return [.. array];  // Convert to managed array

// Setting array values in IL2CPP context
object il2cppArr = sharedMaterialsiI2cppArrayCtor.Invoke([materials.Length]);
for (int i = 0; i < materials.Length; i++)
{
    sharedMaterialsiL2cppArrayIndexer.SetValue(il2cppArr, materials[i], [i]);
}
```

---

## 6. Debugging and Troubleshooting

### BepInEx Console and Log Files

**Log location:** `BepInEx/LogOutput.log`

**Log levels:**
- `Log.LogDebug()` - Debug info (only in debug builds)
- `Log.LogInfo()` - General information
- `Log.LogWarning()` - Warnings
- `Log.LogError()` - Errors

### Common Patch Failures

| Error | Cause | Solution |
|-------|-------|----------|
| `Method not found` | Wrong method signature | Check parameter types, use `new Type[]` for overloads |
| `Harmony patching failed` | Exception during patch | Check inner exception, verify target method exists |
| `ClassInjector failed` | Type already registered or invalid | Check if type was already registered |
| `NullReferenceException` | __instance is null or missing service | Add null checks |

### Verifying Patches

```csharp
try
{
    var harmony = new HarmonyLib.Harmony(MyPluginInfo.PLUGIN_GUID);
    harmony.PatchAll();
}
catch (Exception ex)
{
    Log.LogError($"Harmony patching failed: {ex}");
}
```

### Common Gotchas

1. **Returning false from Prefix without setting __result**: Will return default value
2. **Forgetting `ref` on __result**: Won't modify the return value
3. **Not checking for null**: Unity objects can be destroyed at any time
4. **Thread safety**: Service injection happens on main thread, cache references
5. **IL2CPP type mismatches**: Use `Il2CppSystem.Action` instead of `System.Action`

---

## 7. Code Examples

### Basic Plugin Structure

**File:** `src/plugin/Plugin.cs`

```csharp
using BepInEx;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;

namespace MegabonkTogether
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BasePlugin
    {
        public static Plugin Instance = null!;
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Instance = this;
            Log = base.Log;
            
            // Initialize config
            ModConfig.Initialize(Config);
            
            // Register custom MonoBehaviours
            ClassInjector.RegisterTypeInIl2Cpp<MyCustomComponent>();
            
            // Apply Harmony patches
            var harmony = new HarmonyLib.Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
            
            // Create persistent objects
            var go = new GameObject("MyPersistentObject");
            GameObject.DontDestroyOnLoad(go);
            go.AddComponent<MyCustomComponent>();
        }
    }
}
```

### Simple Prefix/Postfix Patches

**File:** `src/plugin/Patches/PlayerHealth.cs`

```csharp
using HarmonyLib;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(PlayerHealth))]
    internal static class PlayerHealthPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(PlayerHealth.PlayerDied))]
        public static void PlayerDied_Postfix(PlayerHealth __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted()) return;
            synchronizationService.OnPlayerDied();
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(nameof(PlayerHealth.Tick))]
        public static bool Tick_Prefix(PlayerHealth __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted()) return true;
            return !playerManagerService.IsRemotePlayerHealth(__instance);
        }
    }
}
```

### Config File Usage

**File:** `src/plugin/Configuration/ModConfig.cs`

```csharp
using BepInEx.Configuration;

namespace MegabonkTogether.Configuration
{
    public static class ModConfig
    {
        private static ConfigFile configFile;
        
        public static ConfigEntry<string> PlayerName { get; private set; }
        public static ConfigEntry<bool> CheckForUpdates { get; private set; }
        
        public static void Initialize(ConfigFile config)
        {
            configFile = config;
            
            PlayerName = config.Bind(
                "Player",
                "PlayerName",
                "Player",
                "Your display name"
            );
            
            CheckForUpdates = config.Bind(
                "Updates",
                "CheckForUpdates",
                true,
                "Check for updates on startup"
            );
        }
        
        public static void Save() => configFile?.Save();
    }
}
```

### Cross-Plugin Dependencies

```csharp
[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
[BepInDependency("com.example.otherplugin", BepInDependency.DependencyFlags.HardDependency)]
public class Plugin : BasePlugin
{
    public override void Load()
    {
        // Other plugin is guaranteed to be loaded
        var otherPlugin = BepInEx.Bootstrap.Chainloader.Instance.Plugins
            .First(p => p.Metadata.GUID == "com.example.otherplugin");
    }
}
```

### Complex Patch with Multiple Overloads

**File:** `src/plugin/Patches/Enemies/Enemy.cs:86-122`

```csharp
[HarmonyPatch(typeof(Enemy))]
internal static class EnemyPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Enemy.EnemyDied), typeof(DamageContainer))]
    public static void EnemyDied_Postfix(Enemy __instance, DamageContainer dc)
    {
        if (!synchronizationService.HasNetplaySessionStarted()) return;
        uint? ownerId = DynamicData.For(dc).Get<uint?>("ownerId");
        synchronizationService.OnEnemyDied(__instance, ownerId);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Enemy.EnemyDied), new System.Type[0])]
    public static void EnemyDiedWithoutDc_Postfix(Enemy __instance)
    {
        if (!synchronizationService.HasNetplaySessionStarted()) return;
        synchronizationService.OnEnemyDied(__instance);
    }
}
```

### Intercepting Property Access

**File:** `src/plugin/Patches/Unity/UnityComponent.cs`

```csharp
[HarmonyPatch(typeof(Transform))]
internal static class TransformPatches
{
    [HarmonyPrefix]
    [HarmonyPatch("get_position")]
    public static bool get_position_Prefix(Transform __instance, ref Vector3 __result)
    {
        if (!synchronizationService.HasNetplaySessionStarted()) return true;
        
        if (__instance.name == "Hips" && playerManagerService.PeakNetplayerPositionRequest().HasValue)
        {
            var netPlayer = playerManagerService.GetNetPlayerByNetplayId(...);
            if (netPlayer != null)
            {
                __result = netPlayer.Model.transform.position;
                return false;  // Skip original, return modified value
            }
        }
        return true;
    }
}
```

---

## Quick Reference Card

### Harmony Patch Method Signatures

```csharp
// Prefix - runs before original
public static bool MethodName_Prefix(ClassName __instance, ref ReturnType __result, ParamType param)
// Return true = run original, false = skip original

// Postfix - runs after original  
public static void MethodName_Postfix(ClassName __instance, ref ReturnType __result)
// Can modify __result to change return value

// __state - pass data between prefix/postfix
public static bool Prefix(ref SomeType __state) { __state = value; }
public static void Postfix(SomeType __state) { /* use __state */ }
```

### DynamicData Operations

```csharp
var dyn = DynamicData.For(instance);
var value = dyn.Get<T>("fieldName");       // Get field
dyn.Set("fieldName", value);               // Set field
dyn.Data.Clear();                          // Clear stored data
```

### ClassInjector Registration

```csharp
// MUST call before using custom MonoBehaviour
ClassInjector.RegisterTypeInIl2Cpp<MyMonoBehaviour>();
```

### BepInEx Logging

```csharp
Plugin.Log.LogInfo("Info message");
Plugin.Log.LogWarning("Warning message");
Plugin.Log.LogError("Error message");
Plugin.Log.LogDebug("Debug message");
```

---

## Project File Locations

| Component | Path |
|-----------|------|
| Main Plugin | `src/plugin/Plugin.cs` |
| Configuration | `src/plugin/Configuration/ModConfig.cs` |
| Harmony Patches | `src/plugin/Patches/**/*.cs` |
| Custom MonoBehaviours | `src/plugin/Scripts/**/*.cs` |
| Service Layer | `src/plugin/Services/*.cs` |
| Helpers/Utilities | `src/plugin/Helpers/*.cs` |
| Extensions | `src/plugin/Extensions/*.cs` |
| Project File | `src/plugin/MegabonkTogether.Plugin.csproj` |
