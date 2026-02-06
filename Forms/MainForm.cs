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
    /// Main Form
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

        // Toolbar control references (for updating localization)
        private ToolStripLabel? _searchLabel;
        private ToolStripButton? _addFileBtn;
        private ToolStripButton? _addFolderBtn;
        private ToolStripButton? _importTaskbarBtn;
        private ToolStripButton? _deleteSelectedBtn;
        private ToolStripButton? _settingsBtn;

        public MainForm()
        {
            InitializeComponent();

            _localization.LanguageChanged += (s, e) => UpdateLocalization();

            SetupUI();
            LoadConfig();
            UpdateLocalization();

            this.Load += MainForm_Load;
        }

        /// <summary>
        /// Form Load event - Apply theme
        /// </summary>
        private void MainForm_Load(object? sender, EventArgs e)
        {
            var config = _configManager.GetConfig();
            bool darkTheme = config.Theme == QuickLaunchTool.Models.ThemeMode.Dark;
            ThemeManager.ApplyTheme(this, darkTheme);
        }

        /// <summary>
        /// Initialize UI
        /// </summary>
        private void SetupUI()
        {
            this.Text = _localization.GetString("MainForm_Title");
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimumSize = new Size(100, 100);
            this.BackColor = Color.White;

            // Set window icon
            try
            {
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Ico", "QuickLaunchTool.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    this.Icon = new Icon(iconPath);
                }
            }
            catch { }

            // Create toolbar
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

            // Create FlowLayoutPanel (content area)
            _flowLayoutPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(3)
            };

            // Add controls to the form
            this.Controls.Add(_flowLayoutPanel);      // Content
            this.Controls.Add(_toolStrip);             // Toolbar

            // Listen to window size change event
            this.SizeChanged += MainForm_SizeChanged;
        }

        /// <summary>
        /// Adjust status label width when window size changes
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
        /// Load configuration
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

            // Load cached application list
            if (config.CachedAppPaths.Count > 0)
            {
                LoadFromCache(config.CachedAppPaths);
            }
            else
            {
                // First launch: automatically import taskbar applications
                ImportTaskbarPaths();
            }
        }

        /// <summary>
        /// Load application list from cache
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
                        // Add even if file doesn't exist, for showing tips
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
                Utils.Logger.Error($"Failed to load from cache: {ex.Message}");
            }
        }

        /// <summary>
        /// Display welcome message
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
        /// Add file button click handler
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
        /// Add folder button click handler
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

                    // Show loading tip
                    if (_flowLayoutPanel != null)
                    {
                        var statusLabel = _flowLayoutPanel.Controls.OfType<Label>().FirstOrDefault();
                        if (statusLabel != null && statusLabel.Text.Contains(_localization.GetString("MainForm_StatusCount", 0).Split(' ')[0]))
                        {
                            statusLabel.Text = _localization.GetString("MainForm_ScanningFolder");
                        }
                    }

                    // Scan exe files in the folder
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
        /// Add files to list
        /// </summary>
        private void AddFiles(string[] filePaths)
        {
            var newApps = new List<AppInfo>();

            foreach (var path in filePaths)
            {
                if (!System.IO.File.Exists(path))
                    continue;

                // Check if already exists
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
        /// Add applications to list
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

            // Sort and display
            var config = _configManager.GetConfig();
            _appList = SortApps(_appList, config.SortMode);
            DisplayApps(_appList);

            // Update cache
            config.CachedAppPaths = _appList.Select(a => a.FullPath).ToList();
            _configManager.UpdateConfig(config);
        }

        /// <summary>
        /// Import taskbar button click handler
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
        /// Import pinned application paths from taskbar
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
                        // Only import exe file paths
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
                        // Skip shortcuts that cannot be parsed
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
        /// Get the target path of a shortcut
        /// </summary>
        private string GetShortcutTarget(string shortcutPath)
        {
            try
            {
                // Use Shell32 COM object to parse shortcuts
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
        /// Delete selected applications
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

                // Update cache
                var config = _configManager.GetConfig();
                config.CachedAppPaths = _appList.Select(a => a.FullPath).ToList();
                _configManager.UpdateConfig(config);

                // Refresh display
                DisplayApps(_appList);
            }
        }

        /// <summary>
        /// Sort application list
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
        /// Display applications
        /// </summary>
        private void DisplayApps(List<AppInfo> apps)
        {
            if (_flowLayoutPanel == null)
                return;

            _flowLayoutPanel.Controls.Clear();

            // Get icon size settings
            var config = _configManager.GetConfig();
            var iconSize = config.IconSize;
            int totalCount = _appList.Count; // Store total count for context menu

            // Add application buttons
            foreach (var app in apps)
            {
                var button = new AppButton
                {
                    AppInfo = app
                };
                button.SetIconSize(iconSize);
                button.SetTotalAppCount(totalCount);
                button.LaunchApp += (s, e) => SaveConfig();
                button.RemoveFromList += (s, e) => RemoveAppFromList(app);
                button.SelectionChanged += Button_SelectionChanged;
                _flowLayoutPanel.Controls.Add(button);
            }
        }

        /// <summary>
        /// Button selection state changed
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
        /// Remove application from list
        /// </summary>
        private void RemoveAppFromList(AppInfo app)
        {
            if (_appList.Remove(app))
            {
                // Update cache
                var config = _configManager.GetConfig();
                config.CachedAppPaths = _appList.Select(a => a.FullPath).ToList();
                _configManager.UpdateConfig(config);

                // Refresh display
                DisplayApps(_appList);
            }
        }

        /// <summary>
        /// Search filter
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
        /// Settings button click handler
        /// </summary>
        private void SettingsBtn_Click(object? sender, EventArgs e)
        {
            // Temporarily disable TopMost to prevent dialog from being hidden
            bool wasTopMost = this.TopMost;
            this.TopMost = false;

            try
            {
                var settingsForm = new SettingsForm();
                if (settingsForm.ShowDialog(this) == DialogResult.OK)
                {
                    // After settings, reload configuration and apply
                    var config = _configManager.GetConfig();

                    // Apply language settings (this will trigger UI update)
                    _localization.SetLanguage(config.Language);

                    // Apply theme settings
                    bool darkTheme = config.Theme == QuickLaunchTool.Models.ThemeMode.Dark;
                    ThemeManager.ApplyTheme(this, darkTheme);

                    this.TopMost = config.TopMost;
                    this.Opacity = config.Opacity;

                    // Re-sort and display (apply icon size and sort mode changes)
                    _appList = SortApps(_appList, config.SortMode);
                    DisplayApps(_appList);
                }
            }
            finally
            {
                // Restore TopMost state
                var config = _configManager.GetConfig();
                this.TopMost = config.TopMost;
            }
        }

        /// <summary>
        /// Save configuration
        /// </summary>
        private void SaveConfig()
        {
            var config = _configManager.GetConfig();
            config.WindowPosition = this.Location;
            config.WindowSize = this.Size;
            _configManager.UpdateConfig(config);
        }

        /// <summary>
        /// Update UI localization text
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
        /// Initialize component (generated by designer)
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ResumeLayout(false);
        }
    }
}
