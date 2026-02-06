using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using QuickLaunchTool.Models;

namespace QuickLaunchTool.Services
{
    /// <summary>
    /// 文件扫描进度事件参数
    /// </summary>
    public class ScanProgressEventArgs : EventArgs
    {
        public string CurrentPath { get; set; } = string.Empty;
        public int TotalFound { get; set; }
        public int Percentage { get; set; }
    }

    /// <summary>
    /// 文件扫描服务
    /// </summary>
    public class FileScanner
    {
        // 系统文件夹，不进行扫描
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

        // 最大递归深度限制
        private const int MaxRecursionDepth = 10;

        /// <summary>
        /// 扫描进度事件
        /// </summary>
        public event EventHandler<ScanProgressEventArgs>? ScanProgress;

        /// <summary>
        /// 扫描单个文件夹
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
        /// 扫描多个文件夹
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

                    // 检查是否是直接的exe文件路径
                    if (File.Exists(path) && path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        // 直接添加exe文件
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
                    // 检查是否是文件夾
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
                    System.Diagnostics.Debug.WriteLine($"扫描路径异常 {path}: {ex.Message}");
                }
            }

            // 去重：保留第一个具有相同FullPath的项
            var uniqueApps = result
                .GroupBy(a => a.FullPath, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            return uniqueApps;
        }

        /// <summary>
        /// 递归扫描文件夹
        /// </summary>
        private async Task ScanFolderRecursiveAsync(string path, List<AppInfo> result, bool includeSubfolders,
            HashSet<string> visitedPaths, int depth)
        {
            // 限制递归深度
            if (depth > MaxRecursionDepth)
            {
                System.Diagnostics.Debug.WriteLine($"达到最大递归深度限制: {path}");
                return;
            }

            try
            {
                var fullPath = Path.GetFullPath(path);

                // 检查是否已访问过此路径（防止循环）
                if (visitedPaths.Contains(fullPath))
                {
                    System.Diagnostics.Debug.WriteLine($"跳过已访问的路径: {fullPath}");
                    return;
                }

                // 标记为已访问
                visitedPaths.Add(fullPath);

                var directory = new DirectoryInfo(fullPath);

                // 获取当前目录的exe文件
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
                        // 无权限访问该文件夹的子目录
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
                            // 无权限访问该文件夹，跳过
                        }
                        catch (StackOverflowException)
                        {
                            // 防止栈溢出继续传播
                            System.Diagnostics.Debug.WriteLine($"栈溢出捕获: {subDir.FullName}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"递归扫描异常: {subDir.FullName}, {ex.Message}");
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // 权限被拒绝
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"文件夹扫描异常: {path}, {ex.Message}");
            }
        }

        /// <summary>
        /// 检查是否为系统文件夹
        /// </summary>
        private static bool IsSystemFolder(string folderName)
        {
            return SystemFolders.Contains(folderName);
        }

        /// <summary>
        /// 检查是否为系统文件
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
        /// 触发扫描进度事件
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
