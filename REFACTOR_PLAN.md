# UIKit 重构计划：PanelManager 抽取 + 服务容器

> 版本: v0.2 计划 | 日期: 2026-06-18 | 基于: v0.1

---

## 一、动机

### 1.1 重复代码

当前 `UIManager` 同时承担了三个职责：

| 职责 | 代码位置 | 问题 |
|------|----------|------|
| FullPanel 管理 | `UIManager.cs:70-136` | 和 PopupManager 高度重复 |
| Popup 管理代理 | `UIManager.cs:15-16` | 只是透传，多一层无意义的包装 |
| 单例入口 | `UIManager.cs:9` | 合理 |

与 `PopupManager` 的重复点：

```
GetPanel<T>(cb)     ←→  GetPopup<T>(cb)       回调版 Load
GetPanelAsync<T>()  ←→  GetPopupAsync<T>()    异步 Load
HideAsync → Release ←→  HideAsync → Release   Hide+缓存
CloseAsync          ←→  CloseAsync            Close+清理
Provider get/set    ←→  Provider get/set      Provider 热替换
```

---

## 二、目标架构

```
UIManager (MonoSingleton)          ← 服务容器 + 场景入口
├── _managers: Dictionary<Type, object>
│   ├── FullPanelManager           ← 内置，Panel 静态属性直取
│   ├── PopupManager               ← 内置，Popup 静态属性直取
│   └── CustomManager              ← 用户自定义，Get<T>() 显式取
│
PanelManagerBase<T>                ← 泛型基类，消除重复
├── FullPanelManager : PanelManagerBase<FullPanel>
│   └── + ViewStack 导航栈 + SceneUIContext 注册
└── PopupManager : PanelManagerBase<PopupPanel>
    └── + Canvas Root 管理 + ReparentAll
```

---

## 三、PanelManagerBase<T> 设计

基类只做两件事：**Provider 管理** + **Load 抽象**。
活跃追踪、Show/Hide/Close 全部由子类各自管理——因为规则完全不同。

```csharp
public abstract class PanelManagerBase<T> where T : BasePanel
{
    // ===== Provider =====
    private IPanelProvider _provider;
    public IPanelProvider Provider
    {
        get => _provider;
        set
        {
            if (value == null) return;
            MigrateCache(_provider, value);
            _provider = value;
            OnProviderChanged();
        }
    }

    private static void MigrateCache(IPanelProvider from, IPanelProvider to)
    {
        if (from != null) to.Import(from.Export());
    }

    // ===== Load（通用 — 两个 Manager 完全一样） =====
    public async UniTask<TResult> GetAsync<TResult>(Action<TResult> onLoaded) where TResult : T
    {
        var panel = await GetAsync<TResult>();
        onLoaded?.Invoke(panel);
    }

    public async UniTask<TResult> GetAsync<TResult>() where TResult : T
    {
        return await _provider.LoadAsync<TResult>();
    }

    // ===== 生命周期（抽象 — 子类各自实现） =====
    public abstract UniTask ShowAsync(T panel);
    public abstract UniTask HideAsync(T panel);
    public abstract UniTask CloseAsync(T panel);
    public abstract void Dispose();

    // ===== 子类钩子 =====
    protected virtual void OnProviderChanged() { }
}
```

**不放入基类的东西：**
- ❌ `_active` 字典 — FullPanelManager 用 ViewStack 追踪，PopupManager 用自己的 `_active` dict
- ❌ `ShowAsync`/`HideAsync`/`CloseAsync` 通用实现 — 语义完全不同
- ❌ `Root` / Canvas 相关 — 只有 Popup 需要

**基类实际消除的重复：**
- ✅ Provider get/set + Import/Export（当前 UIManager 和 PopupManager 各写一遍）
- ✅ `GetAsync<T>()` / `GetAsync<T>(callback)`（当前 GetPanelAsync/GetPopupAsync 各写一遍）

---

## 四、FullPanelManager

**规则：同时只允许一个活跃面板。栈顶即是活跃。多了报 ABA/ABB。**

