using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// Panel 提供器统一接口。以 Type 为 key 管理面板实例的加载、缓存与迁移。
    /// FullPanelManager 和 PopupManager 各自持有一个 IPanelProvider 实例作为加载代理。
    /// </summary>
    public interface IPanelProvider
    {
        /// <summary>异步加载指定类型的面板实例。优先从缓存返回，缓存未命中则走 InstantiateAsync 创建。</summary>
        UniTask<T> LoadAsync<T>() where T : BasePanel;

        /// <summary>回收面板到缓存池（Hide 后调用）。</summary>
        void Release<T>(T panel) where T : BasePanel;

        /// <summary>将已有面板实例注册到缓存（场景预置面板用）。</summary>
        void Register<T>(T panel) where T : BasePanel;

        /// <summary>尝试从缓存获取指定类型的面板。</summary>
        bool TryGet(Type type, out BasePanel panel);

        /// <summary>从缓存中移除指定类型（不销毁面板）。</summary>
        void Remove(Type type);

        /// <summary>从缓存中移除指定面板实例（Show 时由 Manager 调用，不销毁面板）。</summary>
        void Remove(BasePanel panel);

        /// <summary>清空全部缓存（不销毁面板）。</summary>
        void Clear();

        /// <summary>导出全部缓存数据，用于 Provider 热切换时迁移缓存。</summary>
        Dictionary<Type, object> Export();

        /// <summary>导入缓存数据，用于 Provider 热切换时接收迁移。</summary>
        void Import(Dictionary<Type, object> data);
    }
}
