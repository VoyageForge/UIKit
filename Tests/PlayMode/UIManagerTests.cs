using System.Collections;
using System.Reflection;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoyageForge.UIKit.Runtime;

namespace VoyageForge.UIKit.Tests
{
    /// <summary>
    /// UIManager integration tests — GetPanel/PushAsync/PopAsync/PanelProvider hot-swap.
    /// Manages MonoSingleton static state to prevent cross-test interference.
    /// </summary>
    public class UIManagerTests
    {
        private TestPanelProvider _provider;
        private GameObject _go1, _go2;

        [SetUp]
        public void SetUp()
        {
            _provider = new TestPanelProvider();
            UIManager.Panel.Provider = _provider;
        }

        [TearDown]
        public void TearDown()
        {
            if (_go1 != null) Object.DestroyImmediate(_go1);
            if (_go2 != null) Object.DestroyImmediate(_go2);

            // Use FindObjectOfType to avoid creating a new UIManager just to destroy it
            var instance = Object.FindObjectOfType<UIManager>();
            if (instance != null)
            {
                Object.DestroyImmediate(instance.gameObject);
                ClearSingleton();
            }
        }

        /// <summary>
        /// Reset MonoSingleton static fields and Panel/Popup references
        /// to prevent cross-test interference.
        /// </summary>
        private static void ClearSingleton()
        {
            var baseType = typeof(UIManager).BaseType;

            var instanceField = baseType?.GetField("_instance",
                BindingFlags.NonPublic | BindingFlags.Static);
            instanceField?.SetValue(null, null);

            var quitField = baseType?.GetField("_applicationIsQuitting",
                BindingFlags.NonPublic | BindingFlags.Static);
            quitField?.SetValue(null, false);

            // Reset Panel/Popup static fields on UIManager itself
            var panelField = typeof(UIManager).GetField("_panel",
                BindingFlags.NonPublic | BindingFlags.Static);
            panelField?.SetValue(null, null);

            var popupField = typeof(UIManager).GetField("_popup",
                BindingFlags.NonPublic | BindingFlags.Static);
            popupField?.SetValue(null, null);
        }

        [UnityTest]
        public IEnumerator GetPanel_ShowSelf_ReturnsPanel_Active() => UniTask.ToCoroutine(async () =>
        {
            _go1 = new GameObject("TestPanel");
            var panel = _go1.AddComponent<TestFullPanel>();
            _provider.Register(panel);

            var result = await UIManager.Panel.GetPanel<TestFullPanel>();
            await result.ShowSelfAsync();

            Assert.IsNotNull(result);
            Assert.AreSame(panel, result);
            Assert.AreEqual(BasePanel.PanelState.Active, panel.State);
        });

        [UnityTest]
        public IEnumerator HideAsync_PopsAndResumes() => UniTask.ToCoroutine(async () =>
        {
            _go1 = new GameObject("PanelA");
            _go2 = new GameObject("PanelB");
            var panelA = _go1.AddComponent<TestFullPanelA>();
            var panelB = _go2.AddComponent<TestFullPanelB>();

            _provider.Register(panelA);
            _provider.Register(panelB);

            var a = await UIManager.Panel.GetPanel<TestFullPanelA>();
            await a.ShowSelfAsync();
            var b = await UIManager.Panel.GetPanel<TestFullPanelB>();
            await b.ShowSelfAsync();

            Assert.AreEqual(BasePanel.PanelState.Paused, panelA.State);
            Assert.AreEqual(BasePanel.PanelState.Active, panelB.State);

            await UIManager.Panel.PopAsync();

            Assert.AreEqual(BasePanel.PanelState.Active, panelA.State);
            Assert.AreEqual(BasePanel.PanelState.Inactive, panelB.State);
        });

