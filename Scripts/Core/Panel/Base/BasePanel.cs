using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VoyageForge.UIKit.Runtime
{
    /// <summary>
    /// UI Panel 抽象基类。管理 Show/Hide/Close 生命周期和 CanvasGroup 可见性。
    /// 首次 Show 触发 OnCreate（仅一次），之后每次 Show 触发 OnShow。
    /// Hide 触发 OnHide（回池缓存），Close 触发 OnClose（销毁 GameObject）。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BasePanel : MonoBehaviour
    {
        /// <summary>面板生命周期状态。</summary>
        public enum PanelState
        {
            /// <summary>未激活（初始状态或 Hide 后）。</summary>
            Inactive,
            /// <summary>显示中。</summary>
            Active,
            /// <summary>暂停（仅 FullPanel，被其他面板覆盖时）。</summary>
            Paused,
            /// <summary>关闭中（即将 Destroy）。</summary>
            Exiting
        }

        /// <summary>面板显示后触发。</summary>
        public event Action OnShowed;

        /// <summary>面板隐藏后触发。</summary>
        public event Action OnHided;

        /// <summary>面板关闭销毁后触发。</summary>
        public event Action OnClosed;

        /// <summary>是否已完成首次 OnCreate。</summary>
        private bool _created;

        /// <summary>面板绑定的 CanvasGroup 组件。用于控制可见性和交互。</summary>
        public CanvasGroup CanvasGroup { get; private set; }

        /// <summary>当前面板状态。</summary>
        private protected PanelState _state = PanelState.Inactive;

        /// <summary>获取当前面板状态。</summary>
        public PanelState State => _state;

        protected virtual void Awake()
        {
            CanvasGroup = GetComponent<CanvasGroup>();
        }

        // ---- Internal API（由 ViewStack / PopupManager 调用） ----

        /// <summary>显示面板。首次触发 OnCreate → OnShow，非首次只触发 OnShow。Active 状态下重复调用无效果。</summary>
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

            SetCanvasGroupVisible(true);
            await OnShow();
            OnShowed?.Invoke();
        }

        /// <summary>隐藏面板。设置 Inactive 状态 → 关闭 CanvasGroup 可见性 → OnHide。</summary>
        internal async UniTask Hide()
        {
            if (_state != PanelState.Active && _state != PanelState.Paused) return;
            _state = PanelState.Inactive;

            SetCanvasGroupVisible(false);
            await OnHide();
            OnHided?.Invoke();
        }

        /// <summary>关闭面板。设置 Exiting 状态 → OnClose → Destroy(gameObject)。</summary>
        internal async UniTask Close()
        {
            _state = PanelState.Exiting;
            await OnClose();
            OnClosed?.Invoke();
            Destroy(gameObject);
        }

        /// <summary>设置 CanvasGroup 的可见性（alpha / 射线 / 交互）。</summary>
        protected void SetCanvasGroupVisible(bool visible)
        {
            CanvasGroup.alpha = visible ? 1f : 0f;
            CanvasGroup.blocksRaycasts = visible;
            CanvasGroup.interactable = visible;
        }

        // ---- 可覆写生命周期方法 ----

        /// <summary>首次显示时调用一次。子类在此获取组件引用、注册事件。</summary>
        protected virtual UniTask OnCreate() => UniTask.CompletedTask;

        /// <summary>每次显示时调用。子类在此刷新 UI 数据。</summary>
        protected virtual UniTask OnShow() => UniTask.CompletedTask;

        /// <summary>隐藏时调用。子类在此保存状态。</summary>
        protected virtual UniTask OnHide() => UniTask.CompletedTask;

        /// <summary>关闭销毁时调用。子类在此注销事件、释放资源。</summary>
        protected virtual UniTask OnClose() => UniTask.CompletedTask;

        /// <summary>
        /// 输入事件处理。默认实现：Escape 键触发导航回退（PopAsync）。
        /// 子类可覆写以处理自定义按键。返回 true 表示事件已消费。
        /// </summary>
        public virtual bool OnInput(KeyCode key, bool down)
        {
            if (!down) return false;
            if (key == KeyCode.Escape)
            {
                UIManager.Panel.PopAsync().Forget();
                return true;
            }

            return false;
        }
    }
}
