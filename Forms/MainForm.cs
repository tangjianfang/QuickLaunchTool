using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using QuickLaunchTool.Models;
using QuickLaunchTool.Services;
using QuickLaunchTool.Controls;
using QuickLaunchTool.Utils;

namespace QuickLaunchTool.Forms
{
    /// <summary>
    /// 主窗体
    /// </summary>
    public partial class MainForm : Form
    {
        private readonly FileScanner _fileScanner = new();
        private readonly ConfigManager _configManager = ConfigManager.Instance;
        private readonly LocalizationManager _localization = LocalizationManager.Instance;
        private List<AppInfo> _appList = new();
        private FlowLayoutPanel? _flowLayoutPanel;
        private TextBox? _searchBox;
        private ToolStrip? _toolStrip;
        private HashSet<AppButton> _selectedButtons = new();

        // 工具栏控件引用（用于更新本地化）
        private ToolStripLabel? _searchLabel;
        private ToolStripButton? _addFileBtn;
        private ToolStripButton? _addFolderBtn;
        private ToolStripButton? _importTaskbarBtn;
        private ToolStripButton? _deleteSelectedBtn;
        private ToolStripButton? _settingsBtn;

        public MainForm()
        {
            InitializeComponent();

            // 先订阅语言变更事件
            _localization.LanguageChanged += (s, e) => UpdateLocalization();

            SetupUI();
            LoadConfig();

            // LoadConfig会加载配置文件中的语言设置，需要手动刷新UI
            UpdateLocalization();

            // 订阅 Load 事件，在窗体加载后应用主题（此时 Handle 已创建）
            this.Load += MainForm_Load;
        }

        /// <summary>
        /// 窗体加载事件 - 应用主题
        /// </summary>
        private void MainForm_Load(object? sender, EventArgs e)
        {
            var config = _configManager.GetConfig();
            bool darkTheme = config.Theme == QuickLaunchTool.Models.ThemeMode.Dark;
            ThemeManager.ApplyTheme(this, darkTheme);
        }

        /// <summary>
        /// 初始化UI
        /// </summary>
        private void SetupUI()
        {
            this.Text = _localization.GetString("MainForm_Title");
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimumSize = new Size(250, 200);
            this.BackColor = Color.White;

            // 设置窗口图标
            try
            {
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Ico", "QuickLaunchTool.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    this.Icon = new Icon(iconPath);
                }
            }
            catch { }

            // 创建工具栏
            _toolStrip = new ToolStrip
            {
                Dock = DockStyle.Top
            };
            _searchLabel = new ToolStripLabel(_localization.GetString("MainForm_Toolbar_Search"));
            _searchBox = new TextBox { Width = 150, Height = 25 };
            _searchBox.TextChanged += SearchBox_TextChanged;

            _addFileBtn = new ToolStripButton(_localization.GetString("MainForm_Toolbar_AddFile"));
            _addFileBtn.Click += AddFileBtn_Click;

            _addFolderBtn = new ToolStripButton(_localization.GetString("MainForm_Toolbar_AddFolder"));
            _addFolderBtn.Click += AddFolderBtn_Click;

            _importTaskbarBtn = new ToolStripButton(_localization.GetString("MainForm_Toolbar_ImportTaskbar"));
            _importTaskbarBtn.Click += ImportTaskbarBtn_Click;

            _deleteSelectedBtn = new ToolStripButton(_localization.GetString("MainForm_Toolbar_DeleteSelected"));
            _deleteSelectedBtn.Click += DeleteSelectedBtn_Click;

            _settingsBtn = new ToolStripButton(_localization.GetString("MainForm_Toolbar_Settings"));
            _settingsBtn.Click += SettingsBtn_Click;

            _toolStrip.Items.Add(_searchLabel);
            _toolStrip.Items.Add(new ToolStripControlHost(_searchBox) { AutoSize = false });
            _toolStrip.Items.Add(new ToolStripSeparator());
            _toolStrip.Items.Add(_addFileBtn);
            _toolStrip.Items.Add(_addFolderBtn);
            _toolStrip.Items.Add(_importTaskbarBtn);
            _toolStrip.Items.Add(new ToolStripSeparator());
            _toolStrip.Items.Add(_deleteSelectedBtn);
            _toolStrip.Items.Add(new ToolStripSeparator());
            _toolStrip.Items.Add(_settingsBtn);