        [UnityTest]
        public IEnumerator PanelProviderSwap_CacheMigrated() => UniTask.ToCoroutine(async () =>
        {
            _go1 = new GameObject("TestPanel");
            var panel = _go1.AddComponent<TestFullPanel>();
            _provider.Register(panel);

            var loaded = await UIManager.Panel.GetPanel<TestFullPanel>();
            await loaded.ShowSelfAsync();
            await UIManager.Panel.PopAsync();

            var newProvider = new TestPanelProvider();
            UIManager.Panel.Provider = newProvider;

            Assert.IsTrue(newProvider.TryGet(typeof(TestFullPanel), out var cached));
            Assert.AreSame(panel, cached);
        });

        [UnityTest]
        public IEnumerator HideAsync_ReturnsPoppedPanel() => UniTask.ToCoroutine(async () =>
        {
            _go1 = new GameObject("TestPanel");
            var panel = _go1.AddComponent<TestFullPanel>();
            _provider.Register(panel);

            var loaded = await UIManager.Panel.GetPanel<TestFullPanel>();
            await loaded.ShowSelfAsync();
            var result = await UIManager.Panel.PopAsync();

            Assert.IsNotNull(result);
            Assert.AreSame(panel, result);
        });

        [UnityTest]
        public IEnumerator PushAsync_ABA_Rejected() => UniTask.ToCoroutine(async () =>
        {
            _go1 = new GameObject("PanelA");
            _go2 = new GameObject("PanelB");
            var panelA = _go1.AddComponent<TestFullPanel>();
            var panelB = _go2.AddComponent<TestFullPanelA>();

            _provider.Register(panelA);
            _provider.Register(panelB);

            var a = await UIManager.Panel.GetPanel<TestFullPanel>();
            await a.ShowSelfAsync();
            var b = await UIManager.Panel.GetPanel<TestFullPanelA>();
            await b.ShowSelfAsync();

            _provider.Register(panelA);
            LogAssert.Expect(LogType.Error, "[ViewStack] ABA rejected: TestFullPanel is already in the stack.");
            var a2 = await UIManager.Panel.GetPanel<TestFullPanel>();
            await a2.ShowSelfAsync();

            Assert.IsNotNull(a2);
            Assert.AreEqual(BasePanel.PanelState.Active, panelB.State);
        });

        /// <summary>
        /// GetPanel twice without Show returns the same cached instance (cache not consumed).
        /// </summary>
        [UnityTest]
        public IEnumerator GetPanel_Twice_ReturnsSameInstance() => UniTask.ToCoroutine(async () =>
        {
            _go1 = new GameObject("TestPanel");
            var panel = _go1.AddComponent<TestFullPanel>();
            _provider.Register(panel);

            var a1 = await UIManager.Panel.GetPanel<TestFullPanel>();
            var a2 = await UIManager.Panel.GetPanel<TestFullPanel>();

            Assert.IsNotNull(a1);
            Assert.AreSame(a1, a2, "GetPanel twice should return the same instance (cache not consumed)");

            // After Show, cache is consumed
            await a1.ShowSelfAsync();

            var a3 = await UIManager.Panel.GetPanel<TestFullPanel>();
            Assert.IsNull(a3, "After Show, cache should be empty; GetPanel returns null");
        });

        /// <summary>
        /// GetPanel → Show → Pop → GetPanel returns the re-cached instance.
        /// </summary>
        [UnityTest]
        public IEnumerator GetPanel_AfterShowAndPop_RecyclesCache() => UniTask.ToCoroutine(async () =>
        {
            _go1 = new GameObject("TestPanel");
            var panel = _go1.AddComponent<TestFullPanel>();
            _provider.Register(panel);

            var a1 = await UIManager.Panel.GetPanel<TestFullPanel>();
            await a1.ShowSelfAsync();
            await UIManager.Panel.PopAsync(); // Hide → Release back to cache

            var a2 = await UIManager.Panel.GetPanel<TestFullPanel>();
            Assert.IsNotNull(a2);
            Assert.AreSame(a1, a2, "After Pop, panel should be re-cached and GetPanel returns same instance");
        });
    }
}
