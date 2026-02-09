#!/bin/bash
# RA3 Private Server Launcher - Build Script
# Builds the launcher and packages everything into the release folder

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
RELEASE_DIR="$PROJECT_ROOT/release"

echo "========================================"
echo " RA3 Private Server Launcher - Build"
echo "========================================"

# Create release directory structure
mkdir -p "$RELEASE_DIR/proxy"

# ============================================
# Build Launcher
# ============================================
echo ""
echo "[1/4] Building Launcher..."
cd "$PROJECT_ROOT/launcher/PlayRA3"

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "$RELEASE_DIR"

echo "   -> PlayRA3.exe"

# ============================================
# Check Proxy Files
# ============================================
echo ""
echo "[2/4] Checking Proxy Files..."

if [ -f "$RELEASE_DIR/proxy/winmm.dll" ]; then
    echo "   -> Proxy files present"
else
    echo "   -> Downloading proxy..."
    cd "$RELEASE_DIR/proxy"
    curl -L -o Release.7z "https://github.com/sokie/ra3_game_proxy/releases/download/v1.0.2/Release.7z"
    7z x Release.7z -y
    rm Release.7z
    echo "   -> Proxy files downloaded"
fi

# ============================================
# Check Server
# ============================================
echo ""
echo "[3/4] Checking Server..."

if [ -f "$RELEASE_DIR/kirov_server.exe" ] || [ -f "$RELEASE_DIR/kirov_server" ] || [ -f "$RELEASE_DIR/kirov-server" ]; then
    echo "   -> Server file present"
else
    echo "   -> Server not found. Building..."
    bash -c 'cd '"$PROJECT_ROOT"'/server/kirov-server-emulator && python3.12 -m venv .venv 2>/dev/null || python3 -m venv .venv && source .venv/bin/activate && pip install -q -r requirements.txt pyinstaller && pyinstaller kirov-server.spec --distpath '"$RELEASE_DIR"' --noconfirm'
    echo "   -> Server built"
fi

# ============================================
# Server Config
# ============================================
echo ""
echo "[4/4] Creating Server Config..."

cat > "$RELEASE_DIR/config.json" << 'EOF'
{
  "irc": {
    "host": "0.0.0.0",
    "port": 6667,
    "server_name": "peerchat.ea.com"
  },
  "fesl": {
    "host": "0.0.0.0",
    "port": 18800
  },
  "gp": {
    "host": "0.0.0.0",
    "port": 29900
  },
  "natneg": {
    "host": "0.0.0.0",
    "port": 27901,
    "session_timeout": 30,
    "enabled": true
  },
  "master": {
    "host": "0.0.0.0",
    "port": 28910,
    "udp_port": 27900,
    "enabled": true
  },
  "relay": {
    "host": "0.0.0.0",
    "port_start": 50000,
    "port_end": 50999,
    "session_timeout": 120,
    "pair_ttl": 60,
    "enabled": false
  },
  "game": {
    "gamekey": "Cs2iIq"
  },
  "logging": {
    "level": "INFO"
  }
}
EOF
echo "   -> config.json created"

# ============================================
# Create README
# ============================================
cat > "$RELEASE_DIR/README.txt" << 'EOF'
RA3 Private Server Launcher - BYON Edition
============================================

INSTALLATION:
1. Copy this entire folder anywhere on your PC
2. Run PlayRA3.exe
3. Click Settings > Browse > Select your RA3.exe
4. Click Install to copy proxy files to game

HOSTING:
1. Select HOST mode
2. Click HOST & LAUNCH
3. Give your VPN IP to friends
4. In-game: Login with any username/password

JOINING:
1. Select JOIN mode
2. Enter host VPN IP
3. Click JOIN & LAUNCH
4. In-game: Login with any username/password

REQUIREMENTS:
- All players on same VPN (Radmin, Hamachi, ZeroTier)
- Works with Steam/Proton on Linux
EOF

echo ""
echo "========================================"
echo " Build Complete!"
echo "========================================"
echo ""
echo "Release folder: $RELEASE_DIR"
echo ""
ls -la "$RELEASE_DIR"
echo ""
