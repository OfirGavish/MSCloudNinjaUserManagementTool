using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MSCloudNinjaGraphAPI.Controls
{
    public class ModernButton : Button
    {
        private int borderRadius = 6;
        private Color borderColor = Color.Transparent;
        private Color hoverBackColor;
        private bool isHovered = false;

        public ModernButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = ThemeColors.AccentBlue;
            ForeColor = ThemeColors.TextLight;
            Font = new Font("Segoe UI", 9F);
            hoverBackColor = ThemeColors.ButtonHover;
            Cursor = Cursors.Hand;
            Height = 35;

            MouseEnter += (s, e) => 
            {
                isHovered = true;
                Invalidate();
            };
            MouseLeave += (s, e) => 
            {
                isHovered = false;
                Invalidate();
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Rectangle rectSurface = ClientRectangle;
            Rectangle rectBorder = Rectangle.Inflate(rectSurface, -1, -1);
            int smoothSize = 2;

            using (GraphicsPath pathSurface = GetFigurePath(rectSurface, borderRadius))
            using (GraphicsPath pathBorder = GetFigurePath(rectBorder, borderRadius - 1))
            using (Pen penSurface = new Pen(Parent.BackColor, smoothSize))
            using (Pen penBorder = new Pen(borderColor, 1))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                
                // Button surface
                Region = new Region(pathSurface);
                
                // Draw surface border for HD result
                e.Graphics.DrawPath(penSurface, pathSurface);

                // Draw control border
                if (borderColor != Color.Transparent)
                    e.Graphics.DrawPath(penBorder, pathBorder);

                // Draw button background
                using (SolidBrush brushSurface = new SolidBrush(isHovered ? hoverBackColor : BackColor))
                {
                    e.Graphics.FillPath(brushSurface, pathSurface);
                }

                // Draw text
                TextRenderer.DrawText(e.Graphics, Text, Font, rectSurface, ForeColor, 
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }

        private GraphicsPath GetFigurePath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            float curveSize = radius * 2F;

            path.StartFigure();
            path.AddArc(rect.X, rect.Y, curveSize, curveSize, 180, 90);
            path.AddArc(rect.Right - curveSize, rect.Y, curveSize, curveSize, 270, 90);
            path.AddArc(rect.Right - curveSize, rect.Bottom - curveSize, curveSize, curveSize, 0, 90);
            path.AddArc(rect.X, rect.Bottom - curveSize, curveSize, curveSize, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
