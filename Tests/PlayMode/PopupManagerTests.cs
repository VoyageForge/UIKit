using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace VoyageForge.UIKit.Tests
{
    /// <summary>
    /// PopupManager 测试 — 重点覆盖多弹窗 List 行为。
    /// </summary>
    public class PopupManagerTests
    {
        private PopupManager _manager;
        private TestPopupProvider _provider;

        [SetUp]
        public void SetUp()
        {
            _provider = new TestPopupProvider();
            _manager = new PopupManager { Provider = _provider };
        }

        [TearDown]
        public void TearDown()
        {
            _manager?.Dispose();
            if (_provider?.Root != null)
                Object.DestroyImmediate(_provider.Root.gameObject);
        }

        // ---- 基础 Show / Hide / Close ----

        [UnityTest]
        public IEnumerator ShowAsync_CreatesAndShows() => UniTask.ToCoroutine(async () =>
        {
            var popup = CreatePopup();
            _provider.Register(popup);

            var result = await _manager.ShowAsync<TestPopupPanel>();

            Assert.IsNotNull(result);
            Assert.AreEqual(BasePanel.PanelState.Active, result.State);
            Assert.AreEqual(1, result.OnCreateCount);
            Assert.AreEqual(1, result.OnShowCount);
        });

        [UnityTest]
        public IEnumerator HideAsync_Caches() => UniTask.ToCoroutine(async () =>
        {
            var popup = CreatePopup();
            _provider.Register(popup);
            await _manager.ShowAsync(popup);

            await _manager.HideAsync(popup);

            Assert.AreEqual(BasePanel.PanelState.Inactive, popup.State);
            Assert.AreEqual(1, popup.OnHideCount);
            // Provider 缓存中有回收的 popup
            Assert.IsTrue(_provider.TryGet(typeof(TestPopupPanel), out var cached));
            Assert.AreSame(popup, cached);
        });

        [UnityTest]
        public IEnumerator CloseAsync_Destroys() => UniTask.ToCoroutine(async () =>
        {
            var popup = CreatePopup();
            _provider.Register(popup);
            await _manager.ShowAsync(popup);

            await _manager.CloseAsync(popup);
            yield return null;

            Assert.AreEqual(1, popup.OnCloseCount);
            Assert.IsTrue(popup == null);
        });

        // ---- 多弹窗：同类型 ----

        [UnityTest]
        public IEnumerator ShowTwo_SameType_BothActive() => UniTask.ToCoroutine(async () =>
        {
            var popup1 = CreatePopup("P1");
            var popup2 = CreatePopup("P2");
            _provider.Register(popup1);
            _provider.Register(popup2);

            var r1 = await _manager.ShowAsync<TestPopupPanel>();
            var r2 = await _manager.ShowAsync<TestPopupPanel>();

            Assert.AreNotSame(r1, r2);
            Assert.AreEqual(BasePanel.PanelState.Active, r1.State);
            Assert.AreEqual(BasePanel.PanelState.Active, r2.State);
            // 两个应挂在同一个 Root 下
            Assert.AreSame(_provider.Root, r1.transform.parent);
            Assert.AreSame(_provider.Root, r2.transform.parent);
        });

        [UnityTest]
        public IEnumerator ShowTwo_DifferentType_SeparateLists() => UniTask.ToCoroutine(async () =>
        {
            var popupA = CreatePopupA("A1");
            var popupB = CreatePopupB("B1");
            _provider.Register(popupA);
            _provider.Register(popupB);

            var rA = await _manager.ShowAsync<TestPopupPanelA>();
            var rB = await _manager.ShowAsync<TestPopupPanelB>();

            // 两种类型都正常显示
            Assert.AreEqual(BasePanel.PanelState.Active, rA.State);
            Assert.AreEqual(BasePanel.PanelState.Active, rB.State);
        });

        // ---- 多弹窗：Hide/Close 其中一个 —剩下那个不受影响 ----

        [UnityTest]
        public IEnumerator HideOne_SameType_OtherRemains() => UniTask.ToCoroutine(async () =>
        {
            var popup1 = CreatePopup("P1");
            var popup2 = CreatePopup("P2");
            _provider.Register(popup1);
            _provider.Register(popup2);

            await _manager.ShowAsync(popup1);
            await _manager.ShowAsync(popup2);

            // Hide popup1
            await _manager.HideAsync(popup1);

            Assert.AreEqual(BasePanel.PanelState.Inactive, popup1.State);
            // popup2 不受影响
            Assert.AreEqual(BasePanel.PanelState.Active, popup2.State);
        });

        [UnityTest]
        public IEnumerator CloseOne_SameType_OtherRemains() => UniTask.ToCoroutine(async () =>
        {
            var popup1 = CreatePopup("P1");
            var popup2 = CreatePopup("P2");
            _provider.Register(popup1);
            _provider.Register(popup2);

            await _manager.ShowAsync(popup1);
            await _manager.ShowAsync(popup2);

            await _manager.CloseAsync(popup1);
            yield return null;

            Assert.IsTrue(popup1 == null);
            // popup2 不受影响，仍 Active
            Assert.AreEqual(BasePanel.PanelState.Active, popup2.State);
        });

        // ---- Provider 热切换 ----

        [UnityTest]
        public IEnumerator ProviderSwitch_MigratesCache() => UniTask.ToCoroutine(async () =>
        {
            var popup = CreatePopup();
            _provider.Register(popup);
            await _manager.ShowAsync(popup);
            await _manager.HideAsync(popup);

            // popup 已在旧 provider 缓存中
            var newProvider = new TestPopupProvider();
            _manager.Provider = newProvider;

            // 缓存应迁移到新 provider
            Assert.IsTrue(newProvider.TryGet(typeof(TestPopupPanel), out var cached));
            Assert.AreSame(popup, cached);

            // 清理
            Object.DestroyImmediate(newProvider.Root.gameObject);
        });

        // ---- Helpers ----

        private TestPopupPanel CreatePopup(string name = "TestPopup")
        {
            var go = new GameObject(name);
            return go.AddComponent<TestPopupPanel>();
        }

        private TestPopupPanelA CreatePopupA(string name = "TestPopupA")
        {
            var go = new GameObject(name);
            return go.AddComponent<TestPopupPanelA>();
        }

        private TestPopupPanelB CreatePopupB(string name = "TestPopupB")
        {
            var go = new GameObject(name);
            return go.AddComponent<TestPopupPanelB>();
        }
    }
}
