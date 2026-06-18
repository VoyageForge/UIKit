using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

// ============================================================
//  Prefab Manager Editor Window
//  功能：管理项目中的预制体，按文件夹分类，支持拖拽导入、重命名、
//        双击聚焦，暗色主题，列表斑马纹。
//  作者：xxx
//  版本：1.0
// ============================================================

public class PrefabManagerWindow : EditorWindow
{
    // ---------- 数据模型 ----------
    /// <summary>
    /// 文件夹节点（树结构）
    /// </summary>
    private class FolderNode
    {
        public string id;
        public string name;
        public List<FolderNode> children = new List<FolderNode>();
        public FolderNode parent;
        public List<PrefabEntry> prefabs = new List<PrefabEntry>(); // 该文件夹下的预制体列表
    }

    /// <summary>
    /// 预制体条目（存储 GUID 和对象引用）
    /// </summary>
    private class PrefabEntry
    {
        public string guid; // 预制体的 Asset GUID
        public string name; // 显示名称
        public Object prefab; // 直接引用（用于聚焦）
    }

    // ---------- 数据 ----------
    private FolderNode rootFolder;
    private FolderNode currentFolder; // 当前选中的文件夹
    private HashSet<string> expandedFolders = new HashSet<string>(); // 记录树展开状态

    // ---------- UI 元素 ----------
    private TreeView folderTreeView;
    private Label folderInfoLabel;
    private Label folderCountLabel;
    private VisualElement listContainer; // 右侧列表容器（包含 scrollView 和 dragOverlay）
    private Button newFolderButton;

    [SerializeField] private VisualTreeAsset visualTree; // 主布局 UXML
    [SerializeField] private VisualTreeAsset itemTemplate; // 列表项模板 UXML

    // 新建文件夹输入行相关
    private VisualElement newFolderRow;
    private TextField newFolderInput;
    private bool isCreatingFolder = false;

    // ---------- 窗口入口 ----------
    [MenuItem("Window/Prefab Manager")]
    public static void ShowWindow()
    {
        var window = GetWindow<PrefabManagerWindow>();
        window.titleContent = new GUIContent("Prefab Manager");
        window.minSize = new Vector2(600, 400);
        window.Show();
    }

    // ============================================================
    //  初始化
    // ============================================================
    private void OnEnable()
    {
        // 1. 初始化示例数据（实际使用时改为从 AssetDatabase 扫描）
        InitData();

        // 2. 加载主布局
        if (visualTree == null)
        {
            Debug.LogError("VisualTreeAsset 未赋值，请将 PrefabManagerWindow.uxml 拖入 Inspector 的 visualTree 字段。");
            return;
        }

        visualTree.CloneTree(rootVisualElement);

        // 3. 获取 UI 元素
        folderTreeView = rootVisualElement.Q<TreeView>("folderTreeView");
        folderInfoLabel = rootVisualElement.Q<Label>("folderInfo");
        folderCountLabel = rootVisualElement.Q<Label>("folderCount");
        listContainer = rootVisualElement.Q<VisualElement>("listContainer");
        newFolderButton = rootVisualElement.Q<Button>("newFolderButton");

        if (folderTreeView == null || listContainer == null)
        {
            Debug.LogError("UXML 中缺少关键元素，请检查名称是否匹配。");
            return;
        }

        // 确保 listContainer 可接收拖拽
        listContainer.pickingMode = PickingMode.Position;

        // 4. 绑定事件
        if (newFolderButton != null)
            newFolderButton.clicked += OnNewFolderClicked;

        folderTreeView.selectionChanged += OnTreeSelectionChanged; // 单击切换目录
        folderTreeView.itemsChosen += OnTreeItemChosen; // 双击重命名

        // 5. 初始状态
        expandedFolders.Add(rootFolder.id);
        currentFolder = rootFolder;
        RefreshTreeView();
        RefreshListView();
        SetupDragDrop();

        // 6. 加载列表项模板（若未赋值则报错，但可降级使用代码构建）
        if (itemTemplate == null)
            Debug.LogWarning("itemTemplate 未赋值，将使用代码动态构建列表项（性能略低）。");
    }

