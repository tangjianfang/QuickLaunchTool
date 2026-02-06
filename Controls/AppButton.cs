using System;
using System.Drawing;
using System.Windows.Forms;
using QuickLaunchTool.Models;
using QuickLaunchTool.Services;
using QuickLaunchTool.Utils;

namespace QuickLaunchTool.Controls
{
    /// <summary>
    /// Custom application button control
    /// </summary>
    public partial class AppButton : UserControl
    {
        private readonly LocalizationManager _localization = LocalizationManager.Instance;
        private AppInfo? _appInfo;
        private bool _isHovered = false;
        private bool _isSelected = false;
        private IconSize _iconSize = IconSize.Large;
        private ContextMenuStrip? _contextMenu;
        private int _totalAppCount = 0; // Total count of applications

        /// <summary>
        /// Get or set bound application information
        /// </summary>
        public AppInfo? AppInfo
        {
            get => _appInfo;
            set
            {
                _appInfo = value;
                UpdateDisplay();
            }
        }

        /// <summary>
        /// Get or set selection status
        /// </summary>
        public bool Selected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Launch application event
        /// </summary>
        public event EventHandler? LaunchApp;

        /// <summary>
        /// Remove from list event
        /// </summary>
        public event EventHandler? RemoveFromList;

        /// <summary>
        /// Selection status changed event
        /// </summary>
        public event EventHandler? SelectionChanged;

