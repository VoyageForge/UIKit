using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VoyageForge.UIKit.Runtime
{
    public class PopupManager : IPopupManager
    {
        private readonly Dictionary<Type, List<PopupPanel>> _active = new();

        private IPopupProvider _provider = new PopupResourcesProvider();

        public IPopupProvider Provider
        {
            get => _provider;
            set
            {
                if (value == null) return;
                MigrateCache(_provider, value);
                _provider = value;
                ReparentAll();
            }
        }

        private static void MigrateCache(IPopupProvider from, IPopupProvider to)
        {
            if (from != null) to.Import(from.Export());
        }

        /// <summary>
        /// Reparent all active panels to the new root.
        /// </summary>
        private void ReparentAll()
        {
            foreach (var panels in _active.Values)
                if (panels != null)
                    foreach (var popup in panels)
                    {
                        popup.transform.SetParent(_provider.Root, false);
                    }
        }

        // ---- Get ----

        /// <summary> 异步加载 PopupPanel，完成后回调（不自动显示）。 </summary>
        public async void GetPopup<T>(Action<T> onLoaded) where T : PopupPanel
        {
            var panel = await GetPopupAsync<T>();
            onLoaded?.Invoke(panel);
        }

        /// <summary> 从 Provider 加载 PopupPanel（不自动显示，需手动调用 ShowSelfAsync）。 </summary>
        public async UniTask<T> GetPopupAsync<T>() where T : PopupPanel
        {
            var panel = await _provider.LoadAsync<T>();
            return panel;
        }

        // ---- Show ----

        /// <summary> 显示 PopupPanel 实例。 </summary>
        public async UniTask ShowPopupAsync(PopupPanel panel)
        {
            if (panel == null) return;
            var type = panel.GetType();

            if (!_active.TryGetValue(type, out var popups) || popups == null)
            {
                popups = new List<PopupPanel>();
                _active[type] = popups;
            }

            if (!popups.Contains(panel))
                popups.Add(panel);

            panel.transform.SetParent(_provider.Root, false);
            await panel.Show();
        }

        // ---- Hide ----

       

        public async UniTask HideAsync(PopupPanel panel)
        {
            if (panel == null) return;
            var type = panel.GetType();

            if (_active.TryGetValue(type, out var popups) 
                && popups != null 
                && popups.Contains(panel))
                popups.Remove(panel);
            else
            {
                return;
            }

            await panel.Hide();
            _provider.Release(panel);
        }

    

        public async UniTask CloseAsync(PopupPanel panel)
        {
            if (panel == null) return;
            var type = panel.GetType();

            if (_active.TryGetValue(type, out var popups)
                && popups != null
                && popups.Count > 0 
                && popups.Remove(panel))
            {
               
            }
            
               
            await panel.Close();
        }

       
        public void Dispose() => _active.Clear();
    }
}