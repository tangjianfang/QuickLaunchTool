using System;
using System.Globalization;
using System.Resources;
using System.Reflection;

namespace QuickLaunchTool.Utils
{
    /// <summary>
    /// Localization manager - Responsible for managing multilingual resources
    /// </summary>
    public sealed class LocalizationManager
    {
        private static LocalizationManager? _instance;
        private static readonly object _lock = new object();

        private ResourceManager? _resourceManager;
        private CultureInfo _currentCulture;

        // Supported language list
        private static readonly string[] SupportedLanguages = new[]
        {
            "zh-CN", // Simplified Chinese
            "en-US", // English
            "ja-JP", // Japanese
            "ko-KR", // Korean
            "fr-FR", // French
            "de-DE", // German
            "es-ES"  // Spanish
        };

        /// <summary>
        /// Get singleton instance
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
        /// Language change event
        /// </summary>
        public event EventHandler? LanguageChanged;

        /// <summary>
        /// Current language code
        /// </summary>
        public string CurrentLanguage => _currentCulture.Name;

        /// <summary>
        /// Get list of supported languages
        /// </summary>
        public string[] GetSupportedLanguages() => SupportedLanguages;

        private LocalizationManager()
        {
            // Initialize to system default language, if not supported use Chinese
            var systemLanguage = CultureInfo.CurrentUICulture.Name;
            var defaultLanguage = IsSupportedLanguage(systemLanguage) ? systemLanguage : "zh-CN";
            _currentCulture = new CultureInfo(defaultLanguage);

            InitializeResourceManager();
        }

        /// <summary>
        /// Initialize resource manager
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
                System.Diagnostics.Debug.WriteLine($"Failed to initialize resource manager: {ex.Message}");
            }
        }

        /// <summary>
        /// Set current language
        /// </summary>
        /// <param name="languageCode">Language code (e.g. zh-CN, en-US)</param>
        public void SetLanguage(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
                return;

            // If language is not supported, fall back to Chinese
            if (!IsSupportedLanguage(languageCode))
            {
                languageCode = "zh-CN";
            }

            // If language hasn't changed, don't trigger event
            if (_currentCulture.Name == languageCode)
                return;

            _currentCulture = new CultureInfo(languageCode);

            // Trigger language change event
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Check if language is supported
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
        /// Get localized string
        /// </summary>
        /// <param name="key">Resource key</param>
        /// <param name="args">Format parameters</param>
        /// <returns>Localized string</returns>
        public string GetString(string key, params object[] args)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            try
            {
                var value = _resourceManager?.GetString(key, _currentCulture);

                if (value == null)
                {
                    // If resource not found, try using default language (Chinese)
                    value = _resourceManager?.GetString(key, new CultureInfo("zh-CN"));
                }

                if (value == null)
                {
                    // If still not found, return key name
                    return $"[{key}]";
                }

                // If parameters exist, format
                if (args != null && args.Length > 0)
                {
                    return string.Format(value, args);
                }

                return value;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get resource string [{key}]: {ex.Message}");
                return $"[{key}]";
            }
        }

        /// <summary>
        /// Get enum display name
        /// </summary>
        /// <param name="enumValue">Enum value</param>
        /// <returns>Localized display name</returns>
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
        /// Get language display name
        /// </summary>
        /// <param name="languageCode">Language code</param>
        /// <returns>Localized display name of the language</returns>
        public string GetLanguageDisplayName(string languageCode)
        {
            return GetString($"Lang_{languageCode}");
        }

        /// <summary>
        /// Get native name of language (fixed, not affected by current language)
        /// </summary>
        /// <param name="languageCode">Language code</param>
        /// <returns>Native display name of the language</returns>
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
        /// Get fixed English display name of enum (not affected by current language)
        /// </summary>
        /// <param name="enumValue">Enum value</param>
        /// <returns>Fixed English display name</returns>
        public string GetEnumFixedName(Enum enumValue)
        {
            if (enumValue == null)
                return string.Empty;

            var enumType = enumValue.GetType();
            var enumName = enumValue.ToString();

            // Return fixed English display names based on enum type and value
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
