using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

// ============================================================
//  Prefab Manager Editor Window
//  管理项目中的预制体，按文件夹分类，支持拖拽导入、重命名、
//  双击聚焦，暗色主题，列表斑马纹。
// ============================================================

/// <summary>
/// 预制体管理编辑器窗口。按文件夹树结构组织 prefab，支持拖拽导入、
/// 双击聚焦到 Project 窗口、重命名文件夹，暗色主题带斑马纹交替行。
/// 通过 Window > Prefab Manager 菜单打开。
/// </summary>
public class PrefabManagerWindow : EditorWindow
{
    // ---------- Data Model ----------

    /// <summary>文件夹节点（树结构）。支持无限层级嵌套。</summary>
    private class FolderNode
    {
        public string id;
        public string name;
        public List<FolderNode> children = new List<FolderNode>();
        public FolderNode parent;
        /// <summary>该文件夹下的预制体列表。</summary>
        public List<PrefabEntry> prefabs = new List<PrefabEntry>();
    }

    /// <summary>预制体条目。存储 Asset GUID（跨会话稳定）和对象引用（用于聚焦）。</summary>
    private class PrefabEntry
    {
        public string guid;
        public string name;
        public Object prefab;
    }

    // ---------- Data ----------

    /// <summary>文件夹树根节点。</summary>
    private FolderNode rootFolder;
    /// <summary>当前选中的文件夹（右侧列表展示其内容）。</summary>
    private FolderNode currentFolder;
    /// <summary>树展开状态记录。</summary>
    private HashSet<string> expandedFolders = new HashSet<string>();

    // ---------- UI Elements ----------

    private TreeView folderTreeView;
    private Label folderInfoLabel;
    private Label folderCountLabel;
    /// <summary>右侧列表容器（包含 ScrollView 和 drag overlay）。</summary>
    private VisualElement listContainer;
    private Button newFolderButton;

    [SerializeField] private VisualTreeAsset visualTree;
    [SerializeField] private VisualTreeAsset itemTemplate;

    private VisualElement newFolderRow;
    private TextField newFolderInput;
    private bool isCreatingFolder = false;

    // ---------- Window Entry ----------

    [MenuItem("Window/Prefab Manager")]
    public static void ShowWindow()
    {
        var window = GetWindow<PrefabManagerWindow>();
        window.titleContent = new GUIContent("Prefab Manager");
        window.minSize = new Vector2(600, 400);
        window.Show();
    }

    // ============================================================
    //  Initialization
    // ============================================================

    private void OnEnable()
    {
        InitData();

        if (visualTree == null)
        {
            Debug.LogError("VisualTreeAsset not assigned. Drag PrefabManagerWindow.uxml into the visualTree field.");
            return;
        }

        visualTree.CloneTree(rootVisualElement);

        folderTreeView = rootVisualElement.Q<TreeView>("folderTreeView");
        folderInfoLabel = rootVisualElement.Q<Label>("folderInfo");
        folderCountLabel = rootVisualElement.Q<Label>("folderCount");
        listContainer = rootVisualElement.Q<VisualElement>("listContainer");
        newFolderButton = rootVisualElement.Q<Button>("newFolderButton");

        if (folderTreeView == null || listContainer == null)
        {
            Debug.LogError("Missing key elements in UXML. Check that names match.");
            return;
        }

        listContainer.pickingMode = PickingMode.Position;

        if (newFolderButton != null)
            newFolderButton.clicked += OnNewFolderClicked;

        folderTreeView.selectionChanged += OnTreeSelectionChanged;
        folderTreeView.itemsChosen += OnTreeItemChosen;

        expandedFolders.Add(rootFolder.id);
        currentFolder = rootFolder;
        RefreshTreeView();
        RefreshListView();
        SetupDragDrop();

        if (itemTemplate == null)
            Debug.LogWarning("itemTemplate not assigned. List items will be built via code (slightly lower perf).");
    }

    /// <summary>初始化示例数据。实际使用时改为从 AssetDatabase 扫描。</summary>
    private void InitData()
    {
        rootFolder = new FolderNode { id = "root", name = "Root" };

        var docs = new FolderNode { id = "docs", name = "Documents", parent = rootFolder };
        rootFolder.children.Add(docs);
        var design = new FolderNode { id = "design", name = "Designs", parent = docs };
        docs.children.Add(design);
        var requirements = new FolderNode { id = "req", name = "Requirements", parent = docs };
        docs.children.Add(requirements);

        var resources = new FolderNode { id = "resources", name = "Resources", parent = rootFolder };
        rootFolder.children.Add(resources);
        var images = new FolderNode { id = "images", name = "Images", parent = resources };
        resources.children.Add(images);
        var sounds = new FolderNode { id = "sounds", name = "Audio", parent = resources };
        resources.children.Add(sounds);

        AddPrefabToFolder(design, "Assets/UI/Prefabs/Button.prefab");
        AddPrefabToFolder(design, "Assets/UI/Prefabs/Panel.prefab");

        currentFolder = rootFolder;
    }

