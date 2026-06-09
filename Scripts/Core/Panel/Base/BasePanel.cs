using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// UI Panel 基类 — Show/Hide/Close 生命周期。
    /// 首次 Show 触发 OnCreate → 每次 Show 触发 OnShow。
    /// Hide 触发 OnHide（缓存）；Close 触发 OnClose（销毁）。
    /// </summary>
    public abstract class BasePanel : MonoBehaviour
    {
        public enum PanelState
        {
            Inactive, // 未激活
            Active, // 显示中
            Paused, // 暂停（仅 FullPanel）
            Exiting // 关闭中
        }

        public event Action OnShowed; // Show 后
        public event Action OnHided; // Hide 后
        public event Action OnClosed; // Close 后（销毁前）

        private bool _created;

        private protected PanelState _state = PanelState.Inactive;

        public PanelState State => _state;

        // ---- Internal API ----

        /// <summary> 显示面板。首次触发 OnCreate，之后每次触发 OnShow。 </summary>
        internal async UniTask Show()
        {
            if (_state == PanelState.Active) return;
            _state = PanelState.Active;
            gameObject.SetActive(true);

            if (!_created)
            {
                _created = true;
                await OnCreate();
            }

            await OnShow();
            OnShowed?.Invoke();
        }

        /// <summary> 隐藏面板并回池。 </summary>
        internal async UniTask Hide()
        {
            if (_state != PanelState.Active && _state != PanelState.Paused) return;
            _state = PanelState.Inactive;
            await OnHide();
            OnHided?.Invoke();
        }

        /// <summary> 彻底关闭并销毁。 </summary>
        internal async UniTask Close()
        {
            _state = PanelState.Exiting;
            await OnClose();
            OnClosed?.Invoke();
            Destroy(gameObject);
        }

        // ---- 可覆写生命周期 ----

        /// <summary> 首次显示时调用一次。子类在此获取组件引用。 </summary>
        protected virtual UniTask OnCreate() => UniTask.CompletedTask;

        /// <summary> 每次显示时调用。子类在此刷新数据。 </summary>
        protected virtual UniTask OnShow() => UniTask.CompletedTask;

        /// <summary> 隐藏时调用。子类在此保存状态。 </summary>
        protected virtual UniTask OnHide() => UniTask.CompletedTask;

        /// <summary> 彻底关闭时调用。子类在此注销事件、清理资源。 </summary>
        protected virtual UniTask OnClose() => UniTask.CompletedTask;

        // ---- 输入 ----

        public virtual bool OnInput(KeyCode key, bool down)
        {
            if (!down) return false;
            if (key == KeyCode.Escape)
            {
                UIManager.Instance.Hide();
                return true;
            }

            return false;
        }
    }
}