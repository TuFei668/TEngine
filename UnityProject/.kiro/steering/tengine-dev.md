---
inclusion: fileMatch
fileMatchPattern: "Assets/GameScripts/**/*.cs"
---

# TEngine 开发技能

TEngine 基于 HybridCLR + YooAsset + UniTask + Luban 构建。

## 快速导航

| 需要什么 | 查看文件 |
|---------|---------|
| 项目结构 / 分层架构 / 启动流程 | [architecture.md](../.claude/skills/tengine-dev/references/architecture.md) |
| 所有模块 API 速查（Timer/Scene/Audio/Fsm 等）| [modules.md](../.claude/skills/tengine-dev/references/modules.md) |
| UI 开发（Window/Widget/生命周期）| [ui-development.md](../.claude/skills/tengine-dev/references/ui-development.md) |
| 事件系统（int事件 / 接口事件 / UIEvent）| [event-system.md](../.claude/skills/tengine-dev/references/event-system.md) |
| 资源加载 / 释放（Sprite / GameObject / Asset）| [resource-management.md](../.claude/skills/tengine-dev/references/resource-management.md) |
| 热更包下载 / Manifest 更新 / 缓存清理 | [hotpatch-management.md](../.claude/skills/tengine-dev/references/hotpatch-management.md) |
| 热更代码 / HybridCLR / GameApp 入口 | [hotfix-development.md](../.claude/skills/tengine-dev/references/hotfix-development.md) |
| Luban 配置表生成与访问 | [luban-config.md](../.claude/skills/tengine-dev/references/luban-config.md) |
| 代码规范 / 命名约定 / 设计模式 | [conventions.md](../.claude/skills/tengine-dev/references/conventions.md) |
| 常见问题 / 错误排查 | [troubleshooting.md](../.claude/skills/tengine-dev/references/troubleshooting.md) |
| **unity-mcp 全工具索引 / batch_execute / 目标定位** | [unity-mcp-guide.md](../.claude/skills/tengine-dev/references/unity-mcp-guide.md) |
| **UI Prefab 拼接（节点命名规范 / 工作流 / 模板）** | [ui-prefab-builder.md](../.claude/skills/tengine-dev/references/ui-prefab-builder.md) |
| **场景 / GameObject / 组件 操作** | [scene-gameobject.md](../.claude/skills/tengine-dev/references/scene-gameobject.md) |
| **脚本创建 / 精确编辑 / 资源文件管理** | [script-asset-workflow.md](../.claude/skills/tengine-dev/references/script-asset-workflow.md) |
| **材质 / Shader / VFX / 动画控制器** | [material-shader-vfx.md](../.claude/skills/tengine-dev/references/material-shader-vfx.md) |
| **Editor 自动化 / PlayMode / 测试 / 日志** | [editor-automation.md](../.claude/skills/tengine-dev/references/editor-automation.md) |

## 核心原则

1. **异步优先**：IO 操作用 `UniTask`，禁止同步加载/Coroutine
2. **模块访问**：通过 `GameModule.XXX` 访问，而非 `ModuleSystem.GetModule<T>()`
3. **资源必须释放**：`LoadAssetAsync` 对应 `UnloadAsset`，GameObject 用 `LoadGameObjectAsync`
4. **热更边界**：`GameScripts/Main` 不热更，`GameScripts/HotFix/` 全部热更
5. **事件解耦**：模块间用 `GameEvent`，UI 内部用 `AddUIEvent`

## 程序集分层

```
GameScripts/Main/       → 主包（不热更）
GameScripts/HotFix/
  ├── GameProto/        → Luban 配置代码
  └── GameLogic/        → 业务逻辑（GameApp.cs 入口）
```

**依赖**：GameLogic → TEngine.Runtime（单向）
