# Prefab Manager 封装计划书

> 版本: 1.0 | 日期: 2026-06-18

---

## 一、现状分析

当前 `PrefabManagerWindow` 是一个 EditorWindow，存在以下问题：

| 问题 | 说明 |
|------|------|
| 硬编码数据类型 | 只能管理 `GameObject` 预制体，无法按组件类型过滤 |
| 无数据持久化 | 所有数据存在内存中，关闭窗口/重启 Unity 后丢失 |
| 不可复用 | 逻辑与 UI 强耦合，无法作为子面板嵌入其他窗口 |
| 示例数据 | `InitData()` 用的是写死的假数据，未接入 `AssetDatabase` |

---

## 二、重构目标

### 2.1 泛型化 — 按组件类型过滤

```
当前: 所有 GameObject/Prefab 混在一起
目标: PrefabManagerWindow<T> where T : Component
       → 只显示挂载了 T 组件的预制体
```

**API 设计：**

```csharp
// 打开特定类型的 Prefab Manager
PrefabManagerWindow<Button>.ShowWindow();     // 只管理带 Button 的预制体
PrefabManagerWindow<CustomUI>.ShowWindow();   // 只管理带 CustomUI 的预制体

// 菜单注册示例
[MenuItem("Window/Prefab Manager/UI Button")]
public static void ShowButtonManager() => PrefabManagerWindow<Button>.ShowWindow();
```

**过滤逻辑：**

```csharp
// 扫描时过滤
private void ScanPrefabs()
{
    var guids = AssetDatabase.FindAssets("t:Prefab");
    foreach (var guid in guids)
    {
        var path = AssetDatabase.GUIDToAssetPath(guid);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        // 泛型过滤：只添加挂载了 T 组件的预制体
        if (prefab.TryGetComponent<T>(out _))
            AddEntry(guid, path, prefab);
    }
}
```

### 2.2 组件化 — 封装为可复用组件

将核心逻辑从 EditorWindow 中抽离为 `PrefabManagerCore<T>`，EditorWindow 仅做 UI 壳：

```
┌─────────────────────────────────┐
│ PrefabManagerWindow<T>          │  ← EditorWindow 壳 (UI)
│  ├── UXML/USS 布局              │
│  └── PrefabManagerCore<T>       │  ← 核心逻辑 (可复用)
│       ├── Data Model            │
│       ├── Persistence           │
│       └── Events / Callbacks    │
└─────────────────────────────────┘
```

**核心类设计：**

```csharp
[Serializable]
public class PrefabManagerCore<T> where T : Component
{
    // 数据
    public FolderNode RootFolder { get; private set; }
    public FolderNode CurrentFolder { get; set; }
    public int TotalPrefabCount { get; }

    // 事件
    public event Action DataChanged;
    public event Action<FolderNode> FolderChanged;

    // 操作
    public void ScanPrefabs();                        // 从 AssetDatabase 扫描
    public FolderNode CreateFolder(string name, FolderNode parent);
    public void RenameFolder(FolderNode folder, string newName);
    public void DeleteFolder(FolderNode folder);
    public void AddPrefab(FolderNode folder, GameObject prefab);
    public void RemovePrefab(PrefabEntry entry);
    public void MovePrefab(PrefabEntry entry, FolderNode targetFolder);

    // 持久化
    public void Save(PrefabManagerData data);         // 保存到 ScriptableObject
    public void Load(PrefabManagerData data);         // 从 ScriptableObject 加载
}
```

### 2.3 数据持久化 — ScriptableObject

**存储模型：**

```csharp
[CreateAssetMenu(menuName = "UIKit/Prefab Manager Data")]
public class PrefabManagerData : ScriptableObject
{
    [Serializable]
    public class FolderData
    {
        public string Id;
        public string Name;
        public string ParentId;
    }

    [Serializable]
    public class PrefabEntryData
    {
        public string Guid;          // Asset GUID（跨会话稳定）
        public string FolderId;      // 所属文件夹
    }

    public string ManagerTypeName;   // typeof(T).FullName，恢复时校验类型
    public List<FolderData> Folders = new();
    public List<PrefabEntryData> Entries = new();
}
```

**保存/加载流程：**

```
保存: PrefabManagerCore → PrefabManagerData (ScriptableObject)
       ├── GUID 作为预制体引用（不存直接引用，避免丢失）
       └── 文件夹树扁平化为 FolderData 列表 + ParentId

加载: PrefabManagerData → PrefabManagerCore
       ├── 校验 ManagerTypeName 匹配
       ├── 重建文件夹树
       └── 通过 GUID 恢复预制体引用（缺失的跳过并警告）
```

**ScriptableObject 存放路径：** `Assets/UIKit/Editor/PrefabManagerData/`

### 2.4 拖拽过滤

拖拽时只接受挂载了 `T` 组件的预制体：

```csharp
listContainer.RegisterCallback<DragUpdatedEvent>(evt =>
{
    bool valid = false;
    foreach (var obj in DragAndDrop.objectReferences)
    {
        if (obj is GameObject go && go.TryGetComponent<T>(out _))
        {
            var path = AssetDatabase.GetAssetPath(go);
            if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                valid = true;
                break;
            }
        }
    }
    DragAndDrop.visualMode = valid
        ? DragAndDropVisualMode.Copy
        : DragAndDropVisualMode.Rejected;
    evt.StopPropagation();
});
```

---

## 三、实施步骤

| 阶段 | 内容 | 预估文件 |
|------|------|----------|
| **Phase 1** | 创建 `PrefabManagerData` ScriptableObject | `PrefabManagerData.cs` |
| **Phase 2** | 抽离 `PrefabManagerCore<T>` 核心逻辑 | `PrefabManagerCore.cs` |
| **Phase 3** | 重构 `PrefabManagerWindow<T>` 为泛型壳 | `PrefabManagerWindow.cs` (重写) |
| **Phase 4** | 接入 `AssetDatabase` 扫描，替换示例数据 | 同上 |
| **Phase 5** | 实现 Save/Load，窗口 OnEnable/OnDisable 自动持久化 | `PrefabManagerCore.cs` |
| **Phase 6** | 添加编辑器菜单注册辅助 | `PrefabManagerMenu.cs` |

---

## 四、兼容性

- 旧 `PrefabManagerWindow`（非泛型）标记 `[Obsolete]`，保留一个版本后删除
- `PrefabManagerData` 使用 GUID 引用，跨版本兼容
- `ManagerTypeName` 校验防止错误类型的 data 被加载

---

## 五、风险与待定

- **风险**：泛型 EditorWindow 在 Unity 中有限制 — 带泛型的窗口无法直接通过 `[MenuItem]` 注册（需要非泛型中间类）
- **方案**：提供 `PrefabManagerWindow` 工厂方法 + 具体类型子类注册菜单
- **待定**：是否需要支持同一窗口内切换类型过滤（vs. 每个类型独立窗口）
