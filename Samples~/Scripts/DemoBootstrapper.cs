using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using VoyageForge.UIKit.Runtime;

namespace VoyageForge.UIKit.Samples
{
    /// <summary>
    /// Demo 启动器。挂载到场景任意 GameObject 上，Play 时自动初始化。
    /// </summary>
    public class DemoBootstrapper : MonoBehaviour
    {
        /// <summary>是否自动启动 Demo。</summary>
        [SerializeField] private bool _autoStart = true;

        private async void Start()
        {
            if (!_autoStart) return;

            await UniTask.Yield();

            if (UIManager.Instance == null)
            {
                Debug.LogError("[DemoBootstrapper] UIManager not found!");
                return;
            }

            Debug.Log("[DemoBootstrapper] Starting Demo...");
        }
    }
}