        public AppButton()
        {
            InitializeComponent();
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        private void InitializeComponent()
        {
            this.Size = new Size(50, 60);
            this.BackColor = Color.Transparent; // 使用透明背景，从父容器继承
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.Cursor = Cursors.Hand;
            this.Margin = new Padding(2);
        }

        /// <summary>
        /// Set icon size
        /// </summary>
        public void SetIconSize(IconSize iconSize)
        {
            _iconSize = iconSize;
            switch (iconSize)
            {
                case IconSize.Large:
                    this.Size = new Size(50, 60);
                    break;
                case IconSize.Medium:
                    this.Size = new Size(40, 50);
                    break;
                case IconSize.Small:
                    this.Size = new Size(30, 40);
                    break;
            }
            Invalidate();
        }

        /// <summary>
        /// Set total application count
        /// </summary>
        public void SetTotalAppCount(int count)
        {
            _totalAppCount = count;
        }

        /// <summary>
        /// Update display
        /// </summary>
        private void UpdateDisplay()
        {
            if (_appInfo != null)
            {
                // Asynchronously load icon
                LoadIconAsync();
                Invalidate();
            }
        }

        /// <summary>
        /// Load icon asynchronously
        /// </summary>
        private async void LoadIconAsync()
        {
            if (_appInfo != null && _appInfo.Icon == null)
            {
                _appInfo.Icon = await IconExtractor.ExtractIconAsync(_appInfo.FullPath);
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Set high quality rendering
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            e.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            if (_appInfo == null)
                return;

            // Check if file exists
            bool fileExists = System.IO.File.Exists(_appInfo.FullPath);

            // Get parent container background color (for theme support)
            Color parentBgColor = this.Parent?.BackColor ?? Color.White;
            bool isDark = parentBgColor.GetBrightness() < 0.5f;

            // Draw background
            Color bgColor = parentBgColor;
            if (_isSelected)
                bgColor = Color.FromArgb(180, 200, 255); // Selection state is blue
            else if (_isHovered)
            {
                // Hover state: determine light or dark theme based on parent background color
                bgColor = isDark ? Color.FromArgb(60, 60, 65) : Color.FromArgb(220, 230, 240);
            }

            using (var bgBrush = new SolidBrush(bgColor))
            {
                e.Graphics.FillRectangle(bgBrush, ClientRectangle);
            }

            // If selected, draw border
            if (_isSelected)
            {
                using (var pen = new Pen(Color.FromArgb(100, 120, 200), 2))
                {
                    e.Graphics.DrawRectangle(pen, 1, 1, Width - 3, Height - 3);
                }
            }

            // If file doesn't exist, draw semi-transparent mask
            if (!fileExists)
            {
                using (var maskBrush = new SolidBrush(Color.FromArgb(150, 255, 255, 255)))
                {
                    e.Graphics.FillRectangle(maskBrush, ClientRectangle);
                }
            }

            // 绘制图标（保持透明度）
            if (_appInfo.Icon != null)
            {
                try
                {
                    // 根据图标大小设置计算图标矩形
                    Rectangle iconRect;
                    switch (_iconSize)
                    {
                        case IconSize.Large:
                            iconRect = new Rectangle(9, 6, 32, 32);
                            break;
                        case IconSize.Medium:
                            iconRect = new Rectangle(8, 5, 24, 24);
                            break;
                        case IconSize.Small:
                            iconRect = new Rectangle(7, 4, 16, 16);
                            break;
                        default:
                            iconRect = new Rectangle(9, 6, 32, 32);
                            break;
                    }

                    // Create temporary Bitmap with transparent background to render icon
                    using (var tempBitmap = new Bitmap(iconRect.Width, iconRect.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                    {
                        using (var g = Graphics.FromImage(tempBitmap))
                        {
                            // Set transparent background
                            g.Clear(Color.Transparent);
                            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                            // Draw icon on temporary bitmap
                            g.DrawIcon(_appInfo.Icon, new Rectangle(0, 0, iconRect.Width, iconRect.Height));
                        }

                        // If file doesn't exist, use semi-transparent drawing
                        if (!fileExists)
                        {
                            var colorMatrix = new System.Drawing.Imaging.ColorMatrix
                            {
                                Matrix33 = 0.4f // Transparency 40%
                            };
                            var imageAttributes = new System.Drawing.Imaging.ImageAttributes();
                            imageAttributes.SetColorMatrix(colorMatrix);
                            e.Graphics.DrawImage(tempBitmap, iconRect, 0, 0, tempBitmap.Width, tempBitmap.Height, GraphicsUnit.Pixel, imageAttributes);
                        }
                        else
                        {
                            // Draw temporary bitmap to control
                            e.Graphics.DrawImage(tempBitmap, iconRect);
                        }
                    }
                }
                catch
                {
                    // Degraded handling: draw icon directly
                    Rectangle iconRect;
                    switch (_iconSize)
                    {
                        case IconSize.Large:
                            iconRect = new Rectangle(9, 6, 32, 32);
                            break;
                        case IconSize.Medium:
                            iconRect = new Rectangle(8, 5, 24, 24);
                            break;
                        case IconSize.Small:
                            iconRect = new Rectangle(7, 4, 16, 16);
                            break;
                        default:
                            iconRect = new Rectangle(9, 6, 32, 32);
                            break;
                    }
                    e.Graphics.DrawIconUnstretched(_appInfo.Icon, iconRect);
                }
            }

            // If file doesn't exist, draw red border and warning mark
            if (!fileExists)
            {
                using (var pen = new Pen(Color.FromArgb(200, 255, 0, 0), 2))
                {
                    e.Graphics.DrawRectangle(pen, 1, 1, Width - 3, Height - 3);
                }

                // Draw warning icon
                using (var warningBrush = new SolidBrush(Color.FromArgb(220, 255, 0, 0)))
                using (var warningFont = new Font("Arial", 9, FontStyle.Bold))
                {
                    e.Graphics.DrawString("!", warningFont, warningBrush, new PointF(45, 26));
                }
            }

            // Draw application name - centered
            var font = new Font(Font.FontFamily, 6.8f, FontStyle.Regular);
            var textRect = new Rectangle(2, 40, Width - 4, Height - 42);
            var stringFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            };

            // Choose text color based on theme
            Color textColor;
            if (!fileExists)
            {
                textColor = Color.FromArgb(150, 255, 0, 0); // File doesn't exist, use red
            }
            else
            {
                textColor = isDark ? Color.FromArgb(220, 220, 220) : Color.FromArgb(64, 64, 64);
            }

            using (var brush = new SolidBrush(textColor))
            {
                e.Graphics.DrawString(_appInfo.Name, font, brush, textRect, stringFormat);
            }

            font.Dispose();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnClick(EventArgs e)
        {
            // If Ctrl key is pressed, toggle selection state
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                Selected = !Selected;
                Invalidate(); // Trigger redraw to show selection effect
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
            base.OnClick(e);
        }

        protected override void OnDoubleClick(EventArgs e)
        {
            if (_appInfo != null)
            {
                // Check if file exists
                if (!System.IO.File.Exists(_appInfo.FullPath))
                {
                    MessageBox.Show(
                        $"Application does not exist:\n{_appInfo.FullPath}\n\nPlease rescan or remove this item from the list.",
                        "File Not Found",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }

                // Double-click to launch application
                ProcessLauncher.Launch(_appInfo.FullPath);
                _appInfo.UseCount++;
                LaunchApp?.Invoke(this, EventArgs.Empty);
            }
            base.OnDoubleClick(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && _appInfo != null)
            {
                ShowContextMenu(e.Location);
            }
            base.OnMouseDown(e);
        }

        /// <summary>
        /// Display context menu
        /// </summary>
        private void ShowContextMenu(Point location)
        {
            if (_appInfo == null)
                return;

            // Rebuild menu to support localization
            _contextMenu = new ContextMenuStrip();

            _contextMenu.Items.Add(_localization.GetString("AppButton_MenuLaunch"), null, (s, e) => ProcessLauncher.Launch(_appInfo.FullPath));
            _contextMenu.Items.Add(_localization.GetString("AppButton_MenuRunAsAdmin"), null, (s, e) => ProcessLauncher.LaunchAsAdmin(_appInfo.FullPath));
            _contextMenu.Items.Add("-");
            _contextMenu.Items.Add(_localization.GetString("AppButton_MenuOpenLocation"), null, (s, e) => ProcessLauncher.OpenFileLocation(_appInfo.FullPath));
            _contextMenu.Items.Add(_localization.GetString("AppButton_MenuProperties"), null, (s, e) => ProcessLauncher.ShowProperties(_appInfo.FullPath));
            _contextMenu.Items.Add("-");

            // Add total count menu item (disabled, info only)
            var totalCountItem = new ToolStripMenuItem($"Total: {_totalAppCount}");
            totalCountItem.Enabled = false;
            _contextMenu.Items.Add(totalCountItem);

            _contextMenu.Items.Add("-");
            _contextMenu.Items.Add(_localization.GetString("AppButton_MenuRemove"), null, (s, e) =>
            {
                var result = MessageBox.Show(
                    _localization.GetString("AppButton_RemoveConfirm_Message", _appInfo.Name),
                    _localization.GetString("AppButton_RemoveConfirm_Title"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );
                if (result == DialogResult.Yes)
                {
                    RemoveFromList?.Invoke(this, EventArgs.Empty);
                }
            });

            _contextMenu.Show(this, location);
        }

        /// <summary>
        /// Update localization (for language hot switching)
        /// </summary>
        public void UpdateLocalization()
        {
            // Context menu is rebuilt when displayed, no extra handling needed
            // If you need to update text on controls, handle it here
        }
    }
}
