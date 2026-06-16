using System;
using Cysharp.Threading.Tasks;

namespace VoyageForge.UIKit.Runtime
{
    public interface IPopupManager : IDisposable
    {
        /// <summary>
        /// 从 Provider 加载弹窗（不自动显示）。
        /// </summary>
        UniTask<T> GetPopup<T>() where T : PopupPanel;

        /// <summary>
        /// 显示弹窗实例。
        /// </summary>
        UniTask ShowPopupAsync(PopupPanel panel);

        /// <summary>
        /// 隐藏弹窗
        /// </summary>
        /// <param name="panel"></param>
        /// <returns></returns>
        UniTask HideAsync(PopupPanel panel);

        /// <summary>
        /// 关闭弹窗
        /// </summary>
        /// <param name="panel"></param>
        /// <returns></returns>
        UniTask CloseAsync(PopupPanel panel);
    }
}