using UnityEditor;
using UnityEngine;
using VoyageForge.UIKit.Runtime;

namespace VoyageForge.UIKit.Editor
{
    /// <summary>
    /// SceneUIEntry 的自定义 PropertyDrawer。直接在 Inspector 行内绘制 Panel 字段引用。
    /// </summary>
    [CustomPropertyDrawer(typeof(SceneUIEntry))]
    public class SceneUIEntryDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var panelProp = property.FindPropertyRelative("Panel");
            EditorGUI.PropertyField(position, panelProp, label);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
