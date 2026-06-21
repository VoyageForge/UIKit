# UIKit v0.2

轻量级 Unity UI 框架，基于 **UGUI + UniTask**。单栈导航 + 独立弹窗管理。Type-safe，零字符串 key。

## 架构

```
UIManager (MonoSingleton)          ← 服务容器
├── Panel (FullPanelManager)       ← FullPanel 导航栈，独立 Provider
├── Popup (PopupManager)           ← Popup 弹窗管理，独立 Provider
└── Get<T>()                       ← 用户自定义管理器扩展
```

## 面板类型

| 类型 | 基类 | 压栈 | Pause/Resume | 加载方式 |
|------|------|------|-------------|---------|
| 全屏面板 | `FullPanel` | 是 | 支持 | `UIManager.Panel.GetPanel<T>()` + `.ShowSelfAsync()` |
| 弹窗 | `PopupPanel` | 否 | 无 | `UIManager.Popup.GetPopup<T>()` + `.ShowSelfAsync()` |

---

## 生命周期

### 状态机

```
Inactive → Active ↔ Paused (仅 FullPanel)
               ↘ Exiting → Destroyed (Close)
```

### FullPanel 导航流程

```
GetPanel<T> → 从 Provider 缓存查看（不移除）
ShowSelfAsync → PushAsync → 缓存取出 → ViewStack.Push:
  ├── 栈顶同类型 (ABB) → 跳过
  ├── 栈内已有同类型 (ABA) → 报错
  ├── 当前栈顶.Pause() → OnPause()
  └── panel.Show()
        ├── 首次: OnCreate() → OnShow()
        └── 非首次: OnShow()

PopAsync → ViewStack.Pop:
  ├── 栈顶.Hide() → OnHide() → Provider.Release(回缓存)
  └── 下层.Resume() → OnResume()
```

### 生命周期钩子

| 方法 | 类 | 调用时机 |
|------|----|---------|
| `OnCreate()` | BasePanel | 首次 Show，仅一次 |
| `OnShow()` | BasePanel | 每次 Show |
| `OnHide()` | BasePanel | Hide 时 |
| `OnClose()` | BasePanel | Close 销毁时 |
| `OnPause()` | FullPanel | 被覆盖 |
| `OnResume()` | FullPanel | 恢复 |
| `OnInput(key,down)` | BasePanel | 输入路由（Escape 默认 PopAsync） |

---

## API

### UIManager.Panel (FullPanelManager)

| 方法 | 返回 | 说明 |
|------|------|------|
| `GetPanel<T>(Action<T>)` | `UniTask<T>` | 异步加载面板，回调通知（不自动显示） |
| `GetPanel<T>()` | `UniTask<T>` | 从 Provider 加载面板（缓存查看，不移除） |
| `PushAsync(FullPanel)` | `UniTask` | 压入导航栈并显示（从缓存取出） |
| `PopAsync()` | `UniTask<FullPanel>` | 弹出栈顶，Release 回缓存 |
| `HideAsync(FullPanel)` | `UniTask` | 隐藏指定面板回缓存 |
| `CloseAsync(FullPanel)` | `UniTask` | 关闭并销毁面板 |
| `GetActivePanel()` | `FullPanel?` | 获取栈顶 |
| `Peek()` | `FullPanel?` | 查看栈顶（不出栈） |
| `Count` | `int` | 栈中面板数量 |
| `OnInput(key,down)` | `bool` | 输入路由 |
| `Provider` | `IPanelProvider` | 加载代理，读写，设值时自动迁移缓存 |

### UIManager.Popup (PopupManager)

| 方法 | 返回 | 说明 |
|------|------|------|
| `GetPopup<T>()` | `UniTask<T>` | 加载弹窗（缓存查看，不移除） |
| `ShowAsync(PopupPanel)` | `UniTask` | 显示弹窗（从缓存取出） |
| `HideAsync(PopupPanel)` | `UniTask` | 隐藏弹窗回缓存 |
| `CloseAsync(PopupPanel)` | `UniTask` | 销毁弹窗 |
| `Provider` | `IPanelProvider` | 弹窗加载代理，读写 |

