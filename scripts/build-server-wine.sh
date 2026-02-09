#!/bin/bash
# Build Kirov Server as Windows EXE using Wine + Python
# This creates a proper Windows executable that runs under Wine/Proton

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
SERVER_DIR="$PROJECT_ROOT/server/kirov-server-emulator"
RELEASE_DIR="$PROJECT_ROOT/release"

echo "========================================"
echo " Building Kirov Server (Windows EXE)"
echo "========================================"

# Find Wine Python
WINE_PYTHON=""
for path in \
    "$HOME/.wine/drive_c/users/$(whoami)/AppData/Local/Programs/Python/Python312/python.exe" \
    "$HOME/.wine/drive_c/Program Files/Python312/python.exe" \
    "$HOME/.wine/drive_c/Python312/python.exe" \
    "$HOME/.wine/drive_c/users/$(whoami)/AppData/Local/Programs/Python/Python311/python.exe" \
    "$HOME/.wine/drive_c/Program Files/Python311/python.exe"; do
    if [ -f "$path" ]; then
        WINE_PYTHON="$path"
        break
    fi
done

if [ -z "$WINE_PYTHON" ]; then
    echo "Error: Python not found in Wine. Install Python for Windows first:"
    echo "  wine /path/to/python-3.12-amd64.exe"
    exit 1
fi

echo "Using Python: $WINE_PYTHON"
echo ""
echo "Installing dependencies..."

cd "$SERVER_DIR"
wine "$WINE_PYTHON" -m pip install --upgrade pip -q 2>&1 | grep -v "fixme:" || true
wine "$WINE_PYTHON" -m pip install -r requirements.txt pyinstaller -q 2>&1 | grep -v "fixme:" || true

echo ""
echo "Building Windows executable..."

# Convert Linux path to Wine path
WINE_RELEASE_DIR=$(winepath -w "$RELEASE_DIR" 2>/dev/null || echo "Z:$RELEASE_DIR")

wine "$WINE_PYTHON" -m PyInstaller kirov-server.spec --distpath "$WINE_RELEASE_DIR" --noconfirm 2>&1 | grep -v "fixme:" || true

echo ""
echo "Done! Checking output..."
ls -la "$RELEASE_DIR"/*.exe 2>/dev/null || echo "No .exe files found"
