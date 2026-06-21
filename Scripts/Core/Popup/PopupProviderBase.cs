using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// Popup Provider 抽象基类。实现 IPanelProvider 接口，额外管理弹窗专用 Canvas Root。
    /// 与 PanelProviderBase 不同，缓存使用 List&lt;PopupPanel&gt; 支持同类型多实例。
    /// 子类只需实现 InstantiateAsync(string path) 即可接入任意资源系统。
    /// </summary>
    public abstract class PopupProviderBase : IPanelProvider
    {
        /// <summary>弹窗挂载的 Canvas 根节点。首次访问时由子类懒创建。</summary>
        protected Transform _root;

        /// <summary>弹窗挂载的 Canvas 根节点。子类可覆写以自定义 Canvas 创建逻辑。</summary>
        public virtual Transform Root => _root;

        /// <summary>弹窗缓存字典。Type → List&lt;PopupPanel&gt;，支持每个类型缓存多个实例。</summary>
        private Dictionary<Type, List<PopupPanel>> _cache = new();

        /// <summary>飞行中加载请求字典。同类型并发加载去重。</summary>
        private readonly Dictionary<Type, UniTaskCompletionSource<PopupPanel>> _pendingTcs = new();

        /// <summary>子类实现：根据 Resources 路径异步创建弹窗实例。</summary>
        protected abstract UniTask<T> InstantiateAsync<T>(string path) where T : BasePanel;

        /// <summary>
        /// 异步加载指定类型的弹窗。按类型从 List 缓存取最后一个（LIFO）。
        /// 同类型并发请求自动去重。
        /// </summary>
        public async UniTask<T> LoadAsync<T>() where T : BasePanel
        {
            var type = typeof(T);

            // 1. 缓存命中（只看不取，由 Show 时再移除）
            if (_cache.TryGetValue(type, out var panel) && panel.Count != 0)
                return panel[^1] as T;

            // 2. 飞行中请求去重
            if (_pendingTcs.TryGetValue(type, out var existingTcs))
            {
                var waited = await existingTcs.Task;
                return waited as T;
            }

            var tcs = new UniTaskCompletionSource<PopupPanel>();
            _pendingTcs[type] = tcs;

            try
            {
                var path = PanelPathCache.GetPath<T>();
                var popup = await InstantiateAsync<T>(path);

                if (popup == null)
                {
                    tcs.TrySetResult(null);
                    return null;
                }

                tcs.TrySetResult(popup as PopupPanel);
                return popup as T;
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
                throw;
            }
            finally
            {
                _pendingTcs.Remove(type);
            }
        }

        /// <summary>回收弹窗到缓存（Hide 后调用）。按类型追加到对应 List。</summary>
        public void Release<T>(T panel) where T : BasePanel
        {
            if (panel is not PopupPanel popup) return;
            if (_cache.TryGetValue(popup.GetType(), out var panels))
                panels.Add(popup);
            else
                _cache[popup.GetType()] = new List<PopupPanel> { popup };
        }

        /// <summary>注册已有弹窗到缓存（预置弹窗用）。</summary>
        public void Register<T>(T panel) where T : BasePanel
        {
            if (panel is not PopupPanel popup) return;
            if (_cache.TryGetValue(popup.GetType(), out var panels))
                panels.Add(popup);
            else
                _cache[popup.GetType()] = new List<PopupPanel> { popup };
        }

        /// <summary>尝试从缓存获取指定类型弹窗（从 List 尾部取）。</summary>
        public bool TryGet(Type type, out BasePanel panel)
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

        /// <summary>从缓存移除指定类型的所有弹窗。</summary>
        public void Remove(Type type) => _cache.Remove(type);

        /// <summary>从缓存移除指定面板实例（Show 时调用，不销毁面板）。</summary>
        public void Remove(BasePanel panel)
        {
            if (panel is not PopupPanel popup) return;
            if (_cache.TryGetValue(popup.GetType(), out var panels) && panels != null)
            {
                panels.Remove(popup);
                if (panels.Count == 0) _cache.Remove(popup.GetType());
            }
        }

        /// <summary>清空全部缓存。</summary>
        public void Clear() => _cache.Clear();

        /// <summary>导出全部缓存数据。Provider 热切换时调用。</summary>
        public Dictionary<Type, object> Export()
        {
            var result = new Dictionary<Type, object>();
            foreach (var kv in _cache)
                result[kv.Key] = kv.Value;
            Clear();
            return result;
        }

        /// <summary>导入缓存数据。Provider 热切换时接收迁移。</summary>
        public void Import(Dictionary<Type, object> data)
        {
            if (data == null) return;
            foreach (var kv in data)
                if (kv.Value is List<PopupPanel> panels)
                    _cache[kv.Key] = panels;
        }
    }
}
