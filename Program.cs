using System;
using System.Drawing;
using System.Windows.Forms;
using QuickLaunchTool.Forms;
using QuickLaunchTool.Utils;

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
                var localization = LocalizationManager.Instance;
                MessageBox.Show(
                    localization.GetString("Program_Exception_Message", ex.Message, ex.StackTrace),
                    localization.GetString("Program_Exception_Title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
