using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// 全屏/场景面板抽象基类。可压入 ViewStack 参与导航，支持 Pause/Resume。
    /// 被新面板覆盖时触发 Pause，回到栈顶时触发 Resume。
    /// 提供 ShowSelf / HideSelf / CloseSelf 便捷方法，面板内部即可管理自身。
    /// </summary>
    public abstract class FullPanel : BasePanel
    {
        /// <summary>被暂停时触发（被新面板覆盖）。</summary>
        public event System.Action OnPaused;

        /// <summary>从暂停恢复时触发（重新成为栈顶）。</summary>
        public event System.Action OnResumed;

        // ---- Internal API（由 ViewStack 调用） ----

        /// <summary>暂停面板。Active → Paused → OnPause → 事件。</summary>
        internal async UniTask Pause()
        {
            if (State != PanelState.Active) return;
            _state = PanelState.Paused;
            await OnPause();
            OnPaused?.Invoke();
        }

        /// <summary>恢复面板。Paused → Active（重新激活 GameObject）→ OnResume → 事件。</summary>
        internal async UniTask Resume()
        {
            if (State != PanelState.Paused) return;
            gameObject.SetActive(true);
            _state = PanelState.Active;
            await OnResume();
            OnResumed?.Invoke();
        }

        /// <summary>显示自身（fire-and-forget）。压入 FullPanelManager 导航栈。</summary>
        public void ShowSelf() => ShowSelfAsync().Forget();

        /// <summary>异步显示自身。压入 FullPanelManager 导航栈并等待完成。</summary>
        public async UniTask ShowSelfAsync()
        {
            await UIManager.Panel.PushAsync(this);
        }

        /// <summary>关闭销毁自身（fire-and-forget）。</summary>
        public void CloseSelf() => CloseSelfAsync().Forget();

        /// <summary>异步关闭销毁自身。</summary>
        public async UniTask CloseSelfAsync()
        {
            await UIManager.Panel.CloseAsync(this);
        }

        /// <summary>隐藏自身回池（fire-and-forget）。仅在 Active 状态下有效。</summary>
        public void HideSelf() => HideSelfAsync().Forget();

        /// <summary>异步隐藏自身回池。仅在 Active 状态下执行。</summary>
        public async UniTask HideSelfAsync()
        {
            if (State == PanelState.Active)
                await UIManager.Panel.HideAsync(this);
        }

        // ---- 可覆写 ----

        /// <summary>被遮挡暂停时调用。</summary>
        protected virtual UniTask OnPause() => UniTask.CompletedTask;

        /// <summary>恢复显示时调用。</summary>
        protected virtual UniTask OnResume() => UniTask.CompletedTask;
    }
}
