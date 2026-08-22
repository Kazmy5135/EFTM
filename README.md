# EFTM

EFTM 使用单仓库三工作区架构：策划与技术文档、按需启用的 H5 原型，以及正式 Unity 工程彼此平级。

## 项目结构

```text
Docs/           策划、技术、迭代、研究和追踪的唯一知识库
H5Prototype/    设计者明确要求时启用的 H5 体感与交互验证
UnityProject/   Unity 2022.3.62f2 正式工程
```

根 [AGENTS.md](AGENTS.md) 负责按任务类型路由。H5 是可选验证工具，不是设计批准或 Unity 开发的通用门禁。

## Unity 快速开始

1. 在 Unity Hub 安装 Unity `2022.3.62f2`。
2. 使用 Unity Hub 打开本仓库的 `UnityProject/` 目录。
3. 等待 Package Manager 完成依赖解析与脚本编译。
4. 打开 `Assets/_Project/Scenes/Bootstrap.unity`。
5. 通过 `Window > General > Test Runner` 运行 EditMode 测试。
6. 通过 `Tools > EFTM > Validate Project Framework` 检查框架结构。

Unity 工程内部约定：

```text
UnityProject/Assets/_Project/
  Runtime/       运行时代码；不得依赖 Editor 程序集
  Editor/        仅编辑器工具
  Tests/         自动化测试
  Scenes/        可进入 Build Settings 的场景
```

## H5 原型

`H5Prototype/` 当前只建立工作入口，不预设前端框架。只有设计者明确提出需要验证体感、交互或可用性时，才创建具体原型。详细规则见 [H5Prototype/README.md](H5Prototype/README.md)。

## 文档

`Docs/` 采用“单一当前基线 + 独立变更记录 + Git 历史”的管理方式。详细约定见 [Docs/README.md](Docs/README.md)。

当前 Unity 框架包括：

- `EFTM.Runtime`：运行时程序集。
- `EFTM.Editor`：编辑器工具程序集。
- `EFTM.Tests.EditMode`：EditMode 测试程序集。
- `GameBootstrap`：在首个场景加载前自动建立持久化启动对象。
- `ServiceRegistry`：提供显式注册、初始化和逆序关闭的轻量服务生命周期。

框架不预设渲染管线、输入方案、玩法模块或第三方架构；这些选择应在需求明确后通过技术设计和 ADR 引入。
