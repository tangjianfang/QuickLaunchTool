using System;
using System.Drawing;
using System.Windows.Forms;
using QuickLaunchTool.Forms;

namespace QuickLaunchTool
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // 启用VisualStyles
            Application.EnableVisualStyles();
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"程序异常: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
