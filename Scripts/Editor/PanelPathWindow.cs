using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using VoyageForge.UIKit.Runtime;

namespace VoyageForge.UIKit.Editor
{
    public class PanelPathWindow : EditorWindow
    {
        [SerializeField] private List<PanelPathEntry> _entries = new();
        private SerializedObject _so;
        private Vector2 _scroll;

        [MenuItem("Tools/UIKit/Panel Path Window")]
        public static void Open() => GetWindow<PanelPathWindow>("Panel Path");

        private void OnEnable() => _so = new SerializedObject(this);

        private void OnGUI()
        {
            _so.Update();
            var entriesProp = _so.FindProperty("_entries");

            EditorGUILayout.PropertyField(entriesProp, new GUIContent("Prefab 列表"), true);
            _so.ApplyModifiedProperties();

            EditorGUILayout.Space();
            if (GUILayout.Button("全部应用 [PanelPath]", GUILayout.Height(30)))
                ApplyAll();
        }

        private void ApplyAll()
        {
            foreach (var e in _entries)
            {
                if (e.Prefab == null) continue;
                var panel = e.Prefab.GetComponent<BasePanel>();
                if (panel == null) continue;
                var path = GetPath(e.Prefab);
                if (string.IsNullOrEmpty(path)) continue;
                WriteAttribute(panel.GetType(), path);
            }
            AssetDatabase.Refresh();
            Repaint();
        }

        private static string GetPath(GameObject prefab)
        {
            var assetPath = AssetDatabase.GetAssetPath(prefab);
            var m = Regex.Match(assetPath, @"Resources/(.+)\.prefab$");
            return m.Success ? m.Groups[1].Value : Path.GetFileNameWithoutExtension(assetPath);
        }

        private static void WriteAttribute(System.Type type, string path)
        {
            var guids = AssetDatabase.FindAssets($"{type.Name} t:MonoScript");
            string scriptPath = null;
            foreach (var g in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (p.EndsWith($"/{type.Name}.cs")) { scriptPath = p; break; }
            }
            if (scriptPath == null) return;

            var content = File.ReadAllText(scriptPath);
            var attr = $"[PanelPath(\"{path}\")]";
            if (content.Contains(attr)) return;

            content = Regex.Replace(content, @"\[PanelPath\([^)]*\)\]\s*\n\s*", "");
            content = Regex.Replace(content, $"(class\\s+{type.Name}\\s*:)",
                $"{attr}\n    class {type.Name} :");

            File.WriteAllText(scriptPath, content);
            Debug.Log($"[UIKit] PanelPath: {type.Name} → {path}");
        }

        [System.Serializable]
        private class PanelPathEntry
        {
            public GameObject Prefab;
        }
    }
}
