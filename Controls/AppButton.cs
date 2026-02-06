using System;
using System.Drawing;
using System.Windows.Forms;
using QuickLaunchTool.Models;
using QuickLaunchTool.Services;
using QuickLaunchTool.Utils;

namespace QuickLaunchTool.Controls
{
    /// <summary>
    /// 应用按钮自定义控件
    /// </summary>
    public partial class AppButton : UserControl
    {
        private readonly LocalizationManager _localization = LocalizationManager.Instance;
        private AppInfo? _appInfo;
        private bool _isHovered = false;
        private bool _isSelected = false;
        private IconSize _iconSize = IconSize.Large;
        private ContextMenuStrip? _contextMenu;

        /// <summary>
        /// 获取或设置绑定的应用信息
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
        /// 获取或设置选中状态
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
        /// 启动应用事件
        /// </summary>
        public event EventHandler? LaunchApp;

        /// <summary>
        /// 从列表移除事件
        /// </summary>
        public event EventHandler? RemoveFromList;

        /// <summary>
        /// 选择状态改变事件
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
        /// 设置图标大小
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
        /// 更新显示
        /// </summary>
        private void UpdateDisplay()
        {
            if (_appInfo != null)
            {
                // 异步加载图标
                LoadIconAsync();
                Invalidate();
            }
        }

        /// <summary>
        /// 异步加载图标
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
            // 设置高质量渲染
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            e.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            if (_appInfo == null)
                return;

            // 检查文件是否存在
            bool fileExists = System.IO.File.Exists(_appInfo.FullPath);

            // 获取父容器背景色（用于主题支持）
            Color parentBgColor = this.Parent?.BackColor ?? Color.White;
            bool isDark = parentBgColor.GetBrightness() < 0.5f;

            // 绘制背景
            Color bgColor = parentBgColor;
            if (_isSelected)
                bgColor = Color.FromArgb(180, 200, 255); // 选中状态是蓝色
            else if (_isHovered)
            {
                // 悬停状态：根据父容器背景色判断是深色还是浅色主题
                bgColor = isDark ? Color.FromArgb(60, 60, 65) : Color.FromArgb(220, 230, 240);
            }

            using (var bgBrush = new SolidBrush(bgColor))
            {
                e.Graphics.FillRectangle(bgBrush, ClientRectangle);
            }

            // 如果被选中，绘制边框
            if (_isSelected)
            {
                using (var pen = new Pen(Color.FromArgb(100, 120, 200), 2))
                {
                    e.Graphics.DrawRectangle(pen, 1, 1, Width - 3, Height - 3);
                }
            }

            // 如果文件不存在，绘制半透明遮罩
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

                    // 创建一个透明背景的临时Bitmap来渲染图标
                    using (var tempBitmap = new Bitmap(iconRect.Width, iconRect.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                    {
                        using (var g = Graphics.FromImage(tempBitmap))
                        {
                            // 设置透明背景
                            g.Clear(Color.Transparent);
                            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                            // 在临时位图上绘制图标
                            g.DrawIcon(_appInfo.Icon, new Rectangle(0, 0, iconRect.Width, iconRect.Height));
                        }

                        // 如果文件不存在，使用半透明绘制
                        if (!fileExists)
                        {
                            var colorMatrix = new System.Drawing.Imaging.ColorMatrix
                            {
                                Matrix33 = 0.4f // 透明度40%
                            };
                            var imageAttributes = new System.Drawing.Imaging.ImageAttributes();
                            imageAttributes.SetColorMatrix(colorMatrix);
                            e.Graphics.DrawImage(tempBitmap, iconRect, 0, 0, tempBitmap.Width, tempBitmap.Height, GraphicsUnit.Pixel, imageAttributes);
                        }
                        else
                        {
                            // 将临时位图绘制到控件上
                            e.Graphics.DrawImage(tempBitmap, iconRect);
                        }
                    }
                }
                catch
                {
                    // 降级处理：直接绘制图标
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

            // 如果文件不存在，绘制红色边框和警告标记
            if (!fileExists)
            {
                using (var pen = new Pen(Color.FromArgb(200, 255, 0, 0), 2))
                {
                    e.Graphics.DrawRectangle(pen, 1, 1, Width - 3, Height - 3);
                }

                // 绘制警告图标
                using (var warningBrush = new SolidBrush(Color.FromArgb(220, 255, 0, 0)))
                using (var warningFont = new Font("Arial", 9, FontStyle.Bold))
                {
                    e.Graphics.DrawString("!", warningFont, warningBrush, new PointF(45, 26));
                }
            }

            // 绘制应用名称 - 居中显示
            var font = new Font(Font.FontFamily, 6.8f, FontStyle.Regular);
            var textRect = new Rectangle(2, 40, Width - 4, Height - 42);
            var stringFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            };

            // 根据主题选择文本颜色
            Color textColor;
            if (!fileExists)
            {
                textColor = Color.FromArgb(150, 255, 0, 0); // 文件不存在时用红色
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
            // 如枟按下Ctrl键，切换选中状态
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                Selected = !Selected;
                Invalidate(); // 触发重绘显示选中效果
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
            base.OnClick(e);
        }

        protected override void OnDoubleClick(EventArgs e)
        {
            if (_appInfo != null)
            {
                // 检查文件是否存在
                if (!System.IO.File.Exists(_appInfo.FullPath))
                {
                    MessageBox.Show(
                        $"应用程序不存在:\n{_appInfo.FullPath}\n\n请重新扫描或从列表中移除此项。",
                        "文件不存在",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }

                // 双击启动应用
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
        /// 显示右键菜单
        /// </summary>
        private void ShowContextMenu(Point location)
        {
            if (_appInfo == null)
                return;

            // 重建菜单以支持本地化
            _contextMenu = new ContextMenuStrip();

            _contextMenu.Items.Add(_localization.GetString("AppButton_MenuLaunch"), null, (s, e) => ProcessLauncher.Launch(_appInfo.FullPath));
            _contextMenu.Items.Add(_localization.GetString("AppButton_MenuRunAsAdmin"), null, (s, e) => ProcessLauncher.LaunchAsAdmin(_appInfo.FullPath));
            _contextMenu.Items.Add("-");
            _contextMenu.Items.Add(_localization.GetString("AppButton_MenuOpenLocation"), null, (s, e) => ProcessLauncher.OpenFileLocation(_appInfo.FullPath));
            _contextMenu.Items.Add(_localization.GetString("AppButton_MenuProperties"), null, (s, e) => ProcessLauncher.ShowProperties(_appInfo.FullPath));
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
        /// 更新本地化（用于语言热切换）
        /// </summary>
        public void UpdateLocalization()
        {
            // 右键菜单在显示时重建，无需额外处理
            // 如果需要更新控件上的文本，在此处理
        }
    }
}
