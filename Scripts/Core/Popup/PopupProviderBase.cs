using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace VoyageForge.UIKit.Runtime
{
    public abstract class PopupProviderBase : IPopupProvider
    {
        protected Transform _root;

        public virtual Transform Root
        {
            get => _root;
        }

        private Dictionary<Type, List<PopupPanel>> _cache = new();

        public IReadOnlyDictionary<Type, List<PopupPanel>> Cache => _cache;

        /// <summary> 子类实现：根据路径异步创建实例。 </summary>
        protected abstract UniTask<T> InstantiateAsync<T>(string path) where T : PopupPanel;

        public async UniTask<T> LoadAsync<T>() where T : PopupPanel
        {
            var type = typeof(T);

            if (_cache.TryGetValue(type, out var panel) && panel.Count != 0)
            {
                var lastPanel = panel[^1];
                panel.RemoveAt(panel.Count - 1);

                return lastPanel as T;
            }

            var path = PanelPathCache.GetPath<T>();

            var popup = await InstantiateAsync<T>(path);

            if (popup == null)
                throw new InvalidOperationException(
                    $"[PopupProvider] 加载失败：类型 {typeof(T).Name}，路径 \"{path}\"。请检查预制体是否在 Resources 目录下，以及 PanelPath 特性是否正确。");

            return popup;
        }

        public void Release(PopupPanel popup)
        {
            if (popup == null) return;
            if (_cache.TryGetValue(popup.GetType(), out var panels))
            {
                panels.Add(popup);
            }
            else
            {
                _cache[popup.GetType()] = new List<PopupPanel>() { popup };
            }
        }

        public void Register(PopupPanel panel)
        {
            if (panel == null) return;
            if (_cache.TryGetValue(panel.GetType(), out var panels))
            {
                panels.Add(panel);
            }
            else
            {
                _cache[panel.GetType()] = new List<PopupPanel>() { panel };
            }
        }



        public bool TryGet(Type type, out PopupPanel panel)
        {
            if (_cache.TryGetValue(type, out var panels) && panels.Count != 0)
            {
                panel = panels[^1];
                panels.RemoveAt(panels.Count - 1);
                return true;
            }

            panel = null;
            return false;
        }

        public void Remove(Type type)
        {
            _cache.Remove(type);
        }

        public void Clear()
        {
            _cache.Clear();
        }
    }
}