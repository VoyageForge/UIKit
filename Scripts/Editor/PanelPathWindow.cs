using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using VoyageForge.UIKit.Runtime;

namespace VoyageForge.UIKit.Editor
{
    /// <summary>
    /// 拖入 BasePanel prefab → 自动生成/更新 [PanelPath]。
    /// 菜单: Tools > UIKit > Panel Path Window
    /// </summary>
    public class PanelPathWindow : EditorWindow
    {
        private GameObject _prefab;
        private string _path;

        [MenuItem("Tools/UIKit/Panel Path Window")]
        public static void Open() => GetWindow<PanelPathWindow>("Panel Path");

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("拖入挂载了 BasePanel 的 Prefab，自动生成 [PanelPath] attribute", MessageType.Info);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            _prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", _prefab, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck() && _prefab != null)
            {
                var panel = _prefab.GetComponent<BasePanel>();
                if (panel == null)
                {
                    EditorUtility.DisplayDialog("错误", "该 prefab 未挂载 BasePanel 脚本", "确定");
                    _prefab = null;
                    _path = null;
                    return;
                }

                var assetPath = AssetDatabase.GetAssetPath(_prefab);
                if (string.IsNullOrEmpty(assetPath))
                {
                    EditorUtility.DisplayDialog("错误", "不是 Project 中的资产", "确定");
                    _prefab = null;
                    _path = null;
                    return;
                }

                _path = ExtractResourcesPath(assetPath);
                if (_path == null)
                {
                    EditorUtility.DisplayDialog("错误", "prefab 不在 Resources 目录下", "确定");
                    _prefab = null;
                    return;
                }
            }

            if (_prefab == null) return;

            EditorGUILayout.Space();
            var type = _prefab.GetComponent<BasePanel>().GetType();
            EditorGUILayout.LabelField("类型", type.Name);
            EditorGUILayout.LabelField("路径", _path);

            EditorGUILayout.Space();
            if (GUILayout.Button("应用 [PanelPath]", GUILayout.Height(30)))
            {
                ApplyAttribute(type, _path);
            }
        }

        private static string ExtractResourcesPath(string assetPath)
        {
            var match = Regex.Match(assetPath, @"Resources/(.+)\.prefab$");
            return match.Success ? match.Groups[1].Value : null;
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

            // 移除旧的 [PanelPath]
            content = Regex.Replace(content, @"\[PanelPath\([^)]*\)\]\s*\n\s*", "");

            // 在 class 声明前插入
            var pattern = $"(class\\s+{className}\\s*:)";
            content = Regex.Replace(content, pattern, $"{attr}\n    class {className} :");

            File.WriteAllText(scriptPath, content);
            AssetDatabase.Refresh();
            Debug.Log($"[UIKit] PanelPath applied: {className} → {resPath}");
        }
    }
}
