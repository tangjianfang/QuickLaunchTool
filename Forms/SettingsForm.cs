using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using QuickLaunchTool.Models;
using QuickLaunchTool.Services;

namespace QuickLaunchTool.Forms
{
    /// <summary>
    /// 设置窗体
    /// </summary>
    public partial class SettingsForm : Form
    {
        private readonly ConfigManager _configManager = ConfigManager.Instance;
        private readonly AppConfig _config;
        private ComboBox? _sortModeCombo;
        private ComboBox? _themeCombo;
        private ComboBox? _iconSizeCombo;
        private CheckBox? _topMostCheckBox;
        private NumericUpDown? _opacityNumeric;

        public SettingsForm()
        {
            InitializeComponent();
            _config = _configManager.GetConfig();
            SetupUI();
            LoadConfig();
        }

        /// <summary>
        /// 初始化UI
        /// </summary>
        private void SetupUI()
        {
            this.Text = "设置";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.DialogResult = DialogResult.Cancel;

            var tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                Padding = new Padding(20)
            };

            // 排序方式
            var sortLabel = new Label { Text = "排序方式:", AutoSize = true };
            tableLayout.Controls.Add(sortLabel, 0, 0);
            _sortModeCombo = new ComboBox
            {
                DataSource = Enum.GetValues(typeof(SortMode)),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 200
            };
            tableLayout.Controls.Add(_sortModeCombo, 1, 0);

            // 主题
            var themeLabel = new Label { Text = "主题:", AutoSize = true };
            tableLayout.Controls.Add(themeLabel, 0, 1);
            _themeCombo = new ComboBox
            {
                DataSource = Enum.GetValues(typeof(ThemeMode)),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 200
            };
            tableLayout.Controls.Add(_themeCombo, 1, 1);

            // 图标大小
            var iconSizeLabel = new Label { Text = "图标大小:", AutoSize = true };
            tableLayout.Controls.Add(iconSizeLabel, 0, 2);
            _iconSizeCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 200
            };
            _iconSizeCombo.Items.AddRange(new object[]
            {
                new { Text = "大 (50×60)", Value = IconSize.Large },
                new { Text = "中 (40×50)", Value = IconSize.Medium },
                new { Text = "小 (30×40)", Value = IconSize.Small }
            });
            _iconSizeCombo.DisplayMember = "Text";
            _iconSizeCombo.ValueMember = "Value";
            tableLayout.Controls.Add(_iconSizeCombo, 1, 2);

            // 窗口置顶
            _topMostCheckBox = new CheckBox { Text = "窗口置顶", AutoSize = true };
            tableLayout.Controls.Add(_topMostCheckBox, 0, 3);
            tableLayout.SetColumnSpan(_topMostCheckBox, 2);

            // 不透明度
            var opacityLabel = new Label { Text = "不透明度:", AutoSize = true };
            tableLayout.Controls.Add(opacityLabel, 0, 4);
            _opacityNumeric = new NumericUpDown
            {
                Minimum = 50,
                Maximum = 100,
                Value = 95,
                Width = 200,
                DecimalPlaces = 0
            };
            tableLayout.Controls.Add(_opacityNumeric, 1, 4);

            // 按钮面板
            var okBtn = new Button { Text = "确定", DialogResult = DialogResult.OK, Width = 80 };
            var cancelBtn = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Width = 80 };
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(10)
            };
            buttonPanel.Controls.Add(cancelBtn);
            buttonPanel.Controls.Add(okBtn);

            this.Controls.Add(tableLayout);
            this.Controls.Add(buttonPanel);

            okBtn.Click += (s, e) => SaveConfig();
            this.AcceptButton = okBtn;
            this.CancelButton = cancelBtn;
        }

        /// <summary>
        /// 加载配置到UI
        /// </summary>
        private void LoadConfig()
        {
            if (_sortModeCombo != null)
                _sortModeCombo.SelectedItem = _config.SortMode;

            if (_themeCombo != null)
                _themeCombo.SelectedItem = _config.Theme;

            if (_iconSizeCombo != null)
            {
                // 查找匹配的项
                foreach (var item in _iconSizeCombo.Items)
                {
                    var itemValue = item.GetType().GetProperty("Value")?.GetValue(item);
                    if (itemValue != null && (IconSize)itemValue == _config.IconSize)
                    {
                        _iconSizeCombo.SelectedItem = item;
                        break;
                    }
                }
            }

            if (_topMostCheckBox != null)
                _topMostCheckBox.Checked = _config.TopMost;

            if (_opacityNumeric != null)
                _opacityNumeric.Value = (decimal)(_config.Opacity * 100);
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        private void SaveConfig()
        {
            if (_sortModeCombo != null)
                _config.SortMode = (SortMode)_sortModeCombo.SelectedItem!;

            if (_themeCombo != null)
                _config.Theme = (ThemeMode)_themeCombo.SelectedItem!;

            if (_iconSizeCombo != null && _iconSizeCombo.SelectedItem != null)
            {
                var selectedValue = _iconSizeCombo.SelectedItem.GetType().GetProperty("Value")?.GetValue(_iconSizeCombo.SelectedItem);
                if (selectedValue != null)
                    _config.IconSize = (IconSize)selectedValue;
            }

            if (_topMostCheckBox != null)
                _config.TopMost = _topMostCheckBox.Checked;

            if (_opacityNumeric != null)
                _config.Opacity = (double)_opacityNumeric.Value / 100.0;

            _configManager.UpdateConfig(_config);
            this.DialogResult = DialogResult.OK;
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
