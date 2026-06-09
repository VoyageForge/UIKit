using System;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// 标记 Panel 的 Resources 加载路径。
    /// 未标记时 fallback 到 type.Name。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class PanelPathAttribute : Attribute
    {
        public string Path { get; }
        public PanelPathAttribute(string path) => Path = path;
    }
}
