using UnityEditor;
using UnityEngine;
using VoyageForge.UIKit.Runtime;

namespace VoyageForge.UIKit.Editor
{
    /// <summary>
    /// SceneUI 组件的自定义 Inspector。展示预置面板条目列表。
    /// </summary>
    [CustomEditor(typeof(SceneUI))]
    public class SceneUIEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var entriesProp = serializedObject.FindProperty("_entries");
            EditorGUILayout.PropertyField(entriesProp, new GUIContent("Pre-placed UI Panels"), true);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
