using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VoyageForge.UIKit.Runtime;

namespace VoyageForge.UIKit.Samples
{
    /// <summary>
    /// 示例 Toast 弹窗 — 继承 PopupPanel，不进栈，仅浮动显示。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [PanelPath("UI/Samples/Resources/ToastPopup")]
    public class ToastPopup : PopupPanel
    {
        [SerializeField] private Text _messageText;

        private CancellationTokenSource _cancellationTokenSource;

        protected override UniTask OnCreate()
        {
            if (_messageText != null)
                _messageText.text = "操作成功！";
            return UniTask.CompletedTask;
        }

        protected override UniTask OnShow()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            AutoHide(_cancellationTokenSource.Token).Forget();
            return base.OnShow();
        }

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