using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using VoyageForge.UIKit.Runtime;

namespace VoyageForge.UIKit.Editor
{
    /// <summary>
    /// 监听 prefab 移动/重命名 → 自动更新 [PanelPath] attribute。
    /// </summary>
    public class PanelPathPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            for (int i = 0; i < movedAssets.Length; i++)
            {
                var newPath = movedAssets[i];
                var oldPath = movedFromAssetPaths[i];

                if (!newPath.EndsWith(".prefab")) continue;

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(newPath);
                if (prefab == null) continue;

                var panel = prefab.GetComponent<BasePanel>();
                if (panel == null) continue;

                var oldRes = GetResourcesPath(oldPath);
                var newRes = GetResourcesPath(newPath);
                if (newRes == null || newRes == oldRes) continue;

                UpdateAttribute(panel.GetType(), newRes);
            }
        }

        private static string GetResourcesPath(string assetPath)
        {
            var match = Regex.Match(assetPath, @"Resources/(.+)\.prefab$");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static void UpdateAttribute(System.Type type, string newResPath)
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
            var attr = $"[PanelPath(\"{newResPath}\")]";

            // 替换旧的 [PanelPath]
            content = Regex.Replace(content, @"\[PanelPath\([^)]*\)\]", attr);

            if (!content.Contains(attr))
            {
                // 没有旧的就插入
                var className = type.Name;
                var pattern = $"(class\\s+{className}\\s*:)";
                content = Regex.Replace(content, pattern, $"{attr}\n    class {className} :");
            }

            File.WriteAllText(scriptPath, content);
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log($"[UIKit] PanelPath updated (moved): {type.Name} → {newResPath}");
        }
    }
}
