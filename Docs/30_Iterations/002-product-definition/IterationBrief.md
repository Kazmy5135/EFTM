---
title: 002 - 产品定义与首个 Demo 范围
work_id: "002"
work_type: design
work_status: discussing
change_id: null
target_document_ids:
  - DESIGN-PRODUCT-DEFINITION
  - PROJECT-STATUS
  - TRACEABILITY-REGISTER
last_updated: 2026-08-21
---

# 002 - 产品定义与首个 Demo 范围

## 问题与背景

项目框架已经建立，但产品目标、核心玩法和首个功能模块尚未定义。需要先明确 EFTM 是什么游戏、向玩家承诺什么体验，并将首个 Demo 收敛为可验证范围。

## 目标和预期结果

- 形成 EFTM 的一句话产品定义与核心体验支柱。
- 明确单局循环与局外长期循环的关系。
- 明确 V1 Demo 验证目标、范围和非目标。
- 识别进入核心战斗设计前必须解决的关键问题。

## 范围

- 手机平台的多人实时搜打撤产品定位。
- 半回合制自动枪战的体验边界。
- 竖屏单手操作约束、剪刀石头布式战斗博弈和格子背包搜索。
- 探索、生存资源、撤离、损失与局外成长的高层循环。
- V1 使用机器人 PMC 的验证边界。

## 非目标

- 本工作项不确定代码架构、网络拓扑或后端技术。
- 不在产品定义阶段完成战斗公式、武器数值或经济平衡。
- 不承诺 V1 接入真人玩家。
- 不完整复刻任何参考产品的内容与规则。

## 权威上游与依赖

- 用户于 2026-08-19 提供的产品构想。
- 项目状态：`PROJECT-STATUS`，`Docs/00_Project/ProjectStatus.md`。
- 文档工作流：`PROJECT-WORKFLOW`，`Docs/00_Project/ProjectWorkflow.md`。
- 生效 Git commit：待设计批准后记录。

## 下游影响

- 核心战斗系统设计。
- 地图探索、遭遇和撤离系统设计。
- 物资、仓储与局外成长设计。
- V1 技术规格、原型任务和验证计划。

## 初始验收案例

- 可以用一句话说明平台、品类、玩家活动和差异化战斗方式。
- 可以画出从整备到撤离再到成长的完整循环。
- 已确定内容与待决策内容能够清楚区分。
- V1 范围足以验证核心体验，但不依赖真人玩家和完整长期系统。

## 待决策

- 自动枪战中的玩家决策边界。
- 剪刀石头布关系对应的具体战术动作与情报来源。
- 半回合战斗与实时地图的时间关系。
- 探索视角、移动方式与格子背包的单手手势。
- V1 局长、地图规模、PMC 数量与最小局外成长出口。

## 状态门禁

- [x] proposed
- [x] discussing
- [ ] review
- [ ] approved
- [ ] applied
- [ ] closed

## 关联决策与追踪

- 产品定义草案：`Docs/10_Design/ProductDefinition.md`
- 追踪登记：`Docs/00_Project/TraceabilityRegister.md`
