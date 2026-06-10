using System;
using Cysharp.Threading.Tasks;

namespace VoyageForge.UIKit.Runtime
{
    public interface IPopupManager : IDisposable
    {
        /// <summary>
        /// 显示弹窗
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        UniTask<T> ShowAsync<T>() where T : PopupPanel;
        
        /// <summary>
        /// 显示弹窗
        /// </summary>
        /// <param name="panel"></param>
        /// <returns></returns>
        UniTask ShowAsync(PopupPanel panel);

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