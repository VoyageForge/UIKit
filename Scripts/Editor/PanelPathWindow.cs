using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VoyageForge.UIKit.Runtime;

namespace VoyageForge.UIKit.Editor
{
    public class PanelPathWindow : EditorWindow
    {
        [SerializeField] private List<PanelPathEntry> _entries = new();
        private ListView _listView;

        [MenuItem("Tools/UIKit/Panel Path Window")]
        public static void Open() => GetWindow<PanelPathWindow>("Panel Path");

        private void OnEnable() => RestoreEntries();
        private void OnDisable() => SaveEntries();

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/UI/Scripts/Editor/PanelPathWindow.uss"));

            // 工具栏
            var toolbar = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 8 } };
            toolbar.Add(new Button(() => { _entries.Add(new PanelPathEntry()); _listView.Rebuild(); }) { text = "+ 添加" });
            toolbar.Add(new Button(ApplyAll) { text = "全部应用" });
            root.Add(toolbar);

            // 列表
            _listView = new ListView(_entries, 52, MakeItem, BindItem)
            {
                selectionType = SelectionType.None,
                reorderable = false,
            };
            root.Add(_listView);
        }

        private VisualElement MakeItem()
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

            var prefabField = new ObjectField { objectType = typeof(GameObject), allowSceneObjects = false };
            prefabField.RegisterValueChangedCallback(OnPrefabChanged);
            prefabField.style.flexGrow = 1;
            prefabField.name = "prefab";
            row.Add(prefabField);

            var statusLabel = new Label { name = "status", style = { width = 36, unityTextAlign = TextAnchor.MiddleCenter } };
            row.Add(statusLabel);

            var applyBtn = new Button { text = "应用", name = "apply", style = { width = 50 } };
            applyBtn.clicked += () =>
            {
                int i = (int)applyBtn.userData;
                var e = _entries[i];
                if (e.Prefab == null) return;
                var panel = e.Prefab.GetComponent<BasePanel>();
                var path = GetResourcesPath(e.Prefab);
                if (panel != null && path != null)
                {
                    ApplyAttribute(panel.GetType(), path);
                    e.Applied = true;
                    _listView.Rebuild();
                    SaveEntries();
                }
            };
            row.Add(applyBtn);

            var removeBtn = new Button { text = "×", name = "remove", style = { width = 28 } };
            removeBtn.clicked += () =>
            {
                int i = (int)removeBtn.userData;
                _entries.RemoveAt(i);
                _listView.Rebuild();
                SaveEntries();
            };
            row.Add(removeBtn);

            return row;
        }

        private void BindItem(VisualElement el, int i)
        {
            var entry = _entries[i];

            var prefabField = el.Q<ObjectField>("prefab");
            prefabField.SetValueWithoutNotify(entry.Prefab);

            var statusLabel = el.Q<Label>("status");
            statusLabel.text = entry.Applied ? "✓" : "○";
            statusLabel.style.color = entry.Applied ? new Color(0.2f, 0.7f, 0.2f) : Color.gray;

            el.Q<Button>("apply").userData = i;
            el.Q<Button>("remove").userData = i;
        }

        private static void OnPrefabChanged(ChangeEvent<Object> evt)
        {
            var go = evt.newValue as GameObject;
            if (go == null) return;

            if (go.GetComponent<BasePanel>() == null)
            {
                Debug.LogWarning($"[UIKit] {go.name} 未挂载 BasePanel");
                (evt.target as ObjectField).value = null;
                return;
            }
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(go)))
            {
                Debug.LogWarning($"[UIKit] {go.name} 不是 Project 资产");
                (evt.target as ObjectField).value = null;
            }
        }

        private void ApplyAll()
        {
            foreach (var e in _entries)
            {
                if (e.Prefab == null) continue;
                var panel = e.Prefab.GetComponent<BasePanel>();
                var path = GetResourcesPath(e.Prefab);
                if (panel == null || path == null) continue;
                ApplyAttribute(panel.GetType(), path);
                e.Applied = true;
            }
            _listView.Rebuild();
            SaveEntries();
        }

        private void RestoreEntries()
        {
            var json = EditorPrefs.GetString("UIKit_PanelPath_Entries", "[]");
            var guids = JsonUtility.FromJson<GuidList>(json);
            _entries.Clear();
            foreach (var guid in guids.Items)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && prefab.GetComponent<BasePanel>() != null)
                    _entries.Add(new PanelPathEntry { Prefab = prefab, Applied = true });
            }
        }

        private void SaveEntries()
        {
            var guids = new List<string>();
            foreach (var e in _entries)
            {
                if (e.Prefab == null) continue;
                var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(e.Prefab));
                if (!string.IsNullOrEmpty(guid)) guids.Add(guid);
            }
            EditorPrefs.SetString("UIKit_PanelPath_Entries", JsonUtility.ToJson(new GuidList { Items = guids }));
        }

        private static string GetResourcesPath(GameObject prefab)
        {
            var assetPath = AssetDatabase.GetAssetPath(prefab);
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

            if (content.Contains(attr)) return;

            content = Regex.Replace(content, @"\[PanelPath\([^)]*\)\]\s*\n\s*", "");
            var pattern = $"(class\\s+{className}\\s*:)";
            content = Regex.Replace(content, pattern, $"{attr}\n    class {className} :");

            File.WriteAllText(scriptPath, content);
            AssetDatabase.Refresh();
            Debug.Log($"[UIKit] PanelPath applied: {className} → {resPath}");
        }

        [System.Serializable]
        private class GuidList { public List<string> Items = new(); }

        [System.Serializable]
        private class PanelPathEntry
        {
            public GameObject Prefab;
            public bool Applied;
        }
    }
}
