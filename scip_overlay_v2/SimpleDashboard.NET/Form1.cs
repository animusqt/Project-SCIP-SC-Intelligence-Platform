// SCIP Dashboard - Simple Native Windows Application (C#)
// Version: 1.0 - NO Python/Electron Dependencies
// Tech Stack: Windows Forms + .NET 8 SDK
// Location: d:\SCIP Projects\SCIP Dashboard\scip_overlay_v2

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ScipDashboard.Main
{
    public static class AppStartup
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }

    public partial class Form1 : Form
    {
        private readonly Timer _loadingTimer;
        private Panel _loadingPanel;

        public Form1()
        {
            InitializeComponent();

            // Set up the form with custom styling
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(800, 600);
            Text = "SC Intelligence Platform";

            // Apply custom theme colors
            BackColor = Color.FromArgb(250, 249, 249);
            ForeColor = Color.Black;

            // Timer that swaps the loading screen for the dashboard
            _loadingTimer = new Timer { Interval = 1500 };
            _loadingTimer.Tick += (s, e) =>
            {
                _loadingTimer.Stop();
                LoadDashboard();
            };
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            // Base form setup
            ClientSize = new Size(1000, 700);
            Name = "Form1";

            // Form closing handler
            FormClosing += Form1_Closing;

            ResumeLayout(false);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Show loading screen, then start the swap timer
            ShowLoadingScreen();
            _loadingTimer.Start();
        }

        private void ShowLoadingScreen()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(26, 26, 26)
            };

            // Progress bar (marquee style = animated loading)
            var progressBar = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Size = new Size(400, 8),
                Location = new Point((ClientSize.Width - 400) / 2, ClientSize.Height / 2 - 20)
            };

            // Terminal output (simplified as TextBox)
            var textBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(26, 26, 26),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11),
                Size = new Size(500, 140),
                Location = new Point((ClientSize.Width - 500) / 2, ClientSize.Height / 2 + 10),
                ScrollBars = ScrollBars.Vertical
            };

            // Display initial text
            string[] lines = ["Initializing modules...", "Loading OCR Scanner...", "Connecting to Market API...", "Syncing Fleet Data...", "Ready!"];
            foreach (var line in lines)
            {
                textBox.AppendText(line + "\r\n");
            }

            panel.Controls.Add(progressBar);
            panel.Controls.Add(textBox);

            _loadingPanel = panel;
            Controls.Add(panel);
        }

        private void LoadDashboard()
        {
            // Remove loading screen after delay (2 seconds)
            System.Threading.Thread.Sleep(2000);

            if (_loadingPanel != null)
            {
                Controls.Remove(_loadingPanel);
                _loadingPanel.Dispose();
                _loadingPanel = null;
            }

            // Show dashboard content with custom styling
            Panel contentPanel = CreateContentPanel();
            Controls.Add(contentPanel);
        }

        private Panel CreateContentPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White
            };

            // Left sidebar (narrow)
            Panel leftSidebar = new()

            {
                Size = new Size(240, 36),
                Dock = DockStyle.Left,
                BackColor = Color.FromArgb(61, 61, 61)
            };

            var topLogoPanel = new Panel
            {
                Size = new Size(leftSidebar.Width, 48),
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(61, 61, 61)
            };

            // Logo placeholder (simple text)
            Label logoLabel = new()

            {
                AutoSize = true,
                Text = "SC INTELLIGENCE PLATFORM",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(10, 12)
            };
            topLogoPanel.Controls.Add(logoLabel);

            leftSidebar.Controls.Add(topLogoPanel);

            // Navigation menu items
            var navItems = new[] { "OCR Scanner", "Market Lookup", "Fleet Manager", "Route Planner" };
            int navY = 60;
            foreach (var item in navItems)
            {
                Label label = new()

                {
                    AutoSize = false,
                    Size = new Size(216, 32),
                    Location = new Point(12, navY),
                    Text = item,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Cursor = Cursors.Hand
                };
                label.MouseClick += (s, e) => OnMenuItemClick(item);

                leftSidebar.Controls.Add(label);
                navY += 36;
            }

            // Main content area placeholder
            Panel mainContent = new()

            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            Label contentLabel = new()

            {
                AutoSize = true,
                Text = "Dashboard ready! Select a module from the sidebar.",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Location = new Point(24, 24)
            };
            mainContent.Controls.Add(contentLabel);

            Label helperText = new()

            {
                AutoSize = true,
                Text = "Features available: OCR Scanner, Market Lookup, Fleet Management, Route Planning",
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.FromArgb(102, 102, 102),
                Location = new Point(24, 60)
            };
            mainContent.Controls.Add(helperText);

            // Stats cards section
            Panel statsPanel = new()

            {
                Size = new Size(160 * 4 + 20, 100),
                Location = new Point(24, 110),
                BackColor = Color.White
            };

            string[] statsNames = ["Active Hauls", "Available Routes", "Fleet Available", "Cargo Capacity"];
            int cardX = 0;
            foreach (var name in statsNames)
            {
                var card = CreateStatsCard(name);
                card.Location = new Point(cardX, 0);
                statsPanel.Controls.Add(card);
                cardX += 170;
            }
            mainContent.Controls.Add(statsPanel);

            panel.Controls.Add(mainContent);
            panel.Controls.Add(leftSidebar);

            return panel;
        }

        private Panel CreateStatsCard(string label)
        {
            var panel = new Panel
            {
                Size = new Size(160, 90),
                BackColor = Color.White
            };

            // Label at top
            Label lblLabel = new()

            {
                AutoSize = true,
                Text = label,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(25, 101, 196),
                Location = new Point(12, 10)
            };
            panel.Controls.Add(lblLabel);

            // Value at bottom (placeholder)
            Label valueLabel = new()

            {
                AutoSize = true,
                Text = "0",
                Font = new Font("Segoe UI", 26, FontStyle.Bold),
                ForeColor = Color.FromArgb(21, 78, 138),
                Location = new Point(12, 36)
            };
            panel.Controls.Add(valueLabel);

            // Color bar separator
            Panel sep = new()

            {
                Size = new Size(2, 70),
                Location = new Point(0, 10),
                BackColor = Color.FromArgb(255, 199, 7)
            };
            panel.Controls.Add(sep);

            return panel;
        }

        private void OnMenuItemClick(string itemName)
        {
            // Placeholder - in production this would trigger real module functionality
            MessageBox.Show($"{itemName} module clicked! (Feature coming soon)", "Module", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Form1_Closing(object sender, FormClosingEventArgs e)
        {
            // Save app state before closing
            try
            {
                var jsonFile = "sc_dashboard_last_save.json";
                File.WriteAllText(jsonFile, DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving state: {ex.Message}");
            }
        }
    }
}
