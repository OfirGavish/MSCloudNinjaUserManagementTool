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
        public event CheckBoxClickedHandler OnCheckBoxClicked;

        protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex,
            DataGridViewElementStates dataGridViewElementState, object value, object formattedValue, string errorText,
            DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle,
            DataGridViewPaintParts paintParts)
        {
            base.Paint(graphics, clipBounds, cellBounds, rowIndex, dataGridViewElementState, value,
                formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);

            var checkBoxSize = 15;
            var location = new Point(
                cellBounds.Location.X + (cellBounds.Width - checkBoxSize) / 2,
                cellBounds.Location.Y + (cellBounds.Height - checkBoxSize) / 2);
            var checkBoxRect = new Rectangle(location, new Size(checkBoxSize, checkBoxSize));

            ControlPaint.DrawCheckBox(graphics, checkBoxRect,
                isChecked ? ButtonState.Checked : ButtonState.Normal);
        }

        protected override void OnMouseClick(DataGridViewCellMouseEventArgs e)
        {
            var checkBoxSize = 15;
            var cellBounds = this.DataGridView.GetCellDisplayRectangle(-1, -1, true);
            var checkBoxRect = new Rectangle(
                cellBounds.Location.X + (cellBounds.Width - checkBoxSize) / 2,
                cellBounds.Location.Y + (cellBounds.Height - checkBoxSize) / 2,
                checkBoxSize, checkBoxSize);

            if (checkBoxRect.Contains(e.Location))
            {
                isChecked = !isChecked;
                OnCheckBoxClicked?.Invoke(isChecked);
                this.DataGridView.InvalidateCell(this);
            }

            base.OnMouseClick(e);
        }
    }

    public delegate void CheckBoxClickedHandler(bool state);
}