using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoyageForge.UIKit.Runtime;
using Object = UnityEngine.Object;

namespace VoyageForge.UIKit.Tests
{
    /// <summary>
    /// ViewStack navigation tests — Push/Pop/Pause/Resume flow for FullPanels.
    /// </summary>
    public class ViewStackTests
    {
        private ViewStack _stack;
        private GameObject _go1, _go2;
        private TestFullPanel _panel1, _panel2;

        [SetUp]
        public void SetUp()
        {
            _stack = new ViewStack();
            _go1 = new GameObject("TestPanel1");
            _panel1 = _go1.AddComponent<TestFullPanel>();
            _panel1.gameObject.SetActive(false);
            _go2 = new GameObject("TestPanel2");
            _panel2 = _go2.AddComponent<TestFullPanel>();
            _panel2.gameObject.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            _stack?.Dispose();
            if (_go1 != null) Object.DestroyImmediate(_go1);
            if (_go2 != null) Object.DestroyImmediate(_go2);
        }

        [UnityTest]
        public IEnumerator PushSingle_ActivatesPanel() => UniTask.ToCoroutine(async () =>
        {
            await _stack.Push(_panel1);

            Assert.AreEqual(1, _stack.Count);
            Assert.AreEqual(BasePanel.PanelState.Active, _panel1.State);
            Assert.AreEqual(1, _panel1.OnCreateCount);
            Assert.AreEqual(1, _panel1.OnShowCount);
        });

        [UnityTest]
        public IEnumerator PushTwo_PausesFirst() => UniTask.ToCoroutine(async () =>
        {
            var goB = new GameObject("TestPanelB");
            var panelB = goB.AddComponent<TestFullPanelA>();
            panelB.gameObject.SetActive(false);

            await _stack.Push(_panel1);
            await _stack.Push(panelB);

            Assert.AreEqual(2, _stack.Count);
            Assert.AreEqual(BasePanel.PanelState.Paused, _panel1.State);
            Assert.AreEqual(BasePanel.PanelState.Active, panelB.State);
            Assert.AreEqual(1, _panel1.OnPauseCount);

            Object.DestroyImmediate(goB);
        });

        [UnityTest]
        public IEnumerator Pop_ResumesPrevious() => UniTask.ToCoroutine(async () =>
        {
            var goB = new GameObject("TestPanelB");
            var panelB = goB.AddComponent<TestFullPanelA>();
            panelB.gameObject.SetActive(false);

            await _stack.Push(_panel1);
            await _stack.Push(panelB);

            var popped = await _stack.Pop();

            Assert.AreSame(panelB, popped);
            Assert.AreEqual(1, _stack.Count);
            Assert.AreEqual(BasePanel.PanelState.Inactive, panelB.State);
            Assert.AreEqual(BasePanel.PanelState.Active, _panel1.State);
            Assert.AreEqual(1, _panel1.OnResumeCount);
            Assert.AreEqual(1, panelB.OnHideCount);

            Object.DestroyImmediate(goB);
        });

        [UnityTest]
        public IEnumerator PopEmpty_Throws() => UniTask.ToCoroutine(async () =>
        {
            try
            {
                await _stack.Pop();
                Assert.Fail("Expected InvalidOperationException");
            }
            catch (InvalidOperationException)
            {
                Assert.Pass();
            }
        });

        [UnityTest]
        public IEnumerator PushPopPush_SameInstance_OnCreateOnce() => UniTask.ToCoroutine(async () =>
        {
            await _stack.Push(_panel1);
            await _stack.Pop();
            await _stack.Push(_panel1);

            Assert.AreEqual(1, _panel1.OnCreateCount, "OnCreate should fire only once");
            Assert.AreEqual(2, _panel1.OnShowCount, "OnShow should fire twice");
        });

        [UnityTest]
        public IEnumerator Push_ABB_SkipsDuplicate() => UniTask.ToCoroutine(async () =>
        {
            await _stack.Push(_panel1);
            await _stack.Push(_panel1);

            Assert.AreEqual(1, _stack.Count);
            Assert.AreEqual(1, _panel1.OnShowCount);
            Assert.AreEqual(1, _panel1.OnCreateCount);
        });

        [UnityTest]
        public IEnumerator Push_ABA_LogsError() => UniTask.ToCoroutine(async () =>
        {
            var goB = new GameObject("TestPanelB");
            var panelB = goB.AddComponent<TestFullPanelA>();
            panelB.gameObject.SetActive(false);

            await _stack.Push(_panel1);
            await _stack.Push(panelB);
            LogAssert.Expect(LogType.Error, "[ViewStack] ABA rejected: TestFullPanel is already in the stack.");
            await _stack.Push(_panel1);

            Assert.AreEqual(2, _stack.Count, "ABA rejected, stack should still have 2 entries");
            Assert.AreSame(panelB, _stack.Peek());

            Object.DestroyImmediate(goB);
        });

