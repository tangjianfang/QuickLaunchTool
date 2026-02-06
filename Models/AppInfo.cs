using System;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;

namespace QuickLaunchTool.Models
{
    /// <summary>
    /// Application information model
    /// </summary>
    public class AppInfo
    {
        /// <summary>
        /// Display name of the application
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Full file path
        /// </summary>
        [JsonProperty("fullPath")]
        public string FullPath { get; set; } = string.Empty;

        /// <summary>
        /// File size in bytes
        /// </summary>
        [JsonProperty("fileSize")]
        public long FileSize { get; set; }

        /// <summary>
        /// Last modification timestamp
        /// </summary>
        [JsonProperty("lastModified")]
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Usage count
        /// </summary>
        [JsonProperty("useCount")]
        public int UseCount { get; set; }

        /// <summary>
        /// Icon resource (not serialized)
        /// </summary>
        [JsonIgnore]
        public Icon? Icon { get; set; }

        /// <summary>
        /// Construct AppInfo from a file path
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
        /// Parameterless constructor used for JSON deserialization
        /// </summary>
        public AppInfo() { }

        /// <summary>
        /// Clone the AppInfo instance
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
        /// Compare AppInfo instances by path
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (obj is not AppInfo other)
                return false;
            return FullPath.Equals(other.FullPath, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Generate hash code based on the path
        /// </summary>
        public override int GetHashCode()
        {
            return FullPath.GetHashCode(StringComparison.OrdinalIgnoreCase);
        }
    }
}
