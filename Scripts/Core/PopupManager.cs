using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VoyageForge.UIKit.Runtime
{
    public class PopupManager : IPopupManager
    {
        private readonly Dictionary<Type, PopupPanel> _active = new();
        
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

        private void ReparentAll()
        {
            foreach (var panel in _active.Values)
                if (panel != null)
                    panel.transform.SetParent(_provider.Root, false);
        }

        // ---- Show ----

        public async UniTask<T> ShowAsync<T>() where T : PopupPanel
        {
            var panel = _provider.Load<T>();
            if (panel == null) return null;
            return await ShowInternal(panel);
        }

        public UniTask ShowAsync(PopupPanel panel) => ShowInternal(panel);

        private async UniTask<T> ShowInternal<T>(T panel) where T : PopupPanel
        {
            if (panel == null) return null;
            var type = panel.GetType();

            if (_active.TryGetValue(type, out var existing) && existing == panel)
            {
                await existing.Show();
                return panel;
            }

            panel.transform.SetParent(_provider.Root, false);
            _active[type] = panel;
            await panel.Show();
            return panel;
        }

        // ---- Hide ----

        public async UniTask HideAsync<T>() where T : PopupPanel
        {
            if (_active.Remove(typeof(T), out var panel))
            {
                await panel.Hide();
                _provider.Release(panel);
            }
        }

        public async UniTask HideAsync(PopupPanel panel)
        {
            if (panel == null) return;
            var type = panel.GetType();

            if (_active.TryGetValue(type, out var active) && active == panel)
                _active.Remove(type);

            await panel.Hide();
            _provider.Release(panel);
        }

        // ---- Close ----

        public async UniTask CloseAsync<T>() where T : PopupPanel
        {
            if (_active.Remove(typeof(T), out var panel))
                await panel.Close();
        }

        public async UniTask CloseAsync(PopupPanel panel)
        {
            if (panel == null) return;
            var type = panel.GetType();

            if (_active.TryGetValue(type, out var active) && active == panel)
                _active.Remove(type);

            await panel.Close();
        }

        public bool IsShowing<T>() where T : PopupPanel =>
            _active.TryGetValue(typeof(T), out var p) && p.State == BasePanel.PanelState.Active;

        public void Dispose() => _active.Clear();
    }
}