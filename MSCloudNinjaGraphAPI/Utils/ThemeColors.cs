using System.Drawing;
using System.Windows.Forms;

namespace MSCloudNinjaGraphAPI
{
    public static class ThemeColors
    {
        // VS Code-like colors
        public static Color BackgroundDark = Color.FromArgb(30, 30, 30);      // Main background
        public static Color HeaderBackground = Color.FromArgb(37, 37, 38);    // Title bar
        public static Color ContentBackground = Color.FromArgb(40, 40, 40);   // Editor background
        public static Color TabBackground = Color.FromArgb(45, 45, 45);       // Active tab
        public static Color TabInactiveBackground = Color.FromArgb(37, 37, 38); // Inactive tab
        public static Color AccentBlue = Color.FromArgb(66, 150, 251);        // Active item
        public static Color TextLight = Color.FromArgb(241, 241, 241);        // Primary text
        public static Color TextDark = Color.FromArgb(180, 180, 180);         // Secondary text
        public static Color BorderColor = Color.FromArgb(64, 64, 64);         // Subtle borders
        public static Color ButtonHover = Color.FromArgb(86, 170, 255);       // Button hover
        public static Color ErrorRed = Color.FromArgb(240, 71, 71);          // Error text
        public static Color SuccessGreen = Color.FromArgb(23, 184, 144);     // Success text
        public static Color GridBackground = Color.FromArgb(33, 33, 33);     // Grid background
        public static Color GridHeaderBackground = Color.FromArgb(45, 45, 45); // Grid header
        public static Color GridSelectedBackground = Color.FromArgb(55, 55, 55); // Selected row
    }

    public static class ControlExtensions
    {
        public static T AddControls<T>(this T parent, params Control[] controls) where T : Control
        {
            parent.Controls.AddRange(controls);
            return parent;
        }

        public static void SetDarkTheme(this DataGridView grid)
        {
            grid.EnableHeadersVisualStyles = false;
            grid.BackgroundColor = ThemeColors.GridBackground;
            grid.ForeColor = ThemeColors.TextLight;
            grid.GridColor = ThemeColors.BorderColor;
            grid.BorderStyle = BorderStyle.None;

            grid.ColumnHeadersDefaultCellStyle.BackColor = ThemeColors.GridHeaderBackground;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = ThemeColors.TextLight;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = ThemeColors.GridHeaderBackground;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = ThemeColors.TextLight;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F);
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.ColumnHeadersHeight = 35;

            grid.DefaultCellStyle.BackColor = ThemeColors.GridBackground;
            grid.DefaultCellStyle.ForeColor = ThemeColors.TextLight;
            grid.DefaultCellStyle.SelectionBackColor = ThemeColors.GridSelectedBackground;
            grid.DefaultCellStyle.SelectionForeColor = ThemeColors.TextLight;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9F);

            grid.RowHeadersVisible = false;
            grid.AllowUserToResizeRows = false;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.RowTemplate.Height = 30;
        }
    }
}
