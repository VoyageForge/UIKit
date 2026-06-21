using System;
using System.Collections.Generic;
using System.Reflection;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// 缓存 Panel Type → Resources 路径的映射。反射仅执行一次，后续命中直接走字典。
    /// </summary>
    internal static class PanelPathCache
    {
        private static readonly Dictionary<Type, string> _cache = new();

        /// <summary>获取指定 Panel 类型的 Resources 加载路径。</summary>
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
