using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using VoyageForge.UIKit.Runtime;

namespace VoyageForge.UIKit.Editor
{
    public class PanelPathWindow : EditorWindow
    {
        [SerializeField] private List<GameObject> _prefabs = new();
        private SerializedObject _so;
        private Vector2 _scroll;

        [MenuItem("VoyageForge/UIKit/Panel Path Window")]
        public static void Open() => GetWindow<PanelPathWindow>("Panel Path");

        private void OnEnable() => _so = new SerializedObject(this);

        private void OnGUI()
        {
            _so.Update();
            var entriesProp = _so.FindProperty("_prefabs");

            EditorGUILayout.PropertyField(entriesProp, new GUIContent("Prefab 列表"), true);
            _so.ApplyModifiedProperties();

            EditorGUILayout.Space();
            if (GUILayout.Button("全部应用 [PanelPath]", GUILayout.Height(30)))
                ApplyAll();
        }

        private void ApplyAll()
        {
            foreach (var prefab in _prefabs)
            {
                if (prefab == null) continue;
                var panel = prefab.GetComponent<BasePanel>();
                if (panel == null) continue;
                var path = GetPath(prefab);
                if (string.IsNullOrEmpty(path)) continue;
                WriteAttribute(panel.GetType(), path);
            }

            AssetDatabase.Refresh();
            Repaint();
        }

        private static string GetPath(GameObject prefab)
        {
            return Path.ChangeExtension(AssetDatabase.GetAssetPath(prefab), null);
        }

        private static void WriteAttribute(System.Type type, string path)
        {
            var guids = AssetDatabase.FindAssets($"{type.Name} t:MonoScript");
            string scriptPath = null;
            foreach (var g in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (p.EndsWith($"/{type.Name}.cs"))
                {
                    scriptPath = p;
                    break;
                }
            }

            if (scriptPath == null) return;

            var content = File.ReadAllText(scriptPath);
            var attr = $"[PanelPath(\"{path}\")]";
            if (content.Contains(attr)) return;

            var lines = new List<string>(content.Split('\n'));
            lines.RemoveAll(l => l.TrimStart().StartsWith("[PanelPath("));
            for (int i = 0; i < lines.Count; i++)
            {
                if (!lines[i].Contains($"class {type.Name}")) continue;
                var indent = new string(' ', lines[i].Length - lines[i].TrimStart().Length);
                lines.Insert(i, indent + attr);
                break;
            }

            File.WriteAllText(scriptPath, string.Join("\n", lines));
            Debug.Log($"[UIKit] PanelPath: {type.Name} → {path}");
        }
    }
}