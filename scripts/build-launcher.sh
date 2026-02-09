#!/bin/bash
# Quick build script for just the launcher (faster iteration)

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

cd "$PROJECT_ROOT/launcher/PlayRA3"

echo "Building PlayRA3 Launcher..."
dotnet build -c Release

echo ""
echo "✅ Build complete!"
echo "Output: $PROJECT_ROOT/launcher/PlayRA3/bin/Release/net8.0-windows/"