    /// <summary>向指定文件夹添加一个 prefab 条目。</summary>
    private void AddPrefabToFolder(FolderNode folder, string assetPath)
    {
        var obj = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (obj == null) return;
        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        folder.prefabs.Add(new PrefabEntry { guid = guid, name = obj.name, prefab = obj });
    }

    // ============================================================
    //  Tree View (左侧文件夹树)
    // ============================================================

    /// <summary>刷新左侧文件夹树视图。从 rootFolder 递归构建 TreeView 数据。</summary>
    private void RefreshTreeView()
    {
        if (folderTreeView == null) return;

        var treeData = new List<TreeViewItemData<FolderNode>>();
        BuildTreeData(rootFolder, treeData, 0);
        folderTreeView.SetRootItems(treeData);

        folderTreeView.makeItem = () => new Label();
        folderTreeView.bindItem = (element, index) =>
        {
            var itemData = folderTreeView.GetItemDataForIndex<FolderNode>(index);
            (element as Label).text = itemData.name;
        };

        folderTreeView.RefreshItems();
        folderTreeView.ExpandAll();

        if (currentFolder != null)
            folderTreeView.SetSelectionById(currentFolder.id.GetHashCode());

        if (folderCountLabel != null)
            folderCountLabel.text = CountAllNodes(rootFolder).ToString();
    }

    /// <summary>递归计算文件夹树节点总数。</summary>
    private int CountAllNodes(FolderNode node)
    {
        int count = 1;
        foreach (var child in node.children)
            count += CountAllNodes(child);
        return count;
    }

    /// <summary>递归构建 TreeView 的树数据。</summary>
    private void BuildTreeData(FolderNode node, List<TreeViewItemData<FolderNode>> list, int depth)
    {
        var item = new TreeViewItemData<FolderNode>(
            node.id.GetHashCode(),
            node,
            node.children.Select(c =>
            {
                var childList = new List<TreeViewItemData<FolderNode>>();
                BuildTreeData(c, childList, depth + 1);
                return childList.Count > 0 ? childList[0] : new TreeViewItemData<FolderNode>(c.id.GetHashCode(), c);
            }).ToList()
        );
        list.Add(item);
    }

    // ---------- Tree Events ----------

    /// <summary>单击树节点 → 切换当前文件夹并刷新右侧列表。</summary>
    private void OnTreeSelectionChanged(IEnumerable<object> selectedItems)
    {
        var first = selectedItems.FirstOrDefault();
        if (first is FolderNode node && node != currentFolder)
        {
            currentFolder = node;
            RefreshListView();
            RefreshTreeView();
        }
    }

    /// <summary>双击树节点（或按回车）→ 弹出重命名对话框（根目录除外）。</summary>
    private void OnTreeItemChosen(IEnumerable<object> chosenItems)
    {
        var first = chosenItems.FirstOrDefault();
        if (first is FolderNode node && node.id != "root")
            ShowRenameDialog(node);
    }

    // ---------- Rename Dialog ----------

    /// <summary>弹出内联重命名对话框窗口。</summary>
    private void ShowRenameDialog(FolderNode node)
    {
        var window = EditorWindow.GetWindow<RenamerWindow>(true, "Rename Folder", true);
        window.Init(node, (newName) =>
        {
            if (!string.IsNullOrEmpty(newName))
            {
                node.name = newName;
                RefreshTreeView();
                RefreshListView();
            }
        });
        window.Show();
    }

    /// <summary>重命名对话框内部窗口类。</summary>
    private class RenamerWindow : EditorWindow
    {
        private string newName;
        private FolderNode targetNode;
        private System.Action<string> onRename;

        public void Init(FolderNode node, System.Action<string> callback)
        {
            targetNode = node;
            newName = node.name;
            onRename = callback;
            minSize = new Vector2(300, 80);
            maxSize = new Vector2(400, 100);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            newName = EditorGUILayout.TextField("New Name", newName);
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("OK"))
            {
                onRename?.Invoke(newName);
                Close();
            }