```csharp
public class FullPanelManager : PanelManagerBase<FullPanel>
{
    private ViewStack _stack = new();

    // ===== 导航栈 =====
    public FullPanel Peek() => _stack.Peek();
    public int Count => _stack.Count;

    // ===== 生命周期 =====
    public async UniTask PushAsync(FullPanel panel)
    {
        // ViewStack.Push 内部做 ABA/ABB 检查 + Pause/Show
        await _stack.Push(panel);
    }

    public async UniTask<FullPanel> PopAsync()
    {
        var panel = await _stack.Pop();
        Provider.Release(panel);  // 进缓存
        return panel;
    }

    public override async UniTask ShowAsync(FullPanel panel) => await PushAsync(panel);

    public override async UniTask HideAsync(FullPanel panel)
    {
        if (_stack.Peek() == panel)
            await PopAsync();               // 栈顶 → Pop + Release
        else
        {
            await panel.Hide();             // 非栈顶 → 只 Hide
            if (_stack.Peek() is FullPanel top) await top.Resume();
            Provider.Register(panel);       // 回缓存
        }
    }

    public override async UniTask CloseAsync(FullPanel panel)
    {
        if (_stack.Peek() == panel) await _stack.Pop();
        await panel.Close();
    }

    public override void Dispose() => _stack?.Dispose();

    // ===== SceneUIContext =====
    internal async UniTask RegisterSceneContext(SceneUIContext ctx) { ... }
    internal async UniTask UnregisterSceneContext() { ... }

    // ===== 输入路由 =====
    public bool OnInput(KeyCode key, bool down)
    {
        var top = _stack.Peek();
        return top != null && top.State == BasePanel.PanelState.Active && top.OnInput(key, down);
    }
}
```

**活跃追踪方式：** ViewStack 本身就是活跃追踪 — 栈里有多少个就有多少个活跃/暂停的 Panel。不需要额外的 `_active` 字典。

---

## 五、PopupManager

**规则：允许多个弹窗同时显示，同类型可并存。**

```csharp
public class PopupManager : PanelManagerBase<PopupPanel>
{
    // ===== 活跃追踪（Popup 自己管） =====
    private readonly Dictionary<Type, List<PopupPanel>> _active = new();

    // ===== Canvas Root（Popup 独有） =====
    public Transform Root => (_provider as IPopupRootProvider)?.Root;

    // ===== Provider 切换 =====
    protected override void OnProviderChanged() => ReparentAll();

    private void ReparentAll()
    {
        var root = Root;
        if (root == null) return;
        foreach (var panels in _active.Values)
            if (panels != null)
                foreach (var popup in panels)
                    popup.transform.SetParent(root, false);
    }

    // ===== 生命周期 =====
    public override async UniTask ShowAsync(PopupPanel panel)
    {
        if (panel == null) return;
        var type = panel.GetType();

        if (!_active.TryGetValue(type, out var popups) || popups == null)
            _active[type] = popups = new List<PopupPanel>();

        if (!popups.Contains(panel))
            popups.Add(panel);

        panel.transform.SetParent(Root, false);
        await panel.Show();
    }

    public override async UniTask HideAsync(PopupPanel panel)
    {
        if (panel == null) return;
        var type = panel.GetType();

        if (!_active.TryGetValue(type, out var popups) || popups == null || !popups.Contains(panel))
            return;

        popups.Remove(panel);
        if (popups.Count == 0) _active.Remove(type);

        await panel.Hide();
        Provider.Release(panel);
    }

    public override async UniTask CloseAsync(PopupPanel panel)
    {
        if (panel == null) return;
        var type = panel.GetType();

        if (_active.TryGetValue(type, out var popups) && popups != null)
        {
            popups.Remove(panel);
            if (popups.Count == 0) _active.Remove(type);
        }

        await panel.Close();
    }

    public override void Dispose() => _active.Clear();
}
```

**活跃追踪方式：** `_active` 字典自己管，支持按类型分组、多实例并存。

---

## 六、UIManager 瘦身为容器

```csharp
public class UIManager : MonoSingleton<UIManager>
{
    private readonly Dictionary<Type, object> _managers = new();

    // ===== 内置管理器 — 直接拿，不走容器 =====
    public static FullPanelManager Panel { get; private set; }
    public static PopupManager Popup { get; private set; }

    // ===== 自定义管理器 — 显式 Get =====
    public static T Get<T>() where T : class, new()
    {
        var type = typeof(T);
        if (!Instance._managers.TryGetValue(type, out var mgr))
        {
            mgr = new T();
            Instance._managers[type] = mgr;
        }
        return mgr as T;
    }

    protected override void Awake()
    {
        base.Awake();
        Panel = new FullPanelManager();
        Popup = new PopupManager();
    }

    private void OnDestroy()
    {
        Panel?.Dispose();
        Popup?.Dispose();
    }
}
```

