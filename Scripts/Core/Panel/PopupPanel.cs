using Cysharp.Threading.Tasks;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// 弹窗/提示面板基类 — 不进 ViewStack，不参与导航，无 Pause/Resume。
    /// 通过 UIManager.ShowOverlay 显示。
    /// </summary>
    public abstract class PopupPanel : BasePanel
    {
        /// <summary>
        /// 展示自身
        /// </summary>
        public void ShowSelf() => ShowSelfAsync().Forget();

        /// <summary>
        /// 展示自身
        /// </summary>
        public async UniTask ShowSelfAsync()
        {
            await UIManager.Popup.ShowPopupAsync(this);
        }

        /// <summary>
        /// 关闭自身
        /// </summary>
        public void CloseSelf() => CloseSelfAsync().Forget();

        /// <summary>
        /// 关闭自身
        /// </summary>
        public async UniTask CloseSelfAsync()
        {
            await UIManager.Popup.CloseAsync(this);
        }

        /// <summary>
        /// 隐藏自身
        /// </summary>
        public void HideSelf() => HideSelfAsync().Forget();

        /// <summary>
        /// 隐藏自身
        /// </summary>
        public async UniTask HideSelfAsync()
        {
            await UIManager.Popup.HideAsync(this);
        }
    }
}