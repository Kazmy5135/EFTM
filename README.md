# EFTM

Unity 项目框架，目标编辑器版本为 **2022.3.62f2**。

## 快速开始

1. 在 Unity Hub 安装 Unity `2022.3.62f2`。
2. 使用 Unity Hub 打开本仓库根目录。
3. 等待 Package Manager 完成依赖解析与脚本编译。
4. 打开 `Assets/_Project/Scenes/Bootstrap.unity`。
5. 通过 `Window > General > Test Runner` 运行 EditMode 测试。
6. 通过 `Tools > EFTM > Validate Project Framework` 检查框架结构。

## 目录约定

```text
Assets/_Project/
  Runtime/       运行时代码；不得依赖 Editor 程序集
  Editor/        仅编辑器工具
  Tests/         自动化测试
  Scenes/        可进入 Build Settings 的场景
Docs/
  00_Project/    项目治理、状态与追踪
  10_Design/     当前有效设计基线
  20_Technical/  当前有效技术基线与 ADR
  30_Iterations/ 工作项、变更、任务、验证与复盘
  40_Research/   外部资料和验证性研究
  90_Archive/    已失效但需保留的材料
```

`Docs` 采用“单一当前基线 + 独立变更记录 + Git 历史”的管理方式。详细约定见 [Docs/README.md](Docs/README.md)。

## 当前框架

- `EFTM.Runtime`：运行时程序集。
- `EFTM.Editor`：编辑器工具程序集。
- `EFTM.Tests.EditMode`：EditMode 测试程序集。
- `GameBootstrap`：在首个场景加载前自动建立持久化启动对象。
- `ServiceRegistry`：提供显式注册、初始化和逆序关闭的轻量服务生命周期。

框架不预设渲染管线、输入方案、玩法模块或第三方架构；这些选择应在需求明确后通过技术设计和 ADR 引入。
