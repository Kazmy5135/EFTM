---
title: 004 - 仓库架构调整
work_id: "004"
work_type: governance-change
work_status: applied
change_id: null
target_document_ids:
  - ADR-001
  - PROJECT-STATUS
  - TRACEABILITY-REGISTER
last_updated: 2026-08-22
---

# 004 - 仓库架构调整

## 问题与背景

仓库根目录同时承担 Git 根和 Unity 工程根，难以在不混淆职责的情况下加入 H5 原型工作区。项目还需要为策划设计、可选 H5 验证和 Unity 正式开发建立稳定的 agent 路由。

## 目标和预期结果

- 将 Unity 工程完整迁移到 `UnityProject/`。
- 保持 `Docs/` 为平级独立知识库。
- 建立 `H5Prototype/` 作为设计者明确触发的可选验证工作区。
- 建立根级路由、三个局部 `AGENTS.md` 和长期角色交接文档。
- 更新仓库级路径并验证 Unity 工程仍可用。

## 范围

- 仓库目录迁移和忽略规则。
- Agent 职责、阅读顺序、门禁和交接格式。
- README、项目状态、追踪登记和 ADR。
- Unity 框架与现有 EditMode 测试验证。

## 非目标

- 不实现具体 H5 原型。
- 不选择 H5 前端框架。
- 不修改玩法规则或批准当前 draft 设计。
- 不重构 Unity 运行时代码。
- 不执行 Git 提交或推送。

## 权威上游与依赖

- `PROJECT-WORKFLOW`，`Docs/00_Project/ProjectWorkflow.md`
- `PROJECT-DOCUMENTATION-RULES`，`Docs/00_Project/DocumentationRules.md`
- 用户于 2026-08-22 批准三工作区方案，并明确 H5 只在设计者要求时触发。

## 下游影响

- Unity Hub 打开路径变更。
- 仓库级路径引用增加 `UnityProject/` 前缀。
- 后续任务按 Planning Design、H5 Prototype、Unity Development 三个角色路由。

## 初始验收案例

- 根目录只保留协调文件以及 `Docs/`、`H5Prototype/`、`UnityProject/` 工作区。
- Unity 工程在新目录通过框架校验和现有 EditMode 测试。
- 普通设计任务不会被自动路由到 H5。
- 设计者明确提出 H5 验证时可以获得清晰交接和回传格式。
- Unity 开发规则不把 H5 设为通用门禁。

## 待决策

- H5 前端技术栈留待首次实际原型任务决定。

## 状态门禁

- [x] proposed
- [x] discussing
- [x] review
- [x] approved
- [x] applied
- [ ] closed

## 关联决策与追踪

- `Docs/20_Technical/ADR/ADR-001-monorepo-workspace-layout.md`
- `Docs/00_Project/TraceabilityRegister.md`
