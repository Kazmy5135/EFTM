---
title: ADR-001 单仓库三工作区布局
document_id: ADR-001
status: accepted
last_updated: 2026-08-22
---

# ADR-001 单仓库三工作区布局

## 背景与权威上游

EFTM 原先以仓库根目录作为 Unity 工程根目录，`Docs/` 与 Unity 的 `Assets`、`Packages`、`ProjectSettings` 混在同一层。项目需要增加可选的 H5 交互原型能力，并让策划设计、H5 验证和 Unity 正式开发具有清晰边界。

本决策由工作项 `004-repository-restructure` 执行。设计者明确指出：H5 只在设计者提出需要验证体感时触发，不是所有设计的固定门禁。

## 约束

- Unity 工程必须保持完整，可由 Unity Hub 独立打开。
- `Docs/` 继续作为设计、技术、迭代与追踪的唯一知识库。
- H5 可以验证 draft 假设，但实验结果不能自动成为正式设计。
- 没有 H5 原型不能成为设计批准或 Unity 开发的默认阻塞。
- 移动 Unity 资产时必须保留 `.meta`、GUID 和工程内相对路径。
- 现有未提交文档必须保留，不得因迁移覆盖或回退。

## 候选方案

1. 继续让 Unity 占据仓库根目录，仅新增一个 H5 子目录。
2. 将 Unity、H5 和 Docs 作为三个独立仓库。
3. 保持单仓库，将 `UnityProject/`、`H5Prototype/` 和 `Docs/` 设为平级工作区。

## 决策

采用方案 3：

- `UnityProject/` 是完整 Unity 工程根目录。
- `H5Prototype/` 是设计者按需触发的实验工作区。
- `Docs/` 是策划、技术和交付知识库。
- 仓库根 `AGENTS.md` 负责路由，三个工作区的局部 `AGENTS.md` 负责各自规则。
- Agent 的长期职责和交接格式维护在 `Docs/00_Project/Agents/`。

H5 的流程位置是可选分支：

```text
策划设计 ───────────────→ 设计批准 → 技术设计 → Unity 实现
   └─ 设计者明确触发 → H5 验证 ─→ 证据回传 ─┘
```

只有具体工作项把 H5 验证列为验收项时，它才构成该工作项的局部门禁。

## 理由

- 三个工作区在同一 Git 历史中保留设计、实验和实现的追踪关系。
- Unity Hub、前端工具链和文档工具均获得清晰的工作根目录。
- 根级路由与局部规则能限制 agent 无意跨界。
- 可选 H5 分支既支持低成本体感验证，也避免给所有设计增加固定流程成本。

## 后果、迁移与验证

- Unity Hub 的打开路径改为 `UnityProject/`。
- 仓库级路径引用需要增加 `UnityProject/` 前缀；Unity 工程内部的 `Assets/...` 路径保持不变。
- 根 `.gitignore` 继续覆盖 Unity 生成目录，并增加 H5 依赖、构建和测试产物规则。
- H5 首次实际任务再选择前端技术栈，本 ADR 不绑定框架。
- 验证包括目录结构、Git rename、Unity 框架校验、EditMode 测试和文档链接检查。

## 关联规格与工作项

- `PROJECT-WORKFLOW`
- `PROJECT-DOCUMENTATION-RULES`
- 工作项 `004-repository-restructure`
