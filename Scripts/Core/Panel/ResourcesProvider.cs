using UnityEngine;
using Object = UnityEngine.Object;

namespace VoyageForge.UIKit.Runtime
{
    public class ResourcesProvider : PanelProviderBase
    {
        protected override BasePanel Instantiate(string path)
        {
            var prefab = Resources.Load<BasePanel>(path);
            if (prefab == null)
            {
                Debug.LogError($"[ResourcesProvider] Load failed: {path}");
                return null;
            }
            return Object.Instantiate(prefab);
        }
    }
}