            if (GUILayout.Button("Cancel"))
                Close();
            EditorGUILayout.EndHorizontal();
        }
    }

    // ============================================================
    //  List View (右侧预制体列表)
    // ============================================================

    /// <summary>
    /// 刷新右侧预制体列表。使用 ScrollView + 手动构建以获得精确的行间距和斑马纹效果。
    /// </summary>
    private void RefreshListView()
    {
        if (currentFolder == null) return;

        var oldScrollView = listContainer.Q<ScrollView>("prefabScrollView");
        if (oldScrollView != null)
            oldScrollView.RemoveFromHierarchy();

        var scrollView = new ScrollView();
        scrollView.name = "prefabScrollView";
        scrollView.style.flexGrow = 1;
        scrollView.style.paddingTop = 4;
        scrollView.style.paddingBottom = 4;

        var contentContainer = new VisualElement();
        contentContainer.style.flexDirection = FlexDirection.Column;
        contentContainer.style.width = Length.Percent(100);

        int index = 0;
        foreach (var entry in currentFolder.prefabs)
        {
            VisualElement item = itemTemplate.CloneTree();

            if (item is TemplateContainer container && container.childCount > 0)
                item = container[0];

            item.style.height = 44;
            item.style.marginBottom = 4;
            item.style.paddingLeft = 8;
            item.style.paddingRight = 8;

            // 斑马纹：偶数行深灰，奇数行稍浅
            bool isEven = (index % 2 == 0);
            item.userData = isEven;
            if (isEven)
                item.style.backgroundColor = new Color(0.27f, 0.27f, 0.27f);
            else
                item.style.backgroundColor = new Color(0.20f, 0.20f, 0.20f);

            var icon = item.Q<Image>("item-icon");
            var label = item.Q<Label>("item-name");
            label.text = entry.name;

            // 获取预制体缩略图
            Texture2D preview = null;
            if (entry.prefab != null)
            {
                preview = AssetPreview.GetAssetPreview(entry.prefab);
                if (preview == null)
                    preview = AssetPreview.GetMiniThumbnail(entry.prefab);
            }
            else if (!string.IsNullOrEmpty(entry.guid))
            {
                var path = AssetDatabase.GUIDToAssetPath(entry.guid);
                var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (obj != null)
                {
                    preview = AssetPreview.GetAssetPreview(obj);
                    if (preview == null)
                        preview = AssetPreview.GetMiniThumbnail(obj);
                }
            }
            icon.image = preview;

            // 点击 → 聚焦到 Project 窗口并选中
            item.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (entry.prefab != null)
                {
                    EditorGUIUtility.PingObject(entry.prefab);
                    Selection.activeObject = entry.prefab;
                }
                else if (!string.IsNullOrEmpty(entry.guid))
                {
                    var path = AssetDatabase.GUIDToAssetPath(entry.guid);
                    var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                    if (obj != null)
                    {
                        EditorGUIUtility.PingObject(obj);
                        Selection.activeObject = obj;
                    }
                }
            });

            // 悬停高亮效果
            item.RegisterCallback<MouseEnterEvent>(evt =>
            {
                item.style.backgroundColor = new Color(0.34f, 0.34f, 0.34f);
            });
            item.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                bool even = (bool)item.userData;
                if (even)
                    item.style.backgroundColor = new Color(0.27f, 0.27f, 0.27f);
                else
                    item.style.backgroundColor = new Color(0.20f, 0.20f, 0.20f);
            });

            contentContainer.Add(item);
            index++;
        }

        // 空列表提示
        if (index == 0)
        {
            var emptyLabel = new Label("No prefabs in this folder");
            emptyLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
            emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            emptyLabel.style.fontSize = 16;
            emptyLabel.style.paddingTop = 40;
            contentContainer.Add(emptyLabel);
        }

        scrollView.Add(contentContainer);
        listContainer.Add(scrollView);

        if (folderInfoLabel != null)
            folderInfoLabel.text = $"{currentFolder.name} · {currentFolder.prefabs.Count} items";
    }

    // ============================================================
    //  New Folder
    // ============================================================

    /// <summary>点击新建文件夹按钮。在列表顶部插入一个内联输入行。</summary>
    private void OnNewFolderClicked()
    {
        if (isCreatingFolder || currentFolder == null) return;
        isCreatingFolder = true;

        newFolderRow = new VisualElement();
        newFolderRow.AddToClassList("new-folder-row");
        newFolderRow.style.width = Length.Percent(100);

        var icon = new Label("[F]");
        icon.AddToClassList("icon");
        newFolderRow.Add(icon);

        newFolderInput = new TextField();
        newFolderInput.AddToClassList("input");
        newFolderInput.focusable = true;
        newFolderInput.style.maxWidth = Length.Percent(100);
        newFolderInput.style.flexShrink = 1;
        // Enter 确认，Escape 取消
        newFolderInput.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                ConfirmNewFolder();
            else if (evt.keyCode == KeyCode.Escape)
                CancelNewFolder();
        });
        newFolderRow.Add(newFolderInput);

        var confirmBtn = new Button(ConfirmNewFolder) { text = "✔" };
        confirmBtn.AddToClassList("confirm-btn");
        newFolderRow.Add(confirmBtn);

        var cancelBtn = new Button(CancelNewFolder) { text = "✖" };
        cancelBtn.AddToClassList("cancel-btn");
        newFolderRow.Add(cancelBtn);

        listContainer.Insert(0, newFolderRow);
        newFolderInput.Focus();
        newFolderInput.SelectAll();
    }

    /// <summary>确认创建文件夹。</summary>
    private void ConfirmNewFolder()
    {
        string name = newFolderInput.value.Trim();
        if (string.IsNullOrEmpty(name))
        {
            EditorUtility.DisplayDialog("Notice", "Folder name cannot be empty.", "OK");
            return;
        }

        var newFolder = new FolderNode
        {
            id = "folder_" + System.Guid.NewGuid().ToString().Substring(0, 8),
            name = name,
            parent = currentFolder
        };
        currentFolder.children.Add(newFolder);

        CleanupNewFolderRow();

        RefreshTreeView();
        currentFolder = newFolder;
        RefreshListView();
        RefreshTreeView();

        isCreatingFolder = false;
    }

    /// <summary>取消创建文件夹。</summary>
    private void CancelNewFolder()
    {
        CleanupNewFolderRow();
        isCreatingFolder = false;
    }

    /// <summary>移除内联输入行。</summary>
    private void CleanupNewFolderRow()
    {
        if (newFolderRow != null && newFolderRow.parent != null)
        {
            newFolderRow.parent.Remove(newFolderRow);
            newFolderRow = null;
            newFolderInput = null;
        }
    }

    // ============================================================
    //  Drag & Drop
    // ============================================================

    /// <summary>
    /// 设置拖拽事件。支持从 Project 窗口拖拽预制体到列表区域来导入。
    /// 使用 TrickleDown 确保在冒泡前捕获事件。
    /// </summary>
    private void SetupDragDrop()
    {
        if (listContainer == null) return;

        listContainer.pickingMode = PickingMode.Position;

        listContainer.RegisterCallback<DragEnterEvent>(evt =>
        {
            listContainer.EnableInClassList("drag-over", true);
            evt.StopPropagation();
        }, TrickleDown.TrickleDown);

        listContainer.RegisterCallback<DragLeaveEvent>(evt =>
        {
            listContainer.EnableInClassList("drag-over", false);
            evt.StopPropagation();
        }, TrickleDown.TrickleDown);

        listContainer.RegisterCallback<DragUpdatedEvent>(evt =>
        {
            bool hasPrefab = false;
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is GameObject go)
                {
                    string path = AssetDatabase.GetAssetPath(go);
                    if (!string.IsNullOrEmpty(path) &&
                        path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
                    {
                        hasPrefab = true;
                        break;
                    }
                }
            }

            DragAndDrop.visualMode = hasPrefab ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
            evt.StopPropagation();
        }, TrickleDown.TrickleDown);

        listContainer.RegisterCallback<DragPerformEvent>(evt =>
        {
            DragAndDrop.AcceptDrag();

            listContainer.EnableInClassList("drag-over", false);

            if (currentFolder == null)
            {
                evt.StopPropagation();
                return;
            }

            bool added = false;
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is GameObject go)
                {
                    string path = AssetDatabase.GetAssetPath(go);
                    if (!string.IsNullOrEmpty(path) &&
                        path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
                    {
                        var entry = new PrefabEntry
                        {
                            guid = AssetDatabase.AssetPathToGUID(path),
                            name = go.name,
                            prefab = go
                        };
                        currentFolder.prefabs.Add(entry);
                        added = true;
                    }
                }
            }

            if (added)
            {
                RefreshListView();
                RefreshTreeView();
            }

            evt.StopPropagation();
        }, TrickleDown.TrickleDown);
    }

    private void OnRefresh()
    {
        RefreshTreeView();
        RefreshListView();
    }

    private void OnDisable()
    {
    }
}
