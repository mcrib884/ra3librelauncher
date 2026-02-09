using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PlayRA3
{
    public class SettingsForm : Form
    {
        private static readonly Color BackgroundDark = Color.FromArgb(13, 13, 13);
        private static readonly Color BackgroundMedium = Color.FromArgb(26, 26, 26);
        private static readonly Color BackgroundLight = Color.FromArgb(45, 45, 45);
        private static readonly Color PrimaryRed = Color.FromArgb(227, 27, 35);
        private static readonly Color TextPrimary = Color.White;
        private static readonly Color TextSecondary = Color.FromArgb(179, 179, 179);
        private static readonly Color TextMuted = Color.FromArgb(102, 102, 102);
        private static readonly Color SuccessGreen = Color.FromArgb(34, 197, 94);

        private readonly LauncherSettings _settings;
        private TextBox gamePathTextBox = null!;
        private Label proxyStatusLabel = null!;
        private Button installProxyBtn = null!;

        private bool _isDragging;
        private Point _dragOffset;

        // Proxy DLL files bundled with the release
        private static readonly string[] ProxyFiles = {
            "winmm.dll",
            "libeay32.dll",
            "ssleay32.dll",
            "boost_filesystem-vc143-mt-x32-1_86.dll",
            "boost_log-vc143-mt-x32-1_86.dll",
            "boost_thread-vc143-mt-x32-1_86.dll"
        };

        public SettingsForm(LauncherSettings settings)
        {
            _settings = settings;
            InitializeForm();
            InitializeControls();
            UpdateProxyStatus();
        }

        private void InitializeForm()
        {
            this.Text = "Settings";
            this.Size = new Size(420, 340);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = BackgroundDark;
            this.DoubleBuffered = true;
        }

        private void InitializeControls()
        {
            // Title Bar
            var titleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = BackgroundDark
            };
            titleBar.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { _isDragging = true; _dragOffset = e.Location; } };
            titleBar.MouseMove += (s, e) => { if (_isDragging) { var p = PointToScreen(e.Location); Location = new Point(p.X - _dragOffset.X, p.Y - _dragOffset.Y); } };
            titleBar.MouseUp += (s, e) => _isDragging = false;

            var titleLabel = new Label
            {
                Text = "Settings",
                Location = new Point(15, 10),
                AutoSize = true,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 12, FontStyle.Bold)
            };

            var closeBtn = new Button
            {
                Text = "X",
                Size = new Size(35, 35),
                Location = new Point(this.Width - 40, 2),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = TextSecondary,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.MouseEnter += (s, e) => closeBtn.BackColor = PrimaryRed;
            closeBtn.MouseLeave += (s, e) => closeBtn.BackColor = Color.Transparent;
            closeBtn.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            titleBar.Controls.AddRange(new Control[] { titleLabel, closeBtn });
            this.Controls.Add(titleBar);

            // Content
            int y = 55;

            // Game Path Section
            var pathCard = CreateCard(20, y, this.Width - 40, 90);

            var pathLabel = new Label
            {
                Text = "GAME INSTALLATION",
                Location = new Point(12, 10),
                AutoSize = true,
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };

            gamePathTextBox = new TextBox
            {
                Location = new Point(12, 35),
                Size = new Size(pathCard.Width - 75, 24),
                BackColor = BackgroundDark,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10),
                Text = _settings.GamePath ?? "",
                ReadOnly = true
            };

            var browseBtn = new Button
            {
                Text = "...",
                Size = new Size(45, 26),
                Location = new Point(pathCard.Width - 57, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = BackgroundLight,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            browseBtn.FlatAppearance.BorderSize = 0;
            browseBtn.Click += BrowseBtn_Click;

            var pathHint = new Label
            {
                Text = "Select the folder containing RA3.exe",
                Location = new Point(12, 65),
                AutoSize = true,
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 8)
            };

            pathCard.Controls.AddRange(new Control[] { pathLabel, gamePathTextBox, browseBtn, pathHint });
            this.Controls.Add(pathCard);
            y += 105;

            // Proxy Section
            var proxyCard = CreateCard(20, y, this.Width - 40, 75);

            var proxyLabel = new Label
            {
                Text = "PROXY INSTALLATION (winmm.dll)",
                Location = new Point(12, 10),
                AutoSize = true,
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };

            proxyStatusLabel = new Label
            {
                Text = "Checking...",
                Location = new Point(12, 35),
                Size = new Size(200, 20),
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            var proxyHint = new Label
            {
                Text = "Copies proxy files to game Data folder",
                Location = new Point(12, 55),
                AutoSize = true,
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 8)
            };

            installProxyBtn = new Button
            {
                Text = "Install",
                Size = new Size(80, 30),
                Location = new Point(proxyCard.Width - 95, 25),
                FlatStyle = FlatStyle.Flat,
                BackColor = PrimaryRed,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            installProxyBtn.FlatAppearance.BorderSize = 0;
            installProxyBtn.Click += InstallProxyBtn_Click;

            proxyCard.Controls.AddRange(new Control[] { proxyLabel, proxyStatusLabel, proxyHint, installProxyBtn });
            this.Controls.Add(proxyCard);
            y += 90;

            // Buttons
            var cancelBtn = new Button
            {
                Text = "Cancel",
                Size = new Size(90, 35),
                Location = new Point(this.Width - 210, y),
                FlatStyle = FlatStyle.Flat,
                BackColor = BackgroundLight,
                ForeColor = TextPrimary,
                Cursor = Cursors.Hand
            };
            cancelBtn.FlatAppearance.BorderSize = 0;
            cancelBtn.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var saveBtn = new Button
            {
                Text = "Save",
                Size = new Size(90, 35),
                Location = new Point(this.Width - 110, y),
                FlatStyle = FlatStyle.Flat,
                BackColor = PrimaryRed,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            saveBtn.FlatAppearance.BorderSize = 0;
            saveBtn.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(gamePathTextBox.Text) && Directory.Exists(gamePathTextBox.Text))
                {
                    _settings.GamePath = gamePathTextBox.Text;
                }
                DialogResult = DialogResult.OK;
                Close();
            };

            this.Controls.AddRange(new Control[] { cancelBtn, saveBtn });
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

        private void BrowseBtn_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Select RA3.exe",
                Filter = "RA3 Executable|RA3.exe",
                CheckFileExists = true
            };

            if (!string.IsNullOrEmpty(_settings.GamePath) && Directory.Exists(_settings.GamePath))
                dialog.InitialDirectory = _settings.GamePath;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string folder = Path.GetDirectoryName(dialog.FileName)!;
                gamePathTextBox.Text = folder;
                _settings.GamePath = folder;
                UpdateProxyStatus();
            }
        }

        private void UpdateProxyStatus()
        {
            if (string.IsNullOrEmpty(_settings.GamePath))
            {
                proxyStatusLabel.Text = "Set game path first";
                proxyStatusLabel.ForeColor = TextMuted;
                installProxyBtn.Enabled = false;
                return;
            }

            string proxyPath = Path.Combine(_settings.GamePath, "winmm.dll");
            if (File.Exists(proxyPath))
            {
                proxyStatusLabel.Text = "Installed";
                proxyStatusLabel.ForeColor = SuccessGreen;
                installProxyBtn.Text = "Reinstall";
            }
            else
            {
                proxyStatusLabel.Text = "Not installed";
                proxyStatusLabel.ForeColor = Color.FromArgb(245, 158, 11);
                installProxyBtn.Text = "Install";
            }
            installProxyBtn.Enabled = true;
        }

        private void InstallProxyBtn_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_settings.GamePath))
            {
                MessageBox.Show("Please select the game folder first.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Find proxy folder - should be next to the launcher exe
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string proxySourceDir = Path.Combine(appDir, "proxy");

                if (!Directory.Exists(proxySourceDir) || !File.Exists(Path.Combine(proxySourceDir, "winmm.dll")))
                {
                    MessageBox.Show(
                        "Proxy folder not found!\n\n" +
                        "Make sure the 'proxy' folder is next to PlayRA3.exe and contains winmm.dll",
                        "Proxy Files Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Copy to game folder (next to RA3.exe)
                string gameFolder = _settings.GamePath!;

                // Copy all proxy files
                int copied = 0;
                foreach (string fileName in ProxyFiles)
                {
                    string src = Path.Combine(proxySourceDir, fileName);
                    if (File.Exists(src))
                    {
                        File.Copy(src, Path.Combine(gameFolder, fileName), true);
                        copied++;
                    }
                }

                MessageBox.Show($"Installed {copied} file(s) to game folder.", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                UpdateProxyStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Install Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
