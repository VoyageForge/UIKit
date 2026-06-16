using UnityEditor;
using UnityEngine;
using VoyageForge.UIKit.Runtime;

namespace VoyageForge.UIKit.Editor
{
    [CustomEditor(typeof(SceneUIContext))]
    public class SceneUIContextEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var preplacedProp = serializedObject.FindProperty("_preplacedPanels");
            EditorGUILayout.PropertyField(preplacedProp, new GUIContent("预置 UI 面板"), true);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
