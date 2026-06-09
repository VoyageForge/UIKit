using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using VoyageForge.UIKit.Runtime;

namespace VoyageForge.UIKit.Editor
{
    /// <summary>
    /// 托管 BasePanel prefab → [PanelPath] 的列表窗口。
    /// 拖入多个 prefab，自动检测路径，批量应用/更新 attribute。
    /// 菜单: Tools > UIKit > Panel Path Window
    /// </summary>
    public class PanelPathWindow : EditorWindow
    {
        private List<PanelPathEntry> _entries = new();
        private Vector2 _scroll;

        [MenuItem("Tools/UIKit/Panel Path Window")]
        public static void Open() => GetWindow<PanelPathWindow>("Panel Path");

        private void OnEnable()
        {
            // 恢复已追踪的 prefab
            var tracked = EditorPrefs.GetString("UIKit_PanelPath_Prefabs", "");
            if (!string.IsNullOrEmpty(tracked))
            {
                foreach (var guid in tracked.Split(','))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null && prefab.GetComponent<BasePanel>() != null)
                        _entries.Add(new PanelPathEntry { Prefab = prefab, Applied = true });
                }
            }
        }

        private void OnDisable()
        {
            SaveTrackedPrefabs();
        }

        private void SaveTrackedPrefabs()
        {
            var guids = new List<string>();
            foreach (var e in _entries)
            {
                if (e.Prefab == null) continue;
                var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(e.Prefab));
                if (!string.IsNullOrEmpty(guid)) guids.Add(guid);
            }
            EditorPrefs.SetString("UIKit_PanelPath_Prefabs", string.Join(",", guids));
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "拖入挂载了 BasePanel 的 Prefab，点击应用自动写入 [PanelPath]。\n移动 prefab 后回到此窗口会自动更新路径。",
                MessageType.Info);

            if (GUILayout.Button("+ 添加")) _entries.Add(new PanelPathEntry());
            if (_entries.Count > 0 && GUILayout.Button("全部应用")) ApplyAll();

            EditorGUILayout.Space();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            for (int i = 0; i < _entries.Count; i++)
            {
                DrawEntry(i);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawEntry(int i)
        {
            var entry = _entries[i];
            EditorGUILayout.BeginVertical("box");

            // Prefab 字段 + 验证
            EditorGUI.BeginChangeCheck();
            entry.Prefab = (GameObject)EditorGUILayout.ObjectField(
                $"Prefab {i + 1}", entry.Prefab, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck())
            {
                entry.Applied = false;
                ValidateEntry(entry);
            }

            if (entry.Prefab == null)
            {
                if (GUILayout.Button("移除", GUILayout.Width(60)))
                    _entries.RemoveAt(i);
                EditorGUILayout.EndVertical();
                return;
            }

            // 信息展示
            var type = entry.Prefab.GetComponent<BasePanel>()?.GetType();
            var path = GetPrefabPath(entry.Prefab);

            EditorGUILayout.LabelField("  类型", type?.Name ?? "-");
            EditorGUILayout.LabelField("  路径", path ?? "-");
            if (path == null)
                EditorGUILayout.LabelField("  提示", "不在 Resources 下，需自行保证加载路径正确");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(entry.Applied ? "✓ 已应用" : "○ 未应用",
                entry.Applied ? new GUIStyle(EditorStyles.label) { normal = { textColor = Color.green } } : EditorStyles.label);

            if (type != null && path != null)
            {
                if (GUILayout.Button("应用", GUILayout.Width(60)))
                {
                    ApplyAttribute(type, path);
                    entry.Applied = true;
                    SaveTrackedPrefabs();
                }
            }

            if (GUILayout.Button("移除", GUILayout.Width(60)))
            {
                _entries.RemoveAt(i);
                SaveTrackedPrefabs();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private static void ValidateEntry(PanelPathEntry entry)
        {
            if (entry.Prefab == null) return;
            if (entry.Prefab.GetComponent<BasePanel>() == null)
            {
                Debug.LogWarning($"[UIKit] {entry.Prefab.name} 未挂载 BasePanel");
                entry.Prefab = null;
                return;
            }
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(entry.Prefab)))
            {
                Debug.LogWarning($"[UIKit] {entry.Prefab.name} 不是 Project 资产");
                entry.Prefab = null;
            }
        }

        private static string GetPrefabPath(GameObject prefab)
        {
            var assetPath = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(assetPath)) return null;
            var match = Regex.Match(assetPath, @"Resources/(.+)\.prefab$");
            return match.Success ? match.Groups[1].Value : null;
        }

        private void ApplyAll()
        {
            foreach (var entry in _entries)
            {
                if (entry.Prefab == null) continue;
                var panel = entry.Prefab.GetComponent<BasePanel>();
                if (panel == null) continue;
                var path = GetPrefabPath(entry.Prefab);
                if (path == null) continue;

                ApplyAttribute(panel.GetType(), path);
                entry.Applied = true;
            }
            SaveTrackedPrefabs();
        }

        private static void ApplyAttribute(System.Type type, string resPath)
        {
            var guids = AssetDatabase.FindAssets($"{type.Name} t:MonoScript");
            string scriptPath = null;
            foreach (var g in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (p.EndsWith($"{type.Name}.cs")) { scriptPath = p; break; }
            }
            if (scriptPath == null) return;

            var content = File.ReadAllText(scriptPath);
            var className = type.Name;
            var attr = $"[PanelPath(\"{resPath}\")]";

            if (content.Contains(attr))
            {
                Debug.Log($"[UIKit] {className} already has correct PanelPath.");
                return;
            }

            content = Regex.Replace(content, @"\[PanelPath\([^)]*\)\]\s*\n\s*", "");
            var pattern = $"(class\\s+{className}\\s*:)";
            content = Regex.Replace(content, pattern, $"{attr}\n    class {className} :");

            File.WriteAllText(scriptPath, content);
            AssetDatabase.Refresh();
            Debug.Log($"[UIKit] PanelPath applied: {className} → {resPath}");
        }

        private class PanelPathEntry
        {
            public GameObject Prefab;
            public bool Applied;
        }
    }
}
