# Build & Run Instructions

> Agent-first documentation. Step-by-step build, deployment, and testing instructions.

## Prerequisites

### For Building
- **.NET 8.0 SDK** (targets .NET 6.0 but builds with 8.0 SDK)
- **Git**

### For Running (Plugin)
- **Megabonk** game (Steam)
- **BepInEx 6** (Bleeding Edge IL2CPP build)

### For Running (Server)
- **Docker** (optional, for containerized deployment)
- Or **.NET 6.0 Runtime**

## Environment Setup

### 1. Clone Repository

```bash
git clone https://github.com/Fcornaire/megabonk-together.git
cd megabonk-together
```

### 2. Configure Game Path

Create `Directory.Build.props` in project root:

**Option A: Automatic (Linux)**
```bash
./build.sh  # Creates the file automatically
```

**Option B: Manual**

```xml
<Project>
  <PropertyGroup>
    <MegabonkPath>/path/to/steam/steamapps/common/Megabonk</MegabonkPath>
  </PropertyGroup>
</Project>
```

**Path Locations:**
| Platform | Default Path |
|----------|--------------|
| Windows | `C:\Program Files (x86)\Steam\steamapps\common\Megabonk` |
| Linux | `~/.local/share/Steam/steamapps/common/Megabonk` |

## Building

### Quick Build (Linux)

```bash
./build.sh
```

This script:
1. Creates `Directory.Build.props` if missing
2. Runs `dotnet build`
3. Auto-deploys to game directory (if MegabonkPath set)

### Manual Build

```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build src/plugin/MegabonkTogether.Plugin.csproj
dotnet build src/server/MegabonkTogether.Server.csproj

# Build for release
dotnet build -c Release
```

### Build Output

```
src/plugin/bin/Debug/net6.0/
├── MegabonkTogether.dll        # Main plugin
├── MegabonkTogether.Common.dll # Shared library
├── LiteNetLib.dll
├── MemoryPack.dll
└── ... (other dependencies)

src/server/bin/Debug/net6.0/
├── MegabonkTogether.Server.dll
├── MegabonkTogether.Common.dll
└── ... (other dependencies)
```

## Game Installation (BepInEx)

### Windows

1. **Download BepInEx 6 IL2CPP**
   - URL: https://builds.bepinex.dev/projects/bepinex_be
   - Get: `BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.*`

2. **Extract to game directory**
   ```
   Megabonk/
   ├── BepInEx/
   │   ├── config/
   │   ├── core/
   │   └── plugins/
   ├── winhttp.dll
   ├── doorstop_config.ini
   └── Megabonk.exe
   ```

3. **First Run**
   - Launch game once
   - Wait for BepInEx to generate interop assemblies
   - Close game

4. **Install Plugin**
   ```
   Copy build output to:
   Megabonk/BepInEx/plugins/MegabonkTogether/
   ```

### Linux (Proton)

1. **Enable Proton**
   - Steam → Right-click Megabonk → Properties → Compatibility
   - Select **Proton 9.0** or **Proton Experimental**

2. **Set Launch Options**
   ```
   WINEDLLOVERRIDES="winhttp=n,b" %command%
   ```

3. **Install BepInEx (Windows version)**
   - Same steps as Windows above
   - Use Windows x64 BepInEx build

4. **First Run**
   - Launch game, wait for interop generation
   - Takes longer on first run (1-2 minutes on splash screen)

5. **Install Plugin**
   - Same as Windows

### Directory Structure After Installation

```
Megabonk/
├── BepInEx/
│   ├── config/
│   │   └── MegabonkTogether.cfg    # Generated on first run
│   ├── core/
│   ├── interop/                    # Generated IL2CPP assemblies
│   ├── plugins/
│   │   └── MegabonkTogether/
│   │       ├── MegabonkTogether.dll
│   │       ├── MegabonkTogether.Common.dll
│   │       └── ... (dependencies)
│   ├── unity-libs/                 # Generated Unity assemblies
│   └── LogOutput.log               # Console output
├── winhttp.dll                     # BepInEx hook
├── doorstop_config.ini
└── Megabonk.exe
```

