using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace QuickLaunchTool.Models
{
    /// <summary>
    /// 排序方式枚举
    /// </summary>
    public enum SortMode
    {
        Name,       // 按名称排序
        Modified,   // 按修改时间排序
        UseCount    // 按使用次数排序
    }

    /// <summary>
    /// 主题枚举
    /// </summary>
    public enum ThemeMode
    {
        Light,      // 浅色主题
        Dark        // 深色主题
    }

    /// <summary>
    /// 图标大小枚举
    /// </summary>
    public enum IconSize
    {
        Large,      // 大图标 (50x60)
        Medium,     // 中图标 (40x50)
        Small       // 小图标 (30x40)
    }

    /// <summary>
    /// 应用配置模型
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// 排序方式
        /// </summary>
        [JsonProperty("sortMode")]
        public SortMode SortMode { get; set; } = SortMode.Name;

        /// <summary>
        /// 主题模式
        /// </summary>
        [JsonProperty("theme")]
        public ThemeMode Theme { get; set; } = ThemeMode.Light;

        /// <summary>
        /// 窗口位置
        /// </summary>
        [JsonProperty("windowPosition")]
        public Point WindowPosition { get; set; } = new Point(100, 100);

        /// <summary>
        /// 窗口大小
        /// </summary>
        [JsonProperty("windowSize")]
        public Size WindowSize { get; set; } = new Size(600, 400);

        /// <summary>
        /// 窗口置顶
        /// </summary>
        [JsonProperty("topMost")]
        public bool TopMost { get; set; } = true;

        /// <summary>
        /// 窗口不透明度（0-1）
        /// </summary>
        [JsonProperty("opacity")]
        public double Opacity { get; set; } = 0.95;

        /// <summary>
        /// 图标大小
        /// </summary>
        [JsonProperty("iconSize")]
        public IconSize IconSize { get; set; } = IconSize.Large;

        /// <summary>
        /// 缓存的应用程序路径列表
        /// </summary>
        [JsonProperty("cachedAppPaths")]
        public List<string> CachedAppPaths { get; set; } = new();

        /// <summary>
        /// 验证配置的有效性
        /// </summary>
        public bool Validate()
        {
            // 检查窗口大小的合理性
            if (WindowSize.Width < 200 || WindowSize.Height < 200)
                return false;

            // 检查不透明度范围
            if (Opacity < 0 || Opacity > 1)
                return false;

            return true;
        }

        /// <summary>
        /// 获取默认配置
        /// </summary>
        public static AppConfig GetDefault()
        {
            return new AppConfig
            {
                SortMode = SortMode.Name,
                Theme = ThemeMode.Light,
                WindowPosition = new Point(100, 100),
                WindowSize = new Size(600, 400),
                TopMost = true,
                Opacity = 0.95,
                IconSize = IconSize.Large
            };
        }
    }
}
