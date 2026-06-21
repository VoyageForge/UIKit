using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// 视图导航栈。管理 FullPanel 的 Push（压栈）和 Pop（出栈）。
    /// Push: Pause 当前栈顶 → Show 新面板 → 入栈。
    /// Pop:  Hide 栈顶 → 出栈 → Resume 下层。
    /// 导航规则：每个 FullPanel 类型在栈中最多出现一次（ABB 自动跳过，ABA 报错）。
    /// </summary>
    [Serializable]
    public class ViewStack : IDisposable
    {
        /// <summary>内部面板列表（栈底→栈顶）。</summary>
        [SerializeField] private List<FullPanel> _panels = new();

        /// <summary>栈顶面板变更事件。</summary>
        public event Action<FullPanel> TopPanelChanged;

        /// <summary>栈数量变更事件。</summary>
        public event Action<int> CountChanged;

        /// <summary>栈中面板数量。</summary>
        public int Count => _panels.Count;

        /// <summary>查看栈顶面板（不出栈），空栈返回 null。</summary>
        public FullPanel Peek() => _panels.Count > 0 ? _panels[^1] : null;

        public void Dispose()
        {
            TopPanelChanged = null;
            CountChanged = null;
        }

        /// <summary>
        /// 将面板压入栈顶并显示。
        /// ABB 检测：栈顶同类型 → 跳过不压栈。
        /// ABA 检测：栈内非栈顶已有同类型 → 报错不压栈。
        /// </summary>
        public async UniTask Push(FullPanel panel)
        {
            if (panel == null)
                throw new ArgumentNullException(nameof(panel), "[ViewStack] Cannot push null panel.");

            var t = panel.GetType();

            if (_panels.Count > 0)
            {
                // ABB: 栈顶同类型 → 跳过
                if (t == Peek().GetType())
                    return;

                // ABA: 栈内已有同类型（不在栈顶）→ 报错
                for (var i = 0; i < _panels.Count - 1; i++)
                {
                    if (_panels[i].GetType() == t)
                    {
                        Debug.LogError($"[ViewStack] ABA rejected: {t.Name} is already in the stack.");
                        return;
                    }
                }

                // Pause 当前栈顶
                await _panels[^1].Pause();
            }

            _panels.Add(panel);
            NotifyChanged();
            await panel.Show();
        }

        /// <summary>
        /// 弹出栈顶面板。Hide 栈顶 → 出栈 → Resume 新的栈顶。
        /// 空栈时抛出 InvalidOperationException。
        /// </summary>
        public async UniTask<FullPanel> Pop()
        {
            if (_panels.Count == 0)
                throw new InvalidOperationException();

            var exiting = _panels[^1];
            _panels.RemoveAt(_panels.Count - 1);
            NotifyChanged();
            await exiting.Hide();

            // Resume 新的栈顶
            if (_panels.Count > 0)
            {
                var last = _panels[^1];
                await last.Resume();
            }

            return exiting;
        }

        /// <summary>触发 TopPanelChanged 和 CountChanged 事件。</summary>
        private void NotifyChanged()
        {
            CountChanged?.Invoke(_panels.Count);
            TopPanelChanged?.Invoke(_panels.Count > 0 ? _panels[^1] : null);
        }
    }
}
