using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// FullPanel 管理器。继承 PanelManagerBase，负责 FullPanel 的导航栈管理。
    /// 默认使用 ResourcesProvider 作为加载代理。每个 FullPanel 类型在栈中最多出现一次（ABA 报错，ABB 跳过）。
    /// </summary>
    public class FullPanelManager : PanelManagerBase<FullPanel>
    {
        /// <summary>导航栈。管理 FullPanel 的 Push/Pop/Pause/Resume。</summary>
        private ViewStack _stack = new();

        /// <summary>当前场景挂载的 SceneUI 引用，用于场景卸载时清理。</summary>
        private SceneUI _sceneUI;

        /// <summary>构造函数，默认使用 ResourcesProvider 加载 FullPanel 预制体。</summary>
        public FullPanelManager() : base(new ResourcesProvider()) { }

        /// <summary>
        /// 异步加载指定类型的 FullPanel（不自动显示）。
        /// 语义别名，等同于 GetAsync&lt;T&gt;()。
        /// </summary>
        public async UniTask<T> GetPanel<T>() where T : FullPanel => await GetAsync<T>();

        /// <summary>查看栈顶面板（不出栈）。</summary>
        public FullPanel Peek() => _stack.Peek();

        /// <summary>导航栈中的面板数量。</summary>
        public int Count => _stack.Count;

        /// <summary>获取当前活跃面板（栈顶），空栈返回 null。</summary>
        public FullPanel GetActivePanel() => Count > 0 ? Peek() : null;

        /// <summary>将 FullPanel 压入导航栈并显示。Show 时从缓存取出，Pause 当前栈顶。</summary>
        public async UniTask PushAsync(FullPanel panel)
        {
            if (panel == null) return;
            Provider.Remove(panel);
            await _stack.Push(panel);
        }

        /// <summary>弹出栈顶面板。Hide 当前 → 出栈 → Resume 下层 → Release 回缓存。</summary>
        public async UniTask<FullPanel> PopAsync()
        {
            var panel = await _stack.Pop();
            Provider.Release(panel);
            return panel;
        }

        /// <summary>显示面板（等同于 PushAsync）。</summary>
        public override async UniTask ShowAsync(FullPanel panel) => await PushAsync(panel);

        /// <summary>
        /// 隐藏指定 FullPanel。若为栈顶则执行 PopAsync；若为非栈顶则只 Hide 并 Resume 栈顶。
        /// </summary>
        public override async UniTask HideAsync(FullPanel panel)
        {
            var top = _stack.Peek();

            if (top == panel)
            {
                await PopAsync();
            }
            else
            {
                await panel.Hide();
                if (top != null) await top.Resume();
                Provider.Register(panel);
            }
        }

        /// <summary>关闭指定 FullPanel。确保缓存清理，若在栈顶则先出栈，再执行 Close 销毁。</summary>
        public override async UniTask CloseAsync(FullPanel panel)
        {
            Provider.Remove(panel);
            if (_stack.Peek() == panel) await _stack.Pop();
            await panel.Close();
        }

        /// <summary>释放导航栈资源。</summary>
        public override void Dispose() => _stack?.Dispose();

        /// <summary>
        /// 注册场景预置的 UI 面板。由 SceneUI.Start 调用。
        /// 遍历 SceneUI.Entries：已激活的面板压栈显示，未激活的注册到 Provider 缓存。
        /// </summary>
        public async UniTask RegisterSceneUI(SceneUI sceneUI)
        {
            _sceneUI = sceneUI;
            foreach (var entry in sceneUI.Entries)
            {
                if (entry.Panel == null) continue;
                if (entry.Panel.gameObject.activeSelf)
                    await PushAsync(entry.Panel);
                else
                    Provider.Register(entry.Panel);
            }
        }

        /// <summary>
        /// 注销场景预置面板。由 SceneUI.OnDestroy 调用。
        /// 从 Provider 缓存移除所有预置面板类型，并将仍在栈中的预置面板出栈。
        /// </summary>
        public async UniTask UnregisterSceneUI()
        {
            if (_sceneUI == null) return;

            foreach (var entry in _sceneUI.Entries)
                Provider.Remove(entry.Panel.GetType());

            if (_stack != null)
                while (_stack.Count > 0)
                {
                    var panel = _stack.Peek();
                    if (panel == null) break;
                    if (_sceneUI.Entries.Any(p => p.Panel == panel))
                        await _stack.Pop();
                    else
                        return;
                }
        }

        /// <summary>
        /// 输入路由。将按键转发给栈顶面板处理。
        /// 返回 true 表示事件已消费（阻止 Escape 回退链继续传递）。
        /// </summary>
        public bool OnInput(KeyCode key, bool down)
        {
            var top = _stack.Peek();
            return top != null && top.State == BasePanel.PanelState.Active && top.OnInput(key, down);
        }
    }
}
