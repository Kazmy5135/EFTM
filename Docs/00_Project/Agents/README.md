# EFTM Agent 路由与交接

本目录定义三个长期角色的职责、阅读顺序和交接格式。真正按目录生效的操作规则位于仓库根目录以及 `Docs/`、`H5Prototype/`、`UnityProject/` 各自的 `AGENTS.md`。

## 角色入口

- [Planning Design Agent](PlanningDesignAgent.md)：产品、规则、设计状态与验收。
- [H5 Prototype Agent](H5PrototypeAgent.md)：由设计者明确触发的可选体感验证。
- [Unity Development Agent](UnityDevelopmentAgent.md)：正式 Unity 实现与验证。

## 路由原则

1. 纯设计任务进入 Planning Design Agent。
2. 只有设计者明确要求 H5 验证时，才从策划工作项分支到 H5 Prototype Agent。
3. 正式 Unity 实现进入 Unity Development Agent，不默认要求先有 H5。
4. 跨层冲突必须回到拥有该事实的角色处理，不静默跨权修改。

## 策划到 H5 的可选交接

只有明确触发 H5 时才建立，至少包含：

- 工作项或验证请求标识；
- 设计来源及状态；
- 要验证的体验假设；
- 原型范围与非目标；
- 目标设备、方向、输入限制和测试场景；
- 成功、失败和停止判据；
- 期望回传的证据形式。

## H5 回传策划

- 原型版本与运行方式；
- 实际覆盖范围和未覆盖范围；
- 可重复的操作步骤；
- 观察结果、参与者反馈、agent 推论分栏记录；
- 截图、录屏、测试或数据证据；
- 技术限制和可能的误差来源；
- 需要设计者决定的问题。

H5 不负责把回传结果直接应用到 accepted 设计。

## 策划到 Unity 的交接

- accepted 设计文档标识、路径与适用工作项；
- accepted 技术规格或 ADR；
- 行为范围、非目标和验收案例；
- 数据、接口、失败路径和迁移要求；
- 若该工作项确有 H5 局部门禁，列出所需证据；否则明确“不要求 H5”。

## Unity 回传

- 受影响模块与实现位置；
- 测试、构建和行为验收结果；
- 与设计、技术基线的对应关系；
- 未实现、受限或偏离项；
- 需要同步到工作项和追踪登记的事实。
