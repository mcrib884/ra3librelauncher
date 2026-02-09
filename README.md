# RA3 Private Server Launcher (BYON Edition)

> **B**ring **Y**our **O**wn **N**etwork - A standalone launcher for hosting private Red Alert 3 co-op and multiplayer sessions over VPN.

![License](https://img.shields.io/badge/license-Educational-blue)
![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Proton-green)

## 🎮 Overview

This project provides a complete solution for playing Red Alert 3 online co-op and multiplayer with friends using VPN networks like **Radmin VPN**, **Hamachi**, or **ZeroTier**. No need for public GameSpy servers!

### Features

- ✅ **One-Click Hosting** - Start a private server instantly
- ✅ **Automatic CD Key Rotation** - No more "Key in Use" errors
- ✅ **No Admin Rights Required** - Uses DLL hooking, not hosts file
- ✅ **Proton Compatible** - Works on Linux via Steam Play
- ✅ **Modern UI** - Sleek dark theme with Red Alert aesthetics
- ✅ **Settings Persistence** - Remembers your last used IP

## 📦 Components

| Component | Description | Technology |
|-----------|-------------|------------|
| **PlayRA3.exe** | The launcher UI | C# WPF (.NET 8) |
| **kirov_server.exe** | GameSpy emulator backend | Python (PyInstaller) |
| **winmm.dll** | Proxy hook for traffic redirection | C++ |

## 🚀 Quick Start

### Installation

1. **Download** the latest release (or build from source)
2. **Extract** all files to your RA3 game folder (where `RA3.exe` is located)
3. **Copy** proxy files to the `Data` subfolder:
   ```
   Data/winmm.dll
   Data/libeay32.dll  (if included)
   Data/ssleay32.dll  (if included)
   ```
4. **Run** `PlayRA3.exe`

### For the Host
1. Open `PlayRA3.exe`
2. Click **"HOST"** mode
3. Click **"⚡ HOST & LAUNCH"**
4. Share your VPN IP with friends
5. In-game: Go to **Online** → Login with any username/password

### For Clients (Joining)
1. Open `PlayRA3.exe`
2. Click **"JOIN"** mode
3. Enter the host's VPN IP address
4. Click **"⚡ JOIN & LAUNCH"**
5. In-game: Go to **Online** → Login with any username/password

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         PlayRA3.exe                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌────────────────┐  │
│  │  Generate CD Key │  │  Write Config   │  │ Start Server   │  │
│  │  (Registry)      │  │  (config.json)  │  │ (if hosting)   │  │
│  └─────────────────┘  └─────────────────┘  └────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                          RA3.exe                                │
│                             │                                   │
│                 Loads winmm.dll (Proxy Hook)                    │
│                             │                                   │
│           ┌─────────────────┴─────────────────┐                 │
│           ▼                                   ▼                 │
│   *.gamespy.com traffic      ──►      Redirect to config IP    │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Kirov Server (Host Only)                     │
│  ┌─────────┐ ┌──────────┐ ┌──────────┐ ┌─────────┐              │
│  │  FESL   │ │ Peerchat │ │    GP    │ │ NATNEG  │              │
│  │ :18800  │ │  :6667   │ │  :29900  │ │ :27901  │              │
│  └─────────┘ └──────────┘ └──────────┘ └─────────┘              │
└─────────────────────────────────────────────────────────────────┘
```

## 🛠️ Building from Source

### Prerequisites

- **.NET 8 SDK** ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- **Python 3.11+** with pip
- **PyInstaller** (`pip install pyinstaller`)

### Build the Launcher

```bash
cd launcher/PlayRA3
dotnet build -c Release

# For distributable single-file:
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### Build the Server

```bash
cd server/kirov-server-emulator
python3 -m venv .venv
source .venv/bin/activate  # On Windows: .venv\Scripts\activate
pip install -r requirements.txt
pip install pyinstaller
pyinstaller --onefile --noconsole --name kirov_server run_server.py
```

### Get the Proxy DLL

**Option A:** Download pre-built from [ra3_game_proxy releases](https://github.com/sokie/ra3_game_proxy/releases)

**Option B:** Build on Windows (requires Visual Studio + vcpkg):
```bash
cd hook/ra3_game_proxy
# Follow instructions in that repo's README
```

### Full Build (All Components)

```bash
chmod +x scripts/build.sh
./scripts/build.sh
```

## 📁 Project Structure

```
ra3cooplauncher/
├── launcher/               # C# WPF Launcher
│   └── PlayRA3/
│       ├── MainWindow.xaml     # Main UI
│       ├── SettingsWindow.xaml # Settings dialog
│       └── Themes/             # Dark theme styles
├── server/                 # Python Kirov server (cloned)
│   └── kirov-server-emulator/
├── hook/                   # C++ proxy DLL (cloned)
│   └── ra3_game_proxy/
├── scripts/                # Build scripts
│   ├── build.sh            # Full build
│   ├── build-launcher.sh   # Launcher only
│   └── build-server.sh     # Server only
└── dist/                   # Build outputs
```

## 🔧 Configuration

The launcher automatically generates `config.json` in your game folder with the correct IP settings.

### Manual Config (Advanced)

```json
{
  "hostnames": {
    "host": "127.0.0.1",      // For hosting
    "login": "127.0.0.1",     // Or friend's VPN IP for joining
    "gpcm": "127.0.0.1",
    "peerchat": "127.0.0.1",
    "master": "127.0.0.1",
    "natneg": "127.0.0.1",
    ...
  }
}
```

## ❓ Troubleshooting

| Problem | Solution |
|---------|----------|
| "Key in Use" error | Should not happen - launcher auto-generates keys. Try restarting. |
| Can't connect to host | Verify both are on the same VPN and using correct IPs |
| Server won't start | May need elevated permissions for port 80. Try running as admin. |
| Game doesn't launch | Check that game path is correctly set in Settings |
| Proxy not working | Ensure `winmm.dll` is in the `Data/` subfolder, not the game root |

## ⚠️ Important Notes

- **VPN Required**: All players must be on the same VPN network
- **Proton Compatible**: Works with Steam Play / Proton on Linux
- **Firewall**: Host may need to allow ports 80, 6667, 18800, 27900-27901, 28910, 29900

## 📝 License

This project is for educational and game preservation purposes only.

## 🙏 Credits

- [Kirov Server Emulator](https://github.com/sokie/kirov-server-emulator) by sokie
- [RA3 Game Proxy](https://github.com/sokie/ra3_game_proxy) by sokie

---

*Red Alert 3 and Command & Conquer are trademarks of Electronic Arts Inc.*
