using UnityEngine;
using VoyageForge.UIKit.Runtime;

namespace VoyageForge.UIKit.Samples
{
    /// <summary>
    /// 示例输入处理器。将所有按键事件转发给 UIManager，
    /// 由当前活跃面板自行决定如何处理。
    /// 支持的按键：Enter, Escape, Y, N, 方向键, Tab。
    /// </summary>
    public class UIInputHandler : MonoBehaviour
    {
        private void Update()
        {
            var ui = UIManager.Instance;
            if (ui == null) return;

            foreach (var key in _keys)
            {
                if (Input.GetKeyDown(key))
                    ui.OnInput(key, true);
                if (Input.GetKeyUp(key))
                    ui.OnInput(key, false);
            }
        }

        /// <summary>需要监听的按键列表。</summary>
        private static readonly KeyCode[] _keys =
        {
            KeyCode.Return, KeyCode.KeypadEnter, KeyCode.Escape,
            KeyCode.Y, KeyCode.N,
            KeyCode.UpArrow, KeyCode.DownArrow,
            KeyCode.LeftArrow, KeyCode.RightArrow,
            KeyCode.Tab,
        };
    }
}
