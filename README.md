# UIKit

轻量级 Unity UI 框架，基于 **UGUI + UniTask**。单栈导航 + 独立弹窗管理。Type-safe，零字符串 key。

## 架构

```
UIManager (MonoSingleton)
  ├── ViewStack           ← FullPanel 导航栈（Pause/Resume）
  ├── PopupManager        ← PopupPanel 弹窗管理（Show/Hide/Close）
  ├── PanelProvider       ← FullPanel 工厂（可热替换）
  └── IPopupProvider      ← Popup 工厂（含 DontDestroyOnLoad Root Canvas）
```

## 面板类型

| 类型 | 基类 | 压栈 | Pause/Resume | 打开方式 |
|------|------|------|-------------|---------|
| 全屏面板 | `FullPanel` | 是 | 支持 | `UIManager.Show<T>()` |
| 弹窗 | `PopupPanel` | 否 | 无 | `PopupManager.ShowAsync<T>()` |

---

## 生命周期

### 状态机

```
Inactive → Active ↔ Paused (仅 FullPanel)
               ↘ Exiting → Destroyed (Close)
```

### FullPanel 调用链

```
Show<T> → ViewStack.Push:
  ├── 当前栈顶.Pause() → OnPause()
  └── panel.Show()
        ├── 首次: OnCreate() → OnShow()
        └── 非首次: OnShow()

Hide() → ViewStack.Pop:
  ├── 栈顶.Hide() → OnHide() → Provider.Release(缓存)
  └── 下层.Resume() → OnResume()

Close(panel):
  └── panel.Close() → OnClose() → Destroy
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
| `OnInput(key,down)` | BasePanel | 输入路由 |

---

## API

### UIManager

| 方法 | 说明 |
|------|------|
| `ShowAsync<T>()` | 打开 FullPanel |
| `ShowAsync(FullPanel)` | 打开已有实例 |
| `HideAsync()` | 关闭栈顶 |
| `HideAsync(FullPanel)` | 关闭指定面板 |
| `CloseAsync(BasePanel)` | 销毁面板 |
| `GetActivePanel()` | 获取栈顶 |
| `OnInput(key, down)` | 输入路由 |
| `PanelProvider` | 可读写，设值时自动迁移缓存 |

### PopupManager

| 方法 | 说明 |
|------|------|
| `ShowAsync<T>()` | 显示弹窗 |
| `HideAsync<T>()` | 隐藏弹窗（缓存） |
| `CloseAsync<T>()` | 销毁弹窗 |

### 面板自管理 (ShowSelf / HideSelf / CloseSelf)

FullPanel 和 PopupPanel 均提供 Self 方法，面板内部可直接调：

| 方法 | 说明 |
|------|------|
| `ShowSelf()` / `ShowSelfAsync()` | 显示自身（FullPanel 压栈，PopupPanel 走 PopupManager） |
| `HideSelf()` / `HideSelfAsync()` | 隐藏自身 |
| `CloseSelf()` / `CloseSelfAsync()` | 销毁自身 |

```csharp
public class MyPanel : FullPanel
{
    private void OnConfirm()
    {
        // 面板内部操作自身
        HideSelf();         // 隐藏回池
        // CloseSelf();     // 销毁
        // ShowSelf();      // 重新显示
    }
}
```

### SceneUIContext

挂载到场景 GameObject，持有预置面板列表。Start 自动注册，OnDestroy 自动注销。

---

## 快速开始

### 1. 创建 FullPanel

```csharp
[PanelPath("UI/Panels/ShopPanel")]
public class ShopPanel : FullPanel
{
    protected override UniTask OnCreate() => UniTask.CompletedTask;
    protected override UniTask OnShow() => UniTask.CompletedTask;
    protected override UniTask OnHide() => UniTask.CompletedTask;
    protected override UniTask OnClose() => UniTask.CompletedTask;
}
```

### 2. 使用

```csharp
// 打开面板
await UIManager.Instance.ShowAsync<ShopPanel>();
await UIManager.Instance.HideAsync();  // 返回

// 弹窗
await UIManager.Instance.PopupManager.ShowAsync<ToastPopup>();
```

---

## Provider

| 类 | 说明 |
|----|------|
| `PanelProviderBase` | 抽象基类，子类只需实现 `Instantiate(path)` |
| `ResourcesProvider` | 默认实现，`Resources.Load` |
| 自定义 | 继承 `PanelProviderBase`，15 行写 AB/Addressables 加载器 |

```csharp
public class AddressablesProvider : PanelProviderBase
{
    protected override BasePanel Instantiate(string path)
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(path);
        return Object.Instantiate(handle.WaitForCompletion()).GetComponent<BasePanel>();
    }
}

// 热替换
UIManager.Instance.PanelProvider = new AddressablesProvider();
```

### [PanelPath]

标注非默认加载路径。未标记时 fallback 到 `type.Name`。

```csharp
[PanelPath("UI/Panels/ShopPanel")]
public class ShopPanel : FullPanel { }
```

**Tools > UIKit > Panel Path Window** — 拖入 prefab，一键生成 `[PanelPath]`。

---

## 依赖

| 包 | 用途 |
|----|------|
| UniTask | 异步生命周期 |
| Unity uGUI | UI 渲染 |
