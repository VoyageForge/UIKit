using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// 视图导航栈 — 管理 FullPanel 进出栈。
    /// Push: Pause 当前 → Show 新面板 → 入栈
    /// Pop:  Hide 栈顶 → 出栈 → Resume 下层
    /// </summary>
    [Serializable]
    public class ViewStack : IDisposable
    {
        [SerializeField] private List<FullPanel> _panels = new();

        public event Action<FullPanel> TopPanelChanged;
        public event Action<int> CountChanged;

        public int Count => _panels.Count;

        /// <summary> 查看栈顶面板。 </summary>
        public FullPanel Peek() => _panels.Count > 0 ? _panels[^1] : null;

        public void Dispose()
        {
            TopPanelChanged = null;
            CountChanged = null;
        }

        /// <summary> 推入：暂停当前 → 入栈 → Show。 </summary>
        public async UniTask Push(FullPanel panel)
        {
            if (panel == null)
                throw new ArgumentNullException(nameof(panel), "[ViewStack] 不能 Push null 面板。");

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
                        Debug.LogError($"[ViewStack] 不允许 ABA: {t.Name} 已在栈中，不能重复 Push");
                        return;
                    }
                }

                await _panels[^1].Pause();
            }

            _panels.Add(panel);
            NotifyChanged();
            await panel.Show();
        }

        /// <summary> 弹出：Hide → 出栈 → Resume 下层。 </summary>
        public async UniTask<FullPanel> Pop()
        {
            if (_panels.Count == 0)
                throw new InvalidOperationException();

            var exiting = _panels[^1];
            _panels.RemoveAt(_panels.Count - 1);
            NotifyChanged();
            await exiting.Hide();

            if (_panels.Count > 0)
            {
                var last = _panels[^1];

                await last.Resume();
            }


            return exiting;
        }

        private void NotifyChanged()
        {
            CountChanged?.Invoke(_panels.Count);
            TopPanelChanged?.Invoke(_panels.Count > 0 ? _panels[^1] : null);
        }
    }
}