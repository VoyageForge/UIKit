using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VoyageForge.UIKit.Runtime;

namespace VoyageForge.UIKit.Samples
{
    [RequireComponent(typeof(CanvasGroup))]
   [PanelPath("ConfirmDialog")]
    public class ConfirmDialog : FullPanel
    {
        private Button _confirmButton;

        private Button _cancelButton;

        private CanvasGroup _canvasGroup;

        protected override UniTask OnCreate()
        {
            _canvasGroup = GetComponent<CanvasGroup>();

            var buttons = GetComponentsInChildren<Button>();
            _confirmButton = System.Array.Find(buttons, b => b.name == "ConfirmButton");
            _cancelButton = System.Array.Find(buttons, b => b.name == "CancelButton");

            if (_confirmButton != null)
                _confirmButton.onClick.AddListener(OnConfirm);

            if (_cancelButton != null)
                _cancelButton.onClick.AddListener(OnCancel);

            Debug.Log("[ConfirmDialog] 弹窗打开");

            return UniTask.CompletedTask;
        }


        private async void OnConfirm()
        {
            Debug.Log("[ConfirmDialog] 确认");
            UIManager.Instance.Hide();

            var popup = await UIManager.Instance
                .PopupManager
                .ShowAsync<ToastPopup>();
        }

        private void OnCancel()
        {
            Debug.Log("[ConfirmDialog] 取消");
            UIManager.Instance.Hide();
        }

        protected override UniTask OnShow()
        {
            _canvasGroup.alpha = 1;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;

            return base.OnShow();
        }

        protected override UniTask OnHide()
        {
            _canvasGroup.alpha = 0;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            return base.OnHide();
        }

        public override bool OnInput(KeyCode key, bool down)
        {
            switch (key)
            {
                case KeyCode.Y:
                    if (down) OnConfirm();
                    return true;
                case KeyCode.N:
                    if (down) OnCancel();
                    return true;
                default:
                    return base.OnInput(key, down);
            }
        }
    }
}