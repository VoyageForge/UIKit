using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// Panel 提供器 — 以 Type 为 key 管理面板实例。
    /// </summary>
    public interface IPanelProvider
    {
        IReadOnlyDictionary<Type, BasePanel> Cache { get; }

        /// <summary> 加载 Panel（缓存命中直接返回，否则创建）。 </summary>
        T Load<T>() where T : BasePanel;

        /// <summary> 回收 Panel。 </summary>
        void Release(BasePanel panel);

        /// <summary> 注册已有 Panel 实例（key = panel.GetType()）。 </summary>
        void Register(BasePanel panel);

        /// <summary> 尝试获取缓存。 </summary>
        bool TryGet(Type type, out BasePanel panel);

        /// <summary> 从缓存移除（不 Destroy）。 </summary>
        void Remove(Type type);

        /// <summary> 清空缓存（不 Destroy）。 </summary>
        void Clear();

        /// <summary> 导入缓存（Provider 热切换用）。 </summary>
        void Import(Dictionary<Type, BasePanel> panels)
        {
            foreach (var kv in panels) Register(kv.Value);
        }

        /// <summary> 导出并清空缓存（Provider 热切换用）。 </summary>
        Dictionary<Type, BasePanel> Export()
        {
            var result = new Dictionary<Type, BasePanel>();
            foreach (var kv in Cache) result[kv.Key] = kv.Value;
            Clear();
            return result;
        }
    }
}
