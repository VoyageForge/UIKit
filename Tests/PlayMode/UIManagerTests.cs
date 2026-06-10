using System.Collections;
using System.Reflection;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace VoyageForge.UIKit.Tests
{
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
                // MonoSingleton 静态字段重置
                ClearSingleton();
            }
        }

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

        [UnityTest]
        public IEnumerator HideAsync_PopsAndResumes() => UniTask.ToCoroutine(async () =>
        {
            _go1 = new GameObject("Panel1");
            _go2 = new GameObject("Panel2");
            var panel1 = _go1.AddComponent<TestFullPanel>();
            var panel2 = _go2.AddComponent<TestFullPanel>();

            _provider.Register(panel1);
            _provider.Register(panel2);

            await UIManager.Instance.ShowAsync<TestFullPanel>();
            await UIManager.Instance.ShowAsync<TestFullPanel>();

            Assert.AreEqual(BasePanel.PanelState.Paused, panel1.State);
            Assert.AreEqual(BasePanel.PanelState.Active, panel2.State);

            await UIManager.Instance.HideAsync();

            Assert.AreEqual(BasePanel.PanelState.Active, panel1.State);
            Assert.AreEqual(BasePanel.PanelState.Inactive, panel2.State);
        });

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
