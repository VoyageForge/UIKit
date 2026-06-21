using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// Popup 弹窗管理器。继承 PanelManagerBase，负责 PopupPanel 的叠加显示管理。
    /// 与 FullPanelManager 不同：弹窗不进导航栈，支持多个同类型弹窗同时显示。
    /// 默认使用 PopupResourcesProvider 作为加载代理（自动创建 DontDestroyOnLoad Canvas）。
    /// </summary>
    public class PopupManager : PanelManagerBase<PopupPanel>
    {
        /// <summary>活跃弹窗字典。按类型分组，每组是一个 List，支持同类型多实例。</summary>
        private readonly Dictionary<Type, List<PopupPanel>> _active = new();

        /// <summary>构造函数，默认使用 PopupResourcesProvider 加载弹窗预制体。</summary>
        public PopupManager() : base(new PopupResourcesProvider()) { }

        /// <summary>
        /// 异步加载指定类型的 PopupPanel（不自动显示）。
        /// 语义别名，等同于 GetAsync&lt;T&gt;()。
        /// </summary>
        public async UniTask<T> GetPopup<T>() where T : PopupPanel => await GetAsync<T>();

        /// <summary>弹窗挂载的 Canvas 根节点（来自 PopupProviderBase.Root）。</summary>
        private Transform Root => (Provider as PopupProviderBase)?.Root;

        /// <summary>Provider 切换后，将所有活跃弹窗 Reparent 到新 Provider 的 Root Canvas 下。</summary>
        protected override void OnProviderChanged() => ReparentAll();

        private void ReparentAll()
        {
            var root = Root;
            if (root == null) return;
            foreach (var panels in _active.Values)
                if (panels != null)
                    foreach (var popup in panels)
                        popup.transform.SetParent(root, false);
        }

        /// <summary>
        /// 显示弹窗。将面板 SetParent 到弹窗专用 Canvas Root，添加到活跃列表并调用 Show。
        /// 支持同类型多实例：每次 Show 都会新增到对应列表。
        /// </summary>
        public override async UniTask ShowAsync(PopupPanel panel)
        {
            if (panel == null) return;
            Provider.Remove(panel);
            var type = panel.GetType();

            if (!_active.TryGetValue(type, out var popups) || popups == null)
                _active[type] = popups = new List<PopupPanel>();

            if (!popups.Contains(panel))
                popups.Add(panel);

            panel.transform.SetParent(Root, false);
            await panel.Show();
        }

        /// <summary>
        /// 隐藏弹窗。从活跃列表移除 → 调用 Hide → Release 回 Provider 缓存。
        /// </summary>
        public override async UniTask HideAsync(PopupPanel panel)
        {
            if (panel == null) return;
            var type = panel.GetType();

            if (!_active.TryGetValue(type, out var popups) || popups == null || !popups.Contains(panel))
                return;

            popups.Remove(panel);
            if (popups.Count == 0)
                _active.Remove(type);

            await panel.Hide();
            Provider.Release(panel);
        }

        /// <summary>
        /// 关闭弹窗。确保缓存清理，从活跃列表移除 → 调用 Close 销毁 GameObject。
        /// </summary>
        public override async UniTask CloseAsync(PopupPanel panel)
        {
            if (panel == null) return;
            Provider.Remove(panel);
            var type = panel.GetType();

            if (_active.TryGetValue(type, out var popups) && popups != null)
            {
                popups.Remove(panel);
                if (popups.Count == 0)
                    _active.Remove(type);
            }

            await panel.Close();
        }

        /// <summary>清空所有活跃弹窗记录（不销毁面板实例）。</summary>
        public override void Dispose() => _active.Clear();
    }
}
