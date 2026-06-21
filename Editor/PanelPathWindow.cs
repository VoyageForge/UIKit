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
    /// <summary>
    /// Panel Path 编辑器工具窗口。拖入 prefab，一键批量生成 [PanelPath("...")] 特性到对应的 Panel 类上。
    /// 通过 VoyageForge > UIKit > Panel Path Window 菜单打开。
    /// </summary>
    public class PanelPathWindow : EditorWindow
    {
        /// <summary>拖入的 prefab 列表。</summary>
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

            EditorGUILayout.PropertyField(entriesProp, new GUIContent("Prefabs"), true);
            _so.ApplyModifiedProperties();

            EditorGUILayout.Space();
            if (GUILayout.Button("Apply [PanelPath] to All", GUILayout.Height(30)))
                ApplyAll();
        }

        /// <summary>对所有 prefab 执行 PanelPath 特性写入。</summary>
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

        /// <summary>获取 prefab 的 Asset 路径（去除扩展名）。</summary>
        private static string GetPath(GameObject prefab)
        {
            return Path.ChangeExtension(AssetDatabase.GetAssetPath(prefab), null);
        }

        /// <summary>
        /// 向指定类型的 .cs 源文件写入 [PanelPath] 特性。
        /// 自动定位 class 声明行，在前面插入特性，已有的旧特性会被移除。
        /// </summary>
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
            // 移除已有的旧 PanelPath 特性
            lines.RemoveAll(l => l.TrimStart().StartsWith("[PanelPath("));
            for (int i = 0; i < lines.Count; i++)
            {
                if (!lines[i].Contains($"class {type.Name}")) continue;
                var indent = new string(' ', lines[i].Length - lines[i].TrimStart().Length);
                lines.Insert(i, indent + attr);
                break;
            }

            File.WriteAllText(scriptPath, string.Join("\n", lines));
            Debug.Log($"[UIKit] PanelPath: {type.Name} -> {path}");
        }
    }
}
