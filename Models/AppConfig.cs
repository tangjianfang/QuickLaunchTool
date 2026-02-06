using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace QuickLaunchTool.Models
{
    /// <summary>
    /// Sorting mode options
    /// </summary>
    public enum SortMode
    {
        Name,       // Sort by name
        Modified,   // Sort by last modified date
        UseCount    // Sort by usage count
    }

    /// <summary>
    /// Themes supported by the app
    /// </summary>
    public enum ThemeMode
    {
        Light,      // Light theme
        Dark        // Dark theme
    }

    /// <summary>
    /// Icon size presets
    /// </summary>
    public enum IconSize
    {
        Large,      // Large icon (50x60)
        Medium,     // Medium icon (40x50)
        Small       // Small icon (30x40)
    }

    /// <summary>
    /// App configuration model
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// Sorting mode currently in use
        /// </summary>
        [JsonProperty("sortMode")]
        public SortMode SortMode { get; set; } = SortMode.Name;

        /// <summary>
        /// Theme mode currently in use
        /// </summary>
        [JsonProperty("theme")]
        public ThemeMode Theme { get; set; } = ThemeMode.Light;

        /// <summary>
        /// Window position
        /// </summary>
        [JsonProperty("windowPosition")]
        public Point WindowPosition { get; set; } = new Point(100, 100);

        /// <summary>
        /// Window size
        /// </summary>
        [JsonProperty("windowSize")]
        public Size WindowSize { get; set; } = new Size(600, 400);

        /// <summary>
        /// Whether the window stays on top
        /// </summary>
        [JsonProperty("topMost")]
        public bool TopMost { get; set; } = true;

        /// <summary>
        /// Window opacity (0-1)
        /// </summary>
        [JsonProperty("opacity")]
        public double Opacity { get; set; } = 0.95;

        /// <summary>
        /// Icon size setting
        /// </summary>
        [JsonProperty("iconSize")]
        public IconSize IconSize { get; set; } = IconSize.Large;

        /// <summary>
        /// Interface language (e.g., zh-CN, en-US)
        /// </summary>
        [JsonProperty("language")]
        public string Language { get; set; } = CultureInfo.CurrentUICulture.Name;

        /// <summary>
        /// Cached application path list
        /// </summary>
        [JsonProperty("cachedAppPaths")]
        public List<string> CachedAppPaths { get; set; } = new();

        /// <summary>
        /// Validate the configuration values
        /// </summary>
        public bool Validate()
        {
            // Ensure window dimensions are reasonable
            if (WindowSize.Width < 200 || WindowSize.Height < 200)
                return false;

            // Ensure opacity stays within bounds
            if (Opacity < 0 || Opacity > 1)
                return false;

            return true;
        }

        /// <summary>
        /// Create the default configuration
        /// </summary>
        public static AppConfig GetDefault()
        {
            // Derive the language from the system settings and fall back to English if unsupported
            var systemLang = CultureInfo.CurrentUICulture.Name;
            var supportedLangs = new[] { "zh-CN", "en-US", "ja-JP", "ko-KR", "fr-FR", "de-DE", "es-ES" };
            var defaultLang = "en-US";

            foreach (var lang in supportedLangs)
            {
                if (systemLang.Equals(lang, StringComparison.OrdinalIgnoreCase))
                {
                    defaultLang = lang;
                    break;
                }
            }

            return new AppConfig
            {
                SortMode = SortMode.Name,
                Theme = ThemeMode.Dark,
                WindowPosition = new Point(100, 100),
                WindowSize = new Size(600, 400),
                TopMost = true,
                Opacity = 1.0,
                IconSize = IconSize.Medium,
                Language = defaultLang
            };
        }
    }
}