### UIManager

| 成员 | 说明 |
|------|------|
| `Panel` | `FullPanelManager` 静态属性（懒初始化） |
| `Popup` | `PopupManager` 静态属性（懒初始化） |
| `Get<T>()` | 注册/获取自定义管理器扩展 |
| `OnInput(key,down)` | 输入路由到 Panel |

### 面板自管理 (ShowSelf / HideSelf / CloseSelf)

```csharp
// FullPanel 和 PopupPanel 均提供 Self 方法
ShowSelf() / ShowSelfAsync()    // 显示自身
HideSelf() / HideSelfAsync()    // 隐藏回池
CloseSelf() / CloseSelfAsync()  // 销毁自身
```

---

## GetPanel 缓存语义

`GetPanel<T>()` / `GetPopup<T>()` **只查看缓存，不移除**。这是 v0.2 的核心设计：

```
GetPanel<T>() → 缓存查看 ✓（不移除）
  ↓ 可在显示前做预处理
ShowSelfAsync() → 缓存取出 ✓ → 显示
  ↓
HideSelfAsync() → 缓存放回 ✓
```

```csharp
// 典型用法：获取 → 预处理 → 显示
var panel = await UIManager.Panel.GetPanel<SettingsPanel>();
panel.SetData(myData);  // 显示前预处理
await panel.ShowSelfAsync();
```

---

## Provider

每个 Manager 有独立的 Provider，通过 `Manager.Provider` 属性访问：

```csharp
// 设置 FullPanel 的加载代理
UIManager.Panel.Provider = new ResourcesProvider();

// 设置 Popup 的加载代理
UIManager.Popup.Provider = new PopupResourcesProvider();

// 热替换（自动迁移缓存）
UIManager.Panel.Provider = new AddressablesProvider();
```

| 类 | 说明 |
|----|------|
| `PanelProviderBase` | FullPanel Provider 抽象基类 |
| `ResourcesProvider` | FullPanel 默认实现，`Resources.LoadAsync` |
| `PopupProviderBase` | Popup Provider 抽象基类，管理 Root Canvas |
| `PopupResourcesProvider` | Popup 默认实现，自动创建 DontDestroyOnLoad Canvas |
| 自定义 | 继承对应基类，实现 `InstantiateAsync(path)` |

### [PanelPath]

```csharp
[PanelPath("UI/Panels/ShopPanel")]
public class ShopPanel : FullPanel { }
```

**VoyageForge > UIKit > Panel Path Window** — 拖入 prefab，一键生成 `[PanelPath]`。

---

## SceneUI

挂载到场景 GameObject，持有预置 `FullPanel` 列表。Start 时自动注册到 `FullPanelManager`，OnDestroy 时注销。

```csharp
// SceneUI 组件在 Inspector 中配置 SceneUIEntry 列表
// 每个 SceneUIEntry 引用场景中的一个 FullPanel
```

---

## 快速开始

```csharp
// 创建 FullPanel
[PanelPath("UI/MyPanel")]
public class MyPanel : FullPanel
{
    protected override UniTask OnCreate() { /* 获取组件引用 */ return UniTask.CompletedTask; }
    protected override UniTask OnShow()  { /* 刷新数据 */ return UniTask.CompletedTask; }
}

// 显示
var panel = await UIManager.Panel.GetPanel<MyPanel>();
await panel.ShowSelfAsync();

// 返回
await UIManager.Panel.PopAsync();

// 弹窗
var toast = await UIManager.Popup.GetPopup<ToastPopup>();
await toast.ShowSelfAsync();
```

---

## 依赖

| 包 | 用途 |
|----|------|
| UniTask | 异步生命周期 |
| Unity uGUI | UI 渲染 |
