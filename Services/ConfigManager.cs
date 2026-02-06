using System;
using System.IO;
using Newtonsoft.Json;
using QuickLaunchTool.Models;
using QuickLaunchTool.Utils;

namespace QuickLaunchTool.Services
{
    /// <summary>
    /// Configuration management service
    /// </summary>
    public sealed class ConfigManager
    {
        private static ConfigManager? _instance;
        private static readonly object _lock = new object();

        private AppConfig _config;
        private readonly string _configPath;

        /// <summary>
        /// Get singleton instance
        /// </summary>
        public static ConfigManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ConfigManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private ConfigManager()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "QuickLaunchTool");

            _configPath = Path.Combine(appDataPath, "config.json");
            _config = new AppConfig();
        }

        /// <summary>
        /// Load configuration
        /// </summary>
        public bool Load()
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    _config = AppConfig.GetDefault();
                    // Initialize localization manager
                    LocalizationManager.Instance.SetLanguage(_config.Language);
                    Save();
                    return true;
                }

                var json = File.ReadAllText(_configPath);
                var loaded = JsonConvert.DeserializeObject<AppConfig>(json);

                if (loaded != null && loaded.Validate())
                {
                    _config = loaded;
                    // Initialize localization manager
                    LocalizationManager.Instance.SetLanguage(_config.Language);
                    return true;
                }
                else
                {
                    // Invalid configuration, restore default
                    _config = AppConfig.GetDefault();
                    // Initialize localization manager
                    LocalizationManager.Instance.SetLanguage(_config.Language);
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load configuration: {ex.Message}");
                _config = AppConfig.GetDefault();
                // Initialize localization manager
                LocalizationManager.Instance.SetLanguage(_config.Language);
                return false;
            }
        }

        /// <summary>
        /// Save configuration
        /// </summary>
        public bool Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(_configPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory!);
                }

                var json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                File.WriteAllText(_configPath, json);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save configuration: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Restore default configuration
        /// </summary>
        public void Reset()
        {
            _config = AppConfig.GetDefault();
            Save();
        }

        /// <summary>
        /// Get current configuration
        /// </summary>
        public AppConfig GetConfig()
        {
            return _config;
        }

        /// <summary>
        /// Update configuration
        /// </summary>
        public void UpdateConfig(AppConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            _config = config;

            LocalizationManager.Instance.SetLanguage(_config.Language);
            Save();
        }

        /// <summary>
        /// Get configuration path
        /// </summary>
        public string GetConfigPath()
        {
            return _configPath;
        }
    }
}
