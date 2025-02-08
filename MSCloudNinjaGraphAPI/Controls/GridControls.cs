using System;
using System.Drawing;
using System.Windows.Forms;

namespace MSCloudNinjaGraphAPI.Controls
{
    public class ModernDataGridView : DataGridView
    {
        public ModernDataGridView()
        {
            InitializeGridStyle();
        }

        private void InitializeGridStyle()
        {
            BackgroundColor = Color.FromArgb(30, 30, 30);
            ForeColor = Color.White;
            GridColor = Color.FromArgb(50, 50, 50);
            BorderStyle = BorderStyle.None;
            CellBorderStyle = DataGridViewCellBorderStyle.Single;
            EnableHeadersVisualStyles = false;
            SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            MultiSelect = true;
            ReadOnly = false;
            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AllowUserToResizeRows = false;
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            RowHeadersVisible = false;
            AutoGenerateColumns = false;
            ScrollBars = ScrollBars.Both;
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            DefaultCellStyle.BackColor = Color.FromArgb(30, 30, 30);
            DefaultCellStyle.ForeColor = Color.White;
            DefaultCellStyle.SelectionBackColor = Color.FromArgb(60, 60, 60);
            DefaultCellStyle.SelectionForeColor = Color.White;
            ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(40, 40, 40);
            ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(40, 40, 40);
            ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            ColumnHeadersHeight = 30;
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            RowTemplate.Height = 25;
        }

        public void ConfigureHorizontalScrolling(Control parentControl)
        {
            parentControl.MouseWheel += (sender, e) =>
            {
                if (ModifierKeys.HasFlag(Keys.Shift) && parentControl is ScrollableControl scrollable)
                {
                    // Scroll horizontally when Shift is pressed
                    int scrollAmount = -e.Delta;
                    scrollable.HorizontalScroll.Value = Math.Max(0,
                        Math.Min(scrollable.HorizontalScroll.Value + scrollAmount,
                        scrollable.HorizontalScroll.Maximum));
                }
            };
        }

        public void AddColumns(System.Collections.Generic.IEnumerable<GridColumnDefinition> columns)
        {
            foreach (var col in columns)
            {
                var column = (DataGridViewColumn)Activator.CreateInstance(col.Type);
                column.Name = col.Name;
                column.HeaderText = col.Header;
                column.Width = col.Width;
                Columns.Add(column);
            }
        }
    }

    public class GridColumnDefinition
    {
        public string Name { get; set; }
        public string Header { get; set; }
        public int Width { get; set; }
        public Type Type { get; set; }

        public GridColumnDefinition(string name, string header, int width, Type type)
        {
            Name = name;
            Header = header;
            Width = width;
            Type = type;
        }
    }

    public class ModernTextBox : TextBox
    {
        public ModernTextBox()
        {
            InitializeStyle();
        }

        private void InitializeStyle()
        {
            BackColor = Color.FromArgb(40, 40, 40);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 11);
            BorderStyle = BorderStyle.FixedSingle;
        }
    }

    public class ModernLabel : Label
    {
        public ModernLabel()
        {
            InitializeStyle();
        }

        private void InitializeStyle()
        {
            ForeColor = Color.White;
            AutoSize = true;
            Padding = new Padding(5);
        }
    }

    public class DataGridViewCheckBoxHeaderCell : DataGridViewColumnHeaderCell
    {
        private bool isChecked = false;
        public event CheckBoxClickedHandler? OnCheckBoxClicked;

        public DataGridViewCheckBoxHeaderCell()
        {
            // Initialize the event to a no-op to avoid null checks
            OnCheckBoxClicked += (state) => { };
        }

        protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex,
            DataGridViewElementStates dataGridViewElementState, object value, object formattedValue, string errorText,
            DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle,
            DataGridViewPaintParts paintParts)
        {
            base.Paint(graphics, clipBounds, cellBounds, rowIndex, dataGridViewElementState, value,
                formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);

            Rectangle checkBoxRect = new Rectangle(
                cellBounds.X + (cellBounds.Width - 15) / 2,
                cellBounds.Y + (cellBounds.Height - 15) / 2,
                15, 15);

            ButtonState state = isChecked ? ButtonState.Checked : ButtonState.Normal;
            ControlPaint.DrawCheckBox(graphics, checkBoxRect, state);
        }

        protected override void OnMouseClick(DataGridViewCellMouseEventArgs e)
        {
            Point clickPoint = new Point(e.X, e.Y);
            Rectangle cellBounds = this.DataGridView.GetCellDisplayRectangle(-1, -1, true);
            Rectangle checkBoxRect = new Rectangle(
                cellBounds.X + (cellBounds.Width - 15) / 2,
                cellBounds.Y + (cellBounds.Height - 15) / 2,
                15, 15);

            if (checkBoxRect.Contains(clickPoint))
            {
                isChecked = !isChecked;
                OnCheckBoxClicked?.Invoke(isChecked);
                this.DataGridView.InvalidateCell(this);
            }

            base.OnMouseClick(e);
        }
    }

    public delegate void CheckBoxClickedHandler(bool state);

    public class ModernButton : Button
    {
        public ModernButton()
        {
            InitializeStyle();
        }

        private void InitializeStyle()
        {
            BackColor = Color.FromArgb(45, 45, 48);
            ForeColor = Color.White;
            FlatStyle = FlatStyle.Flat;
            Font = new Font("Segoe UI", 11);
            Padding = new Padding(10);
            FlatAppearance.BorderColor = Color.FromArgb(60, 60, 60);
            FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 60, 60);
            FlatAppearance.MouseDownBackColor = Color.FromArgb(80, 80, 80);
        }
    }

    public class ModernCheckBox : CheckBox
    {
        public ModernCheckBox()
        {
            InitializeStyle();
        }

        private void InitializeStyle()
        {
            ForeColor = Color.White;
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 11);
        }
    }

    public class ModernComboBox : ComboBox
    {
        public ModernComboBox()
        {
            InitializeStyle();
        }

        private void InitializeStyle()
        {
            BackColor = Color.FromArgb(40, 40, 40);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 11);
            FlatStyle = FlatStyle.Flat;
            DropDownStyle = ComboBoxStyle.DropDownList;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Draw a custom border
            using (var pen = new Pen(Color.FromArgb(60, 60, 60), 1))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            // Draw the background
            e.DrawBackground();

            // Get the item text
            string text = GetItemText(Items[e.Index]);

            // Set the color based on selection state
            Color textColor = (e.State & DrawItemState.Selected) == DrawItemState.Selected
                ? Color.White
                : Color.White;

            // Set the background color based on selection state
            Color backColor = (e.State & DrawItemState.Selected) == DrawItemState.Selected
                ? Color.FromArgb(60, 60, 60)
                : Color.FromArgb(40, 40, 40);

            // Fill the background
            using (var brush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            // Draw the text
            using (var brush = new SolidBrush(textColor))
            {
                e.Graphics.DrawString(text, e.Font ?? Font, brush, e.Bounds.X + 3, e.Bounds.Y + 2);
            }

            // Draw focus rectangle if needed
            if ((e.State & DrawItemState.Focus) == DrawItemState.Focus)
            {
                e.DrawFocusRectangle();
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0F) // WM_PAINT
            {
                this.DrawMode = DrawMode.OwnerDrawFixed;
            }
            base.WndProc(ref m);
        }
    }
}