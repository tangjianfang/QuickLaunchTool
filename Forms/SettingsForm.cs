using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using QuickLaunchTool.Models;
using QuickLaunchTool.Services;
using QuickLaunchTool.Utils;

namespace QuickLaunchTool.Forms
{
    /// <summary>
    /// 设置窗体
    /// </summary>
    public partial class SettingsForm : Form
    {
        private readonly ConfigManager _configManager = ConfigManager.Instance;
        private readonly LocalizationManager _localization = LocalizationManager.Instance;
        private readonly AppConfig _config;
        private ComboBox? _languageCombo;
        private ComboBox? _sortModeCombo;
        private ComboBox? _themeCombo;
        private ComboBox? _iconSizeCombo;
        private CheckBox? _topMostCheckBox;
        private NumericUpDown? _opacityNumeric;

        // 标签引用（用于更新本地化）
        private Label? _languageLabel;
        private Label? _sortLabel;
        private Label? _themeLabel;
        private Label? _iconSizeLabel;
        private Label? _opacityLabel;
        private Button? _okBtn;
        private Button? _cancelBtn;

        // 防止递归更新的标志
        private bool _isUpdating = false;

        public SettingsForm()
        {
            System.Diagnostics.Debug.WriteLine("========== SettingsForm 构造函数开始 ==========");

            InitializeComponent();
            _config = _configManager.GetConfig();

            System.Diagnostics.Debug.WriteLine($"[SettingsForm] 从配置管理器获取的语言: {_config.Language}");
            System.Diagnostics.Debug.WriteLine($"[SettingsForm] LocalizationManager 当前语言: {_localization.CurrentLanguage}");

            // 设置标志，防止初始化时触发事件
            _isUpdating = true;

            SetupUI();

            // 初始化完成，恢复标志
            _isUpdating = false;

            // 订阅语言变更事件
            _localization.LanguageChanged += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsForm] 收到 LanguageChanged 事件");
                UpdateLocalization();
            };

            // 订阅 Load 事件，在窗体加载后设置语言选中项
            this.Load += SettingsForm_Load;

