# AGENTS.md - EFTM Unity 开发

## 项目入口

本目录是完整 Unity 工程根目录，目标编辑器版本为 `2022.3.62f2`。

开始 Unity 任务前按需要读取：

1. 仓库根目录 `AGENTS.md`
2. `Docs/00_Project/Agents/UnityDevelopmentAgent.md`
3. `Docs/00_Project/ProjectStatus.md`
4. 当前任务绑定的 accepted 设计规格
5. 当前任务绑定的 accepted 技术规格或 ADR
6. 对应 `Docs/30_Iterations` 工作项
7. 受影响代码、资产和测试

以上 `Docs/` 路径均相对于仓库根目录，不在 Unity 工程内复制第二份文档。

## 开发门禁

- 正式玩家行为实现必须绑定当前 accepted 设计和技术基线。
- draft 设计只能进入明确登记、范围受限且可丢弃的技术 Spike，不能伪装成正式功能。
- H5 原型不是 Unity 开发的通用门禁；没有 H5 不构成阻塞。
- 只有具体工作项明确把 H5 结果列为前置条件时，才检查该项证据。
- H5 结果与正式设计冲突时，以当前 accepted 设计为基线并报告冲突，不直接照搬原型。

## 工程边界

- `Assets/_Project/Runtime`：运行时代码，不得依赖 Editor 程序集。
- `Assets/_Project/Editor`：仅编辑器工具。
- `Assets/_Project/Tests`：自动化测试。
- `Assets/_Project/Scenes`：可进入 Build Settings 的场景。
- `Packages` 和 `ProjectSettings` 属于 Unity 工程配置。

## 工作规则

- 修改 Unity 资产时保留并提交对应 `.meta`，不手写、伪造或随意替换 GUID。
- Unity 生成目录 `Library`、`Logs`、`TestResults` 和本地 `UserSettings` 不提交。
- 不直接修改生成代码或导入产物；修改其权威源后重新生成。
- 不通过修改设计文档掩盖实现偏差；发现不一致时报告并更新追踪状态。
- 只实现当前工作项范围内的最小连贯切片。
- 完成前运行与风险相称的 EditMode、PlayMode、框架校验或构建验证。
- 检查 `git diff`，避免因 Unity 导入产生无关资产和配置变化。
