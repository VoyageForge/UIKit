using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoyageForge.UIKit.Runtime;

namespace VoyageForge.UIKit.Tests
{
    /// <summary>
    /// BasePanel 生命周期测试 — 验证 OnCreate/OnShow/OnHide/OnClose 核心流程。
    /// 通过 PopupManager 间接驱动，不依赖 UIManager 全局单例。
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

        /// <summary>
        /// 首次 Show 应依次调用 OnCreate → OnShow。
        /// </summary>
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

        /// <summary>
        /// Show → Hide → 再次 Show：OnCreate 只触发一次，OnShow 触发两次。
        /// </summary>
        [UnityTest]
        public IEnumerator ShowHideShow_OnCreateOnlyOnce() => UniTask.ToCoroutine(async () =>
        {
            _provider.Register(_panel);
            await _popupManager.ShowAsync(_panel);
            await _popupManager.HideAsync(_panel);

            Assert.IsTrue(_provider.TryGet(typeof(TestPopupPanel), out var cached));
            await _popupManager.ShowAsync(cached);

            Assert.AreEqual(1, _panel.OnCreateCount, "OnCreate 只应触发一次");
            Assert.AreEqual(2, _panel.OnShowCount, "OnShow 应触发两次");
            Assert.AreEqual(1, _panel.OnHideCount);
        });

        /// <summary>
        /// Close 应触发 OnClose 并将 State 置为 Exiting，随后 gameObject 被销毁。
        /// </summary>
        [UnityTest]
        public IEnumerator Close_RunsOnClose_AndSetsExiting() => UniTask.ToCoroutine(async () =>
        {
            _provider.Register(_panel);
            await _popupManager.ShowAsync(_panel);

            var go = _panel.gameObject;
            await _popupManager.CloseAsync(_panel);

            Assert.AreEqual(1, _panel.OnCloseCount);
            Assert.AreEqual(BasePanel.PanelState.Exiting, _panel.State);
            // Destroy 是延迟执行的，等待一帧后确认已销毁
            await UniTask.Yield();
            Assert.IsTrue(go == null);
        });

        /// <summary>
        /// 面板已是 Active 状态时再次 Show 应为空操作，不重复触发 OnShow。
        /// </summary>
        [UnityTest]
        public IEnumerator DuplicateShow_WhenAlreadyActive_IsNoop() => UniTask.ToCoroutine(async () =>
        {
            _provider.Register(_panel);
            await _popupManager.ShowAsync(_panel);

            var showCount = _panel.OnShowCount;
            await _popupManager.ShowAsync(_panel);

            Assert.AreEqual(showCount, _panel.OnShowCount, "Active 状态不应重复触发 OnShow");
        });
    }
}
