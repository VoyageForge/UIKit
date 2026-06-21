using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// FullPanel 默认加载器。使用 Resources.LoadAsync 从 Resources 目录加载预制体并实例化。
    /// 路径自动去除 "Resources/" 前缀以适配 Resources.LoadAsync 的路径格式。
    /// </summary>
    public class ResourcesProvider : PanelProviderBase
    {
        /// <summary>
        /// 根据路径异步创建 FullPanel 实例。
        /// </summary>
        /// <param name="path">完整资源路径（如 "Assets/Resources/UI/Panels/ShopPanel.prefab"）。</param>
        protected override async UniTask<T> InstantiateAsync<T>(string path)
        {
            // 截取 "Resources/" 之后的路径，去除扩展名
            var idx = path.LastIndexOf("Resources/", StringComparison.Ordinal);
            var resPath = idx >= 0 ? path[(idx + 10)..] : path;

            var req = Resources.LoadAsync<GameObject>(resPath);
            await req;
            if (req.asset == null)
            {
                Debug.LogError($"[ResourcesProvider] Load failed: {path} -> {resPath}");
                return null;
            }
            var instance = Object.Instantiate((GameObject)req.asset);
            return instance.GetComponent<T>();
        }
    }
}
