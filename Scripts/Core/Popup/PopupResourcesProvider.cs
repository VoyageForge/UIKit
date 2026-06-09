using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace VoyageForge.UIKit.Runtime
{
    public class PopupResourcesProvider : PanelProviderBase, IPopupProvider
    {
        private Transform _root;

        public Transform Root
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

        protected override BasePanel Instantiate(string path)
        {
            var prefab = Resources.Load<BasePanel>(path);
            if (prefab == null) return null;
            return Object.Instantiate(prefab);
        }
    }
}
