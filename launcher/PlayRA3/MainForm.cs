using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PlayRA3
{
    public class MainForm : Form
    {
        // UI Colors - Red Alert theme
        private static readonly Color BackgroundDark = Color.FromArgb(13, 13, 13);
        private static readonly Color BackgroundMedium = Color.FromArgb(26, 26, 26);
        private static readonly Color BackgroundLight = Color.FromArgb(45, 45, 45);
        private static readonly Color PrimaryRed = Color.FromArgb(227, 27, 35);
        private static readonly Color PrimaryRedDark = Color.FromArgb(184, 20, 25);
        private static readonly Color TextPrimary = Color.White;
        private static readonly Color TextSecondary = Color.FromArgb(179, 179, 179);
        private static readonly Color TextMuted = Color.FromArgb(102, 102, 102);
        private static readonly Color SuccessGreen = Color.FromArgb(34, 197, 94);

        // Controls
        private Panel titleBar = null!;
        private Label titleLabel = null!;
        private Button minimizeBtn = null!;
        private Button closeBtn = null!;
        
        private Label heroTitle = null!;
        private Label heroSubtitle = null!;
        
        private Panel statusPanel = null!;
        private Panel statusDot = null!;
        private Label statusText = null!;
        private Label statusSubtext = null!;
        private Button settingsBtn = null!;
        
        private RadioButton hostRadio = null!;
        private RadioButton joinRadio = null!;
        
        private Panel ipPanel = null!;
        private TextBox ipTextBox = null!;
        
        private Button launchBtn = null!;
        private Label footerLabel = null!;
        private Label gamePathLabel = null!;

        // State
        private Process? _serverProcess;
        private LauncherSettings _settings = null!;
        private readonly string _settingsPath;
        private bool _isHostMode = true;
        private bool _isDragging;
        private Point _dragOffset;

        private static readonly string[] CdKeyRegistryPaths = new[]
        {
            @"SOFTWARE\Electronic Arts\Electronic Arts\Red Alert 3\ergc",
            @"SOFTWARE\WOW6432Node\Electronic Arts\Electronic Arts\Red Alert 3\ergc"
        };

        public MainForm()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            _settingsPath = Path.Combine(appDir, "launcher_settings.json");
            
            InitializeForm();
            InitializeControls();
            LoadSettings();
            UpdateUI();
        }

        private void InitializeForm()
        {
            this.Text = "RA3 Private Server Launcher";
            this.Size = new Size(460, 580);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = BackgroundDark;
            this.DoubleBuffered = true;
            this.MaximizeBox = false;
        }

        private void InitializeControls()
        {
            // Title Bar
            titleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                BackColor = BackgroundDark
            };
            titleBar.MouseDown += TitleBar_MouseDown;
            titleBar.MouseMove += TitleBar_MouseMove;
            titleBar.MouseUp += TitleBar_MouseUp;

            var logoPanel = new Panel
            {
                Size = new Size(28, 28),
                Location = new Point(15, 8),
                BackColor = PrimaryRed
            };
            logoPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var font = new Font("Segoe UI", 10, FontStyle.Bold);
                TextRenderer.DrawText(e.Graphics, "RA3", font, new Point(0, 5), TextPrimary);
            };

            titleLabel = new Label
            {
                Text = "RA3 LAUNCHER",
                Location = new Point(50, 12),
                AutoSize = true,
                ForeColor = TextSecondary,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            closeBtn = CreateIconButton("X", new Point(this.Width - 45, 5));
            closeBtn.Click += (s, e) => { CleanupAndClose(); };
            closeBtn.MouseEnter += (s, e) => closeBtn.BackColor = PrimaryRed;
            closeBtn.MouseLeave += (s, e) => closeBtn.BackColor = Color.Transparent;

            minimizeBtn = CreateIconButton("_", new Point(this.Width - 85, 5));
            minimizeBtn.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            titleBar.Controls.AddRange(new Control[] { logoPanel, titleLabel, minimizeBtn, closeBtn });
            this.Controls.Add(titleBar);

            // Main Content Panel
            var mainPanel = new Panel
            {
                Location = new Point(25, 55),
                Size = new Size(this.Width - 50, this.Height - 70),
                BackColor = BackgroundDark
            };

            int y = 0;

            // Hero Section
            heroTitle = new Label
            {
                Text = "RED ALERT 3",
                Location = new Point(0, y),
                Size = new Size(mainPanel.Width, 40),
                ForeColor = PrimaryRed,
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            y += 45;

            heroSubtitle = new Label
            {
                Text = "Private Server Launcher - BYON Edition",
                Location = new Point(0, y),
                Size = new Size(mainPanel.Width, 20),
                ForeColor = TextSecondary,
                Font = new Font("Segoe UI", 9),
                TextAlign = ContentAlignment.MiddleCenter
            };
            y += 40;

            // Status Panel
            statusPanel = CreateCard(0, y, mainPanel.Width, 60);
            
            statusDot = new Panel
            {
                Size = new Size(10, 10),
                Location = new Point(15, 25),
                BackColor = SuccessGreen
            };
            statusDot.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(statusDot.BackColor);
                e.Graphics.FillEllipse(brush, 0, 0, 9, 9);
            };

            statusText = new Label
            {
                Text = "Ready to Play",
                Location = new Point(35, 15),
                Size = new Size(250, 18),
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };

            statusSubtext = new Label
            {
                Text = "All components detected",
                Location = new Point(35, 35),
                Size = new Size(250, 16),
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 9)
            };

            settingsBtn = new Button
            {
                Text = "Settings",
                Size = new Size(70, 30),
                Location = new Point(statusPanel.Width - 85, 15),
                FlatStyle = FlatStyle.Flat,
                BackColor = BackgroundLight,
                ForeColor = TextSecondary,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand
            };
            settingsBtn.FlatAppearance.BorderSize = 0;
            settingsBtn.Click += SettingsBtn_Click;

            statusPanel.Controls.AddRange(new Control[] { statusDot, statusText, statusSubtext, settingsBtn });
            y += 75;

            // Mode Selection
            var modeLabel = new Label
            {
                Text = "CONNECTION MODE",
                Location = new Point(5, y),
                AutoSize = true,
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            y += 25;

            var modePanel = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(mainPanel.Width, 90),
                BackColor = Color.Transparent
            };

            hostRadio = CreateModeRadio("HOST", "Create Server", true, 0);
            joinRadio = CreateModeRadio("JOIN", "Connect to Host", false, modePanel.Width / 2 + 5);

            hostRadio.CheckedChanged += (s, e) =>
            {
                if (hostRadio.Checked)
                {
                    _isHostMode = true;
                    ipPanel.Visible = false;
                    launchBtn.Text = "HOST & LAUNCH";
                    UpdateModeRadioStyle(hostRadio, true);
                    UpdateModeRadioStyle(joinRadio, false);
                }
            };

            joinRadio.CheckedChanged += (s, e) =>
            {
                if (joinRadio.Checked)
                {
                    _isHostMode = false;
                    ipPanel.Visible = true;
                    launchBtn.Text = "JOIN & LAUNCH";
                    UpdateModeRadioStyle(hostRadio, false);
                    UpdateModeRadioStyle(joinRadio, true);
                    if (!string.IsNullOrEmpty(_settings?.LastHostIp))
                        ipTextBox.Text = _settings.LastHostIp;
                }
            };

            modePanel.Controls.AddRange(new Control[] { hostRadio, joinRadio });
            y += 105;

            // IP Input Panel (hidden by default)
            ipPanel = CreateCard(0, y, mainPanel.Width, 85);
            ipPanel.Visible = false;

            var ipLabel = new Label
            {
                Text = "HOST IP ADDRESS",
                Location = new Point(15, 12),
                AutoSize = true,
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };

            ipTextBox = new TextBox
            {
                Location = new Point(15, 35),
                Size = new Size(ipPanel.Width - 30, 28),
                BackColor = BackgroundDark,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 11)
            };

            var ipHint = new Label
            {
                Text = "Enter the VPN IP of your friend (e.g., 26.105.90.12)",
                Location = new Point(15, 65),
                AutoSize = true,
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 8)
            };

            ipPanel.Controls.AddRange(new Control[] { ipLabel, ipTextBox, ipHint });
            y += 100;

            // Launch Button
            launchBtn = new Button
            {
                Text = "HOST & LAUNCH",
                Location = new Point(0, y),
                Size = new Size(mainPanel.Width, 50),
                FlatStyle = FlatStyle.Flat,
                BackColor = PrimaryRed,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            launchBtn.FlatAppearance.BorderSize = 0;
            launchBtn.MouseEnter += (s, e) => launchBtn.BackColor = PrimaryRedDark;
            launchBtn.MouseLeave += (s, e) => launchBtn.BackColor = PrimaryRed;
            launchBtn.Click += LaunchBtn_Click;
            y += 70;

            // Footer
            footerLabel = new Label
            {
                Text = "Login in-game with any username/password",
                Location = new Point(0, y),
                Size = new Size(mainPanel.Width, 18),
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 9),
                TextAlign = ContentAlignment.MiddleCenter
            };
            y += 22;

            gamePathLabel = new Label
            {
                Text = "Game: Not configured",
                Location = new Point(0, y),
                Size = new Size(mainPanel.Width, 16),
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 8),
                TextAlign = ContentAlignment.MiddleCenter
            };

            mainPanel.Controls.AddRange(new Control[]
            {
                heroTitle, heroSubtitle, statusPanel, modeLabel, modePanel,
                ipPanel, launchBtn, footerLabel, gamePathLabel
            });

            this.Controls.Add(mainPanel);
        }

        private Button CreateIconButton(string text, Point location)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(35, 35),
                Location = location,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = TextSecondary,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) => btn.BackColor = BackgroundLight;
            btn.MouseLeave += (s, e) => btn.BackColor = Color.Transparent;
            return btn;
        }

        private Panel CreateCard(int x, int y, int width, int height)
        {
            var panel = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = BackgroundMedium
            };
            panel.Paint += (s, e) =>
            {
                using var pen = new Pen(BackgroundLight, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
            };
            return panel;
        }

        private RadioButton CreateModeRadio(string title, string subtitle, bool isChecked, int x)
        {
            var radio = new RadioButton
            {
                Location = new Point(x, 0),
                Size = new Size(195, 85),
                Appearance = Appearance.Button,
                FlatStyle = FlatStyle.Flat,
                BackColor = BackgroundMedium,
                ForeColor = TextPrimary,
                TextAlign = ContentAlignment.MiddleCenter,
                Checked = isChecked,
                Cursor = Cursors.Hand
            };
            radio.FlatAppearance.BorderSize = 2;
            radio.FlatAppearance.BorderColor = isChecked ? PrimaryRed : BackgroundLight;
            
            radio.Paint += (s, e) =>
            {
                e.Graphics.Clear(radio.BackColor);
                
                using var titleFont = new Font("Segoe UI", 12, FontStyle.Bold);
                using var subFont = new Font("Segoe UI", 9);
                
                var titleSize = e.Graphics.MeasureString(title, titleFont);
                var subSize = e.Graphics.MeasureString(subtitle, subFont);
                
                float totalHeight = titleSize.Height + subSize.Height + 5;
                float startY = (radio.Height - totalHeight) / 2;
                
                TextRenderer.DrawText(e.Graphics, title, titleFont,
                    new Point((int)(radio.Width - titleSize.Width) / 2, (int)startY),
                    TextPrimary);
                    
                TextRenderer.DrawText(e.Graphics, subtitle, subFont,
                    new Point((int)(radio.Width - subSize.Width) / 2, (int)(startY + titleSize.Height + 5)),
                    TextMuted);
            };

            return radio;
        }

        private void UpdateModeRadioStyle(RadioButton radio, bool selected)
        {
            radio.FlatAppearance.BorderColor = selected ? PrimaryRed : BackgroundLight;
            radio.BackColor = selected ? Color.FromArgb(26, 227, 27, 35) : BackgroundMedium;
            radio.Invalidate();
        }

        #region Window Dragging

        private void TitleBar_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _dragOffset = e.Location;
            }
        }

        private void TitleBar_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point currentScreenPos = PointToScreen(e.Location);
                Location = new Point(currentScreenPos.X - _dragOffset.X, currentScreenPos.Y - _dragOffset.Y);
            }
        }

        private void TitleBar_MouseUp(object? sender, MouseEventArgs e)
        {
            _isDragging = false;
        }

        #endregion

        #region Settings

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    string json = File.ReadAllText(_settingsPath);
                    _settings = JsonSerializer.Deserialize<LauncherSettings>(json) ?? new LauncherSettings();
                }
                else
                {
                    _settings = new LauncherSettings();
                }
            }
            catch
            {
                _settings = new LauncherSettings();
            }
        }

        private void SaveSettings()
        {
            try
            {
                if (!_isHostMode && !string.IsNullOrWhiteSpace(ipTextBox.Text))
                {
                    _settings.LastHostIp = ipTextBox.Text.Trim();
                }
                string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
            }
            catch { }
        }

        private void SettingsBtn_Click(object? sender, EventArgs e)
        {
            using var dialog = new SettingsForm(_settings);
            dialog.ShowDialog();
            // Always save and refresh UI after settings dialog closes
            SaveSettings();
            UpdateUI();
        }

        #endregion

        private void UpdateUI()
        {
            bool allGood = true;
            string message = "All components detected";

            if (string.IsNullOrEmpty(_settings.GamePath) || !File.Exists(Path.Combine(_settings.GamePath, "RA3.exe")))
            {
                allGood = false;
                message = "Game not found - configure in settings";
            }
            else if (!File.Exists(Path.Combine(_settings.GamePath, "winmm.dll")))
            {
                allGood = false;
                message = "Proxy DLL not installed - click Settings";
            }

            statusDot.BackColor = allGood ? SuccessGreen : Color.FromArgb(245, 158, 11);
            statusText.Text = allGood ? "Ready to Play" : "Setup Required";
            statusSubtext.Text = message;
            statusDot.Invalidate();

            gamePathLabel.Text = !string.IsNullOrEmpty(_settings.GamePath)
                ? $"Game: {_settings.GamePath}"
                : "Game: Click Settings to configure";
        }

        #region Launch Logic

        private async void LaunchBtn_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_settings.GamePath) || !File.Exists(Path.Combine(_settings.GamePath, "RA3.exe")))
            {
                MessageBox.Show("Please configure the game path in settings.", "Game Not Found",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string targetIp = "127.0.0.1";
            if (!_isHostMode)
            {
                targetIp = ipTextBox.Text.Trim();
                if (string.IsNullOrEmpty(targetIp) || !IsValidIp(targetIp))
                {
                    MessageBox.Show("Please enter a valid IP address.", "Invalid IP",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                _settings.LastHostIp = targetIp;
            }

            try
            {
                launchBtn.Enabled = false;
                launchBtn.Text = "Launching...";

                // Generate CD Key
                GenerateCdKey();

                // Write config
                WriteProxyConfig(targetIp);

                // Start server if hosting
                if (_isHostMode)
                {
                    if (!await StartServerAsync())
                    {
                        launchBtn.Enabled = true;
                        launchBtn.Text = _isHostMode ? "HOST & LAUNCH" : "JOIN & LAUNCH";
                        return;
                    }
                }

                // Launch game
                LaunchGame();
                SaveSettings();

                // Watch for game exit
                await WatchGameAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Launch Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                launchBtn.Enabled = true;
                launchBtn.Text = _isHostMode ? "HOST & LAUNCH" : "JOIN & LAUNCH";
            }
        }

        private void GenerateCdKey()
        {
            var random = new Random();
            string cdKey = "";
            for (int i = 0; i < 20; i++)
                cdKey += random.Next(16).ToString("X");

            foreach (string path in CdKeyRegistryPaths)
            {
                try
                {
                    using var key = Registry.LocalMachine.CreateSubKey(path);
                    key?.SetValue("", cdKey);
                    return;
                }
                catch { }
            }
        }

        private void WriteProxyConfig(string targetIp)
        {
            string configPath = Path.Combine(_settings.GamePath!, "config.json");
            var config = new
            {
                debug = new { showConsole = false, createLog = true, logDecryption = false, logLevelConsole = 0, logLevelFile = 0 },
                patches = new { SSL = true },
                proxy = new { enable = true, destinationPort = 18800, secure = false },
                game = new { gameKey = "" },
                hostnames = new
                {
                    host = targetIp, login = targetIp, gpcm = targetIp, peerchat = targetIp,
                    master = targetIp, natneg = targetIp, stats = targetIp, sake = targetIp,
                    server = targetIp, register = "", website = "", detour = "localhost", mac = "mac", tos = ""
                }
            };
            File.WriteAllText(configPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
        }

        private async Task<bool> StartServerAsync()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] paths = new[]
            {
                Path.Combine(appDir, "kirov_server.exe"),
                Path.Combine(appDir, "kirov_server"),
                Path.Combine(appDir, "kirov-server.exe"),
                Path.Combine(appDir, "kirov-server"),
                Path.Combine(appDir, "server", "kirov_server.exe"),
                Path.Combine(appDir, "server", "kirov_server"),
            };

            string? serverPath = null;
            foreach (string p in paths)
                if (File.Exists(p)) { serverPath = p; break; }

            if (serverPath == null)
            {
                MessageBox.Show("Server not found (kirov_server.exe).\nDownload from releases or build it.",
                    "Server Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            try
            {
                _serverProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = serverPath,
                    WorkingDirectory = Path.GetDirectoryName(serverPath),
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                // Give server time to initialize
                await Task.Delay(3000);

                // Under Wine/Proton, HasExited doesn't work reliably for native Linux processes
                // Just assume success if Process.Start didn't throw
                if (_serverProcess == null)
                {
                    MessageBox.Show("Server failed to start.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                statusText.Text = "Server Running";
                statusSubtext.Text = "Listening on localhost";
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Server error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void LaunchGame()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(_settings.GamePath!, "RA3.exe"),
                WorkingDirectory = _settings.GamePath,
                UseShellExecute = true
            });
        }

        private async Task WatchGameAsync()
        {
            await Task.Delay(5000);

            Process? gameProc = null;
            foreach (var p in Process.GetProcessesByName("RA3"))
            {
                gameProc = p;
                break;
            }

            if (gameProc != null)
            {
                statusText.Text = "Game Running";
                statusSubtext.Text = _isHostMode ? "Server active" : "Connected";
                await Task.Run(() => gameProc.WaitForExit());
            }

            if (_isHostMode) StopServer();
            statusText.Text = "Ready to Play";
            statusSubtext.Text = "Game closed";
        }

        private void StopServer()
        {
            try
            {
                _serverProcess?.Kill(true);
                _serverProcess?.Dispose();
            }
            catch { }
            finally { _serverProcess = null; }
        }

        private bool IsValidIp(string ip)
        {
            var parts = ip.Split('.');
            if (parts.Length != 4) return false;
            foreach (var p in parts)
                if (!int.TryParse(p, out int n) || n < 0 || n > 255) return false;
            return true;
        }

        #endregion

        private void CleanupAndClose()
        {
            StopServer();
            SaveSettings();
            Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            CleanupAndClose();
            base.OnFormClosing(e);
        }
    }

    public class LauncherSettings
    {
        public string? GamePath { get; set; }
        public string? LastHostIp { get; set; }
        public bool MinimizeToTray { get; set; } = true;
    }
}
