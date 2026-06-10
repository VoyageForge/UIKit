using Cysharp.Threading.Tasks;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// 全屏/场景面板基类 — 可压入 ViewStack，参与导航（Pause/Resume）。
    /// </summary>
    public abstract class FullPanel : BasePanel
    {
        // ---- 事件 ----

        /// <summary> 被暂停时触发。 </summary>
        public event System.Action OnPaused;

        /// <summary> 从暂停恢复时触发。 </summary>
        public event System.Action OnResumed;

        // ---- Internal API（由 ViewStack 调用） ----

        /// <summary> 暂停 — Active → Paused → OnPause → 事件。 </summary>
        internal async UniTask Pause()
        {
            if (State != PanelState.Active) return;
            _state = PanelState.Paused;
            await OnPause();
            OnPaused?.Invoke();
        }

        /// <summary> 恢复 — Paused → Active → OnResume → 事件。 </summary>
        internal async UniTask Resume()
        {
            if (State != PanelState.Paused) return;
            gameObject.SetActive(true);
            _state = PanelState.Active;
            await OnResume();
            OnResumed?.Invoke();
        }

        public void ShowSelf() => ShowSelfAsync().Forget();

        public async UniTask ShowSelfAsync()
        {
           await UIManager.Instance.ShowAsync(this);
        }
        
        public void CloseSelf() => CloseSelfAsync().Forget();

        public async UniTask CloseSelfAsync()
        {
            await UIManager.Instance.CloseAsync(this);
        }
        
        /// <summary>
        /// 关闭自身
        /// </summary>
        public  void HideSelf() => HideSelfAsync().Forget();

        /// <summary>
        /// 关闭自身
        /// </summary>
        public async UniTask HideSelfAsync()
        {
            // 只有在 Active 状态下才关闭
            if (State == PanelState.Active)
            {
                await UIManager.Instance.HideAsync(this);
            }
            
        }

        // ---- 可覆写 ----

        /// <summary> 被遮挡暂停时调用。 </summary>
        protected virtual UniTask OnPause() => UniTask.CompletedTask;

        /// <summary> 恢复显示时调用。 </summary>
        protected virtual UniTask OnResume() => UniTask.CompletedTask;
    }
}