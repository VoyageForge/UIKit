using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace VoyageForge.UIKit.Tests
{
    /// <summary>
    /// 测试用 FullPanel — 覆写所有生命周期钩子记录调用。
    /// </summary>
    public class TestFullPanel : FullPanel
    {
        public int OnCreateCount;
        public int OnShowCount;
        public int OnHideCount;
        public int OnCloseCount;
        public int OnPauseCount;
        public int OnResumeCount;
        public readonly List<string> CallOrder = new();

        // 方便测试：设定 Show 是否应成功
        public bool FailOnCreate;

        protected override UniTask OnCreate()
        {
            if (FailOnCreate)
                throw new Exception("OnCreate failed");
            OnCreateCount++;
            CallOrder.Add(nameof(OnCreate));
            return UniTask.CompletedTask;
        }

        protected override UniTask OnShow()
        {
            OnShowCount++;
            CallOrder.Add(nameof(OnShow));
            return UniTask.CompletedTask;
        }

        protected override UniTask OnHide()
        {
            OnHideCount++;
            CallOrder.Add(nameof(OnHide));
            return UniTask.CompletedTask;
        }

        protected override UniTask OnClose()
        {
            OnCloseCount++;
            CallOrder.Add(nameof(OnClose));
            return UniTask.CompletedTask;
        }

        protected override UniTask OnPause()
        {
            OnPauseCount++;
            CallOrder.Add(nameof(OnPause));
            return UniTask.CompletedTask;
        }

        protected override UniTask OnResume()
        {
            OnResumeCount++;
            CallOrder.Add(nameof(OnResume));
            return UniTask.CompletedTask;
        }
    }

    /// <summary>
    /// 测试用 PopupPanel — 覆写所有生命周期钩子记录调用。
    /// </summary>
    public class TestPopupPanel : PopupPanel
    {
        public int OnCreateCount;
        public int OnShowCount;
        public int OnHideCount;
        public int OnCloseCount;
        public readonly List<string> CallOrder = new();

        protected override UniTask OnCreate()
        {
            OnCreateCount++;
            CallOrder.Add(nameof(OnCreate));
            return UniTask.CompletedTask;
        }

        protected override UniTask OnShow()
        {
            OnShowCount++;
            CallOrder.Add(nameof(OnShow));
            return UniTask.CompletedTask;
        }

        protected override UniTask OnHide()
        {
            OnHideCount++;
            CallOrder.Add(nameof(OnHide));
            return UniTask.CompletedTask;
        }

        protected override UniTask OnClose()
        {
            OnCloseCount++;
            CallOrder.Add(nameof(OnClose));
            return UniTask.CompletedTask;
        }
    }

    /// <summary>
    /// 测试用 PopupPanel A 类型 — 用于不同弹窗类型混合测试。
    /// </summary>
    public class TestPopupPanelA : PopupPanel
    {
        protected override UniTask OnCreate() => UniTask.CompletedTask;
        protected override UniTask OnShow() => UniTask.CompletedTask;
        protected override UniTask OnHide() => UniTask.CompletedTask;
        protected override UniTask OnClose() => UniTask.CompletedTask;
    }

    /// <summary>
    /// 测试用 PopupPanel B 类型 — 与 A 类型不同，用于验证独立 List。
    /// </summary>
    public class TestPopupPanelB : PopupPanel
    {
        protected override UniTask OnCreate() => UniTask.CompletedTask;
        protected override UniTask OnShow() => UniTask.CompletedTask;
        protected override UniTask OnHide() => UniTask.CompletedTask;
        protected override UniTask OnClose() => UniTask.CompletedTask;
    }

    /// <summary>
    /// 测试用 PanelProvider — 不真正加载资源，通过 Register 预注入 panel。
    /// </summary>
    public class TestPanelProvider : PanelProviderBase
    {
        protected override UniTask<T> InstantiateAsync<T>(string path)
        {
            return UniTask.FromResult<T>(null);
        }
    }

    /// <summary>
    /// 测试用 PopupProvider — 创建测试 Canvas Root，通过 Register 预注入 popup。
    /// </summary>
    public class TestPopupProvider : PopupProviderBase
    {
        public TestPopupProvider()
        {
            var go = new GameObject("[Test] PopupCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            Object.DontDestroyOnLoad(go);
            _root = go.transform;
        }

        protected override UniTask<T> InstantiateAsync<T>(string path)
        {
            return UniTask.FromResult<T>(null);
        }
    }
}