    // ---------- 数据初始化（示例） ----------
    private void InitData()
    {
        rootFolder = new FolderNode { id = "root", name = "根目录" };

        var docs = new FolderNode { id = "docs", name = "项目文档", parent = rootFolder };
        rootFolder.children.Add(docs);
        var design = new FolderNode { id = "design", name = "设计稿", parent = docs };
        docs.children.Add(design);
        var requirements = new FolderNode { id = "req", name = "需求文档", parent = docs };
        docs.children.Add(requirements);

        var resources = new FolderNode { id = "resources", name = "资源文件", parent = rootFolder };
        rootFolder.children.Add(resources);
        var images = new FolderNode { id = "images", name = "图片", parent = resources };
        resources.children.Add(images);
        var sounds = new FolderNode { id = "sounds", name = "音效", parent = resources };
        resources.children.Add(sounds);

        // 示例预制体（请确保这些路径存在，或修改为项目中的实际预制体）
        AddPrefabToFolder(design, "Assets/UI/Prefabs/Button.prefab");
        AddPrefabToFolder(design, "Assets/UI/Prefabs/Panel.prefab");

        currentFolder = rootFolder;
    }

    private void AddPrefabToFolder(FolderNode folder, string assetPath)
    {
        var obj = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (obj == null) return;
        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        folder.prefabs.Add(new PrefabEntry { guid = guid, name = obj.name, prefab = obj });
    }

    // ============================================================
    //  树视图（左侧文件夹树）
    // ============================================================
    private void RefreshTreeView()
    {
        if (folderTreeView == null) return;

        // 构建树数据
        var treeData = new List<TreeViewItemData<FolderNode>>();
        BuildTreeData(rootFolder, treeData, 0);
        folderTreeView.SetRootItems(treeData);

        // 设置显示模板（每个节点用一个 Label 显示名称）
        folderTreeView.makeItem = () => new Label();
        folderTreeView.bindItem = (element, index) =>
        {
            var itemData = folderTreeView.GetItemDataForIndex<FolderNode>(index);
            (element as Label).text = itemData.name;
        };

        folderTreeView.RefreshItems();
        folderTreeView.ExpandAll(); // 展开所有（可根据 expandedFolders 控制）

        // 高亮当前选中
        if (currentFolder != null)
            folderTreeView.SetSelectionById(currentFolder.id.GetHashCode());

        // 更新文件夹总数
        if (folderCountLabel != null)
            folderCountLabel.text = CountAllNodes(rootFolder).ToString();
    }

    private int CountAllNodes(FolderNode node)
    {
        int count = 1;
        foreach (var child in node.children)
            count += CountAllNodes(child);
        return count;
    }

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

    // ---------- 树事件 ----------
    /// <summary>单击树节点 → 切换当前文件夹并刷新列表</summary>
    private void OnTreeSelectionChanged(IEnumerable<object> selectedItems)
    {
        var first = selectedItems.FirstOrDefault();
        if (first is FolderNode node && node != currentFolder)
        {
            currentFolder = node;
            RefreshListView();
            RefreshTreeView(); // 更新高亮
        }
    }

    /// <summary>双击树节点（或按回车）→ 重命名文件夹（根目录除外）</summary>
    private void OnTreeItemChosen(IEnumerable<object> chosenItems)
    {
        var first = chosenItems.FirstOrDefault();
        if (first is FolderNode node && node.id != "root")
            ShowRenameDialog(node);
    }

    // ---------- 重命名对话框 ----------
    private void ShowRenameDialog(FolderNode node)
    {
        var window = EditorWindow.GetWindow<RenamerWindow>(true, "重命名文件夹", true);
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
            newName = EditorGUILayout.TextField("新名称", newName);
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("确定"))
            {
                onRename?.Invoke(newName);
                Close();
            }

