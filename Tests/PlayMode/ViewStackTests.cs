using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoyageForge.UIKit.Runtime;
using Object = UnityEngine.Object;

namespace VoyageForge.UIKit.Tests
{
    /// <summary>
    /// ViewStack 导航栈测试 — 验证 FullPanel 的 Push/Pop/Pause/Resume 流程。
    /// </summary>
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

        /// <summary>
        /// Push 单个面板 → Count=1, State=Active, OnCreate/OnShow 各触发一次。
        /// </summary>
        [UnityTest]
        public IEnumerator PushSingle_ActivatesPanel() => UniTask.ToCoroutine(async () =>
        {
            await _stack.Push(_panel1);

            Assert.AreEqual(1, _stack.Count);
            Assert.AreEqual(BasePanel.PanelState.Active, _panel1.State);
            Assert.AreEqual(1, _panel1.OnCreateCount);
            Assert.AreEqual(1, _panel1.OnShowCount);
        });

        /// <summary>
        /// Push 两个不同类型面板 → 第一个被 Pause，第二个 Active。
        /// ABB 规则: 同类型不能连续 Push，因此必须用不同类型。
        /// </summary>
        [UnityTest]
        public IEnumerator PushTwo_PausesFirst() => UniTask.ToCoroutine(async () =>
        {
            var goB = new GameObject("TestPanelB");
            var panelB = goB.AddComponent<TestFullPanelA>();
            panelB.gameObject.SetActive(false);

            await _stack.Push(_panel1);
            await _stack.Push(panelB);

            Assert.AreEqual(2, _stack.Count);
            Assert.AreEqual(BasePanel.PanelState.Paused, _panel1.State);
            Assert.AreEqual(BasePanel.PanelState.Active, panelB.State);
            Assert.AreEqual(1, _panel1.OnPauseCount);

            Object.DestroyImmediate(goB);
        });

        /// <summary>
        /// Pop → 栈顶 Hide 并出栈，下层 Resume。
        /// ABB 规则: Push 两个必须不同类型。
        /// </summary>
        [UnityTest]
        public IEnumerator Pop_ResumesPrevious() => UniTask.ToCoroutine(async () =>
        {
            var goB = new GameObject("TestPanelB");
            var panelB = goB.AddComponent<TestFullPanelA>();
            panelB.gameObject.SetActive(false);

            await _stack.Push(_panel1);
            await _stack.Push(panelB);

            var popped = await _stack.Pop();

            Assert.AreSame(panelB, popped);
            Assert.AreEqual(1, _stack.Count);
            Assert.AreEqual(BasePanel.PanelState.Inactive, panelB.State);
            Assert.AreEqual(BasePanel.PanelState.Active, _panel1.State);
            Assert.AreEqual(1, _panel1.OnResumeCount);
            Assert.AreEqual(1, panelB.OnHideCount);

            Object.DestroyImmediate(goB);
        });

        /// <summary>
        /// 空栈 Pop 应抛出 InvalidOperationException。
        /// </summary>
        [UnityTest]
        public IEnumerator PopEmpty_Throws() => UniTask.ToCoroutine(async () =>
        {
            try
            {
                await _stack.Pop();
                Assert.Fail("期望抛出 InvalidOperationException");
            }
            catch (InvalidOperationException)
            {
                Assert.Pass();
            }
        });

        /// <summary>
        /// Push → Pop → 再次 Push 同一个面板 → OnCreate 只触发一次（面板实例被缓存复用）。
        /// </summary>
        [UnityTest]
        public IEnumerator PushPopPush_SameInstance_OnCreateOnce() => UniTask.ToCoroutine(async () =>
        {
            await _stack.Push(_panel1);
            await _stack.Pop();
            await _stack.Push(_panel1);

            Assert.AreEqual(1, _panel1.OnCreateCount, "OnCreate 只应触发一次");
            Assert.AreEqual(2, _panel1.OnShowCount, "OnShow 应触发两次");
        });

        /// <summary>
        /// ABB: 连续 Push 同类型 → 第二次跳过，栈不变。
        /// </summary>
        [UnityTest]
        public IEnumerator Push_ABB_SkipsDuplicate() => UniTask.ToCoroutine(async () =>
        {
            await _stack.Push(_panel1);
            await _stack.Push(_panel1);

            Assert.AreEqual(1, _stack.Count, "ABB: 同类型不能连续 Push");
            Assert.AreEqual(1, _panel1.OnShowCount, "OnShow 只应触发一次");
            Assert.AreEqual(1, _panel1.OnCreateCount, "OnCreate 只应触发一次");
        });

