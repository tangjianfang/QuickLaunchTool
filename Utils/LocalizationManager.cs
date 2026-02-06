using System;
using System.Globalization;
using System.Resources;
using System.Reflection;

namespace QuickLaunchTool.Utils
{
    /// <summary>
    /// 本地化管理器 - 负责管理多语言资源
    /// </summary>
    public sealed class LocalizationManager
    {
        private static LocalizationManager? _instance;
        private static readonly object _lock = new object();

        private ResourceManager? _resourceManager;
        private CultureInfo _currentCulture;

        // 支持的语言列表
        private static readonly string[] SupportedLanguages = new[]
        {
            "zh-CN", // 简体中文
            "en-US", // 英语
            "ja-JP", // 日语
            "ko-KR", // 韩语
            "fr-FR", // 法语
            "de-DE", // 德语
            "es-ES"  // 西班牙语
        };

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static LocalizationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new LocalizationManager();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 语言变更事件
        /// </summary>
        public event EventHandler? LanguageChanged;

        /// <summary>
        /// 当前语言代码
        /// </summary>
        public string CurrentLanguage => _currentCulture.Name;

        /// <summary>
        /// 获取支持的语言列表
        /// </summary>
        public string[] GetSupportedLanguages() => SupportedLanguages;

        private LocalizationManager()
        {
            // 初始化为系统默认语言，如果不支持则使用中文
            var systemLanguage = CultureInfo.CurrentUICulture.Name;
            var defaultLanguage = IsSupportedLanguage(systemLanguage) ? systemLanguage : "zh-CN";
            _currentCulture = new CultureInfo(defaultLanguage);

            InitializeResourceManager();
        }

        /// <summary>
        /// 初始化资源管理器
        /// </summary>
        private void InitializeResourceManager()
        {
            try
            {
                _resourceManager = new ResourceManager(
                    "QuickLaunchTool.Resources.Strings",
                    Assembly.GetExecutingAssembly());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化资源管理器失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置当前语言
        /// </summary>
        /// <param name="languageCode">语言代码（如 zh-CN, en-US）</param>
        public void SetLanguage(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
                return;

            // 如果语言不支持，回退到中文
            if (!IsSupportedLanguage(languageCode))
            {
                languageCode = "zh-CN";
            }

            // 如果语言没有变化，不触发事件
            if (_currentCulture.Name == languageCode)
                return;

            _currentCulture = new CultureInfo(languageCode);

            // 触发语言变更事件
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 检查语言是否支持
        /// </summary>
        private bool IsSupportedLanguage(string languageCode)
        {
            foreach (var lang in SupportedLanguages)
            {
                if (lang.Equals(languageCode, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 获取本地化字符串
        /// </summary>
        /// <param name="key">资源键</param>
        /// <param name="args">格式化参数</param>
        /// <returns>本地化后的字符串</returns>
        public string GetString(string key, params object[] args)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            try
            {
                var value = _resourceManager?.GetString(key, _currentCulture);

                if (value == null)
                {
                    // 如果找不到资源，尝试使用默认语言（中文）
                    value = _resourceManager?.GetString(key, new CultureInfo("zh-CN"));
                }

                if (value == null)
                {
                    // 如果还是找不到，返回键名
                    return $"[{key}]";
                }

                // 如果有参数，进行格式化
                if (args != null && args.Length > 0)
                {
                    return string.Format(value, args);
                }

                return value;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取资源字符串失败 [{key}]: {ex.Message}");
                return $"[{key}]";
            }
        }

        /// <summary>
        /// 获取枚举的显示名称
        /// </summary>
        /// <param name="enumValue">枚举值</param>
        /// <returns>本地化后的显示名称</returns>
        public string GetEnumDisplayName(Enum enumValue)
        {
            if (enumValue == null)
                return string.Empty;

            var enumType = enumValue.GetType();
            var enumName = enumValue.ToString();
            var key = $"Enum_{enumType.Name}_{enumName}";

            return GetString(key);
        }

        /// <summary>
        /// 获取语言的显示名称
        /// </summary>
        /// <param name="languageCode">语言代码</param>
        /// <returns>语言的本地化显示名称</returns>
        public string GetLanguageDisplayName(string languageCode)
        {
            return GetString($"Lang_{languageCode}");
        }

        /// <summary>
        /// 获取语言的原生名称（固定不变，不受当前语言影响）
        /// </summary>
        /// <param name="languageCode">语言代码</param>
        /// <returns>语言的原生显示名称</returns>
        public string GetLanguageNativeName(string languageCode)
        {
            return languageCode switch
            {
                "zh-CN" => "简体中文",
                "en-US" => "English",
                "ja-JP" => "日本語",
                "ko-KR" => "한국어",
                "fr-FR" => "Français",
                "de-DE" => "Deutsch",
                "es-ES" => "Español",
                _ => languageCode
            };
        }

        /// <summary>
        /// 获取枚举的固定英文显示名称（不受当前语言影响）
        /// </summary>
        /// <param name="enumValue">枚举值</param>
        /// <returns>固定的英文显示名称</returns>
        public string GetEnumFixedName(Enum enumValue)
        {
            if (enumValue == null)
                return string.Empty;

            var enumType = enumValue.GetType();
            var enumName = enumValue.ToString();

            // 根据枚举类型和值返回固定的英文显示名称
            if (enumType.Name == "SortMode")
            {
                return enumName switch
                {
                    "Name" => "Name",
                    "Modified" => "Modified Time",
                    "UseCount" => "Use Count",
                    _ => enumName
                };
            }
            else if (enumType.Name == "ThemeMode")
            {
                return enumName switch
                {
                    "Light" => "Light",
                    "Dark" => "Dark",
                    _ => enumName
                };
            }
            else if (enumType.Name == "IconSize")
            {
                return enumName switch
                {
                    "Large" => "Large",
                    "Medium" => "Medium",
                    "Small" => "Small",
                    _ => enumName
                };
            }

            return enumName;
        }
    }
}