## Running the Server

### Option 1: Docker (Recommended)

```bash
# Build image
docker build -t megabonk-server .

# Run container
docker run -d \
  -p 5432:5432 \
  -p 5678:5678/udp \
  --name megabonk-server \
  megabonk-server
```

### Option 2: Direct Execution

```bash
cd src/server

# Development
dotnet run

# Production
dotnet publish -c Release
./bin/Release/net6.0/MegabonkTogether.Server.dll
```

### Server Ports

| Port | Protocol | Purpose |
|------|----------|---------|
| 5432 | HTTP | WebSocket matchmaking |
| 5678 | UDP | NAT punch / Relay |

### Server Configuration

**File:** `src/server/appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "MegabonkTogether.Server.Services.WebSocketHandler": "Information",
      "MegabonkTogether.Server.Services.RendezVousServer": "Information"
    }
  }
}
```

## Configuration

### Plugin Config

**File:** `BepInEx/config/MegabonkTogether.cfg`

Generated on first run. Key settings:

```ini
[Network]
ServerUrl = wss://your-server.com/ws

[Player]
PlayerName = Player

[Updates]
CheckForUpdates = true

[Gameplay]
AllowSave = false
```

### Target Local Server

To connect to local server during development:

1. Edit `BepInEx/config/MegabonkTogether.cfg`
2. Change `ServerUrl`:
   ```ini
   [Network]
   ServerUrl = ws://127.0.0.1:5000/ws
   ```

## Testing

### Development Testing

1. Build and deploy plugin
2. Run server locally
3. Launch game via Steam
4. Monitor logs

```bash
# Monitor game logs
tail -f ~/.local/share/Steam/steamapps/common/Megabonk/BepInEx/LogOutput.log

# Monitor server logs
# (Output to console)
```

### Cross-Play Verification

1. **Join Test**: Join a room code from Windows player
2. **Host Test**: Host and provide code to Windows player
3. **Sync Check**: Verify enemies, items, movement synchronized

### Unit Tests

```bash
cd src/tests
dotnet test
```

## Troubleshooting

### Build Errors

| Error | Solution |
|-------|----------|
| `Assembly-CSharp not found` | Set correct `MegabonkPath` in `Directory.Build.props` |
| `Type 'X' not found` | BepInEx hasn't generated interop - run game first |
| `CS0131` null-conditional | Fixed in codebase - ensure using latest |

### Runtime Errors

| Error | Solution |
|-------|----------|
| `PAL_SEHException` (Linux native) | Use Proton instead |
| Game crashes on launch | Check BepInEx version matches (IL2CPP x64) |
| Button not appearing | Check `LogOutput.log` for plugin load errors |
| Can't connect | Check `ServerUrl` in config, verify server running |

### BepInEx Log

Check for errors:
```bash
grep -E "(ERROR|Exception)" BepInEx/LogOutput.log
```

## Release Builds

### Thunderstore Package

```bash
# Set environment variable
export THUNDERSTORE_BUILD=true

# Build
dotnet build -c Release

# Package will be in artifacts/
```

### GitHub Actions

Automatic builds configured in `.github/workflows/`

Triggered on:
- Push to main
- Tag creation

## Version Compatibility

| Mod Version | Game Version | BepInEx |
|-------------|--------------|---------|
| 2.0.2 | 1.0.49 | 6.0.0-be.752+ |
| 2.0.0 | 1.0.49 | 6.0.0-be.752+ |

**Warning:** Game updates may break the mod due to internal game changes.

## Hot Reload (Development)

During development, the post-build task auto-deploys:

```xml
<!-- In .csproj -->
<Target Name="PostBuild" AfterTargets="PostBuildEvent" Condition="...">
  <Exec Command="cp -r $(TargetDir)* $(PluginPath)/MegabonkTogether/" />
</Target>
```

To test changes:
1. Build
2. Restart game (BepInEx loads on startup)