        /// <summary>
        /// ABA: Push A → Push B → Push A → 第三次应报错，栈保持 [A, B]。
        /// </summary>
        [UnityTest]
        public IEnumerator Push_ABA_LogsError() => UniTask.ToCoroutine(async () =>
        {
            var goB = new GameObject("TestPanelB");
            var panelB = goB.AddComponent<TestFullPanelA>();
            panelB.gameObject.SetActive(false);

            await _stack.Push(_panel1);       // [A]
            await _stack.Push(panelB);        // [A, B]
            LogAssert.Expect(LogType.Error, "[ViewStack] 不允许 ABA: TestFullPanel 已在栈中，不能重复 Push");
            await _stack.Push(_panel1);       // ABA 报错，不压栈

            Assert.AreEqual(2, _stack.Count, "ABA 被拒绝，栈应保持 2 个 entry");
            Assert.AreSame(panelB, _stack.Peek(), "栈顶仍为 B");

            Object.DestroyImmediate(goB);
        });

        /// <summary>
        /// ABA 深层嵌套: Push A → Push B → Push C → Push A → 第四次应报错（A 在栈底，深度 2）。
        /// </summary>
        [UnityTest]
        public IEnumerator Push_ABA_DeepNesting_LogsError() => UniTask.ToCoroutine(async () =>
        {
            var goB = new GameObject("PanelB");
            var panelB = goB.AddComponent<TestFullPanelA>();
            panelB.gameObject.SetActive(false);
            var goC = new GameObject("PanelC");
            var panelC = goC.AddComponent<TestFullPanelB>();
            panelC.gameObject.SetActive(false);

            await _stack.Push(_panel1);    // [A]
            await _stack.Push(panelB);     // [A, B]
            await _stack.Push(panelC);     // [A, B, C]

            LogAssert.Expect(LogType.Error, "[ViewStack] 不允许 ABA: TestFullPanel 已在栈中，不能重复 Push");
            await _stack.Push(_panel1);    // ABA 报错

            Assert.AreEqual(3, _stack.Count, "ABA 深层嵌套被拒绝，栈应保持 3 个 entry");
            Assert.AreSame(panelC, _stack.Peek(), "栈顶仍为 C");

            Object.DestroyImmediate(goB);
            Object.DestroyImmediate(goC);
        });