**API 对比：**

| 之前 | 之后 |
|------|------|
| `UIManager.Popup.ShowPopupAsync(p)` | `UIManager.Popup.ShowAsync(p)` |
| `UIManager.Popup.GetPopupAsync<T>()` | `UIManager.Popup.GetAsync<T>()` |
| `await UIManager.Instance.GetPanelAsync<T>()` | `await UIManager.Panel.GetAsync<T>()` |
| `自定义逻辑散落各处` | `UIManager.Get<MyManager>()` |

---

## 七、接口统一

当前 `IPanelProvider` 和 `IPopupProvider` 几乎一样，只是缓存类型不同（`BasePanel` vs `List<PopupPanel>`）。统一为：

```csharp
public interface IPanelProvider
{
    UniTask<T> LoadAsync<T>() where T : BasePanel;
    void Release<T>(T panel) where T : BasePanel;
    void Register<T>(T panel) where T : BasePanel;
    bool TryGet(Type type, out BasePanel panel);
    void Remove(Type type);
    void Clear();
    // 缓存迁移
    Dictionary<Type, object> Export();
    void Import(Dictionary<Type, object> data);
}
```

`Transform Root` 移到 `PopupProviderBase`（只有 Popup 需要 Canvas 根节点）。

---

## 八、文件变更清单

| 操作 | 文件 |
|------|------|
| **新建** | `Scripts/Core/PanelManagerBase.cs` |
| **新建** | `Scripts/Core/FullPanelManager.cs` |
| **修改** | `Scripts/Core/UIManager.cs` — 从 137 行瘦身到 ~50 行 |
| **修改** | `Scripts/Core/PopupManager.cs` — 继承基类，删除重复代码 |
| **合并** | `Scripts/Core/Interface/IPanelProvider.cs` + `IPopupProvider.cs` → 单一 `IPanelProvider` |
| **不动** | `PopupProviderBase.cs` / `PanelProviderBase.cs` / `ResourcesProvider.cs` / `PopupResourcesProvider.cs` |
| **不动** | `ViewStack.cs` / `SceneUIContext.cs` / `BasePanel.cs` / `FullPanel.cs` / `PopupPanel.cs` |

---

## 九、迁移兼容

- `UIManager.Popup` 静态属性保持不变（二进制兼容）
- `GetPopupAsync<T>()` → `GetAsync<T>()`（编译期 break，但改动量很小）
- `ShowPopupAsync` → `ShowAsync`（同上）
- 可在 v0.2 周期内保留 `[Obsolete]` 桥接方法

---

## 十、测试影响

- `PopupManagerTests` 需要更新 API 名称
- `UIManagerTests` 需要改为 `FullPanelManagerTests`，但测试逻辑不变
- ViewStack 测试不受影响
- 新增 `PanelManagerBaseTests` 验证基类通用逻辑

---

## 十一、附加改动：SceneUIContext → SceneUI

`SceneUIContext` 名字太学术，"Context" 在这里没有承载任何上下文语义——它就是一个挂在场景 GameObject 上、持有预置 Panel 列表的入口组件。

### 改名为 `SceneUI`

| 之前 | 之后 | 理由 |
|------|------|------|
| `SceneUIContext` | `SceneUI` | 直白：场景里的 UI 入口 |

连带改名：
- `PreplacedPanelEntry` → `SceneUIEntry`（更短，语义不变）
- `RegisterSceneContext` → `RegisterSceneUI`
- `UnregisterSceneContext` → `UnregisterSceneUI`

### 文件影响
| 操作 | 文件 |
|------|------|
| 改名 | `SceneUIContext.cs` → `SceneUI.cs` |
| 改名 | `PreplacedPanelEntry.cs` → `SceneUIEntry.cs` |
| 修改 | `UIManager.cs` / `FullPanelManager.cs` 中的引用 |
| 修改 | `SceneUIContextEditor.cs` → `SceneUIEditor.cs` |
| 修改 | `PreplacedPanelEntryDrawer.cs` → `SceneUIEntryDrawer.cs`
