using UnityEngine;
using VoyageForge.UIKit.Runtime;

namespace VoyageForge.UIKit.Samples
{
    /// <summary>
    /// 示例输入处理 — 把所有按键转发给 UIManager，由活跃面板自己决定怎么处理。
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
