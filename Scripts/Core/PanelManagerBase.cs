using System;
using Cysharp.Threading.Tasks;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// Panel 管理器泛型基类。统一管理 Provider 的持有、热切换（含缓存迁移），以及异步加载面板的方法。
    /// FullPanelManager（T=FullPanel）和 PopupManager（T=PopupPanel）均继承自此基类。
    /// 子类只需实现 ShowAsync/HideAsync/CloseAsync/Dispose 四个抽象方法即可获得完整的 Provider 管理能力。
    /// </summary>
    public abstract class PanelManagerBase<T> where T : BasePanel
    {
        /// <summary>当前的面板加载代理。通过此 Provider 执行 LoadAsync 创建面板实例。</summary>
        private IPanelProvider _provider;

        /// <summary>构造时可注入默认 Provider，子类通过 base(defaultProvider) 设置初始加载器。</summary>
        protected PanelManagerBase(IPanelProvider defaultProvider = null)
        {
            _provider = defaultProvider;
        }

        /// <summary>
        /// 面板加载代理。设值时自动执行缓存迁移：旧 Provider 的缓存导出并导入到新 Provider。
        /// 设为 null 会被忽略。
        /// </summary>
        public IPanelProvider Provider
        {
            get => _provider;
            set
            {
                if (value == null) return;
                MigrateCache(_provider, value);
                _provider = value;
                OnProviderChanged();
            }
        }

        /// <summary>从旧 Provider 导出缓存并导入到新 Provider。</summary>
        private static void MigrateCache(IPanelProvider from, IPanelProvider to)
        {
            if (from != null) to.Import(from.Export());
        }

        /// <summary>
        /// 异步加载指定类型的面板（不自动显示）。从 Provider 获取实例，走缓存或新建。
        /// 约束 TResult 必须是当前 T（FullPanel 或 PopupPanel）的子类型。
        /// </summary>
        public async UniTask<TResult> GetAsync<TResult>() where TResult : T
        {
            return await _provider.LoadAsync<TResult>();
        }

        /// <summary>显示面板。具体行为由子类定义（FullPanelManager 压栈，PopupManager 叠加）。</summary>
        public abstract UniTask ShowAsync(T panel);

        /// <summary>隐藏面板。具体行为由子类定义。</summary>
        public abstract UniTask HideAsync(T panel);

        /// <summary>关闭并销毁面板。具体行为由子类定义。</summary>
        public abstract UniTask CloseAsync(T panel);

        /// <summary>释放资源，清理内部状态。</summary>
        public abstract void Dispose();

        /// <summary>Provider 切换后的钩子。PopupManager 覆写此方法以 Reparent 所有活跃弹窗。</summary>
        protected virtual void OnProviderChanged() { }
    }
}
