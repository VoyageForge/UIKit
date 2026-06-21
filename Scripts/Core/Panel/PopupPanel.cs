using Cysharp.Threading.Tasks;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// 弹窗/提示面板抽象基类。不进 ViewStack，不参与导航，无 Pause/Resume。
    /// 通过 PopupManager 以叠加方式显示。支持多个同类型弹窗同时存在。
    /// 提供 ShowSelf / HideSelf / CloseSelf 便捷方法。
    /// </summary>
    public abstract class PopupPanel : BasePanel
    {
        /// <summary>显示自身（fire-and-forget）。通过 PopupManager 叠加显示。</summary>
        public void ShowSelf() => ShowSelfAsync().Forget();

        /// <summary>异步显示自身。通过 PopupManager.ShowAsync 叠加到弹窗 Canvas。</summary>
        public async UniTask ShowSelfAsync()
        {
            await UIManager.Popup.ShowAsync(this);
        }

        /// <summary>关闭销毁自身（fire-and-forget）。</summary>
        public void CloseSelf() => CloseSelfAsync().Forget();

        /// <summary>异步关闭销毁自身。</summary>
        public async UniTask CloseSelfAsync()
        {
            await UIManager.Popup.CloseAsync(this);
        }

        /// <summary>隐藏自身回池（fire-and-forget）。</summary>
        public void HideSelf() => HideSelfAsync().Forget();

        /// <summary>异步隐藏自身回池。</summary>
        public async UniTask HideSelfAsync()
        {
            await UIManager.Popup.HideAsync(this);
        }
    }
}
