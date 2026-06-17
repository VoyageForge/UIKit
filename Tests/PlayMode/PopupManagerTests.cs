using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoyageForge.UIKit.Runtime;

namespace VoyageForge.UIKit.Tests
{
    /// <summary>
    /// PopupManager 弹窗管理测试 — 重点覆盖多弹窗 List 行为及 Provider 热切换。
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

        // ==================== 基础 Show / Hide / Close ====================

        /// <summary>
        /// GetPopup + ShowSelfAsync 创建并显示弹窗 → State=Active，OnCreate+OnShow 触发。
        /// </summary>
        [UnityTest]
        public IEnumerator GetPopup_ShowSelf_CreatesAndShows() => UniTask.ToCoroutine(async () =>
        {
            var popup = CreatePopup();
            _provider.Register(popup);

            var result = await _manager.GetPopupAsync<TestPopupPanel>();
            await _manager.ShowPopupAsync(result);

            Assert.IsNotNull(result);
            Assert.AreEqual(BasePanel.PanelState.Active, result.State);
            Assert.AreEqual(1, result.OnCreateCount);
            Assert.AreEqual(1, result.OnShowCount);
        });

        /// <summary>
        /// Hide 后将弹窗回收到 Provider 缓存中，可再次取出。
        /// </summary>
        [UnityTest]
        public IEnumerator HideAsync_Caches() => UniTask.ToCoroutine(async () =>
        {
            var popup = CreatePopup();
            _provider.Register(popup);
            await _manager.ShowPopupAsync(popup);

            await _manager.HideAsync(popup);

            Assert.AreEqual(BasePanel.PanelState.Inactive, popup.State);
            Assert.AreEqual(1, popup.OnHideCount);
            Assert.IsTrue(_provider.TryGet(typeof(TestPopupPanel), out var cached));
            Assert.AreSame(popup, cached);
        });

        /// <summary>
        /// Close 销毁弹窗 → OnClose 触发，gameObject 被 Destroy。
        /// </summary>
        [UnityTest]
        public IEnumerator CloseAsync_Destroys() => UniTask.ToCoroutine(async () =>
        {
            var popup = CreatePopup();
            _provider.Register(popup);
            await _manager.ShowPopupAsync(popup);

            await _manager.CloseAsync(popup);
            await UniTask.Yield();

            Assert.AreEqual(1, popup.OnCloseCount);
            Assert.IsTrue(popup == null);
        });

        // ==================== 多弹窗：同类型 / 不同类型 ====================

        /// <summary>
        /// 同类型弹两个 → 两个都 Active，挂在同一个 Root 下。
        /// </summary>
        [UnityTest]
        public IEnumerator ShowTwo_SameType_BothActive() => UniTask.ToCoroutine(async () =>
        {
            var popup1 = CreatePopup("P1");
            var popup2 = CreatePopup("P2");
            _provider.Register(popup1);
            _provider.Register(popup2);

            var r1 = await _manager.GetPopupAsync<TestPopupPanel>();
            await _manager.ShowPopupAsync(r1);
            var r2 = await _manager.GetPopupAsync<TestPopupPanel>();
            await _manager.ShowPopupAsync(r2);

            Assert.AreNotSame(r1, r2);
            Assert.AreEqual(BasePanel.PanelState.Active, r1.State);
            Assert.AreEqual(BasePanel.PanelState.Active, r2.State);


            Assert.AreSame(_provider.Root, r1.transform.parent);
            Assert.AreSame(_provider.Root, r2.transform.parent);
        });

        /// <summary>
        /// 不同类型各弹一个 → 各自独立 List，互不影响。
        /// </summary>
        [UnityTest]
        public IEnumerator ShowTwo_DifferentType_SeparateLists() => UniTask.ToCoroutine(async () =>
        {
            var popupA = CreatePopupA("A1");
            var popupB = CreatePopupB("B1");
            _provider.Register(popupA);
            _provider.Register(popupB);

            var rA = await _manager.GetPopupAsync<TestPopupPanelA>();
            await _manager.ShowPopupAsync(rA);
            var rB = await _manager.GetPopupAsync<TestPopupPanelB>();
            await _manager.ShowPopupAsync(rB);

            Assert.AreEqual(BasePanel.PanelState.Active, rA.State);
            Assert.AreEqual(BasePanel.PanelState.Active, rB.State);
        });

        // ==================== 多弹窗：Hide/Close 其中一个 ====================

        /// <summary>
        /// 同类型弹两个，Hide 其中一个 → 被 Hide 的变为 Inactive，另一个不受影响。
        /// </summary>
        [UnityTest]
        public IEnumerator HideOne_SameType_OtherRemains() => UniTask.ToCoroutine(async () =>
        {
            var popup1 = CreatePopup("P1");
            var popup2 = CreatePopup("P2");
            _provider.Register(popup1);
            _provider.Register(popup2);

            await _manager.ShowPopupAsync(popup1);
            await _manager.ShowPopupAsync(popup2);

            await _manager.HideAsync(popup1);

            Assert.AreEqual(BasePanel.PanelState.Inactive, popup1.State);
            Assert.AreEqual(BasePanel.PanelState.Active, popup2.State);
        });

        /// <summary>
        /// 同类型弹两个，Close 其中一个 → 被销毁，另一个仍 Active。
        /// </summary>
        [UnityTest]
        public IEnumerator CloseOne_SameType_OtherRemains() => UniTask.ToCoroutine(async () =>
        {
            var popup1 = CreatePopup("P1");
            var popup2 = CreatePopup("P2");
            _provider.Register(popup1);
            _provider.Register(popup2);

            await _manager.ShowPopupAsync(popup1);
            await _manager.ShowPopupAsync(popup2);

            await _manager.CloseAsync(popup1);
            await UniTask.Yield();

            Assert.IsTrue(popup1 == null);
            Assert.AreEqual(BasePanel.PanelState.Active, popup2.State);
        });

        // ==================== Provider 热切换 ====================

        /// <summary>
        /// 运行时切换 Provider → 旧缓存迁移到新 Provider，已激活弹窗 Reparent 到新 Root。
        /// </summary>
        [UnityTest]
        public IEnumerator ProviderSwitch_MigratesCache() => UniTask.ToCoroutine(async () =>
        {
            var popup = CreatePopup();
            _provider.Register(popup);
            await _manager.ShowPopupAsync(popup);
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
