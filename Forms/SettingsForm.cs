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
    /// Settings Form
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

        // Label references (for updating localization)
        private Label? _languageLabel;
        private Label? _sortLabel;
        private Label? _themeLabel;
        private Label? _iconSizeLabel;
        private Label? _opacityLabel;
        private Button? _okBtn;
        private Button? _cancelBtn;

        // Prevent recursive update flag
        private bool _isUpdating = false;

        public SettingsForm()
        {
            System.Diagnostics.Debug.WriteLine("========== SettingsForm Constructor Start ==========");

            InitializeComponent();
            _config = _configManager.GetConfig();

            System.Diagnostics.Debug.WriteLine($"[SettingsForm] Language from config manager: {_config.Language}");
            System.Diagnostics.Debug.WriteLine($"[SettingsForm] LocalizationManager current language: {_localization.CurrentLanguage}");

            // Set flag to prevent event triggering during initialization
            _isUpdating = true;

            SetupUI();

            // Initialization complete, restore flag
            _isUpdating = false;

            // Subscribe to language change event
            _localization.LanguageChanged += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsForm] Received LanguageChanged event");
                UpdateLocalization();
            };

            // Subscribe to Load event to set all dropdown selections after form is loaded
            this.Load += SettingsForm_Load;

            System.Diagnostics.Debug.WriteLine("========== SettingsForm Constructor End ==========");
        }

        /// <summary>
        /// Form Load event - Set all dropdown selections after form is loaded
        /// </summary>
        private void SettingsForm_Load(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[SettingsForm_Load] Form loaded");

            // Set update flag to avoid triggering SelectedIndexChanged event
            _isUpdating = true;
            try
            {
                // Set language selection
                if (_languageCombo != null && _languageCombo.Items.Count > 0)
                {
                    var currentLang = _config.Language;
                    System.Diagnostics.Debug.WriteLine($"[SettingsForm_Load] Setting language: {currentLang}");

                    _languageCombo.SelectedValue = currentLang;
                    if (_languageCombo.SelectedIndex == -1)
                    {
                        _languageCombo.SelectedIndex = 0;
                    }
                }

                // Set sort mode selection
                if (_sortModeCombo != null && _sortModeCombo.Items.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[SettingsForm_Load] Setting sort mode: {_config.SortMode}");
                    foreach (EnumItem<SortMode> item in _sortModeCombo.Items)
                    {
                        if (item.Value == _config.SortMode)
                        {
                            _sortModeCombo.SelectedItem = item;
                            break;
                        }
                    }
                }

                // Set theme selection
                if (_themeCombo != null && _themeCombo.Items.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[SettingsForm_Load] Setting theme: {_config.Theme}");
                    foreach (EnumItem<ThemeMode> item in _themeCombo.Items)
                    {
                        if (item.Value == _config.Theme)
                        {
                            _themeCombo.SelectedItem = item;
                            break;
                        }
                    }
                }

                // Set icon size selection
                if (_iconSizeCombo != null && _iconSizeCombo.Items.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[SettingsForm_Load] Setting icon size: {_config.IconSize}");
                    foreach (EnumItem<IconSize> item in _iconSizeCombo.Items)
                    {
                        if (item.Value == _config.IconSize)
                        {
                            _iconSizeCombo.SelectedItem = item;
                            break;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine("[SettingsForm_Load] All dropdowns set up");
            }
            finally
            {
                _isUpdating = false;
            }
        }

        /// <summary>
        /// Initialize UI
        /// </summary>
        private void SetupUI()
        {
            System.Diagnostics.Debug.WriteLine("[SetupUI] Starting UI initialization");

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

            // Set column widths
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // ========== Language Selection ==========
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

            // Populate language list (using fixed native names)
            var languageList = new List<LanguageItem>();
            System.Diagnostics.Debug.WriteLine("[SetupUI] Starting to populate language list");
            foreach (var lang in _localization.GetSupportedLanguages())
            {
                var nativeName = _localization.GetLanguageNativeName(lang);
                languageList.Add(new LanguageItem
                {
                    Code = lang,
                    DisplayName = nativeName
                });
                System.Diagnostics.Debug.WriteLine($"[SetupUI] Added language: Code={lang}, DisplayName={nativeName}");
            }
            _languageCombo.DataSource = languageList;

            // Note: Selection of the selected item is moved after UI construction is complete (see bottom)
            _languageCombo.SelectedIndexChanged += LanguageCombo_SelectedIndexChanged;
            tableLayout.Controls.Add(_languageCombo, 1, 0);

            // ... Other controls remain unchanged ...
            // ========== Sort Mode ==========
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

            // ========== Theme ==========
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

            // ========== Icon Size ==========
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

            // ========== Always on Top ==========
            _topMostCheckBox = new CheckBox
            {
                Text = _localization.GetString("SettingsForm_TopMost"),
                Checked = _config.TopMost,
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };
            tableLayout.Controls.Add(_topMostCheckBox, 1, 4);

            // ========== Opacity ==========
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

            // ========== Button Panel ==========
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

            System.Diagnostics.Debug.WriteLine("[SetupUI] UI initialization complete");
        }

        /// <summary>
        /// Save configuration
        /// </summary>
        private void SaveConfig()
        {
            System.Diagnostics.Debug.WriteLine("========== SaveConfig Start ==========");

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

            System.Diagnostics.Debug.WriteLine($"[SaveConfig] Calling UpdateConfig, language={_config.Language}");
            _configManager.UpdateConfig(_config);

            System.Diagnostics.Debug.WriteLine($"[SaveConfig] After UpdateConfig, LocalizationManager.CurrentLanguage={_localization.CurrentLanguage}");

            this.DialogResult = DialogResult.OK;

            System.Diagnostics.Debug.WriteLine("========== SaveConfig End ==========");
        }

        /// <summary>
        /// Language selection change event
        /// </summary>
        private void LanguageCombo_SelectedIndexChanged(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[LanguageCombo_SelectedIndexChanged] Triggered, _isUpdating={_isUpdating}");

            // If batch updating UI, ignore this event
            if (_isUpdating)
            {
                System.Diagnostics.Debug.WriteLine("[LanguageCombo_SelectedIndexChanged] Ignored (_isUpdating=true)");
                return;
            }

            if (_languageCombo?.SelectedItem is LanguageItem langItem)
            {
                System.Diagnostics.Debug.WriteLine($"[LanguageCombo_SelectedIndexChanged] Selected language: {langItem.Code} ({langItem.DisplayName})");
                System.Diagnostics.Debug.WriteLine($"[LanguageCombo_SelectedIndexChanged] Calling SetLanguage");

                // Switch language, this will trigger LanguageChanged event, which calls UpdateLocalization()
                _localization.SetLanguage(langItem.Code);

                System.Diagnostics.Debug.WriteLine($"[LanguageCombo_SelectedIndexChanged] SetLanguage complete");
            }
        }

        /// <summary>
        /// Update UI localization text
        /// </summary>
        private void UpdateLocalization()
        {
            System.Diagnostics.Debug.WriteLine("[UpdateLocalization] Starting localization update");

            // Set update flag to prevent recursion from dropdown SelectedIndexChanged event
            _isUpdating = true;
            try
            {
                // Update window title
                this.Text = _localization.GetString("SettingsForm_Title");

                // Update labels
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

                // Update buttons
                if (_okBtn != null)
                    _okBtn.Text = _localization.GetString("SettingsForm_ButtonOK");
                if (_cancelBtn != null)
                    _cancelBtn.Text = _localization.GetString("SettingsForm_ButtonCancel");

                // Note: All dropdowns (language, sort mode, theme, icon size) use fixed display names,
                // no need to update when language changes

                System.Diagnostics.Debug.WriteLine("[UpdateLocalization] Localization update complete");
            }
            finally
            {
                // Restore flag
                _isUpdating = false;
            }
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

    /// <summary>
    /// Language item helper class
    /// </summary>
    public class LanguageItem
    {
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Enum item helper class
    /// </summary>
    public class EnumItem<T> where T : Enum
    {
        public T Value { get; set; } = default!;
        public string DisplayName { get; set; } = string.Empty;
    }
}