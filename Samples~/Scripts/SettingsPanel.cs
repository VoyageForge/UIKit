using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VoyageForge.UIKit.Runtime;

namespace VoyageForge.UIKit.Samples
{
    /// <summary>
    /// 示例设置面板（FullPanel）。演示 FullPanel 的 Pause/Resume 状态保存：
    /// 音量滑块的值在 OnPause 和 OnClose 时自动保存。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [PanelPath("UI/Samples/Resources/SettingsPanel")]
    public class SettingsPanel : FullPanel
    {
        private Button _backButton;
        private Slider _volumeSlider;
        private Text _volumeLabel;
        /// <summary>音量值持久化（Pause/Close 时保存）。</summary>
        private float _savedVolume = 1f;

        protected override UniTask OnCreate()
        {
            _backButton = GetComponentInChildrenByName<Button>("BackButton");
            _volumeSlider = GetComponentInChildren<Slider>();
            _volumeLabel = GetComponentInChildrenByName<Text>("VolumeLabel");

            if (_backButton != null)
                _backButton.onClick.AddListener(() =>
                    UIManager.Panel.PopAsync().Forget());

            if (_volumeSlider != null)
                _volumeSlider.onValueChanged.AddListener(val =>
                {
                    if (_volumeLabel != null) _volumeLabel.text = $"Volume: {val * 100f:F0}%";
                });

            Debug.Log("[SettingsPanel] Entered");
            if (_volumeSlider != null) _volumeSlider.value = _savedVolume;

            return UniTask.CompletedTask;
        }

        /// <summary>按名称查找子组件。</summary>
        private T GetComponentInChildrenByName<T>(string childName) where T : Component
        {
            foreach (var t in GetComponentsInChildren<T>())
                if (t.name == childName)
                    return t;
            return null;
        }

        /// <summary>被其他面板覆盖时保存音量。</summary>
        protected override UniTask OnPause()
        {
            if (_volumeSlider != null) _savedVolume = _volumeSlider.value;
            return UniTask.CompletedTask;
        }

        /// <summary>面板关闭时保存音量。</summary>
        protected override UniTask OnClose()
        {
            if (_volumeSlider != null) _savedVolume = _volumeSlider.value;
            return UniTask.CompletedTask;
        }

        protected override UniTask OnShow()
        {
            return base.OnShow();
        }

        protected override UniTask OnHide()
        {
            return base.OnHide();
        }
    }
}
