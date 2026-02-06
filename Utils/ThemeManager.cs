using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace QuickLaunchTool.Utils
{
    /// <summary>
    /// 主题管理器
    /// </summary>
    public static class ThemeManager
    {
        // 浅色主题颜色
        private static readonly Color LightBackground = Color.White;
        private static readonly Color LightForeground = Color.Black;
        private static readonly Color LightHover = Color.LightGray;
        private static readonly Color LightBorder = Color.DarkGray;

        // 深色主题颜色
        private static readonly Color DarkBackground = Color.FromArgb(45, 45, 48);
        private static readonly Color DarkForeground = Color.White;
        private static readonly Color DarkHover = Color.FromArgb(60, 60, 65);
        private static readonly Color DarkBorder = Color.FromArgb(100, 100, 100);

        // Windows API 用于设置深色标题栏
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        /// <summary>
        /// 获取背景色
        /// </summary>
        public static Color GetBackgroundColor(bool darkTheme)
        {
            return darkTheme ? DarkBackground : LightBackground;
        }

        /// <summary>
        /// 获取前景色
        /// </summary>
        public static Color GetForegroundColor(bool darkTheme)
        {
            return darkTheme ? DarkForeground : LightForeground;
        }

        /// <summary>
        /// 获取悬停色
        /// </summary>
        public static Color GetHoverColor(bool darkTheme)
        {
            return darkTheme ? DarkHover : LightHover;
        }

        /// <summary>
        /// 获取边框色
        /// </summary>
        public static Color GetBorderColor(bool darkTheme)
        {
            return darkTheme ? DarkBorder : LightBorder;
        }

        /// <summary>
        /// 应用主题到窗体（包括标题栏）
        /// </summary>
        public static void ApplyTheme(Form form, bool darkTheme)
        {
            // 设置深色标题栏（Windows 10 1809+ / Windows 11）
            try
            {
                if (form.Handle != IntPtr.Zero)
                {
                    int darkMode = darkTheme ? 1 : 0;
                    DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
                }
            }
            catch
            {
                // 忽略错误（旧版 Windows 不支持）
            }

            // 应用主题到控件
            ApplyThemeToControl(form, darkTheme);
        }

        /// <summary>
        /// 应用主题到控件
        /// </summary>
        public static void ApplyTheme(Control control, bool darkTheme)
        {
            if (control is Form form)
            {
                ApplyTheme(form, darkTheme);
            }
            else
            {
                ApplyThemeToControl(control, darkTheme);
            }
        }

        /// <summary>
        /// 递归应用主题到控件及其子控件
        /// </summary>
        private static void ApplyThemeToControl(Control control, bool darkTheme)
        {
            var bgColor = GetBackgroundColor(darkTheme);
            var fgColor = GetForegroundColor(darkTheme);

            // 设置控件颜色
            control.BackColor = bgColor;
            control.ForeColor = fgColor;

            // 特殊处理 ToolStrip
            if (control is ToolStrip toolStrip)
            {
                toolStrip.BackColor = bgColor;
                toolStrip.ForeColor = fgColor;
                toolStrip.Renderer = new ToolStripProfessionalRenderer(new ThemeColorTable(darkTheme));

                foreach (ToolStripItem item in toolStrip.Items)
                {
                    item.BackColor = bgColor;
                    item.ForeColor = fgColor;

                    // 处理 ToolStripControlHost 中的控件
                    if (item is ToolStripControlHost host && host.Control != null)
                    {
                        host.Control.BackColor = darkTheme ? DarkBackground : Color.White;
                        host.Control.ForeColor = fgColor;
                    }
                }
            }

            // 递归处理子控件
            foreach (Control child in control.Controls)
            {
                ApplyThemeToControl(child, darkTheme);
            }
        }
    }

    /// <summary>
    /// 自定义 ToolStrip 颜色表
    /// </summary>
    internal class ThemeColorTable : ProfessionalColorTable
    {
        private readonly bool _darkTheme;

        public ThemeColorTable(bool darkTheme)
        {
            _darkTheme = darkTheme;
        }

        public override Color ToolStripDropDownBackground => _darkTheme ? Color.FromArgb(45, 45, 48) : base.ToolStripDropDownBackground;
        public override Color ImageMarginGradientBegin => _darkTheme ? Color.FromArgb(45, 45, 48) : base.ImageMarginGradientBegin;
        public override Color ImageMarginGradientMiddle => _darkTheme ? Color.FromArgb(45, 45, 48) : base.ImageMarginGradientMiddle;
        public override Color ImageMarginGradientEnd => _darkTheme ? Color.FromArgb(45, 45, 48) : base.ImageMarginGradientEnd;
        public override Color MenuBorder => _darkTheme ? Color.FromArgb(100, 100, 100) : base.MenuBorder;
        public override Color MenuItemBorder => _darkTheme ? Color.FromArgb(100, 100, 100) : base.MenuItemBorder;
        public override Color MenuItemSelected => _darkTheme ? Color.FromArgb(60, 60, 65) : base.MenuItemSelected;
        public override Color MenuItemSelectedGradientBegin => _darkTheme ? Color.FromArgb(60, 60, 65) : base.MenuItemSelectedGradientBegin;
        public override Color MenuItemSelectedGradientEnd => _darkTheme ? Color.FromArgb(60, 60, 65) : base.MenuItemSelectedGradientEnd;
        public override Color MenuStripGradientBegin => _darkTheme ? Color.FromArgb(45, 45, 48) : base.MenuStripGradientBegin;
        public override Color MenuStripGradientEnd => _darkTheme ? Color.FromArgb(45, 45, 48) : base.MenuStripGradientEnd;
        public override Color ToolStripBorder => _darkTheme ? Color.FromArgb(100, 100, 100) : base.ToolStripBorder;
        public override Color ToolStripGradientBegin => _darkTheme ? Color.FromArgb(45, 45, 48) : base.ToolStripGradientBegin;
        public override Color ToolStripGradientMiddle => _darkTheme ? Color.FromArgb(45, 45, 48) : base.ToolStripGradientMiddle;
        public override Color ToolStripGradientEnd => _darkTheme ? Color.FromArgb(45, 45, 48) : base.ToolStripGradientEnd;
    }
}
