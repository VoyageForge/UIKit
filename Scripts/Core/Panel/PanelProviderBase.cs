using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// FullPanel Provider 抽象基类。实现 IPanelProvider 接口的核心逻辑：
    /// 字典缓存（每个类型最多一个实例）、飞行中请求去重、PanelPath 路径解析。
    /// 子类只需实现 InstantiateAsync(string path) 即可接入任意资源系统。
    /// </summary>
    public abstract class PanelProviderBase : IPanelProvider
    {
        /// <summary>面板缓存字典。Type → 缓存的 panel 实例。</summary>
        private readonly Dictionary<Type, BasePanel> _cache = new();

        /// <summary>飞行中加载请求字典。用于同类型并发加载去重。</summary>
        private readonly Dictionary<Type, UniTaskCompletionSource<BasePanel>> _pendingTcs = new();

        /// <summary>
        /// 异步加载指定类型的面板。缓存命中直接返回，缓存未命中走 InstantiateAsync 创建。
        /// 同类型并发请求自动去重（共用同一个飞行中任务）。
        /// </summary>
        public virtual async UniTask<T> LoadAsync<T>() where T : BasePanel
        {
            var type = typeof(T);

            // 1. 缓存命中（只看不取，由 Show 时再移除）
            if (_cache.TryGetValue(type, out var panel))
                return panel as T;

            // 2. 飞行中请求去重：同类型并发加载时复用同一个任务
            if (_pendingTcs.TryGetValue(type, out var existingTcs))
            {
                var waited = await existingTcs.Task;
                return waited as T;
            }

            var tcs = new UniTaskCompletionSource<BasePanel>();
            _pendingTcs[type] = tcs;

            try
            {
                var path = PanelPathCache.GetPath<T>();
                var loaded = await InstantiateAsync<T>(path);

                if (loaded == null)
                {
                    tcs.TrySetResult(null);
                    return null;
                }

                loaded.gameObject.SetActive(false);
                tcs.TrySetResult(loaded);
                return (T)(BasePanel)loaded;
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

        /// <summary>子类实现：根据 Resources 路径异步创建面板实例。</summary>
        protected abstract UniTask<T> InstantiateAsync<T>(string path) where T : BasePanel;

        /// <summary>回收面板到缓存（Hide 后调用）。</summary>
        public void Release<T>(T panel) where T : BasePanel
        {
            if (panel == null) return;
            _cache[panel.GetType()] = panel;
        }

        /// <summary>注册已有面板到缓存（场景预置面板用）。</summary>
        public void Register<T>(T panel) where T : BasePanel
        {
            if (panel == null) return;
            _cache[panel.GetType()] = panel;
        }

        /// <summary>尝试从缓存获取指定类型面板。</summary>
        public bool TryGet(Type type, out BasePanel panel) => _cache.TryGetValue(type, out panel);

        /// <summary>从缓存移除指定类型（不销毁面板）。</summary>
        public void Remove(Type type) => _cache.Remove(type);

        /// <summary>从缓存移除指定面板实例（Show 时调用，不销毁面板）。</summary>
        public void Remove(BasePanel panel)
        {
            if (panel == null) return;
            _cache.Remove(panel.GetType());
        }

        /// <summary>清空全部缓存（不销毁面板）。</summary>
        public void Clear() => _cache.Clear();

        /// <summary>导出全部缓存数据。Provider 热切换时调用。</summary>
        public Dictionary<Type, object> Export()
        {
            var result = new Dictionary<Type, object>();
            foreach (var kv in _cache) result[kv.Key] = kv.Value;
            Clear();
            return result;
        }

        /// <summary>导入缓存数据。Provider 热切换时接收迁移。</summary>
        public void Import(Dictionary<Type, object> data)
        {
            if (data == null) return;
            foreach (var kv in data)
                if (kv.Value is BasePanel panel)
                    _cache[kv.Key] = panel;
        }
    }
}