            // 创建FlowLayoutPanel（内容区域）
            _flowLayoutPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(3)
            };

            // 添加控件到窗体
            this.Controls.Add(_flowLayoutPanel);      // 内容
            this.Controls.Add(_toolStrip);             // 工具栏

            // 监听窗口大小改变事件，调整状态标签宽度
            this.SizeChanged += MainForm_SizeChanged;
        }

        /// <summary>
        /// 窗口大小改变时调整状态标签宽度
        /// </summary>
        private void MainForm_SizeChanged(object? sender, EventArgs e)
        {
            if (_flowLayoutPanel != null && _flowLayoutPanel.Controls.Count > 0)
            {
                var firstControl = _flowLayoutPanel.Controls[0];
                if (firstControl is Label statusLabel)
                {
                    statusLabel.Width = _flowLayoutPanel.ClientSize.Width - 10;
                }
            }
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        private void LoadConfig()
        {
            _configManager.Load();
            var config = _configManager.GetConfig();

            if (config.WindowPosition != Point.Empty)
            {
                this.Location = config.WindowPosition;
            }

            if (config.WindowSize != Size.Empty)
            {
                this.Size = config.WindowSize;
            }

            this.TopMost = config.TopMost;
            this.Opacity = config.Opacity;

            // 加载缓存的应用列表
            if (config.CachedAppPaths.Count > 0)
            {
                LoadFromCache(config.CachedAppPaths);
            }
            else
            {
                // 显示欢迎提示
                DisplayWelcomeMessage();
            }
        }

        /// <summary>
        /// 从缓存加载应用列表
        /// </summary>
        private void LoadFromCache(List<string> cachedPaths)
        {
            try
            {
                _appList = new List<AppInfo>();
                foreach (var path in cachedPaths)
                {
                    if (System.IO.File.Exists(path))
                    {
                        var fileInfo = new System.IO.FileInfo(path);
                        _appList.Add(new AppInfo
                        {
                            Name = Path.GetFileNameWithoutExtension(path),
                            FullPath = path,
                            FileSize = fileInfo.Length,
                            LastModified = fileInfo.LastWriteTime,
                            UseCount = 0
                        });
                    }
                    else
                    {
                        // 即使文件不存在也添加，用于显示提示
                        _appList.Add(new AppInfo
                        {
                            Name = Path.GetFileNameWithoutExtension(path),
                            FullPath = path,
                            FileSize = 0,
                            LastModified = DateTime.MinValue,
                            UseCount = 0
                        });
                    }
                }

                var config = _configManager.GetConfig();
                _appList = SortApps(_appList, config.SortMode);
                DisplayApps(_appList);
            }
            catch (Exception ex)
            {
                Utils.Logger.Error($"从缓存加载失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示欢迎消息
        /// </summary>
        private void DisplayWelcomeMessage()
        {
            if (_flowLayoutPanel == null)
                return;

            _flowLayoutPanel.Controls.Clear();
            var welcomeLabel = new Label
            {
                Text = _localization.GetString("MainForm_WelcomeMessage"),
                AutoSize = true,
                Location = new Point(20, 20),
                Font = new Font("Microsoft YaHei", 10f)
            };
            _flowLayoutPanel.Controls.Add(welcomeLabel);
        }

        /// <summary>
        /// 添加文件按钮点击
        /// </summary>
        private void AddFileBtn_Click(object? sender, EventArgs e)
        {
            bool wasTopMost = this.TopMost;
            this.TopMost = false;

            try
            {
                using var dialog = new OpenFileDialog
                {
                    Title = _localization.GetString("MainForm_FileDialog_Title"),
                    Filter = _localization.GetString("MainForm_FileDialog_Filter"),
                    Multiselect = true
                };

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    AddFiles(dialog.FileNames);
                }
            }
            finally
            {
                this.TopMost = wasTopMost;
            }
        }

        /// <summary>
        /// 添加文件夹按钮点击
        /// </summary>
        private async void AddFolderBtn_Click(object? sender, EventArgs e)
        {
            bool wasTopMost = this.TopMost;
            this.TopMost = false;

            try
            {
                using var dialog = new FolderBrowserDialog
                {
                    Description = _localization.GetString("MainForm_FolderDialog_Title"),
                    ShowNewFolderButton = false
                };

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    var folderPath = dialog.SelectedPath;

                    // 显示加载提示
                    if (_flowLayoutPanel != null)
                    {
                        var statusLabel = _flowLayoutPanel.Controls.OfType<Label>().FirstOrDefault();
                        if (statusLabel != null && statusLabel.Text.Contains(_localization.GetString("MainForm_StatusCount", 0).Split(' ')[0]))
                        {
                            statusLabel.Text = _localization.GetString("MainForm_ScanningFolder");
                        }
                    }

                    // 扫描文件夹中的exe文件
                    var exeFiles = await _fileScanner.ScanFolderAsync(folderPath, true);

                    if (exeFiles.Count > 0)
                    {
                        AddApps(exeFiles);
                        MessageBox.Show(
                            _localization.GetString("MainForm_AddSuccess_Message", exeFiles.Count),
                            _localization.GetString("MainForm_AddSuccess_Title"),
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            _localization.GetString("MainForm_NoExeFound_Message"),
                            _localization.GetString("MainForm_NoExeFound_Title"),
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            finally
            {
                this.TopMost = wasTopMost;
            }
        }

        /// <summary>
        /// 添加文件到列表
        /// </summary>
        private void AddFiles(string[] filePaths)
        {
            var newApps = new List<AppInfo>();

            foreach (var path in filePaths)
            {
                if (!System.IO.File.Exists(path))
                    continue;

                // 检查是否已存在
                if (_appList.Any(a => a.FullPath.Equals(path, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var fileInfo = new FileInfo(path);
                newApps.Add(new AppInfo
                {
                    Name = Path.GetFileNameWithoutExtension(path),
                    FullPath = path,
                    FileSize = fileInfo.Length,
                    LastModified = fileInfo.LastWriteTime,
                    UseCount = 0
                });
            }

            if (newApps.Count > 0)
            {
                AddApps(newApps);
            }
        }

        /// <summary>
        /// 添加应用到列表
        /// </summary>
        private void AddApps(List<AppInfo> apps)
        {
            foreach (var app in apps)
            {
                if (!_appList.Any(a => a.FullPath.Equals(app.FullPath, StringComparison.OrdinalIgnoreCase)))
                {
                    _appList.Add(app);
                }
            }

            // 排序并显示
            var config = _configManager.GetConfig();
            _appList = SortApps(_appList, config.SortMode);
            DisplayApps(_appList);

            // 更新缓存
            config.CachedAppPaths = _appList.Select(a => a.FullPath).ToList();
            _configManager.UpdateConfig(config);
        }

        /// <summary>
        /// 导入任务栏按钮点击
        /// </summary>
        private void ImportTaskbarBtn_Click(object? sender, EventArgs e)
        {
            bool wasTopMost = this.TopMost;
            this.TopMost = false;

            try
            {
                ImportTaskbarPaths();
            }
            finally
            {
                this.TopMost = wasTopMost;
            }
        }

        /// <summary>
        /// 导入任务栏已锁定的应用路径
        /// </summary>
        private void ImportTaskbarPaths()
        {
            try
            {
                var taskbarPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar"
                );

                if (!Directory.Exists(taskbarPath))
                {
                    MessageBox.Show(
                        _localization.GetString("MainForm_NoTaskbarDir_Message"),
                        _localization.GetString("MainForm_NoTaskbarDir_Title"),
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var shortcuts = Directory.GetFiles(taskbarPath, "*.lnk");
                var newApps = new List<AppInfo>();

                foreach (var shortcut in shortcuts)
                {
                    try
                    {
                        var targetPath = GetShortcutTarget(shortcut);
                        // 只导入exe文件路径
                        if (!string.IsNullOrEmpty(targetPath) &&
                            System.IO.File.Exists(targetPath) &&
                            targetPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
                            !_appList.Any(a => a.FullPath.Equals(targetPath, StringComparison.OrdinalIgnoreCase)))
                        {
                            var fileInfo = new FileInfo(targetPath);
                            newApps.Add(new AppInfo
                            {
                                Name = Path.GetFileNameWithoutExtension(targetPath),
                                FullPath = targetPath,
                                FileSize = fileInfo.Length,
                                LastModified = fileInfo.LastWriteTime,
                                UseCount = 0
                            });
                        }
                    }
                    catch
                    {
                        // 跳过无法解析的快捷方式
                    }
                }

                if (newApps.Count > 0)
                {
                    AddApps(newApps);
                    MessageBox.Show(
                        _localization.GetString("MainForm_ImportSuccess_Message", newApps.Count),
                        _localization.GetString("MainForm_ImportSuccess_Title"),
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(
                        _localization.GetString("MainForm_NoNewApps_Message"),
                        _localization.GetString("MainForm_NoNewApps_Title"),
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    _localization.GetString("MainForm_ImportError_Message", ex.Message),
                    _localization.GetString("MainForm_ImportError_Title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 获取快捷方式的目标路径
        /// </summary>
        private string GetShortcutTarget(string shortcutPath)
        {
            try
            {
                // 使用Shell32 COM对象解析快捷方式
                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return string.Empty;

                dynamic? shell = Activator.CreateInstance(shellType);
                if (shell == null) return string.Empty;

                try
                {
                    dynamic shortcut = shell.CreateShortcut(shortcutPath);
                    string targetPath = shortcut.TargetPath;
                    return targetPath ?? string.Empty;
                }
                finally
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(shell);
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 删除选中的应用
        /// </summary>
        private void DeleteSelectedBtn_Click(object? sender, EventArgs e)
        {
            if (_selectedButtons.Count == 0)
            {
                MessageBox.Show(
                    _localization.GetString("MainForm_DeletePrompt_Message"),
                    _localization.GetString("MainForm_DeletePrompt_Title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                _localization.GetString("MainForm_DeleteConfirm_Message", _selectedButtons.Count),
                _localization.GetString("MainForm_DeleteConfirm_Title"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                var appsToRemove = _selectedButtons.Select(btn => btn.AppInfo).Where(app => app != null).ToList();

                foreach (var app in appsToRemove)
                {
                    _appList.Remove(app!);
                }

                _selectedButtons.Clear();

                // 更新缓存
                var config = _configManager.GetConfig();
                config.CachedAppPaths = _appList.Select(a => a.FullPath).ToList();
                _configManager.UpdateConfig(config);

                // 重新显示
                DisplayApps(_appList);
            }
        }

        /// <summary>
        /// 排序应用列表
        /// </summary>
        private List<AppInfo> SortApps(List<AppInfo> apps, SortMode sortMode)
        {
            return sortMode switch
            {
                SortMode.Name => apps.OrderBy(a => a.Name).ToList(),
                SortMode.Modified => apps.OrderByDescending(a => a.LastModified).ToList(),
                SortMode.UseCount => apps.OrderByDescending(a => a.UseCount).ToList(),
                _ => apps
            };
        }

        /// <summary>
        /// 显示应用程序
        /// </summary>
        private void DisplayApps(List<AppInfo> apps)
        {
            if (_flowLayoutPanel == null)
                return;

            _flowLayoutPanel.Controls.Clear();

            // 添加状态标签（应用数量）
            var statusLabel = new Label
            {
                Text = _localization.GetString("MainForm_StatusCount", apps.Count),
                Width = _flowLayoutPanel.ClientSize.Width - 10,
                Height = 16,
                BackColor = Color.FromArgb(220, 220, 220),
                Font = new Font("Microsoft YaHei", 8.5f, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 2),
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            _flowLayoutPanel.Controls.Add(statusLabel);
            _flowLayoutPanel.SetFlowBreak(statusLabel, true); // 强制下一个控件换行

            // 获取图标大小设置
            var config = _configManager.GetConfig();
            var iconSize = config.IconSize;

            // 添加应用按钮
            foreach (var app in apps)
            {
                var button = new AppButton
                {
                    AppInfo = app
                };
                button.SetIconSize(iconSize);
                button.LaunchApp += (s, e) => SaveConfig();
                button.RemoveFromList += (s, e) => RemoveAppFromList(app);
                button.SelectionChanged += Button_SelectionChanged;
                _flowLayoutPanel.Controls.Add(button);
            }
        }

        /// <summary>
        /// 按钮选择状态改变
        /// </summary>
        private void Button_SelectionChanged(object? sender, EventArgs e)
        {
            if (sender is AppButton button)
            {
                if (button.Selected)
                {
                    _selectedButtons.Add(button);
                }
                else
                {
                    _selectedButtons.Remove(button);
                }
            }
        }

        /// <summary>
        /// 从列表移除应用
        /// </summary>
        private void RemoveAppFromList(AppInfo app)
        {
            if (_appList.Remove(app))
            {
                // 更新缓存
                var config = _configManager.GetConfig();
                config.CachedAppPaths = _appList.Select(a => a.FullPath).ToList();
                _configManager.UpdateConfig(config);

                // 重新显示
                DisplayApps(_appList);
            }
        }

        /// <summary>
        /// 过滤搜索
        /// </summary>
        private void SearchBox_TextChanged(object? sender, EventArgs e)
        {
            var searchText = _searchBox?.Text ?? "";
            var filtered = _appList
                .Where(a => a.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            DisplayApps(filtered);
        }

        /// <summary>
        /// 设置按钮点击
        /// </summary>
        private void SettingsBtn_Click(object? sender, EventArgs e)
        {
            // 临时禁用 TopMost 以避免对话框被压在后面
            bool wasTopMost = this.TopMost;
            this.TopMost = false;

            try
            {
                var settingsForm = new SettingsForm();
                if (settingsForm.ShowDialog(this) == DialogResult.OK)
                {
                    // 设置完成后，重新加载配置并应用
                    var config = _configManager.GetConfig();

                    // 应用语言设置（这会触发界面更新）
                    _localization.SetLanguage(config.Language);

                    // 应用主题设置
                    bool darkTheme = config.Theme == QuickLaunchTool.Models.ThemeMode.Dark;
                    ThemeManager.ApplyTheme(this, darkTheme);

                    this.TopMost = config.TopMost;
                    this.Opacity = config.Opacity;

                    // 重新排序和显示（应用图标大小和排序方式的变化）
                    _appList = SortApps(_appList, config.SortMode);
                    DisplayApps(_appList);
                }
            }
            finally
            {
                // 恢复 TopMost 状态
                var config = _configManager.GetConfig();
                this.TopMost = config.TopMost;
            }
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        private void SaveConfig()
        {
            var config = _configManager.GetConfig();
            config.WindowPosition = this.Location;
            config.WindowSize = this.Size;
            _configManager.UpdateConfig(config);
        }

        /// <summary>
        /// 更新界面本地化文本
        /// </summary>
        private void UpdateLocalization()
        {
            // 更新窗口标题
            this.Text = _localization.GetString("MainForm_Title");

            // 更新工具栏按钮文本
            if (_searchLabel != null)
                _searchLabel.Text = _localization.GetString("MainForm_Toolbar_Search");
            if (_addFileBtn != null)
                _addFileBtn.Text = _localization.GetString("MainForm_Toolbar_AddFile");
            if (_addFolderBtn != null)
                _addFolderBtn.Text = _localization.GetString("MainForm_Toolbar_AddFolder");
            if (_importTaskbarBtn != null)
                _importTaskbarBtn.Text = _localization.GetString("MainForm_Toolbar_ImportTaskbar");
            if (_deleteSelectedBtn != null)
                _deleteSelectedBtn.Text = _localization.GetString("MainForm_Toolbar_DeleteSelected");
            if (_settingsBtn != null)
                _settingsBtn.Text = _localization.GetString("MainForm_Toolbar_Settings");

            // 更新应用按钮的本地化文本
            if (_flowLayoutPanel != null)
            {
                foreach (var control in _flowLayoutPanel.Controls)
                {
                    if (control is AppButton appButton)
                    {
                        appButton.UpdateLocalization();
                    }
                    else if (control is Label label && label.Text.Contains(_localization.GetString("MainForm_WelcomeMessage").Substring(0, 5)))
                    {
                        // 更新欢迎消息
                        label.Text = _localization.GetString("MainForm_WelcomeMessage");
                    }
                }

                // 更新状态标签文本
                var statusLabel = _flowLayoutPanel.Controls.OfType<Label>().FirstOrDefault();
                if (statusLabel != null && _appList.Count > 0)
                {
                    statusLabel.Text = _localization.GetString("MainForm_StatusCount", _appList.Count);
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveConfig();
            base.OnFormClosing(e);
        }

        /// <summary>
        /// 初始化组件（由设计器生成）
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ResumeLayout(false);
        }
    }
}
