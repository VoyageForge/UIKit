using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoyageForge.UIKit.Runtime;

namespace VoyageForge.UIKit.Tests
{
    /// <summary>
    /// PopupManager tests — multi-popup list behavior and Provider hot-swap.
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

        // ==================== Basic Show / Hide / Close ====================

        [UnityTest]
        public IEnumerator GetPopup_ShowSelf_CreatesAndShows() => UniTask.ToCoroutine(async () =>
        {
            var popup = CreatePopup();
            _provider.Register(popup);

            var result = await _manager.GetPopup<TestPopupPanel>();
            await _manager.ShowAsync(result);

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
            await UniTask.Yield();

            Assert.AreEqual(1, popup.OnCloseCount);
            Assert.IsTrue(popup == null);
        });

        // ==================== Multiple popups: same type / different types ====================

        [UnityTest]
        public IEnumerator ShowTwo_SameType_BothActive() => UniTask.ToCoroutine(async () =>
        {
            var popup1 = CreatePopup("P1");
            var popup2 = CreatePopup("P2");
            _provider.Register(popup1);
            _provider.Register(popup2);

            var r1 = await _manager.GetPopup<TestPopupPanel>();
            await _manager.ShowAsync(r1);
            var r2 = await _manager.GetPopup<TestPopupPanel>();
            await _manager.ShowAsync(r2);

            Assert.AreNotSame(r1, r2);
            Assert.AreEqual(BasePanel.PanelState.Active, r1.State);
            Assert.AreEqual(BasePanel.PanelState.Active, r2.State);

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

            var rA = await _manager.GetPopup<TestPopupPanelA>();
            await _manager.ShowAsync(rA);
            var rB = await _manager.GetPopup<TestPopupPanelB>();
            await _manager.ShowAsync(rB);

            Assert.AreEqual(BasePanel.PanelState.Active, rA.State);
            Assert.AreEqual(BasePanel.PanelState.Active, rB.State);
        });

        // ==================== Hide/Close one of multiple popups ====================

        [UnityTest]
        public IEnumerator HideOne_SameType_OtherRemains() => UniTask.ToCoroutine(async () =>
        {
            var popup1 = CreatePopup("P1");
            var popup2 = CreatePopup("P2");
            _provider.Register(popup1);
            _provider.Register(popup2);

            await _manager.ShowAsync(popup1);
            await _manager.ShowAsync(popup2);

            await _manager.HideAsync(popup1);

            Assert.AreEqual(BasePanel.PanelState.Inactive, popup1.State);
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
            await UniTask.Yield();

            Assert.IsTrue(popup1 == null);
            Assert.AreEqual(BasePanel.PanelState.Active, popup2.State);
        });

        // ==================== GetPopup cache semantics ====================

        /// <summary>
        /// GetPopup twice without Show returns the same cached instance (cache not consumed).
        /// </summary>
        [UnityTest]
        public IEnumerator GetPopup_Twice_BeforeShow_ReturnsSameInstance() => UniTask.ToCoroutine(async () =>
        {
            var popup = CreatePopup();
            _provider.Register(popup);

            var p1 = await _manager.GetPopup<TestPopupPanel>();
            var p2 = await _manager.GetPopup<TestPopupPanel>();

            Assert.IsNotNull(p1);
            Assert.AreSame(p1, p2, "GetPopup twice should return the same instance (cache not consumed)");
        });

        /// <summary>
        /// GetPopup → Show → GetPopup again returns null (cache consumed by Show).
        /// </summary>
        [UnityTest]
        public IEnumerator GetPopup_AfterShow_ConsumesCache() => UniTask.ToCoroutine(async () =>
        {
            var popup = CreatePopup();
            _provider.Register(popup);

            var p1 = await _manager.GetPopup<TestPopupPanel>();
            await _manager.ShowAsync(p1); // Show consumes from cache

            var p2 = await _manager.GetPopup<TestPopupPanel>();
            Assert.IsNull(p2, "After Show, cache should be empty; GetPopup returns null");
        });

        // ==================== Provider hot-swap ====================

        [UnityTest]
        public IEnumerator ProviderSwitch_MigratesCache() => UniTask.ToCoroutine(async () =>
        {
            var popup = CreatePopup();
            _provider.Register(popup);
            await _manager.ShowAsync(popup);
            await _manager.HideAsync(popup);

            var newProvider = new TestPopupProvider();
            _manager.Provider = newProvider;

            Assert.IsTrue(newProvider.TryGet(typeof(TestPopupPanel), out var cached));
            Assert.AreSame(popup, cached);

            Object.DestroyImmediate(newProvider.Root.gameObject);
        });

        // ==================== Helpers ====================

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
