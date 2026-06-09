using UnityEditor;
using UnityEngine;
using VoyageForge.UIKit.Runtime;

namespace VoyageForge.UIKit.Editor
{
    [CustomPropertyDrawer(typeof(PreplacedPanelEntry))]
    public class PreplacedPanelEntryDrawer : PropertyDrawer
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
