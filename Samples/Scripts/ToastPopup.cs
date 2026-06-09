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
    [PanelPath("ToastPopup")]
    public class ToastPopup : PopupPanel
    {
        [SerializeField] private Text _messageText;


        private CancellationTokenSource _cancellationTokenSource; // 任务的生命周期管理者
        private CanvasGroup _canvasGroup;

        protected override async UniTask OnCreate()
        {
            _canvasGroup = GetComponent<CanvasGroup>();

            if (_messageText != null)
                _messageText.text = "操作成功！";

            await base.OnCreate();
        }

        protected override UniTask OnShow()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            AutoHide(_cancellationTokenSource.Token).Forget();

            _canvasGroup.alpha = 1;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
            return base.OnShow();
        }

        private async UniTask AutoHide(CancellationToken token)
        {
            await UniTask.Delay(2000, cancellationToken: token);
            await HideSelfAsync();
        }


        protected override UniTask OnHide()
        {
            _canvasGroup.alpha = 0;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            return base.OnHide();
        }
    }
}