using System;
using System.Collections.Generic;
using System.Reflection;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// 缓存 Panel → Resources 路径映射，反射仅一次。
    /// </summary>
    internal static class PanelPathCache
    {
        private static readonly Dictionary<Type, string> _cache = new();

        public static string GetPath<T>() where T : BasePanel
        {
            var type = typeof(T);
            if (_cache.TryGetValue(type, out var path))
                return path;

            var attr = type.GetCustomAttribute<PanelPathAttribute>();
            path = attr?.Path ?? type.Name;
            _cache[type] = path;
            return path;
        }
    }
}
