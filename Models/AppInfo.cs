using System;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;

namespace QuickLaunchTool.Models
{
    /// <summary>
    /// 应用程序信息模型
    /// </summary>
    public class AppInfo
    {
        /// <summary>
        /// 应用名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 完整路径
        /// </summary>
        [JsonProperty("fullPath")]
        public string FullPath { get; set; } = string.Empty;

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        [JsonProperty("fileSize")]
        public long FileSize { get; set; }

        /// <summary>
        /// 最后修改时间
        /// </summary>
        [JsonProperty("lastModified")]
        public DateTime LastModified { get; set; }

        /// <summary>
        /// 使用次数
        /// </summary>
        [JsonProperty("useCount")]
        public int UseCount { get; set; }

        /// <summary>
        /// 图标资源（不序列化）
        /// </summary>
        [JsonIgnore]
        public Icon? Icon { get; set; }

        /// <summary>
        /// 从文件路径创建AppInfo
        /// </summary>
        public AppInfo(string filePath)
        {
            FullPath = filePath;
            Name = Path.GetFileNameWithoutExtension(filePath);

            var fileInfo = new FileInfo(filePath);
            FileSize = fileInfo.Length;
            LastModified = fileInfo.LastWriteTime;
            UseCount = 0;
        }

        /// <summary>
        /// 无参构造函数（用于JSON反序列化）
        /// </summary>
        public AppInfo() { }

        /// <summary>
        /// 克隆对象
        /// </summary>
        public AppInfo Clone()
        {
            return new AppInfo
            {
                Name = this.Name,
                FullPath = this.FullPath,
                FileSize = this.FileSize,
                LastModified = this.LastModified,
                UseCount = this.UseCount,
                Icon = this.Icon
            };
        }

        /// <summary>
        /// 重写Equals方法
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (obj is not AppInfo other)
                return false;
            return FullPath.Equals(other.FullPath, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 重写GetHashCode方法
        /// </summary>
        public override int GetHashCode()
        {
            return FullPath.GetHashCode(StringComparison.OrdinalIgnoreCase);
        }
    }
}
