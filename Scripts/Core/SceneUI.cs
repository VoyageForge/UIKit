using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// 场景 UI 入口组件。挂载到场景 GameObject 上，持有预置的 FullPanel 列表。
    /// Start 时自动将面板注册到 FullPanelManager，OnDestroy 时注销。
    /// </summary>
    public class SceneUI : MonoBehaviour
    {
        /// <summary>场景中预置的 UI Panel 条目列表。</summary>
        [SerializeField] [Tooltip("Pre-placed UI panels in the scene")]
        private List<SceneUIEntry> _entries = new();

        /// <summary>只读的预置面板条目列表。</summary>
        public IReadOnlyList<SceneUIEntry> Entries => _entries;

        private void Start()
        {
            UIManager.Panel.RegisterSceneUI(this).Forget();
        }

        private void OnDestroy()
        {
            // 场景卸载时 UIManager 可能已销毁，用 FindObjectOfType 而非 Instance 避免触发单例重建
            var ui = FindObjectOfType<UIManager>();
            if (ui != null) UIManager.Panel.UnregisterSceneUI().Forget();
        }
    }
}
