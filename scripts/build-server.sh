#!/bin/bash
# Build the Kirov server using PyInstaller

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
SERVER_DIR="$PROJECT_ROOT/server/kirov-server-emulator"

cd "$SERVER_DIR"

echo "Building Kirov Server..."

# Create venv if needed
if [ ! -d ".venv" ]; then
    python3 -m venv .venv
fi

source .venv/bin/activate
pip install -q -r requirements.txt
pip install -q pyinstaller

# Use the included spec file if it exists, otherwise create basic build
if [ -f "kirov-server.spec" ]; then
    pyinstaller kirov-server.spec
else
    pyinstaller --onefile --noconsole --name kirov_server run_server.py
fi

deactivate

echo ""
echo "✅ Server build complete!"
echo "Output: $SERVER_DIR/dist/"
