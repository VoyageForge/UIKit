using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VoyageForge.UIKit.Runtime;

namespace VoyageForge.UIKit.Samples
{
    /// <summary>
    /// 示例主菜单面板（FullPanel）。演示 FullPanel 导航栈用法：
    /// 按钮打开 SettingsPanel 或 ConfirmDialog，均为 FullPanel 压栈导航。
    /// </summary>
    [PanelPath("UI/Samples/Resources/MainPanel")]
    public class MainPanel : FullPanel
    {
        private Button _settingsButton;
        private Button _dialogButton;

        protected override UniTask OnCreate()
        {
            var buttons = GetComponentsInChildren<Button>();
            _settingsButton = System.Array.Find(buttons, b => b.name == "SettingsButton");
            _dialogButton = System.Array.Find(buttons, b => b.name == "DialogButton");

            if (_settingsButton != null)
                _settingsButton.onClick.AddListener(async () =>
                {
                    // FullPanel 导航：GetAsync 加载 + ShowSelfAsync 压栈
                    var panel = await UIManager.Panel.GetPanel<SettingsPanel>();
                    await panel.ShowSelfAsync();
                });

            if (_dialogButton != null)
                _dialogButton.onClick.AddListener(async () =>
                {
                    var panel = await UIManager.Panel.GetPanel<ConfirmDialog>();
                    await panel.ShowSelfAsync();
                });

            return UniTask.CompletedTask;
        }

        protected override UniTask OnShow()
        {
            Debug.Log("[MainPanel] Showing main panel");
            return UniTask.CompletedTask;
        }

        public override bool OnInput(KeyCode key, bool down) => true;
    }
}
