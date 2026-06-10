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
    /// UIManager 集成测试 — 验证 ShowAsync/HideAsync/PanelProvider 热切换。
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
        /// ShowAsync 应返回对应面板实例，State=Active。
        /// </summary>
        [UnityTest]
        public IEnumerator ShowAsync_ReturnsPanel_Active() => UniTask.ToCoroutine(async () =>
        {
            _go1 = new GameObject("TestPanel");
            var panel = _go1.AddComponent<TestFullPanel>();
            _provider.Register(panel);

            var result = await UIManager.Instance.ShowAsync<TestFullPanel>();

            Assert.IsNotNull(result);
            Assert.AreSame(panel, result);
            Assert.AreEqual(BasePanel.PanelState.Active, panel.State);
        });

        /// <summary>
        /// Show 两个不同类型面板 → HideAsync → 栈顶出栈，下层 Resume。
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

            await UIManager.Instance.ShowAsync<TestFullPanelA>();
            await UIManager.Instance.ShowAsync<TestFullPanelB>();

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

            await UIManager.Instance.ShowAsync<TestFullPanel>();
            await UIManager.Instance.HideAsync();

            var newProvider = new TestPanelProvider();
            UIManager.PanelProvider = newProvider;

            Assert.IsTrue(newProvider.TryGet(typeof(TestFullPanel), out var cached));
            Assert.AreSame(panel, cached);
        });
    }
}
