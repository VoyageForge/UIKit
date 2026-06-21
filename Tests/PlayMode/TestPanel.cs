using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VoyageForge.UIKit.Runtime;
using Object = UnityEngine.Object;

namespace VoyageForge.UIKit.Tests
{
    /// <summary>测试用 FullPanel。记录所有生命周期钩子的调用次数和顺序。</summary>
    public class TestFullPanel : FullPanel
    {
        public int OnCreateCount;
        public int OnShowCount;
        public int OnHideCount;
        public int OnCloseCount;
        public int OnPauseCount;
        public int OnResumeCount;
        public readonly List<string> CallOrder = new();

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

    /// <summary>测试用 FullPanel 变体 A。用于 ABA/ABB 栈测试。</summary>
    public class TestFullPanelA : FullPanel
    {
        public int OnCreateCount;
        public int OnShowCount;
        public int OnHideCount;
        public int OnPauseCount;
        public int OnResumeCount;
        public readonly List<string> CallOrder = new();

        protected override UniTask OnCreate() { OnCreateCount++; CallOrder.Add(nameof(OnCreate)); return UniTask.CompletedTask; }
        protected override UniTask OnShow()   { OnShowCount++;   CallOrder.Add(nameof(OnShow));   return UniTask.CompletedTask; }
        protected override UniTask OnHide()   { OnHideCount++;   CallOrder.Add(nameof(OnHide));   return UniTask.CompletedTask; }
        protected override UniTask OnClose()  { CallOrder.Add(nameof(OnClose)); return UniTask.CompletedTask; }
        protected override UniTask OnPause()  { OnPauseCount++;  CallOrder.Add(nameof(OnPause));  return UniTask.CompletedTask; }
        protected override UniTask OnResume() { OnResumeCount++; CallOrder.Add(nameof(OnResume)); return UniTask.CompletedTask; }
    }

    /// <summary>测试用 FullPanel 变体 B。用于多层 Push 混合测试。</summary>
    public class TestFullPanelB : FullPanel
    {
        public int OnCreateCount;
        public int OnShowCount;
        public int OnHideCount;
        public int OnPauseCount;
        public int OnResumeCount;

        protected override UniTask OnCreate() { OnCreateCount++; return UniTask.CompletedTask; }
        protected override UniTask OnShow()   { OnShowCount++;   return UniTask.CompletedTask; }
        protected override UniTask OnHide()   { OnHideCount++;   return UniTask.CompletedTask; }
        protected override UniTask OnClose()  => UniTask.CompletedTask;
        protected override UniTask OnPause()  { OnPauseCount++;  return UniTask.CompletedTask; }
        protected override UniTask OnResume() { OnResumeCount++; return UniTask.CompletedTask; }
    }

    /// <summary>测试用 PopupPanel。记录所有生命周期钩子的调用次数和顺序。</summary>
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

    /// <summary>测试用 PopupPanel 变体 A。验证不同弹窗类型各自维护独立列表。</summary>
    public class TestPopupPanelA : PopupPanel
    {
        protected override UniTask OnCreate() => UniTask.CompletedTask;
        protected override UniTask OnShow()   => UniTask.CompletedTask;
        protected override UniTask OnHide()   => UniTask.CompletedTask;
        protected override UniTask OnClose()  => UniTask.CompletedTask;
    }

    /// <summary>测试用 PopupPanel 变体 B。验证不同类型间的列表隔离。</summary>
    public class TestPopupPanelB : PopupPanel
    {
        protected override UniTask OnCreate() => UniTask.CompletedTask;
        protected override UniTask OnShow()   => UniTask.CompletedTask;
        protected override UniTask OnHide()   => UniTask.CompletedTask;
        protected override UniTask OnClose()  => UniTask.CompletedTask;
    }

    /// <summary>测试用 FullPanel Provider。不真正加载资源，通过 Register 预注入 panel 实例。</summary>
    public class TestPanelProvider : PanelProviderBase
    {
        protected override UniTask<T> InstantiateAsync<T>(string path)
        {
            return UniTask.FromResult<T>(null);
        }
    }

    /// <summary>测试用 Popup Provider。自动创建 Canvas Root，通过 Register 预注入 popup 实例。</summary>
    public class TestPopupProvider : PopupProviderBase
    {
        public override Transform Root
        {
            get
            {
                if (_root != null) return _root;
                var go = new GameObject("[Test] PopupCanvas");
                var canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 9999;
                go.AddComponent<CanvasScaler>();
                go.AddComponent<GraphicRaycaster>();
                Object.DontDestroyOnLoad(go);
                _root = go.transform;
                return _root;
            }
        }

        protected override UniTask<T> InstantiateAsync<T>(string path)
        {
            return UniTask.FromResult<T>(null);
        }
    }
}
