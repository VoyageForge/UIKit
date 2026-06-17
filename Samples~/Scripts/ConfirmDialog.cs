using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VoyageForge.UIKit.Runtime;

namespace VoyageForge.UIKit.Samples
{
    [RequireComponent(typeof(CanvasGroup))]
    [PanelPath("UI/Samples/Resources/ConfirmDialog")]
    public class ConfirmDialog : FullPanel
    {
        private Button _confirmButton;
        private Button _cancelButton;

        protected override UniTask OnCreate()
        {
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
            await UIManager.Instance.HideAsync();

            var popup = await UIManager.Popup.GetPopupAsync<ToastPopup>();
            if (popup != null)
                await popup.ShowSelfAsync();
        }

        private void OnCancel()
        {
            Debug.Log("[ConfirmDialog] 取消");
            HideSelf();
        }

        protected override UniTask OnShow()
        {
            return base.OnShow();
        }

        protected override UniTask OnHide()
        {
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