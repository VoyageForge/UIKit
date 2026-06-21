using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// Popup 默认加载器。继承 PopupProviderBase，使用 Resources.LoadAsync 加载预制体。
    /// 首次访问 Root 时自动创建 DontDestroyOnLoad 的 ScreenSpaceOverlay Canvas（sortingOrder=5000）。
    /// </summary>
    public class PopupResourcesProvider : PopupProviderBase
    {
        /// <summary>
        /// 弹窗专用 Canvas 根节点。首次访问时自动创建带 Canvas + CanvasScaler + GraphicRaycaster 的 GameObject，
        /// 并标记为 DontDestroyOnLoad（场景切换时弹窗不销毁）。
        /// </summary>
        public override Transform Root
        {
            get
            {
                if (_root != null) return _root;
                var go = new GameObject("[PopupCanvas]");
                Object.DontDestroyOnLoad(go);
                var canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 5000;
                go.AddComponent<CanvasScaler>();
                go.AddComponent<GraphicRaycaster>();
                _root = go.transform;
                return _root;
            }
        }

        /// <summary>
        /// 根据路径异步创建 Popup 实例。
        /// </summary>
        /// <param name="path">完整资源路径。</param>
        protected override async UniTask<T> InstantiateAsync<T>(string path)
        {
            var idx = path.LastIndexOf("Resources/", StringComparison.Ordinal);
            var resPath = idx >= 0 ? path[(idx + 10)..] : path;

            var req = Resources.LoadAsync<GameObject>(resPath);
            await req;
            if (req.asset == null) return null;
            var instance = Object.Instantiate((GameObject)req.asset);
            return instance.GetComponent<T>();
        }
    }
}
