using System;
using System.Collections.Generic;

namespace QuickLaunchTool.Utils
{
    /// <summary>
    /// 缓存管理器
    /// </summary>
    public sealed class CacheManager
    {
        private static CacheManager? _instance;
        private static readonly object _lock = new object();

        private readonly Dictionary<string, object> _cache = new();

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static CacheManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new CacheManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private CacheManager() { }

        /// <summary>
        /// 设置缓存值
        /// </summary>
        public void Set<T>(string key, T value)
        {
            lock (_lock)
            {
                _cache[key] = value!;
            }
        }

        /// <summary>
        /// 获取缓存值
        /// </summary>
        public bool TryGet<T>(string key, out T? value)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var obj) && obj is T typedObj)
                {
                    value = typedObj;
                    return true;
                }
                value = default;
                return false;
            }
        }

        /// <summary>
        /// 移除缓存
        /// </summary>
        public bool Remove(string key)
        {
            lock (_lock)
            {
                return _cache.Remove(key);
            }
        }

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _cache.Clear();
            }
        }

        /// <summary>
        /// 检查缓存是否存在
        /// </summary>
        public bool ContainsKey(string key)
        {
            lock (_lock)
            {
                return _cache.ContainsKey(key);
            }
        }
    }
}
