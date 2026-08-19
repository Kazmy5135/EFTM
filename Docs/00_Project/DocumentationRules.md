---
title: 文档维护规范
document_id: PROJECT-DOCUMENTATION-RULES
status: accepted
last_updated: 2026-08-18
---

# 文档维护规范

## 文档类型

- 项目文档：边界、状态、流程、追踪和交接。
- 设计规格：可观察目标、规则、状态、边界和验收。
- 技术规格：架构、模块职责、数据、接口、生命周期、迁移和技术验证。
- 工作记录：提案、差异、任务、证据和复盘。
- DDR / ADR：解释重要取舍的原因和后果，不替代正式规格。
- 研究与归档：保存来源、实验和已失效材料。

## 状态

正式设计和技术文档使用：

- `draft`：正在形成，不能作为实现依据。
- `review`：内容完整，等待批准。
- `accepted`：当前有效基线。
- `deprecated`：已失效，不能用于新工作。

工作项使用：

- `proposed → discussing → review → approved → applied → closed`

实现交付使用：

- `not_assessed / ready_for_development / developing / verifying / implemented / diverged`

## 维护规则

1. 正式规格只保存当前结论，变更原因与证据留在工作记录或决策记录。
2. 修改 accepted 规格的语义前，先建立变更工作项；讨论期间原基线保持有效。
3. 获批变更涉及多份权威文档时，必须在同一批修改中完成同步。
4. 错别字、链接、排版和不改变验收结果的措辞可直接维护。
5. 无法判断是否改变含义时，按语义变更处理。
6. 基线引用至少记录 `document_id`、路径、状态、日期、适用工作项和 Git commit。
7. README 只做导航，不复制实时状态或规则正文。

## 命名

- 正式文件使用清晰英文名，正文默认中文。
- 设计决策：`DDR-###-short-name.md`。
- 技术决策：`ADR-###-short-name.md`。
- 工作目录：`###-short-name`。
- 日期：`YYYY-MM-DD`。
