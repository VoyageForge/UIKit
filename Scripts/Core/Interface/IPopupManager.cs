using System;
using Cysharp.Threading.Tasks;

namespace VoyageForge.UIKit.Runtime
{
    public interface IPopupManager : IDisposable
    {
        UniTask<T> ShowAsync<T>() where T : PopupPanel;
        UniTask ShowAsync(PopupPanel panel);
        UniTask HideAsync<T>() where T : PopupPanel;
        UniTask HideAsync(PopupPanel panel);
        UniTask CloseAsync<T>() where T : PopupPanel;
        UniTask CloseAsync(PopupPanel panel);
        bool IsShowing<T>() where T : PopupPanel;
    }
}
