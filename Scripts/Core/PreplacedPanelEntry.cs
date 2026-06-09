using UnityEngine;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// 预置面板条目。key 自动从 Panel.GetType() 获取。
    /// </summary>
    [System.Serializable]
    public struct PreplacedPanelEntry
    {
        [Tooltip("场景中已放置的 Panel（需继承 FullPanel）")]
        public FullPanel Panel;
    }
}
