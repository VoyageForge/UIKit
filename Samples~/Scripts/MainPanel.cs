using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VoyageForge.UIKit.Runtime;

namespace VoyageForge.UIKit.Samples
{
    /// <summary> 主菜单面板 — FullPanel，可压栈导航。 </summary>
    [PanelPath("UI/Samples/Resources/MainPanel")]
    public  class MainPanel : FullPanel
    {
        private Button _settingsButton;
        private Button _dialogButton;

        protected override UniTask OnCreate()
        {
            // 仅一次：获取组件引用
            var buttons = GetComponentsInChildren<Button>();
            _settingsButton = System.Array.Find(buttons, b => b.name == "SettingsButton");
            _dialogButton = System.Array.Find(buttons, b => b.name == "DialogButton");

            if (_settingsButton != null)
                _settingsButton.onClick.AddListener(async () =>
                {
                    var panel = await UIManager.Instance.GetPanelAsync<SettingsPanel>();
                    await panel.ShowSelfAsync();
                });

            if (_dialogButton != null)
                _dialogButton.onClick.AddListener(async () =>
                {
                    var panel = await UIManager.Instance.GetPanelAsync<ConfirmDialog>();
                    await panel.ShowSelfAsync();
                });

            
            return UniTask.CompletedTask;
        }

        protected override UniTask OnShow()
        {
            Debug.Log("[MainPanel] 显示主面板");
            return UniTask.CompletedTask;
        }

        public override bool OnInput(KeyCode key, bool down) => true;
    }
}
