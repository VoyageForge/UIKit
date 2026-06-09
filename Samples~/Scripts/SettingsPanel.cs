using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VoyageForge.UIKit.Runtime;

namespace VoyageForge.UIKit.Samples
{
    
    [RequireComponent(typeof(CanvasGroup))]
    [PanelPath("UI/Samples/Resources/SettingsPanel")]
    public class SettingsPanel : FullPanel
    {
        private Button _backButton;
        private Slider _volumeSlider;
        private Text _volumeLabel;
        private float _savedVolume = 1f;
        private CanvasGroup _canvasGroup;


        protected override UniTask OnCreate()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            
            _backButton = GetComponentInChildrenByName<Button>("BackButton");
            _volumeSlider = GetComponentInChildren<Slider>();
            _volumeLabel = GetComponentInChildrenByName<Text>("VolumeLabel");

            if (_backButton != null)
                _backButton.onClick.AddListener(() =>
                    UIManager.Instance.HideAsync().Forget());

            if (_volumeSlider != null)
                _volumeSlider.onValueChanged.AddListener(val =>
                {
                    if (_volumeLabel != null) _volumeLabel.text = $"音量: {val * 100f:F0}%";
                });

            Debug.Log("[SettingsPanel] 进入");
            if (_volumeSlider != null) _volumeSlider.value = _savedVolume;

            return UniTask.CompletedTask;
        }

        private T GetComponentInChildrenByName<T>(string childName) where T : Component
        {
            foreach (var t in GetComponentsInChildren<T>())
                if (t.name == childName)
                    return t;
            return null;
        }

        protected override UniTask OnPause()
        {
            if (_volumeSlider != null) _savedVolume = _volumeSlider.value;
            return UniTask.CompletedTask;
        }

        protected override UniTask OnClose()
        {
            if (_volumeSlider != null) _savedVolume = _volumeSlider.value;
            return UniTask.CompletedTask;
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
        
    }
}