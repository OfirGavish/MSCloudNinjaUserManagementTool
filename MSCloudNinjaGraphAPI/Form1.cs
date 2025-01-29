using Microsoft.Graph;
using MSCloudNinjaGraphAPI.Controls;
using MSCloudNinjaGraphAPI.Services;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using Azure.Identity;
using Microsoft.Kiota.Authentication.Azure;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using System.Runtime.InteropServices;
using Microsoft.Identity.Client;

// And Eric Cartman said: Respect My Authoritah!
namespace MSCloudNinjaGraphAPI
{
    public partial class MainForm : Form
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        private static bool UseImmersiveDarkMode(IntPtr handle, bool enabled)
        {
            if (IsWindows10OrGreater(17763))
            {
                var attribute = DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1;
                if (IsWindows10OrGreater(18985))
                {
                    attribute = DWMWA_USE_IMMERSIVE_DARK_MODE;
                }

                int useImmersiveDarkMode = enabled ? 1 : 0;
                return DwmSetWindowAttribute(handle, attribute, ref useImmersiveDarkMode, sizeof(int)) == 0;
            }

            return false;
        }

        private static bool IsWindows10OrGreater(int build = -1)
        {
            return Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= build;
        }

        private readonly string[] _scopes = { 
            "User.ReadWrite.All", 
            "Group.ReadWrite.All"
        };
        private GraphServiceClient? _graphClient;
        private Label statusLabel = null!;
        private Button browserAuthButton = null!;

        public MainForm()
        {
            InitializeComponent();
            UseImmersiveDarkMode(Handle, true); // Enable dark mode for title bar
            this.Size = new Size(1200, 800);
            this.Text = "User Offboarding Tool by MSCloudNinja";
            SetupAuthPanel();
        }

        private void SetupAuthPanel()
        {
            Controls.Clear();

            // Create main container panel that fills the form
            var containerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            // Create centered auth panel
            var authPanel = new Panel
            {
                Width = 500,
                Height = 400,
                BackColor = Color.FromArgb(45, 45, 48),
                Padding = new Padding(20)
            };

            // Center the auth panel in the container
            authPanel.Location = new Point(
                (containerPanel.ClientSize.Width - authPanel.Width) / 2,
                (containerPanel.ClientSize.Height - authPanel.Height) / 2
            );

            // Create logo picture box
            var logoPictureBox = new PictureBox
            {
                Size = new Size(100, 100),
                SizeMode = PictureBoxSizeMode.Zoom
            };

            try
            {
                // Load logo from embedded resources
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MSCloudNinjaGraphAPI.logo.png");
                if (stream != null)
                {
                    logoPictureBox.Image = Image.FromStream(stream);
                }
            }
            catch (Exception ex)
            {
                // If logo loading fails, continue without it
                System.Diagnostics.Debug.WriteLine($"Failed to load logo: {ex.Message}");
            }

            // Center the logo in the auth panel
            logoPictureBox.Location = new Point(
                (authPanel.Width - logoPictureBox.Width) / 2,
                30
            );

            // Create title label
            var titleLabel = new Label
            {
                Text = "User Offboarding Tool",
                Font = new Font("Segoe UI", 24, FontStyle.Regular),
                ForeColor = Color.White,
                AutoSize = true
            };

            // Center the title in the auth panel
            titleLabel.Location = new Point(
                (authPanel.Width - titleLabel.PreferredWidth) / 2,
                logoPictureBox.Bottom + 20
            );

            // Create status label with larger font
            statusLabel = new Label
            {
                Text = "Please authenticate to continue",
                Font = new Font("Segoe UI", 14, FontStyle.Regular),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(
                    (authPanel.Width - 250) / 2,
                    titleLabel.Bottom + 30
                )
            };

            // Create browser auth button
            browserAuthButton = new Button
            {
                Text = "Sign in with Microsoft",
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Width = 200,
                Height = 40,
                Location = new Point(
                    (authPanel.Width - 200) / 2,
                    statusLabel.Bottom + 30
                )
            };
            browserAuthButton.FlatAppearance.BorderSize = 0;
            browserAuthButton.Click += BrowserAuthButton_Click;

            // Add controls to the auth panel
            authPanel.Controls.AddRange(new Control[] {
                logoPictureBox,
                titleLabel,
                statusLabel,
                browserAuthButton
            });

            // Add the auth panel to the container
            containerPanel.Controls.Add(authPanel);

            // Handle container panel resize to keep auth panel centered
            containerPanel.Resize += (s, e) =>
            {
                authPanel.Location = new Point(
                    (containerPanel.ClientSize.Width - authPanel.Width) / 2,
                    (containerPanel.ClientSize.Height - authPanel.Height) / 2
                );
            };

            // Add the container panel to the form
            Controls.Add(containerPanel);
        }

        private async void BrowserAuthButton_Click(object sender, EventArgs e)
        {
            try
            {
                statusLabel.Text = "Opening browser for authentication...";
                System.Windows.Forms.Application.DoEvents();

                var options = new InteractiveBrowserCredentialOptions
                {
                    TenantId = "organizations"
                };

                var credential = new InteractiveBrowserCredential(options);
                var authProvider = new AzureIdentityAuthenticationProvider(credential);
                var requestAdapter = new HttpClientRequestAdapter(authProvider);
                _graphClient = new GraphServiceClient(requestAdapter);

                // Test authentication by making a simple API call
                var users = await _graphClient.Users.GetAsync();

                if (users?.Value?.FirstOrDefault() != null)
                {
                    statusLabel.Text = $"Authentication successful! Welcome {users.Value.First().DisplayName}";
                    InitializeMainInterface();
                }
                else
                {
                    throw new Exception("Could not verify user authentication");
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Authentication failed: {ex.Message}";
                MessageBox.Show($"Error during authentication: {ex.Message}", "Authentication Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeMainInterface()
        {
            Controls.Clear();

            // Create main container
            var mainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            // Create header panel
            var headerPanel = new Panel
            {
                Height = 60,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(45, 45, 48),
                Padding = new Padding(10)
            };

            // Create title label
            var titleLabel = new Label
            {
                Text = "User Offboarding Tool",
                Font = new Font("Segoe UI", 16, FontStyle.Regular),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 15)
            };

            // Create logout button in header
            var logoutButton = new Button
            {
                Text = "Logout",
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(80, 30),
                Location = new Point(headerPanel.Width - 100, 15),
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };

            headerPanel.Controls.AddRange(new Control[] { titleLabel, logoutButton });

            // Create content panel
            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(0, 20, 0, 0) // Add top padding for spacing
            };

            // Create user offboarding control
            var userOffboardingControl = new UserOffboardingControl(_graphClient);

            // Handle logout
            logoutButton.Click += (s, e) => 
            {
                _graphClient = null;
                Controls.Clear();
                SetupAuthPanel();
            };

            contentPanel.Controls.Add(userOffboardingControl);
            mainContainer.Controls.Add(headerPanel);
            mainContainer.Controls.Add(contentPanel);
            Controls.Add(mainContainer);
        }
    }
}
