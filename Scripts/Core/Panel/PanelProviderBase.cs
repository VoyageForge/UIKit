using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// Provider 基类 — 缓存/PanelPath/Import/Export 统一处理。
    /// 子类只需实现 Instantiate(string path)，返回实例化后的 BasePanel。
    /// </summary>
    public abstract class PanelProviderBase : IPanelProvider
    {
        private readonly Dictionary<Type, BasePanel> _cache = new();
        public IReadOnlyDictionary<Type, BasePanel> Cache => _cache;

        public T Load<T>() where T : BasePanel
        {
            var type = typeof(T);

            if (_cache.TryGetValue(type, out var panel))
            {
                _cache.Remove(type);
                return panel as T;
            }

            var path = PanelPathCache.GetPath<T>();
            var instance = Instantiate(path);
            if (instance == null) return null;

            instance.gameObject.SetActive(false);
            return instance as T;
        }

        /// <summary> 子类实现：根据路径创建实例。 </summary>
        protected abstract BasePanel Instantiate(string path);

        public void Release(BasePanel panel)
        {
            if (panel == null) return;
            _cache[panel.GetType()] = panel;
        }

        public void Register(BasePanel panel)
        {
            if (panel == null) return;
            _cache[panel.GetType()] = panel;
        }

        public bool TryGet(Type type, out BasePanel panel) => _cache.TryGetValue(type, out panel);
        public void Remove(Type type) => _cache.Remove(type);
        public void Clear() => _cache.Clear();
    }
}
