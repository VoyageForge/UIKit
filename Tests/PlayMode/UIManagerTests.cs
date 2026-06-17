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
    /// UIManager 集成测试 — 验证 GetPanel/PushAsync/HideAsync/PanelProvider 热切换。
    /// 需要管理 MonoSingleton 的静态状态防止测试间交叉影响。
    /// </summary>
    public class UIManagerTests
    {
        private TestPanelProvider _provider;
        private GameObject _go1, _go2;

        [SetUp]
        public void SetUp()
        {
            _provider = new TestPanelProvider();
            UIManager.PanelProvider = _provider;
        }

        [TearDown]
        public void TearDown()
        {
            if (_go1 != null) Object.DestroyImmediate(_go1);
            if (_go2 != null) Object.DestroyImmediate(_go2);

            var instance = UIManager.Instance;
            if (instance != null)
            {
                Object.DestroyImmediate(instance.gameObject);
                ClearSingleton();
            }
        }

        /// <summary>
        /// 重置 MonoSingleton 静态字段以免测试间交叉污染。
        /// </summary>
        private static void ClearSingleton()
        {
            var baseType = typeof(UIManager).BaseType;
            var field = baseType?.GetField("_instance",
                BindingFlags.NonPublic | BindingFlags.Static);
            field?.SetValue(null, null);

            var quitField = baseType?.GetField("_applicationIsQuitting",
                BindingFlags.NonPublic | BindingFlags.Static);
            quitField?.SetValue(null, false);
        }

        /// <summary>
        /// GetPanel + ShowSelfAsync 应返回对应面板实例，State=Active。
        /// </summary>
        [UnityTest]
        public IEnumerator GetPanel_ShowSelf_ReturnsPanel_Active() => UniTask.ToCoroutine(async () =>
        {
            _go1 = new GameObject("TestPanel");
            var panel = _go1.AddComponent<TestFullPanel>();
            _provider.Register(panel);

            var result = await UIManager.Instance.GetPanelAsync<TestFullPanel>();
            await result.ShowSelfAsync();

            Assert.IsNotNull(result);
            Assert.AreSame(panel, result);
            Assert.AreEqual(BasePanel.PanelState.Active, panel.State);
        });

        /// <summary>
        /// Push 两个不同类型面板 → HideAsync → 栈顶出栈，下层 Resume。
        /// </summary>
        [UnityTest]
        public IEnumerator HideAsync_PopsAndResumes() => UniTask.ToCoroutine(async () =>
        {
            _go1 = new GameObject("PanelA");
            _go2 = new GameObject("PanelB");
            var panelA = _go1.AddComponent<TestFullPanelA>();
            var panelB = _go2.AddComponent<TestFullPanelB>();

            _provider.Register(panelA);
            _provider.Register(panelB);

            var a = await UIManager.Instance.GetPanelAsync<TestFullPanelA>();
            await a.ShowSelfAsync();
            var b = await UIManager.Instance.GetPanelAsync<TestFullPanelB>();
            await b.ShowSelfAsync();

            Assert.AreEqual(BasePanel.PanelState.Paused, panelA.State);
            Assert.AreEqual(BasePanel.PanelState.Active, panelB.State);

            await UIManager.Instance.HideAsync();

            Assert.AreEqual(BasePanel.PanelState.Active, panelA.State);
            Assert.AreEqual(BasePanel.PanelState.Inactive, panelB.State);
        });

        /// <summary>
        /// 运行时切换 PanelProvider → 旧缓存自动迁移到新 Provider。
        /// </summary>
        [UnityTest]
        public IEnumerator PanelProviderSwap_CacheMigrated() => UniTask.ToCoroutine(async () =>
        {
            _go1 = new GameObject("TestPanel");
            var panel = _go1.AddComponent<TestFullPanel>();
            _provider.Register(panel);

            var loaded = await UIManager.Instance.GetPanelAsync<TestFullPanel>();
            await loaded.ShowSelfAsync();
            await UIManager.Instance.HideAsync();

            var newProvider = new TestPanelProvider();
            UIManager.PanelProvider = newProvider;

            Assert.IsTrue(newProvider.TryGet(typeof(TestFullPanel), out var cached));
            Assert.AreSame(panel, cached);
        });

        /// <summary>
        /// HideAsync() 返回被 Pop 的栈顶面板实例。
        /// </summary>
        [UnityTest]
        public IEnumerator HideAsync_ReturnsPoppedPanel() => UniTask.ToCoroutine(async () =>
        {
            _go1 = new GameObject("TestPanel");
            var panel = _go1.AddComponent<TestFullPanel>();
            _provider.Register(panel);

            var loaded = await UIManager.Instance.GetPanelAsync<TestFullPanel>();
            await loaded.ShowSelfAsync();
            var result = await UIManager.Instance.HideAsync();

            Assert.IsNotNull(result);
            Assert.AreSame(panel, result);
        });

        /// <summary>
        /// ABA: Push<A> → Push<B> → 重新注册 A → Push<A> → Push 检测 ABA 并拒绝，栈保持 [A, B]。
        /// </summary>
        [UnityTest]
        public IEnumerator PushAsync_ABA_Rejected() => UniTask.ToCoroutine(async () =>
        {
            _go1 = new GameObject("PanelA");
            _go2 = new GameObject("PanelB");
            var panelA = _go1.AddComponent<TestFullPanel>();
            var panelB = _go2.AddComponent<TestFullPanelA>();

            _provider.Register(panelA);
            _provider.Register(panelB);

            var a = await UIManager.Instance.GetPanelAsync<TestFullPanel>();
            await a.ShowSelfAsync();                              // [A]
            var b = await UIManager.Instance.GetPanelAsync<TestFullPanelA>();
            await b.ShowSelfAsync();                              // [A, B]

            // 重新注册 A 到 Provider（模拟 A 在缓存中可用）
            _provider.Register(panelA);
            LogAssert.Expect(LogType.Error, "[ViewStack] 不允许 ABA: TestFullPanel 已在栈中，不能重复 Push");
            var a2 = await UIManager.Instance.GetPanelAsync<TestFullPanel>();
            await a2.ShowSelfAsync();                             // ABA → Push 报错

            Assert.IsNotNull(a2, "GetPanel 成功返回 panel，但 Push 因 ABA 拒绝压栈");
            Assert.AreEqual(BasePanel.PanelState.Active, panelB.State, "B 仍在栈顶 Active");
        });
    }
}
