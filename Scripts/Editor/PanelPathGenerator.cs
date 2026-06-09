using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using VoyageForge.UIKit.Runtime;

namespace VoyageForge.UIKit.Editor
{
    /// <summary>
    /// 扫描 Resources 下所有 BasePanel prefab，自动生成 [PanelPath] attribute。
    /// 菜单: Tools > UIKit > Generate PanelPath Attributes
    /// </summary>
    public static class PanelPathGenerator
    {
        [MenuItem("Tools/UIKit/Generate PanelPath Attributes")]
        public static void Generate()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            int count = 0;

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab == null) continue;

                var panel = prefab.GetComponent<BasePanel>();
                if (panel == null) continue;

                // 提取 Resources 相对路径
                var resPath = ExtractResourcesPath(assetPath);
                if (resPath == null) continue;

                var type = panel.GetType();
                var scriptPath = FindScriptPath(type);
                if (scriptPath == null) continue;

                // 读脚本内容
                var content = File.ReadAllText(scriptPath);

                // 检查是否已有 [PanelPath]
                if (content.Contains($"[PanelPath(\"{resPath}\")]"))
                    continue;

                // 注入 attribute
                var className = type.Name;
                var pattern = $"(class\\s+{className}\\s*:)";
                var newContent = Regex.Replace(content, pattern, $"[PanelPath(\"{resPath}\")]\n    class {className} :");

                if (newContent != content)
                {
                    File.WriteAllText(scriptPath, newContent);
                    count++;
                    Debug.Log($"[UIKit] PanelPath: {className} → {resPath}");
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[UIKit] Generated {count} PanelPath attributes.");
        }

        private static string ExtractResourcesPath(string assetPath)
        {
            var match = Regex.Match(assetPath, @"Resources/(.+)\.prefab$");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static string FindScriptPath(System.Type type)
        {
            var guids = AssetDatabase.FindAssets($"{type.Name} t:MonoScript");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith($"{type.Name}.cs"))
                    return path;
            }
            return null;
        }
    }
}
