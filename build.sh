#!/bin/bash

# Exit on error
set -e

# Parse arguments
CONFIGURATION="Debug"
CLEAN=false
RESTORE=false

while [[ $# -gt 0 ]]; do
    case $1 in
        -c|--configuration|-Configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        --clean|-Clean)
            CLEAN=true
            shift
            ;;
        --restore|-Restore)
            RESTORE=true
            shift
            ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: $0 [-c|--configuration Debug|Release] [--clean] [--restore]"
            exit 1
            ;;
    esac
done

# Validate configuration
if [[ "$CONFIGURATION" != "Debug" && "$CONFIGURATION" != "Release" ]]; then
    echo "ERROR: Configuration must be Debug or Release"
    exit 1
fi

echo "Building Megabonk Together for Linux..."
echo "Configuration: $CONFIGURATION"

# Define the game path (edit this if your game is installed elsewhere)
GAME_PATH="${MEGABONK_PATH:-$HOME/.local/share/Steam/steamapps/common/Megabonk}"

echo "Using game path: $GAME_PATH"

# Create Directory.Build.props if it doesn't exist to override the path
if [ ! -f "Directory.Build.props" ]; then
    echo "Creating Directory.Build.props..."
    cat > Directory.Build.props <<EOF
<Project>
  <PropertyGroup>
    <MegabonkPath>$GAME_PATH</MegabonkPath>
  </PropertyGroup>
</Project>
EOF
fi

# Clean if requested
if [ "$CLEAN" = true ]; then
    echo "Cleaning build artifacts..."
    dotnet clean -c "$CONFIGURATION"
fi

# Restore if requested
if [ "$RESTORE" = true ]; then
    echo "Restoring packages..."
    dotnet restore
fi

# Build the project
echo "Running dotnet build ($CONFIGURATION)..."
dotnet build -c "$CONFIGURATION"

echo "Build complete!"
if [ -f "$GAME_PATH/Megabonk.exe" ]; then
    echo "Files deployed to: $GAME_PATH/BepInEx/plugins/MegabonkTogether/"
else
    echo "WARNING: Megabonk.exe not found at $GAME_PATH. Is the path correct? (Proton version required)"
fi
