$ErrorActionPreference = "Stop"

Write-Host "Building Megabonk Together for Windows..." -ForegroundColor Cyan

# Determine game path
$GamePath = $env:MEGABONK_PATH

if (-not $GamePath) {
    # Common Steam installation paths
    $SteamPaths = @(
        "${env:ProgramFiles(x86)}\Steam\steamapps\common\Megabonk",
        "C:\SteamLibrary\steamapps\common\Megabonk",
        "D:\SteamLibrary\steamapps\common\Megabonk",
        "${env:USERPROFILE}\Steam\steamapps\common\Megabonk"
    )
    
    foreach ($Path in $SteamPaths) {
        if (Test-Path "$Path\Megabonk.exe") {
            $GamePath = $Path
            break
        }
    }
}

if (-not $GamePath) {
    Write-Host "ERROR: Could not find Megabonk installation." -ForegroundColor Red
    Write-Host "Set MEGABONK_PATH environment variable or ensure Steam default path exists." -ForegroundColor Yellow
    Write-Host "Example: `$env:MEGABONK_PATH = 'C:\Games\Megabonk'" -ForegroundColor Yellow
    exit 1
}

Write-Host "Using game path: $GamePath" -ForegroundColor Green

# Create Directory.Build.props if it doesn't exist
$PropsFile = "Directory.Build.props"
if (-not (Test-Path $PropsFile)) {
    Write-Host "Creating $PropsFile..." -ForegroundColor Yellow
    
    $PropsContent = @"
<Project>
  <PropertyGroup>
    <MegabonkPath>$GamePath</MegabonkPath>
  </PropertyGroup>
</Project>
"@
    
    $PropsContent | Out-File -FilePath $PropsFile -Encoding utf8
}

# Build the project
Write-Host "Running dotnet build..." -ForegroundColor Yellow
dotnet build

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build complete!" -ForegroundColor Green
    
    $PluginPath = "$GamePath\BepInEx\plugins\MegabonkTogether"
    if (Test-Path "$GamePath\Megabonk.exe") {
        Write-Host "Files deployed to: $PluginPath" -ForegroundColor Green
    } else {
        Write-Host "WARNING: Megabonk.exe not found at $GamePath" -ForegroundColor Yellow
    }
} else {
    Write-Host "Build failed with exit code $LASTEXITCODE" -ForegroundColor Red
    exit $LASTEXITCODE
}