            System.Diagnostics.Debug.WriteLine("========== SettingsForm 构造函数结束 ==========");
        }

        /// <summary>
        /// 窗体加载事件 - 在此设置所有下拉框的选中项
        /// </summary>
        private void SettingsForm_Load(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[SettingsForm_Load] 窗体加载完成");

            // 设置更新标志，避免触发 SelectedIndexChanged 事件
            _isUpdating = true;
            try
            {
                // 设置语言选中项
                if (_languageCombo != null && _languageCombo.Items.Count > 0)
                {
                    var currentLang = _config.Language;
                    System.Diagnostics.Debug.WriteLine($"[SettingsForm_Load] 设置语言: {currentLang}");

                    _languageCombo.SelectedValue = currentLang;
                    if (_languageCombo.SelectedIndex == -1)
                    {
                        _languageCombo.SelectedIndex = 0;
                    }
                }

                // 设置排序方式选中项
                if (_sortModeCombo != null && _sortModeCombo.Items.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[SettingsForm_Load] 设置排序方式: {_config.SortMode}");
                    foreach (EnumItem<SortMode> item in _sortModeCombo.Items)
                    {
                        if (item.Value == _config.SortMode)
                        {
                            _sortModeCombo.SelectedItem = item;
                            break;
                        }
                    }
                }

                // 设置主题选中项
                if (_themeCombo != null && _themeCombo.Items.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[SettingsForm_Load] 设置主题: {_config.Theme}");
                    foreach (EnumItem<ThemeMode> item in _themeCombo.Items)
                    {
                        if (item.Value == _config.Theme)
                        {
                            _themeCombo.SelectedItem = item;
                            break;
                        }
                    }
                }

                // 设置图标大小选中项
                if (_iconSizeCombo != null && _iconSizeCombo.Items.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[SettingsForm_Load] 设置图标大小: {_config.IconSize}");
                    foreach (EnumItem<IconSize> item in _iconSizeCombo.Items)
                    {
                        if (item.Value == _config.IconSize)
                        {
                            _iconSizeCombo.SelectedItem = item;
                            break;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine("[SettingsForm_Load] 所有下拉框设置完成");
            }
            finally
            {
                _isUpdating = false;
            }
        }

        /// <summary>
        /// 初始化UI
        /// </summary>
        private void SetupUI()
        {
            System.Diagnostics.Debug.WriteLine("[SetupUI] 开始初始化UI");

            this.Text = _localization.GetString("SettingsForm_Title");
            this.Size = new Size(400, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.DialogResult = DialogResult.Cancel;

            var tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 7,
                Padding = new Padding(20)
            };

            // 设置列宽
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // ========== 语言选择 ==========
            _languageLabel = new Label
            {
                Text = _localization.GetString("SettingsForm_Language"),
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                TextAlign = ContentAlignment.MiddleLeft
            };
            tableLayout.Controls.Add(_languageLabel, 0, 0);

            _languageCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            _languageCombo.DisplayMember = "DisplayName";
            _languageCombo.ValueMember = "Code";

            // 填充语言列表（使用固定的原生名称）
            var languageList = new List<LanguageItem>();
            System.Diagnostics.Debug.WriteLine("[SetupUI] 开始填充语言列表");
            foreach (var lang in _localization.GetSupportedLanguages())
            {
                var nativeName = _localization.GetLanguageNativeName(lang);
                languageList.Add(new LanguageItem
                {
                    Code = lang,
                    DisplayName = nativeName
                });
                System.Diagnostics.Debug.WriteLine($"[SetupUI] 添加语言: Code={lang}, DisplayName={nativeName}");
            }
            _languageCombo.DataSource = languageList;

            // 注意：选中项的设置移到了 UI 构建完成后（见底部）
            _languageCombo.SelectedIndexChanged += LanguageCombo_SelectedIndexChanged;
            tableLayout.Controls.Add(_languageCombo, 1, 0);

            // ... 其他控件保持不变 ...
            // ========== 排序方式 ==========
            _sortLabel = new Label
            {
                Text = _localization.GetString("SettingsForm_SortMode"),
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                TextAlign = ContentAlignment.MiddleLeft
            };
            tableLayout.Controls.Add(_sortLabel, 0, 1);

            _sortModeCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            _sortModeCombo.DisplayMember = "DisplayName";
            _sortModeCombo.ValueMember = "Value";

            var sortModes = new List<EnumItem<SortMode>>();
            foreach (SortMode mode in Enum.GetValues(typeof(SortMode)))
            {
                sortModes.Add(new EnumItem<SortMode>
                {
                    Value = mode,
                    DisplayName = _localization.GetEnumFixedName(mode)
                });
            }
            _sortModeCombo.DataSource = sortModes;
            tableLayout.Controls.Add(_sortModeCombo, 1, 1);

            // ========== 主题 ==========
            _themeLabel = new Label
            {
                Text = _localization.GetString("SettingsForm_Theme"),
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                TextAlign = ContentAlignment.MiddleLeft
            };
            tableLayout.Controls.Add(_themeLabel, 0, 2);

            _themeCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            _themeCombo.DisplayMember = "DisplayName";
            _themeCombo.ValueMember = "Value";

            var themes = new List<EnumItem<ThemeMode>>();
            foreach (ThemeMode theme in Enum.GetValues(typeof(ThemeMode)))
            {
                themes.Add(new EnumItem<ThemeMode>
                {
                    Value = theme,
                    DisplayName = _localization.GetEnumFixedName(theme)
                });
            }
            _themeCombo.DataSource = themes;
            tableLayout.Controls.Add(_themeCombo, 1, 2);

            // ========== 图标大小 ==========
            _iconSizeLabel = new Label
            {
                Text = _localization.GetString("SettingsForm_IconSize"),
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                TextAlign = ContentAlignment.MiddleLeft
            };
            tableLayout.Controls.Add(_iconSizeLabel, 0, 3);

            _iconSizeCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            _iconSizeCombo.DisplayMember = "DisplayName";
            _iconSizeCombo.ValueMember = "Value";

            var iconSizes = new List<EnumItem<IconSize>>();
            foreach (IconSize size in Enum.GetValues(typeof(IconSize)))
            {
                iconSizes.Add(new EnumItem<IconSize>
                {
                    Value = size,
                    DisplayName = _localization.GetEnumFixedName(size)
                });
            }
            _iconSizeCombo.DataSource = iconSizes;
            tableLayout.Controls.Add(_iconSizeCombo, 1, 3);

            // ========== 置顶 ==========
            _topMostCheckBox = new CheckBox
            {
                Text = _localization.GetString("SettingsForm_TopMost"),
                Checked = _config.TopMost,
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };
            tableLayout.Controls.Add(_topMostCheckBox, 1, 4);

            // ========== 不透明度 ==========
            _opacityLabel = new Label
            {
                Text = _localization.GetString("SettingsForm_Opacity"),
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                TextAlign = ContentAlignment.MiddleLeft
            };
            tableLayout.Controls.Add(_opacityLabel, 0, 5);

            _opacityNumeric = new NumericUpDown
            {
                Minimum = 10,
                Maximum = 100,
                Value = (decimal)(_config.Opacity * 100),
                Increment = 5,
                Dock = DockStyle.Fill,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            tableLayout.Controls.Add(_opacityNumeric, 1, 5);

            // ========== 按钮面板 ==========
            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Fill,
                AutoSize = true
            };

            _okBtn = new Button
            {
                Text = _localization.GetString("SettingsForm_ButtonOK"),
                DialogResult = DialogResult.OK,
                Width = 75,
                Height = 23
            };
            _okBtn.Click += (s, e) => SaveConfig();

            _cancelBtn = new Button
            {
                Text = _localization.GetString("SettingsForm_ButtonCancel"),
                DialogResult = DialogResult.Cancel,
                Width = 75,
                Height = 23
            };

            buttonPanel.Controls.Add(_cancelBtn);
            buttonPanel.Controls.Add(_okBtn);

            tableLayout.SetColumnSpan(buttonPanel, 2);
            tableLayout.Controls.Add(buttonPanel, 0, 6);

            this.Controls.Add(tableLayout);
            this.AcceptButton = _okBtn;
            this.CancelButton = _cancelBtn;

            System.Diagnostics.Debug.WriteLine("[SetupUI] UI初始化完成");
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        private void SaveConfig()
        {
            System.Diagnostics.Debug.WriteLine("========== SaveConfig 开始 ==========");

            // 保存语言选择
            if (_languageCombo != null && _languageCombo.SelectedItem is LanguageItem langItem)
            {
                System.Diagnostics.Debug.WriteLine($"[SaveConfig] 保存语言: {langItem.Code} ({langItem.DisplayName})");
                _config.Language = langItem.Code;
            }

            if (_sortModeCombo != null && _sortModeCombo.SelectedItem is EnumItem<SortMode> sortItem)
                _config.SortMode = sortItem.Value;

            if (_themeCombo != null && _themeCombo.SelectedItem is EnumItem<ThemeMode> themeItem)
                _config.Theme = themeItem.Value;

            if (_iconSizeCombo != null && _iconSizeCombo.SelectedItem is EnumItem<IconSize> sizeItem)
                _config.IconSize = sizeItem.Value;

            if (_topMostCheckBox != null)
                _config.TopMost = _topMostCheckBox.Checked;

            if (_opacityNumeric != null)
                _config.Opacity = (double)_opacityNumeric.Value / 100.0;

            System.Diagnostics.Debug.WriteLine($"[SaveConfig] 调用 UpdateConfig，语言={_config.Language}");
            _configManager.UpdateConfig(_config);

            System.Diagnostics.Debug.WriteLine($"[SaveConfig] UpdateConfig 完成后，LocalizationManager.CurrentLanguage={_localization.CurrentLanguage}");

            this.DialogResult = DialogResult.OK;

            System.Diagnostics.Debug.WriteLine("========== SaveConfig 结束 ==========");
        }

        /// <summary>
        /// 语言选择变更事件
        /// </summary>
        private void LanguageCombo_SelectedIndexChanged(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[LanguageCombo_SelectedIndexChanged] 触发，_isUpdating={_isUpdating}");

            // 如果正在批量更新UI，忽略此事件
            if (_isUpdating)
            {
                System.Diagnostics.Debug.WriteLine("[LanguageCombo_SelectedIndexChanged] 忽略（_isUpdating=true）");
                return;
            }

            if (_languageCombo?.SelectedItem is LanguageItem langItem)
            {
                System.Diagnostics.Debug.WriteLine($"[LanguageCombo_SelectedIndexChanged] 选中语言: {langItem.Code} ({langItem.DisplayName})");
                System.Diagnostics.Debug.WriteLine($"[LanguageCombo_SelectedIndexChanged] 调用 SetLanguage");

                // 切换语言，这会触发 LanguageChanged 事件，进而调用 UpdateLocalization()
                _localization.SetLanguage(langItem.Code);

                System.Diagnostics.Debug.WriteLine($"[LanguageCombo_SelectedIndexChanged] SetLanguage 完成");
            }
        }

        /// <summary>
        /// 更新界面本地化文本
        /// </summary>
        private void UpdateLocalization()
        {
            System.Diagnostics.Debug.WriteLine("[UpdateLocalization] 开始更新本地化");

            // 设置更新标志，防止触发下拉框的SelectedIndexChanged事件导致递归
            _isUpdating = true;
            try
            {
                // 更新窗口标题
                this.Text = _localization.GetString("SettingsForm_Title");

                // 更新标签
                if (_languageLabel != null)
                    _languageLabel.Text = _localization.GetString("SettingsForm_Language");
                if (_sortLabel != null)
                    _sortLabel.Text = _localization.GetString("SettingsForm_SortMode");
                if (_themeLabel != null)
                    _themeLabel.Text = _localization.GetString("SettingsForm_Theme");
                if (_iconSizeLabel != null)
                    _iconSizeLabel.Text = _localization.GetString("SettingsForm_IconSize");
                if (_topMostCheckBox != null)
                    _topMostCheckBox.Text = _localization.GetString("SettingsForm_TopMost");
                if (_opacityLabel != null)
                    _opacityLabel.Text = _localization.GetString("SettingsForm_Opacity");

                // 更新按钮
                if (_okBtn != null)
                    _okBtn.Text = _localization.GetString("SettingsForm_ButtonOK");
                if (_cancelBtn != null)
                    _cancelBtn.Text = _localization.GetString("SettingsForm_ButtonCancel");

                // 注意：所有下拉框（语言、排序方式、主题、图标大小）都使用固定的显示名称，
                // 不需要在语言切换时更新

                System.Diagnostics.Debug.WriteLine("[UpdateLocalization] 更新本地化完成");
            }
            finally
            {
                // 恢复标志
                _isUpdating = false;
            }
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

    /// <summary>
    /// 语言项辅助类
    /// </summary>
    public class LanguageItem
    {
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    /// <summary>
    /// 枚举项辅助类
    /// </summary>
    public class EnumItem<T> where T : Enum
    {
        public T Value { get; set; } = default!;
        public string DisplayName { get; set; } = string.Empty;
    }
}