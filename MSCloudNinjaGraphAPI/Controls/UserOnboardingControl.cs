using Microsoft.Graph;
using Microsoft.Graph.Models;
using MSCloudNinjaGraphAPI.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MSCloudNinjaGraphAPI.Controls
{
    public partial class UserOnboardingControl : UserControl
    {
        private readonly IUserManagementService _userService;
        private readonly LogService _logService;
        private List<Group> _groups;
        private List<License> _licenses;
        private List<User> _managers;
        private ModernDataGridView groupsGrid;
        private ModernTextBox managerSearchBox;
        private ModernTextBox licenseSearchBox;
        private FlowLayoutPanel licenseCheckboxPanel;
        private Panel managerDropdownPanel;
        private FlowLayoutPanel managerResultsPanel;
        private User selectedManager;
        private ModernTextBox usernameTextBox;
        private ModernComboBox domainComboBox;
        private List<string> _domains;
        private ModernTextBox firstNameTextBox;
        private ModernTextBox surnameTextBox;
        private ModernTextBox displayNameTextBox;
        private ModernTextBox additionalEmailTextBox;
        private ModernCheckBox primaryEmailCheckBox;
        private ModernButton btnCreateUser;
        private Panel contentPanel;
        private FlowLayoutPanel searchPanel;
        private ModernTextBox searchBox;
        private ModernLabel searchLabel;

        public UserOnboardingControl(IUserManagementService userService, LogService logService)
        {
            _groups = new List<Group>();
            _licenses = new List<License>();
            _managers = new List<User>();
            _domains = new List<string>();

            _userService = userService;
            _logService = logService;

            InitializeUI();
            LoadGroupsAndLicenses();
        }

        private void InitializeUI()
        {
            // Main content panel
            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(40),
                BackColor = Color.FromArgb(30, 30, 30)
            };

            // Create all panels
            btnCreateUser = CreateButton();
            var groupsPanel = CreateGroupsPanel();
            var licensesPanel = CreateLicensesPanel();
            var userDetailsPanel = CreateUserDetailsPanel();
            var emailPanel = CreateEmailPanel();

            // Add panels to content panel in reverse order (bottom to top)
            contentPanel.Controls.Add(btnCreateUser);      // Bottom
            contentPanel.Controls.Add(groupsPanel);        // Fourth from top
            contentPanel.Controls.Add(licensesPanel);      // Third from top
            contentPanel.Controls.Add(emailPanel);         // Second from top
            contentPanel.Controls.Add(userDetailsPanel);   // Top

            this.Controls.Add(contentPanel);
        }

        private FlowLayoutPanel CreateUserDetailsPanel()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(5),
                Margin = new Padding(0, 10, 0, 30)
            };

            // Row 1: Username with domain selection
            var row1 = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 10)
            };

            var usernameLabel = new ModernLabel
            {
                Text = "Username:",
                Width = 80,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(0, 5, 10, 0)
            };

            var usernameContainer = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false
            };

            usernameTextBox = new ModernTextBox { Width = 150 };
            var atLabel = new ModernLabel { Text = "@", AutoSize = true, Margin = new Padding(5, 5, 5, 0) };
            domainComboBox = new ModernComboBox { Width = 180 };

            usernameContainer.Controls.AddRange(new Control[] { usernameTextBox, atLabel, domainComboBox });
            row1.Controls.AddRange(new Control[] { usernameLabel, usernameContainer });

            // Row 2: First Name and Surname
            var row2 = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 10)
            };

            var firstNameLabel = new ModernLabel
            {
                Text = "First Name:",
                Width = 120,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(0, 5, 5, 0)
            };
            firstNameTextBox = new ModernTextBox { Width = 250 };

            var surnameLabel = new ModernLabel
            {
                Text = "Surname:",
                Width = 120,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(0, 5, 5, 0)
            };
            surnameTextBox = new ModernTextBox { Width = 250 };

            row2.Controls.AddRange(new Control[] { firstNameLabel, firstNameTextBox, surnameLabel, surnameTextBox });

            // Row 3: Display Name
            var row3 = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 10)
            };

            var displayNameLabel = new ModernLabel
            {
                Text = "Display Name:",
                Width = 120,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(0, 5, 5, 0)
            };
            displayNameTextBox = new ModernTextBox { Width = 250 };

            row3.Controls.AddRange(new Control[] { displayNameLabel, displayNameTextBox });

            // Add all rows to the panel
            panel.Controls.AddRange(new Control[] { row1, row2, row3 });

            // Auto-generate display name when first name or surname changes
            firstNameTextBox.TextChanged += (s, e) => UpdateDisplayName();
            surnameTextBox.TextChanged += (s, e) => UpdateDisplayName();

            // Update full username when either part changes
            usernameTextBox.TextChanged += (s, e) => UpdateFullUsername();
            domainComboBox.SelectedIndexChanged += (s, e) => UpdateFullUsername();

            return panel;
        }

        private void UpdateDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(firstNameTextBox.Text) || !string.IsNullOrWhiteSpace(surnameTextBox.Text))
            {
                displayNameTextBox.Text = $"{firstNameTextBox.Text.Trim()} {surnameTextBox.Text.Trim()}".Trim();
            }
        }

        private void UpdateFullUsername()
        {
            if (domainComboBox.SelectedItem != null)
            {
                string username = usernameTextBox.Text.Trim();
                string domain = domainComboBox.SelectedItem.ToString();
                _fullUsername = $"{username}@{domain}";
            }
        }

        private string _fullUsername = string.Empty;

        private FlowLayoutPanel CreateEmailPanel()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(5),
                Margin = new Padding(0, 0, 0, 20)
            };

            // Row 1: Additional Email
            var emailRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 10)
            };

            var emailLabel = new ModernLabel 
            { 
                Text = "Additional Email:", 
                Width = 120,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(0, 5, 5, 0) 
            };
            additionalEmailTextBox = new ModernTextBox { Width = 250, Margin = new Padding(0, 0, 15, 0) };
            primaryEmailCheckBox = new ModernCheckBox { Text = "Set as Primary Email", AutoSize = true, Margin = new Padding(0, 5, 0, 0) };

            emailRow.Controls.AddRange(new Control[] { emailLabel, additionalEmailTextBox, primaryEmailCheckBox });

            // Row 2: Manager
            var managerRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 10)
            };

            var managerLabel = new ModernLabel
            {
                Text = "Manager:",
                Width = 120,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(0, 5, 5, 0)
            };

            var managerContainer = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false
            };

            managerSearchBox = new ModernTextBox { Width = 250 };
            managerSearchBox.TextChanged += ManagerSearchBox_TextChanged;

            // Create dropdown panel for results
            managerDropdownPanel = new Panel
            {
                Width = 250,
                Height = 200,
                Visible = false,
                BackColor = Color.FromArgb(45, 45, 48)
            };

            // Create results panel inside dropdown
            managerResultsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.FromArgb(45, 45, 48)
            };

            managerDropdownPanel.Controls.Add(managerResultsPanel);
            managerContainer.Controls.Add(managerSearchBox);
            managerRow.Controls.AddRange(new Control[] { managerLabel, managerContainer });

            // Add rows to panel
            panel.Controls.AddRange(new Control[] { emailRow, managerRow });

            // Add dropdown panel to the form (not the flow panel)
            this.Controls.Add(managerDropdownPanel);

            return panel;
        }

        private async Task LoadLicenses()
        {
            try
            {
                licenseCheckboxPanel.Controls.Clear();
                var licenses = await _userService.GetAvailableLicensesAsync();

                int yPos = 10;
                foreach (var license in licenses.OrderBy(l => l.FriendlyName))
                {
                    var checkbox = new CheckBox
                    {
                        Text = license.GetDisplayText(),
                        Tag = license.SkuId,
                        AutoSize = true,
                        Location = new Point(10, yPos),
                        ForeColor = Color.White,
                        Enabled = license.HasAvailableLicenses
                    };

                    // Add tooltip to show more details
                    var tooltip = new ToolTip();
                    tooltip.SetToolTip(checkbox, 
                        $"SKU: {license.SkuPartNumber}\n" +
                        $"Total Licenses: {license.TotalLicenses}\n" +
                        $"Used Licenses: {license.UsedLicenses}\n" +
                        $"Available Licenses: {license.AvailableLicenses}");

                    licenseCheckboxPanel.Controls.Add(checkbox);
                    yPos += 25;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading licenses: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LicenseSearchBox_TextChanged(object sender, EventArgs e)
        {
            string searchText = licenseSearchBox.Text.ToLower();
            foreach (Control control in licenseCheckboxPanel.Controls)
            {
                if (control is CheckBox checkbox)
                {
                    var license = _licenses.FirstOrDefault(l => l.SkuId == checkbox.Tag.ToString());
                    if (license != null)
                    {
                        checkbox.Visible = string.IsNullOrEmpty(searchText) ||
                            license.FriendlyName.ToLower().Contains(searchText) ||
                            license.SkuPartNumber.ToLower().Contains(searchText);
                    }
                }
            }
        }

        private Panel CreateLicensesPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 250,
                Margin = new Padding(0, 0, 0, 20)
            };

            var label = new ModernLabel
            {
                Text = "Licenses:",
                Font = new Font("Segoe UI Semibold", 12),
                Margin = new Padding(0, 0, 0, 10),
                Dock = DockStyle.Top
            };

            // Create search box
            licenseSearchBox = new ModernTextBox
            {
                Dock = DockStyle.Top,
                Width = 300,
                Height = 25,
                Margin = new Padding(0, 0, 0, 10)
            };
            licenseSearchBox.TextChanged += LicenseSearchBox_TextChanged;

            // Create checkbox panel
            licenseCheckboxPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.FromArgb(45, 45, 48)
            };

            panel.Controls.Add(licenseCheckboxPanel);
            panel.Controls.Add(licenseSearchBox);
            panel.Controls.Add(label);

            return panel;
        }

        private Panel CreateGroupsPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 20, 0, 20),
                MinimumSize = new Size(0, 200),
                Height = 300  // Set a default height
            };

            // 1. Create and add Label
            var label = new ModernLabel
            {
                Text = "Groups:",
                Font = new Font("Segoe UI Semibold", 12),
                Margin = new Padding(0, 0, 0, 10),
                Dock = DockStyle.Top,
                Height = 25
            };

            // 2. Create and configure Search Panel
            searchPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(0, 5, 0, 5),
                BackColor = Color.FromArgb(45, 45, 48)  // Match the theme
            };

            searchLabel = new ModernLabel
            {
                Text = "Search Groups:",
                Margin = new Padding(5, 5, 5, 0),
                ForeColor = Color.White,
                AutoSize = true
            };

            searchBox = new ModernTextBox
            {
                Width = 300,
                Height = 25,
                Margin = new Padding(5, 0, 0, 0)
            };
            searchBox.TextChanged += SearchBox_TextChanged;

            searchPanel.Controls.Clear();
            searchPanel.Controls.AddRange(new Control[] { searchLabel, searchBox });

            // 3. Create and configure Groups Grid
            groupsGrid = new ModernDataGridView
            {
                Dock = DockStyle.Fill,
                EnableHeadersVisualStyles = false,
                ColumnHeadersVisible = true,
                ColumnHeadersHeight = 32,
                BackgroundColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                GridColor = Color.FromArgb(50, 50, 50),
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false
            };

            var columns = new[]
            {
                new GridColumnDefinition("Select", "Select", 70, typeof(DataGridViewCheckBoxColumn)),
                new GridColumnDefinition("Id", "Id", 0, typeof(DataGridViewTextBoxColumn)),
                new GridColumnDefinition("Name", "Group Name", 300, typeof(DataGridViewTextBoxColumn)),
                new GridColumnDefinition("Description", "Description", 400, typeof(DataGridViewTextBoxColumn))
            };
            groupsGrid.AddColumns(columns);
            groupsGrid.Columns["Id"].Visible = false;
            groupsGrid.Columns["Description"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            // Add controls in correct order (bottom to top)
            panel.Controls.Add(groupsGrid);    // Bottom layer
            panel.Controls.Add(searchPanel);   // Middle layer
            panel.Controls.Add(label);         // Top layer

            return panel;
        }

        private ModernButton CreateButton()
        {
            var button = new ModernButton
            {
                Text = "Create User",
                Dock = DockStyle.Bottom,
                Height = 45,  // Increased height
                BackColor = Color.FromArgb(0, 120, 212),
                Margin = new Padding(10),
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                Padding = new Padding(5)  // Added padding
            };
            button.Click += BtnCreateUser_Click;
            return button;
        }

        private async void LoadGroupsAndLicenses()
        {
            try
            {
                // Load domains
                var domains = await _userService.GetDomainNamesAsync();
                _domains = domains;
                domainComboBox.Items.Clear();
                domainComboBox.Items.AddRange(domains.ToArray());
                if (domains.Any())
                {
                    domainComboBox.SelectedIndex = 0;
                }

                // Load managers
                var users = await _userService.GetAllUsersAsync();
                _managers = users.OrderBy(u => u.DisplayName).ToList();

                // Load groups
                var groups = await _userService.GetAllGroupsAsync();
                _groups = groups.ToList();

                groupsGrid.Rows.Clear();
                foreach (var group in _groups)
                {
                    groupsGrid.Rows.Add(false, group.Id, group.DisplayName, group.Description);
                }

                // Load licenses
                await LoadLicenses();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                await _logService.LogAsync($"Error in LoadGroupsAndLicenses: {ex.Message}", true);
            }
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            string searchText = searchBox.Text.ToLower();
            foreach (DataGridViewRow row in groupsGrid.Rows)
            {
                bool visible = true;
                if (!string.IsNullOrEmpty(searchText))
                {
                    string name = row.Cells["Name"].Value?.ToString()?.ToLower() ?? "";
                    string description = row.Cells["Description"].Value?.ToString()?.ToLower() ?? "";
                    visible = name.Contains(searchText) || description.Contains(searchText);
                }
                row.Visible = visible;
            }
        }

        private void ManagerSearchBox_TextChanged(object sender, EventArgs e)
        {
            string searchText = managerSearchBox.Text.ToLower();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                managerDropdownPanel.Visible = false;
                return;
            }

            // Filter managers based on search text
            var matchingManagers = _managers
                .Where(m => (m.DisplayName?.ToLower().Contains(searchText) ?? false) ||
                           (m.UserPrincipalName?.ToLower().Contains(searchText) ?? false))
                .Take(10)  // Limit to 10 results
                .ToList();

            // Clear previous results
            managerResultsPanel.Controls.Clear();

            // Add matching managers to the results panel
            foreach (var manager in matchingManagers)
            {
                var resultButton = new Button
                {
                    Text = $"{manager.DisplayName} ({manager.UserPrincipalName})",
                    Width = managerResultsPanel.Width - 20,
                    Height = 30,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(45, 45, 48),
                    ForeColor = Color.White,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Tag = manager
                };

                resultButton.Click += (s, ev) =>
                {
                    selectedManager = manager;
                    managerSearchBox.Text = manager.DisplayName;
                    managerDropdownPanel.Visible = false;
                };

                managerResultsPanel.Controls.Add(resultButton);
            }

            // Position and show dropdown
            if (matchingManagers.Any())
            {
                var searchBoxScreen = managerSearchBox.PointToScreen(Point.Empty);
                var formScreen = this.PointToScreen(Point.Empty);
                managerDropdownPanel.Location = new Point(
                    searchBoxScreen.X - formScreen.X,
                    searchBoxScreen.Y - formScreen.Y + managerSearchBox.Height
                );
                managerDropdownPanel.BringToFront();
                managerDropdownPanel.Visible = true;
            }
            else
            {
                managerDropdownPanel.Visible = false;
            }
        }

        private async void BtnCreateUser_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(usernameTextBox.Text) || string.IsNullOrWhiteSpace(displayNameTextBox.Text))
                {
                    MessageBox.Show("Username and Display Name are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                btnCreateUser.Enabled = false;
                Cursor = Cursors.WaitCursor;

                var request = new CreateUserRequest
                {
                    UserPrincipalName = _fullUsername,
                    FirstName = firstNameTextBox.Text.Trim(),
                    Surname = surnameTextBox.Text.Trim(),
                    DisplayName = displayNameTextBox.Text.Trim(),
                    AdditionalEmail = additionalEmailTextBox.Text.Trim(),
                    SetAdditionalEmailAsPrimary = primaryEmailCheckBox.Checked,
                    ManagerId = selectedManager?.Id,
                    GroupIds = groupsGrid.Rows.Cast<DataGridViewRow>()
                        .Where(r => Convert.ToBoolean(r.Cells["Select"].Value))
                        .Select(r => r.Cells["Id"].Value.ToString())
                        .ToList(),
                    LicenseIds = licenseCheckboxPanel.Controls.OfType<CheckBox>()
                        .Where(cb => cb.Checked)
                        .Select(cb => cb.Tag.ToString())
                        .ToList()
                };

                await _userService.CreateUserAsync(request);
                MessageBox.Show("User created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearForm();
            }
            catch (AggregateException ex)
            {
                // This is our custom exception for partial success
                MessageBox.Show(ex.Message, "Partial Success", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating user: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnCreateUser.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        private void ClearForm()
        {
            usernameTextBox.Clear();
            firstNameTextBox.Clear();
            surnameTextBox.Clear();
            displayNameTextBox.Clear();
            additionalEmailTextBox.Clear();
            primaryEmailCheckBox.Checked = false;
            managerSearchBox.Clear();
            selectedManager = null;

            // Clear group selections
            foreach (DataGridViewRow row in groupsGrid.Rows)
            {
                row.Cells["Select"].Value = false;
            }

            // Clear license selections
            foreach (CheckBox checkbox in licenseCheckboxPanel.Controls.OfType<CheckBox>())
            {
                checkbox.Checked = false;
            }
        }
    }
}
