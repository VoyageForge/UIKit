using System;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// 标记 Panel 类的 Resources 加载路径。未标记时 fallback 到 type.Name。
    /// 例如 [PanelPath("UI/Panels/ShopPanel")] 会让 Provider 从 Resources/UI/Panels/ShopPanel 加载预制体。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class PanelPathAttribute : Attribute
    {
        /// <summary>Resources 相对路径（不含 "Resources/" 前缀和扩展名）。</summary>
        public string Path { get; }

        public PanelPathAttribute(string path) => Path = path;
    }
}