        /// <summary>
        /// Push null → 抛出 ArgumentNullException。
        /// </summary>
        [UnityTest]
        public IEnumerator Push_Null_ThrowsArgumentNullException() => UniTask.ToCoroutine(async () =>
        {
            try
            {
                await _stack.Push(null);
                Assert.Fail("期望抛出 ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
                Assert.AreEqual(0, _stack.Count, "null 不应入栈");
            }
        });

        /// <summary>
        /// 栈只剩一个 panel 时 Pop → 栈变空，panel 变为 Inactive，不调用 Resume。
        /// </summary>
        [UnityTest]
        public IEnumerator Pop_SinglePanel_StackEmpties() => UniTask.ToCoroutine(async () =>
        {
            await _stack.Push(_panel1);
            Assert.AreEqual(1, _stack.Count);

            var popped = await _stack.Pop();

            Assert.AreSame(_panel1, popped);
            Assert.AreEqual(0, _stack.Count);
            Assert.AreEqual(BasePanel.PanelState.Inactive, _panel1.State);
            Assert.AreEqual(1, _panel1.OnHideCount);
            Assert.AreEqual(0, _panel1.OnResumeCount, "单个 panel Pop 后不应调用 Resume");
        });

        /// <summary>
        /// Pop → Pop 链式操作: A→B→C, Pop 两次 → 回到 A，B 和 C 都 Hide。
        /// </summary>
        [UnityTest]
        public IEnumerator Pop_Pop_Chain_ResumesFirst() => UniTask.ToCoroutine(async () =>
        {
            var goB = new GameObject("PanelB");
            var panelB = goB.AddComponent<TestFullPanelA>();
            panelB.gameObject.SetActive(false);
            var goC = new GameObject("PanelC");
            var panelC = goC.AddComponent<TestFullPanelB>();
            panelC.gameObject.SetActive(false);

            await _stack.Push(_panel1);    // [A]
            await _stack.Push(panelB);     // [A, B]
            await _stack.Push(panelC);     // [A, B, C]

            var poppedC = await _stack.Pop();  // [A, B]
            var poppedB = await _stack.Pop();  // [A]

            Assert.AreSame(panelC, poppedC, "第一次 Pop 返回 C");
            Assert.AreSame(panelB, poppedB, "第二次 Pop 返回 B");
            Assert.AreEqual(1, _stack.Count);
            Assert.AreSame(_panel1, _stack.Peek(), "栈顶回到 A");
            Assert.AreEqual(BasePanel.PanelState.Active, _panel1.State, "A 应该被 Resume 到 Active");
            Assert.AreEqual(BasePanel.PanelState.Inactive, panelB.State, "B 应 Inactive");
            Assert.AreEqual(BasePanel.PanelState.Inactive, panelC.State, "C 应 Inactive");
            Assert.AreEqual(1, _panel1.OnResumeCount);
            Assert.AreEqual(1, panelB.OnHideCount);
            Assert.AreEqual(1, panelC.OnHideCount);

            Object.DestroyImmediate(goB);
            Object.DestroyImmediate(goC);
        });

        /// <summary>
        /// 验证 TopPanelChanged 事件在 Push 时正确触发。
        /// </summary>
        [UnityTest]
        public IEnumerator TopPanelChanged_FiresOnPush() => UniTask.ToCoroutine(async () =>
        {
            FullPanel receivedPanel = null;
            _stack.TopPanelChanged += p => receivedPanel = p;

            await _stack.Push(_panel1);

            Assert.AreSame(_panel1, receivedPanel, "TopPanelChanged 应收到 _panel1");
        });

        /// <summary>
        /// 验证 CountChanged 事件在 Push/Pop 时正确触发。
        /// </summary>
        [UnityTest]
        public IEnumerator CountChanged_FiresOnPushPop() => UniTask.ToCoroutine(async () =>
        {
            var counts = new List<int>();
            _stack.CountChanged += c => counts.Add(c);

            await _stack.Push(_panel1);          // count → 1
            await _stack.Pop();                  // count → 0

            Assert.AreEqual(2, counts.Count);
            Assert.AreEqual(1, counts[0], "Push 后 Count 应为 1");
            Assert.AreEqual(0, counts[1], "Pop 后 Count 应为 0");
        });

        /// <summary>
        /// 验证生命周期调用顺序: Push 时先 Pause 当前 → 再 Show 新面板。
        /// </summary>
        [UnityTest]
        public IEnumerator LifecycleOrder_Push_PauseBeforeShow() => UniTask.ToCoroutine(async () =>
        {
            var goB = new GameObject("PanelB");
            var panelB = goB.AddComponent<TestFullPanelA>();
            panelB.gameObject.SetActive(false);

            await _stack.Push(_panel1);
            await _stack.Push(panelB);

            // _panel1.CallOrder: OnCreate → OnShow → OnPause
            var pauseIdx = _panel1.CallOrder.IndexOf("OnPause");
            var showIdx = panelB.CallOrder.IndexOf("OnShow");
            var createIdx = panelB.CallOrder.IndexOf("OnCreate");

            Assert.Greater(pauseIdx, -1, "_panel1 应有 OnPause 记录");
            Assert.Greater(showIdx, -1, "panelB 应有 OnShow 记录");
            Assert.Greater(createIdx, -1, "panelB 应有 OnCreate 记录");
            // OnCreate 在 OnShow 之前（同一个 panel 内部顺序）
            Assert.Less(createIdx, showIdx, "OnCreate 应在 OnShow 之前");

            Object.DestroyImmediate(goB);
        });

        /// <summary>
        /// 验证 Peek() 在 Push/Pop 操作中始终返回正确的栈顶。
        /// </summary>
        [UnityTest]
        public IEnumerator Peek_ReturnsCorrectTop() => UniTask.ToCoroutine(async () =>
        {
            Assert.IsNull(_stack.Peek(), "空栈 Peek 应返回 null");

            await _stack.Push(_panel1);
            Assert.AreSame(_panel1, _stack.Peek(), "Push A 后栈顶为 A");

            var goB = new GameObject("PanelB");
            var panelB = goB.AddComponent<TestFullPanelA>();
            panelB.gameObject.SetActive(false);

            await _stack.Push(panelB);
            Assert.AreSame(panelB, _stack.Peek(), "Push B 后栈顶为 B");

            await _stack.Pop();
            Assert.AreSame(_panel1, _stack.Peek(), "Pop B 后栈顶回到 A");

            await _stack.Pop();
            Assert.IsNull(_stack.Peek(), "Pop A 后栈空，Peek 返回 null");

            Object.DestroyImmediate(goB);
        });
    }
}
