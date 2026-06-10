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

        // ---- Show ----

      
        public async UniTask<T> ShowAsync<T>() where T : PopupPanel
        {
            var panel = await _provider.LoadAsync<T>();
            if (panel == null) return null;
            return await ShowInternal(panel);
        }
        

        public UniTask ShowAsync(PopupPanel panel) => ShowInternal(panel);

        private async UniTask<T> ShowInternal<T>(T panel) where T : PopupPanel
        {
            if (panel == null) return null;
            var type = panel.GetType();

            if (_active.TryGetValue(type, out var popups) 
                && popups != null
                && popups.Contains(panel))
            {
                popups.Add(panel);
               
            }
            else
            {
                if (popups == null)
                {
                    popups = new List<PopupPanel>();
                    _active[type] = popups;
                }
                popups.Add(panel);
            }

            panel.transform.SetParent(_provider.Root, false);
            _active[type].Add(panel);
            await panel.Show();
            return panel;
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