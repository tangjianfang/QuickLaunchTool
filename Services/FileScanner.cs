using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using QuickLaunchTool.Models;

namespace QuickLaunchTool.Services
{
    /// <summary>
    /// File scan progress event arguments
    /// </summary>
    public class ScanProgressEventArgs : EventArgs
    {
        public string CurrentPath { get; set; } = string.Empty;
        public int TotalFound { get; set; }
        public int Percentage { get; set; }
    }

    /// <summary>
    /// File scanning service
    /// </summary>
    public class FileScanner
    {
        // System folders that are not scanned
        private static readonly HashSet<string> SystemFolders = new(StringComparer.OrdinalIgnoreCase)
        {
            "Windows",
            "System32",
            "SysWOW64",
            "ProgramData",
            "$RECYCLE.BIN",
            ".git",
            "node_modules",
            "AppData",
            "ProgramData",
            "Recovery",
            "$Recycle.Bin",
            "System Volume Information"
        };

        // Maximum recursion depth limit
        private const int MaxRecursionDepth = 10;

        /// <summary>
        /// Scan progress event
        /// </summary>
        public event EventHandler<ScanProgressEventArgs>? ScanProgress;

        /// <summary>
        /// Scan a single folder
        /// </summary>
        public async Task<List<AppInfo>> ScanFolderAsync(string path, bool includeSubfolders = true)
        {
            var result = new List<AppInfo>();
            var visitedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                if (!Directory.Exists(path))
                    return result;

                var fullPath = Path.GetFullPath(path);
                await ScanFolderRecursiveAsync(fullPath, result, includeSubfolders, visitedPaths, 0);
            }
            catch (StackOverflowException ex)
            {
                System.Diagnostics.Debug.WriteLine($"栈溢出异常（可能是循环目录）: {ex.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"扫描出错: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Scan multiple folders
        /// </summary>
        public async Task<List<AppInfo>> ScanMultipleFoldersAsync(List<string> paths, bool includeSubfolders = true)
        {
            var result = new List<AppInfo>();
            var addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var path in paths)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(path))
                        continue;

                    // Check if it's a direct exe file path
                    if (File.Exists(path) && path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        // Directly add exe file
                        if (!addedPaths.Contains(path))
                        {
                            var fileInfo = new FileInfo(path);
                            result.Add(new AppInfo
                            {
                                Name = Path.GetFileNameWithoutExtension(path),
                                FullPath = path,
                                FileSize = fileInfo.Length,
                                LastModified = fileInfo.LastWriteTime,
                                UseCount = 0
                            });
                            addedPaths.Add(path);
                        }
                    }
                    // Check if it's a folder
                    else if (Directory.Exists(path))
                    {
                        var apps = await ScanFolderAsync(path, includeSubfolders);
                        foreach (var app in apps)
                        {
                            if (!addedPaths.Contains(app.FullPath))
                            {
                                result.Add(app);
                                addedPaths.Add(app.FullPath);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Scan path exception {path}: {ex.Message}");
                }
            }

            // Deduplication: keep the first item with the same FullPath
            var uniqueApps = result
                .GroupBy(a => a.FullPath, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            return uniqueApps;
        }

        /// <summary>
        /// Recursively scan folders
        /// </summary>
        private async Task ScanFolderRecursiveAsync(string path, List<AppInfo> result, bool includeSubfolders,
            HashSet<string> visitedPaths, int depth)
        {
            // Limit recursion depth
            if (depth > MaxRecursionDepth)
            {
                System.Diagnostics.Debug.WriteLine($"Reached maximum recursion depth limit: {path}");
                return;
            }

            try
            {
                var fullPath = Path.GetFullPath(path);

                // Check if this path has been visited (prevent loops)
                if (visitedPaths.Contains(fullPath))
                {
                    System.Diagnostics.Debug.WriteLine($"Skip visited path: {fullPath}");
                    return;
                }

                // Mark as visited
                visitedPaths.Add(fullPath);

                var directory = new DirectoryInfo(fullPath);

                // Get exe files in the current directory
                var exeFiles = directory.GetFiles("*.exe", SearchOption.TopDirectoryOnly)
                    .Where(f => !IsSystemFile(f.FullName))
                    .ToList();

                foreach (var file in exeFiles)
                {
                    try
                    {
                        var appInfo = new AppInfo(file.FullName);
                        if (!result.Any(a => a.FullPath == file.FullName))
                        {
                            result.Add(appInfo);
                            OnScanProgress(file.FullName, result.Count);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"处理文件异常: {file.FullName}, {ex.Message}");
                    }
                }

                // 递归扫描子目录
                if (includeSubfolders)
                {
                    DirectoryInfo[] subDirectories = Array.Empty<DirectoryInfo>();
                    try
                    {
                        subDirectories = directory.GetDirectories()
                            .Where(d => !IsSystemFolder(d.Name))
                            .ToArray();
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // No permission to access subdirectories of this folder
                        return;
                    }

                    foreach (var subDir in subDirectories)
                    {
                        try
                        {
                            await ScanFolderRecursiveAsync(subDir.FullName, result, true, visitedPaths, depth + 1);
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // No permission to access this folder, skip
                        }
                        catch (StackOverflowException)
                        {
                            // 防止栈溢出继续传播
                            System.Diagnostics.Debug.WriteLine($"栈溢出捕获: {subDir.FullName}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Recursive scan exception: {subDir.FullName}, {ex.Message}");
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Permission denied
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Folder scan exception: {path}, {ex.Message}");
            }
        }

        /// <summary>
        /// Check if it's a system folder
        /// </summary>
        private static bool IsSystemFolder(string folderName)
        {
            return SystemFolders.Contains(folderName);
        }

        /// <summary>
        /// Check if it's a system file
        /// </summary>
        private static bool IsSystemFile(string filePath)
        {
            try
            {
                var attr = File.GetAttributes(filePath);
                return (attr & FileAttributes.System) == FileAttributes.System;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Trigger scan progress event
        /// </summary>
        protected virtual void OnScanProgress(string currentPath, int totalFound)
        {
            ScanProgress?.Invoke(this, new ScanProgressEventArgs
            {
                CurrentPath = currentPath,
                TotalFound = totalFound,
                Percentage = 0
            });
        }
    }
}
