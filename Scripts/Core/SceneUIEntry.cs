using UnityEngine;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// 场景预置面板条目。key 自动从 Panel.GetType() 获取。
    /// 挂载在 SceneUI 组件上使用，填写场景中已放置的 FullPanel 引用即可。
    /// </summary>
    [System.Serializable]
    public struct SceneUIEntry
    {
        /// <summary>场景中已放置的 FullPanel 引用。</summary>
        [Tooltip("Pre-placed panel in the scene (must inherit FullPanel)")]
        public FullPanel Panel;
    }
}
