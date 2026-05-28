using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using VoyageForge.UIKit.Runtime;

namespace VoyageForge.UIKit.Samples
{
    /// <summary>
    /// Demo 启动器。挂到场景任意 GameObject 上，Play 即自动推入 MainPanel。
    /// </summary>
    public class DemoBootstrapper : MonoBehaviour
    {
        [SerializeField] private bool _autoStart = true;

        private void Awake()
        {
           // UIManager.Instance.PanelProvider = new PanelCache();
        }

        private async void Start()
        {
            if (!_autoStart) return;
            
            await UniTask.Yield();

            if (UIManager.Instance == null)
            {
                Debug.LogError("[DemoBootstrapper] UIManager 未找到！");
                return;
            }

            Debug.Log("[DemoBootstrapper] 启动 Demo...");
        }
    }
}
