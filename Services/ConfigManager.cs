using System;
using System.IO;
using Newtonsoft.Json;
using QuickLaunchTool.Models;

namespace QuickLaunchTool.Services
{
    /// <summary>
    /// 配置管理服务
    /// </summary>
    public sealed class ConfigManager
    {
        private static ConfigManager? _instance;
        private static readonly object _lock = new object();

        private AppConfig _config;
        private readonly string _configPath;

        /// <summary>
        /// 获取单例实例
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
        /// 加载配置
        /// </summary>
        public bool Load()
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    _config = AppConfig.GetDefault();
                    Save();
                    return true;
                }

                var json = File.ReadAllText(_configPath);
                var loaded = JsonConvert.DeserializeObject<AppConfig>(json);

                if (loaded != null && loaded.Validate())
                {
                    _config = loaded;
                    return true;
                }
                else
                {
                    // 配置无效，恢复默认
                    _config = AppConfig.GetDefault();
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载配置失败: {ex.Message}");
                _config = AppConfig.GetDefault();
                return false;
            }
        }

        /// <summary>
        /// 保存配置
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
                System.Diagnostics.Debug.WriteLine($"保存配置失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 恢复默认配置
        /// </summary>
        public void Reset()
        {
            _config = AppConfig.GetDefault();
            Save();
        }

        /// <summary>
        /// 获取当前配置
        /// </summary>
        public AppConfig GetConfig()
        {
            return _config;
        }

        /// <summary>
        /// 更新配置
        /// </summary>
        public void UpdateConfig(AppConfig config)
        {
            if (config.Validate())
            {
                _config = config;
                Save();
            }
        }

        /// <summary>
        /// 获取配置路径
        /// </summary>
        public string GetConfigPath()
        {
            return _configPath;
        }
    }
}
