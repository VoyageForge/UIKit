using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VoyageForge.UIKit.Runtime;

namespace VoyageForge.UIKit.Samples
{
    /// <summary>
    /// 示例确认对话框（FullPanel）。演示 FullPanel + PopupPanel 混合使用：
    /// 确认 → PopAsync 返回上一级 → 弹出 ToastPopup 弹窗提示。
    /// 支持 Y/N 键盘快捷键。
    /// </summary>
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

            Debug.Log("[ConfirmDialog] Dialog opened");

            return UniTask.CompletedTask;
        }

        /// <summary>确认按钮：退出当前面板（PopAsync），然后弹出一个 Toast 弹窗。</summary>
        private async void OnConfirm()
        {
            Debug.Log("[ConfirmDialog] Confirmed");
            await UIManager.Panel.PopAsync();

            // 弹出 Toast 弹窗（PopupPanel，不进栈）
            var popup = await UIManager.Popup.GetPopup<ToastPopup>();
            if (popup != null)
                await popup.ShowSelfAsync();
        }

        /// <summary>取消按钮：直接隐藏回池。</summary>
        private void OnCancel()
        {
            Debug.Log("[ConfirmDialog] Cancelled");
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

        /// <summary>Y 键确认，N 键取消，Escape 回退（走基类默认逻辑）。</summary>
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