        [UnityTest]
        public IEnumerator Push_ABA_DeepNesting_LogsError() => UniTask.ToCoroutine(async () =>
        {
            var goB = new GameObject("PanelB");
            var panelB = goB.AddComponent<TestFullPanelA>();
            panelB.gameObject.SetActive(false);
            var goC = new GameObject("PanelC");
            var panelC = goC.AddComponent<TestFullPanelB>();
            panelC.gameObject.SetActive(false);

            await _stack.Push(_panel1);
            await _stack.Push(panelB);
            await _stack.Push(panelC);

            LogAssert.Expect(LogType.Error, "[ViewStack] ABA rejected: TestFullPanel is already in the stack.");
            await _stack.Push(_panel1);

            Assert.AreEqual(3, _stack.Count, "ABA deep nesting rejected, stack should still have 3 entries");
            Assert.AreSame(panelC, _stack.Peek());

            Object.DestroyImmediate(goB);
            Object.DestroyImmediate(goC);
        });

        [UnityTest]
        public IEnumerator Push_Null_ThrowsArgumentNullException() => UniTask.ToCoroutine(async () =>
        {
            try
            {
                await _stack.Push(null);
                Assert.Fail("Expected ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
                Assert.AreEqual(0, _stack.Count);
            }
        });

        [UnityTest]
        public IEnumerator Pop_SinglePanel_StackEmpties() => UniTask.ToCoroutine(async () =>
        {
            await _stack.Push(_panel1);
            Assert.AreEqual(1, _stack.Count);

            var popped = await _stack.Pop();

            Assert.AreSame(_panel1, popped);
            Assert.AreEqual(0, _stack.Count);
            Assert.AreEqual(BasePanel.PanelState.Inactive, _panel1.State);
            Assert.AreEqual(1, _panel1.OnHideCount);
            Assert.AreEqual(0, _panel1.OnResumeCount, "Single panel: Resume should not be called on Pop");
        });

        [UnityTest]
        public IEnumerator Pop_Pop_Chain_ResumesFirst() => UniTask.ToCoroutine(async () =>
        {
            var goB = new GameObject("PanelB");
            var panelB = goB.AddComponent<TestFullPanelA>();
            panelB.gameObject.SetActive(false);
            var goC = new GameObject("PanelC");
            var panelC = goC.AddComponent<TestFullPanelB>();
            panelC.gameObject.SetActive(false);

            await _stack.Push(_panel1);
            await _stack.Push(panelB);
            await _stack.Push(panelC);

            var poppedC = await _stack.Pop();
            var poppedB = await _stack.Pop();

            Assert.AreSame(panelC, poppedC);
            Assert.AreSame(panelB, poppedB);
            Assert.AreEqual(1, _stack.Count);
            Assert.AreSame(_panel1, _stack.Peek());
            Assert.AreEqual(BasePanel.PanelState.Active, _panel1.State);
            Assert.AreEqual(BasePanel.PanelState.Inactive, panelB.State);
            Assert.AreEqual(BasePanel.PanelState.Inactive, panelC.State);
            Assert.AreEqual(1, _panel1.OnResumeCount);
            Assert.AreEqual(1, panelB.OnHideCount);
            Assert.AreEqual(1, panelC.OnHideCount);

            Object.DestroyImmediate(goB);
            Object.DestroyImmediate(goC);
        });

        [UnityTest]
        public IEnumerator TopPanelChanged_FiresOnPush() => UniTask.ToCoroutine(async () =>
        {
            FullPanel receivedPanel = null;
            _stack.TopPanelChanged += p => receivedPanel = p;

            await _stack.Push(_panel1);

            Assert.AreSame(_panel1, receivedPanel);
        });

        [UnityTest]
        public IEnumerator CountChanged_FiresOnPushPop() => UniTask.ToCoroutine(async () =>
        {
            var counts = new List<int>();
            _stack.CountChanged += c => counts.Add(c);

            await _stack.Push(_panel1);
            await _stack.Pop();

            Assert.AreEqual(2, counts.Count);
            Assert.AreEqual(1, counts[0]);
            Assert.AreEqual(0, counts[1]);
        });

        [UnityTest]
        public IEnumerator LifecycleOrder_Push_PauseBeforeShow() => UniTask.ToCoroutine(async () =>
        {
            var goB = new GameObject("PanelB");
            var panelB = goB.AddComponent<TestFullPanelA>();
            panelB.gameObject.SetActive(false);

            await _stack.Push(_panel1);
            await _stack.Push(panelB);

            var pauseIdx = _panel1.CallOrder.IndexOf("OnPause");
            var showIdx = panelB.CallOrder.IndexOf("OnShow");
            var createIdx = panelB.CallOrder.IndexOf("OnCreate");

            Assert.Greater(pauseIdx, -1);
            Assert.Greater(showIdx, -1);
            Assert.Greater(createIdx, -1);
            Assert.Less(createIdx, showIdx, "OnCreate should fire before OnShow");

            Object.DestroyImmediate(goB);
        });

        [UnityTest]
        public IEnumerator Peek_ReturnsCorrectTop() => UniTask.ToCoroutine(async () =>
        {
            Assert.IsNull(_stack.Peek());

            await _stack.Push(_panel1);
            Assert.AreSame(_panel1, _stack.Peek());

            var goB = new GameObject("PanelB");
            var panelB = goB.AddComponent<TestFullPanelA>();
            panelB.gameObject.SetActive(false);

            await _stack.Push(panelB);
            Assert.AreSame(panelB, _stack.Peek());

            await _stack.Pop();
            Assert.AreSame(_panel1, _stack.Peek());

            await _stack.Pop();
            Assert.IsNull(_stack.Peek());

            Object.DestroyImmediate(goB);
        });
    }
}
