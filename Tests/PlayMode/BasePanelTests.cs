using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace VoyageForge.UIKit.Tests
{
    /// <summary>
    /// BasePanel 生命周期核心测试 — 通过 PopupManager 驱动。
    /// </summary>
    public class BasePanelTests
    {
        private PopupManager _popupManager;
        private TestPopupProvider _provider;
        private GameObject _go;
        private TestPopupPanel _panel;

        [SetUp]
        public void SetUp()
        {
            _provider = new TestPopupProvider();
            _popupManager = new PopupManager { Provider = _provider };
            _go = new GameObject("TestPanel");
            _panel = _go.AddComponent<TestPopupPanel>();
            _panel.gameObject.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            _popupManager?.Dispose();
            if (_go != null) Object.DestroyImmediate(_go);
            if (_provider?.Root != null) Object.DestroyImmediate(_provider.Root.gameObject);
        }

        [UnityTest]
        public IEnumerator FirstShow_CallsOnCreateThenOnShow() => UniTask.ToCoroutine(async () =>
        {
            _provider.Register(_panel);
            await _popupManager.ShowAsync(_panel);

            Assert.AreEqual(1, _panel.OnCreateCount);
            Assert.AreEqual(1, _panel.OnShowCount);
            Assert.AreEqual(2, _panel.CallOrder.Count);
            Assert.AreEqual("OnCreate", _panel.CallOrder[0]);
            Assert.AreEqual("OnShow", _panel.CallOrder[1]);
            Assert.AreEqual(BasePanel.PanelState.Active, _panel.State);
        });

        [UnityTest]
        public IEnumerator ShowHideShow_OnCreateOnlyOnce() => UniTask.ToCoroutine(async () =>
        {
            _provider.Register(_panel);
            await _popupManager.ShowAsync(_panel);
            await _popupManager.HideAsync(_panel);

            // 从缓存重新取出
            Assert.IsTrue(_provider.TryGet(typeof(TestPopupPanel), out var cached));
            await _popupManager.ShowAsync(cached);

            Assert.AreEqual(1, _panel.OnCreateCount, "OnCreate should fire only once");
            Assert.AreEqual(2, _panel.OnShowCount, "OnShow should fire twice");
            Assert.AreEqual(1, _panel.OnHideCount);
        });

        [UnityTest]
        public IEnumerator Close_RunsOnClose_AndSetsExiting() => UniTask.ToCoroutine(async () =>
        {
            _provider.Register(_panel);
            await _popupManager.ShowAsync(_panel);

            var go = _panel.gameObject;
            await _popupManager.CloseAsync(_panel);

            Assert.AreEqual(1, _panel.OnCloseCount);
            Assert.AreEqual(BasePanel.PanelState.Exiting, _panel.State);
            // Destroy 是延迟的，等待一帧
            yield return null;
            Assert.IsTrue(go == null);
        });

        [UnityTest]
        public IEnumerator DuplicateShow_WhenAlreadyActive_IsNoop() => UniTask.ToCoroutine(async () =>
        {
            _provider.Register(_panel);
            await _popupManager.ShowAsync(_panel);

            var showCount = _panel.OnShowCount;
            // 再次 Show 同一个 Active 的 panel
            await _popupManager.ShowAsync(_panel);

            Assert.AreEqual(showCount, _panel.OnShowCount, "Active 状态不应重复触发 OnShow");
        });
    }
}
