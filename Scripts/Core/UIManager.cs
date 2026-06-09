using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VoyageForge.Depot.Runtime.Utilities;

namespace VoyageForge.UIKit.Runtime
{
    public class UIManager : MonoSingleton<UIManager>
    {
        private readonly ViewStack _stack = new();
        
        private SceneUIContext _sceneContext;

        private readonly PopupManager _popupManager = new();
        public PopupManager PopupManager => _popupManager;

        private IPanelProvider _provider = new ResourcesProvider();

        public IPanelProvider PanelProvider
        {
            get => _provider;
            set
            {
                if (_provider != null) value.Import(_provider.Export());
                _provider = value;
            }
        }

        private void OnDestroy() => _stack?.Dispose();
        
        protected override void OnApplicationQuit() => _stack?.Dispose();

        // ---- 场景上下文 ----

        internal async UniTask RegisterSceneContext(SceneUIContext context)
        {
            _sceneContext = context;
            foreach (var entry in context.PreplacedPanels)
            {
                if (entry.Panel == null) continue;
                if (entry.Panel.gameObject.activeSelf)
                    await ShowAsync(entry.Panel);
                else
                    PanelProvider.Register(entry.Panel);
            }
        }

        internal async UniTask UnregisterSceneContext()
        {
            if (_sceneContext == null) return;

            foreach (var entry in _sceneContext.PreplacedPanels)
                PanelProvider.Remove(entry.Panel.GetType());

            if (_stack != null)
                while (_stack.Count > 0)
                {
                    var panel = _stack.Peek();
                    if (panel == null) break;
                    if (_sceneContext.PreplacedPanels.Any(p => p.Panel == panel))
                        await _stack.Pop();
                    else
                        return;
                }
        }

        // ---- 公开 API ----

        /// <summary> 显示 FullPanel（Fire & Forget）。 </summary>
        public void Show<T>() where T : FullPanel => ShowAsync<T>().Forget();

        /// <summary> 显示 FullPanel（异步）。 </summary>
        public async UniTask<T> ShowAsync<T>() where T : FullPanel
        {
            var panel = PanelProvider.Load<T>();
            if (panel == null)
            {
                Debug.LogError($"[UIManager] Show failed: {typeof(T).Name}");
                return null;
            }
            await _stack.Push(panel);
            return panel;
        }

        /// <summary> 显示已有 FullPanel 实例（Fire & Forget）。 </summary>
        public void Show(FullPanel panel) => ShowAsync(panel).Forget();

        /// <summary> 显示已有 FullPanel 实例（异步）。 </summary>
        public async UniTask ShowAsync(FullPanel panel)
        {
            if (panel == null) return;
            await _stack.Push(panel);
        }

        /// <summary> 隐藏栈顶（Fire & Forget）。 </summary>
        public void Hide() => HideAsync().Forget();

        /// <summary> 隐藏栈顶（异步）。 </summary>
        public async UniTask HideAsync()
        {
            PanelProvider.Release(await _stack.Pop());
        }

        public void Hide(FullPanel panel) => HideAsync(panel).Forget();

        public async UniTask HideAsync(FullPanel panel)
        {
            if (_stack.Peek() == panel) await _stack.Pop();
            await panel.Hide();
        }

        public FullPanel GetActivePanel() => _stack.Count > 0 ? _stack.Peek() : null;

        public bool OnInput(KeyCode key, bool down)
        {
            var top = _stack.Peek();
            return top != null && top.State == BasePanel.PanelState.Active && top.OnInput(key, down);
        }

        public void Close(BasePanel panel) => CloseAsync(panel).Forget();

        public async UniTask CloseAsync(BasePanel panel)
        {
            if (_stack.Peek() == panel) await _stack.Pop();
            await panel.Close();
        }
    }
}
