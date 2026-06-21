using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VoyageForge.UIKit.Runtime;

namespace VoyageForge.UIKit.Samples
{
    /// <summary>
    /// 示例 Toast 弹窗（PopupPanel）。不进栈，叠加显示，2 秒后自动隐藏。
    /// 演示 PopupPanel 的 ShowSelf + AutoHide 模式。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [PanelPath("UI/Samples/Resources/ToastPopup")]
    public class ToastPopup : PopupPanel
    {
        [SerializeField] private Text _messageText;

        /// <summary>自动隐藏的取消令牌。</summary>
        private CancellationTokenSource _cancellationTokenSource;

        protected override UniTask OnCreate()
        {
            if (_messageText != null)
                _messageText.text = "Operation success!";
            return UniTask.CompletedTask;
        }

        protected override UniTask OnShow()
        {
            // 取消上一次的自动隐藏计时
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            // 启动 2 秒后自动隐藏
            AutoHide(_cancellationTokenSource.Token).Forget();
            return base.OnShow();
        }

        /// <summary>延迟 2 秒后自动调用 HideSelfAsync 隐藏回池。</summary>
        private async UniTask AutoHide(CancellationToken token)
        {
            await UniTask.Delay(2000, cancellationToken: token);
            await HideSelfAsync();
        }

        protected override UniTask OnHide()
        {
            return base.OnHide();
        }
    }
}
