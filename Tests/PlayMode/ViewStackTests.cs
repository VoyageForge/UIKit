using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace VoyageForge.UIKit.Tests
{
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
            await _stack.Push(_panel1);
            await _stack.Push(_panel2);

            Assert.AreEqual(2, _stack.Count);
            Assert.AreEqual(BasePanel.PanelState.Paused, _panel1.State);
            Assert.AreEqual(BasePanel.PanelState.Active, _panel2.State);
            Assert.AreEqual(1, _panel1.OnPauseCount);
        });

        [UnityTest]
        public IEnumerator Pop_ResumesPrevious() => UniTask.ToCoroutine(async () =>
        {
            await _stack.Push(_panel1);
            await _stack.Push(_panel2);

            var popped = await _stack.Pop();

            Assert.AreSame(_panel2, popped);
            Assert.AreEqual(1, _stack.Count);
            Assert.AreEqual(BasePanel.PanelState.Inactive, _panel2.State);
            Assert.AreEqual(BasePanel.PanelState.Active, _panel1.State);
            Assert.AreEqual(1, _panel1.OnResumeCount);
            Assert.AreEqual(1, _panel2.OnHideCount);
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

            Assert.AreEqual(1, _panel1.OnCreateCount, "OnCreate 只应调用一次");
            Assert.AreEqual(2, _panel1.OnShowCount, "OnShow 应调用两次");
        });
    }
}
