using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// Popup Provider。
    /// 
    /// 继承自 IPanelProvider。
    /// 
    /// 额外负责：
    /// - Popup Canvas Root
    /// </summary>
    public interface IPopupProvider 
    {
        /// <summary>
        /// Popup 挂载 Root。
        /// 
        /// 一般为：
        /// DontDestroyOnLoad Canvas Root。
        /// </summary>
        Transform Root { get; }
        
        IReadOnlyDictionary<Type, List<PopupPanel>> Cache { get; }

    

        /// <summary> 加载 Panel（异步）。默认委托到 Load。 </summary>
        UniTask<T> LoadAsync<T>() where T : PopupPanel;
       

        /// <summary> 回收 Panel。 </summary>
        void Release(PopupPanel panel);

        /// <summary> 注册已有 Panel 实例（key = panel.GetType()）。 </summary>
        void Register(PopupPanel panel);

        /// <summary> 尝试获取缓存。 </summary>
        bool TryGet(Type type, out PopupPanel panel);

        /// <summary> 从缓存移除（不 Destroy）。 </summary>
        void Remove(Type type);

        /// <summary> 清空缓存（不 Destroy）。 </summary>
        void Clear();

        /// <summary> 导入缓存（Provider 热切换用）。 </summary>
        void Import(Dictionary<Type, List<PopupPanel>> panels)
        {
            foreach (var kv in panels)
            {
                foreach (var panel in kv.Value)
                {
                    Register(panel);
                }
            }
        }

        /// <summary> 导出并清空缓存（Provider 热切换用）。 </summary>
        Dictionary<Type, List<PopupPanel>> Export()
        {
            var result = new Dictionary<Type, List<PopupPanel>>();
            foreach (var kv in Cache) 
                result[kv.Key] = kv.Value;
            Clear();
            return result;
        }
    }
}