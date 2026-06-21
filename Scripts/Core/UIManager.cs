using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VoyageForge.Depot.Runtime.Utilities;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// UIKit 总入口 — MonoSingleton 服务容器。
    /// 持有 FullPanelManager（导航栈）和 PopupManager（弹窗管理）两个内置管理器。
    /// 支持通过 Get&lt;T&gt;() 注册用户自定义管理器扩展。
    /// </summary>
    public class UIManager : MonoSingleton<UIManager>
    {
        /// <summary>用户自定义管理器字典，按类型存储。</summary>
        private readonly Dictionary<Type, object> _managers = new();

        private static FullPanelManager _panel;
        private static PopupManager _popup;

        /// <summary>FullPanel 导航栈管理器。首次访问时自动创建 UIManager 实例并初始化。</summary>
        public static FullPanelManager Panel
        {
            get
            {
                if (_panel == null) { _ = Instance; _panel ??= new FullPanelManager(); }
                return _panel;
            }
            private set => _panel = value;
        }

        /// <summary>Popup 弹窗管理器。首次访问时自动创建 UIManager 实例并初始化。</summary>
        public static PopupManager Popup
        {
            get
            {
                if (_popup == null) { _ = Instance; _popup ??= new PopupManager(); }
                return _popup;
            }
            private set => _popup = value;
        }

        /// <summary>
        /// 获取或注册自定义管理器。首次访问时创建实例并缓存。
        /// 要求 T 具有无参构造函数。
        /// </summary>
        public static T Get<T>() where T : class, new()
        {
            var type = typeof(T);
            if (!Instance._managers.TryGetValue(type, out var mgr))
            {
                mgr = new T();
                Instance._managers[type] = mgr;
            }
            return mgr as T;
        }

       

        private void OnDestroy()
        {
            _panel?.Dispose();
            _popup?.Dispose();
            _panel = null;
            _popup = null;
        }

        /// <summary>输入路由。将按键事件转发给 FullPanelManager，由栈顶活跃面板处理。</summary>
        public bool OnInput(KeyCode key, bool down) => Panel.OnInput(key, down);
    }
}