            if (GUILayout.Button("取消"))
                Close();
            EditorGUILayout.EndHorizontal();
        }
    }

    // ============================================================
    //  列表视图（右侧预制体列表）
    // ============================================================
    /// <summary>
    /// 刷新列表：使用 ScrollView + 手动构建，以精确控制间距和斑马纹。
    /// 坑点：
    ///   1. ListView 的 margin 无法产生行间距，需用 ScrollView 手动构建。
    ///   2. 斑马纹闭包问题：将奇偶标志存入 item.userData，避免循环变量引用错误。
    ///   3. 样式兼容性：borderRadius、borderWidth 等需用独立属性（如 borderTopLeftRadius）。
    /// </summary>
   private void RefreshListView()
{
    if (currentFolder == null) return;

    // 移除旧的 ScrollView
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
        // 克隆模板
        VisualElement item = itemTemplate.CloneTree();

        // 处理 TemplateContainer 包装（如果有）
        if (item is TemplateContainer container && container.childCount > 0)
            item = container[0];

        // ---------- 强制样式（确保间距和圆角生效） ----------
        item.style.height = 44;
        item.style.marginBottom = 4;
        item.style.paddingLeft = 8;
        item.style.paddingRight = 8;

        // ---------- 斑马纹（增强对比度） ----------
        bool isEven = (index % 2 == 0);
        item.userData = isEven;
        if (isEven)
            item.style.backgroundColor = new Color(0.27f, 0.27f, 0.27f); // #454545
        else
            item.style.backgroundColor = new Color(0.20f, 0.20f, 0.20f); // #333333

        // ---------- 绑定数据 ----------
        var icon = item.Q<Image>("item-icon");
        var label = item.Q<Label>("item-name");
        label.text = entry.name;

        // 获取缩略图
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

        // ---------- 点击聚焦 ----------
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

        // ---------- 悬停效果 ----------
        item.RegisterCallback<MouseEnterEvent>(evt =>
        {
            item.style.backgroundColor = new Color(0.34f, 0.34f, 0.34f); // #575757
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
        var emptyLabel = new Label("此文件夹中没有预制体");
        emptyLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
        emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        emptyLabel.style.fontSize = 16;
        emptyLabel.style.paddingTop = 40;
        contentContainer.Add(emptyLabel);
    }

    scrollView.Add(contentContainer);
    listContainer.Add(scrollView);

    // 更新信息标签
    if (folderInfoLabel != null)
        folderInfoLabel.text = $"{currentFolder.name} · {currentFolder.prefabs.Count} 项";
}

    // ============================================================
    //  新建文件夹
    // ============================================================
    private void OnNewFolderClicked()
    {
        if (isCreatingFolder || currentFolder == null) return;
        isCreatingFolder = true;

        // 创建输入行容器，强制宽度为 100%
        newFolderRow = new VisualElement();
        newFolderRow.AddToClassList("new-folder-row");
        newFolderRow.style.width = Length.Percent(100);

        var icon = new Label("[F]");
        icon.AddToClassList("icon");
        newFolderRow.Add(icon);

        newFolderInput = new TextField();
        newFolderInput.AddToClassList("input");
        newFolderInput.focusable = true;
        // 限制输入框最大宽度，防止溢出
        newFolderInput.style.maxWidth = Length.Percent(100);
        newFolderInput.style.flexShrink = 1;
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

        // 插入到 listContainer 顶部（注意：listContainer 是 flex 容器，宽度由父级决定）
        listContainer.Insert(0, newFolderRow);
        newFolderInput.Focus();
        newFolderInput.SelectAll();
    }

    private void ConfirmNewFolder()
    {
        string name = newFolderInput.value.Trim();
        if (string.IsNullOrEmpty(name))
        {
            EditorUtility.DisplayDialog("提示", "文件夹名称不能为空", "确定");
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
        currentFolder = newFolder; // 自动进入新文件夹
        RefreshListView();
        RefreshTreeView(); // 高亮新文件夹

        isCreatingFolder = false;
    }

    private void CancelNewFolder()
    {
        CleanupNewFolderRow();
        isCreatingFolder = false;
    }

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
    //  拖拽导入预制体
    // ============================================================
    /// <summary>
    /// 设置拖拽事件，支持从 Project 窗口拖拽预制体到列表区域。
    /// 坑点：
    ///   1. 必须调用 DragAndDrop.AcceptDrag() 才能触发 DragPerform。
    ///   2. 使用 TrickleDown 确保事件在冒泡前捕获。
    ///   3. 设置 DragAndDrop.visualMode 控制鼠标图标。
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
            // 检查是否包含预制体
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
            // 必须接受拖拽，否则 DragPerform 不会触发
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
                RefreshTreeView(); // 更新文件夹计数
            }

            evt.StopPropagation();
        }, TrickleDown.TrickleDown);
    }

    // ============================================================
    //  刷新（手动调用）
    // ============================================================
    private void OnRefresh()
    {
        RefreshTreeView();
        RefreshListView();
    }

    // ============================================================
    //  生命周期清理
    // ============================================================
    private void OnDisable()
    {
        // 注意：由于我们使用 ScrollView 手动构建，不再需要取消 ListView 事件。
        // 如无特殊资源，可留空。
    }
}