---
title: 005 - Unity 战斗模块 V1 技术设计
work_id: "005"
work_type: new-technical
work_status: closed
change_id: null
target_document_ids:
  - DESIGN-PRODUCT-DEFINITION
  - DESIGN-CORE-COMBAT
  - DDR-001
  - TECH-COMBAT-FOUNDATION-V1
  - PROJECT-STATUS
  - TRACEABILITY-REGISTER
last_updated: 2026-09-04
---

# 005 - Unity 战斗模块 V1 技术设计

## 问题与背景

战斗模块 V1 已经过“设计讨论 → H5 体感验证 → 设计回传与批准”，但 Unity 当前只有应用启动和服务注册骨架，没有战斗状态、摄像机、输入、场景或测试。本工作项只负责形成并批准 Unity 技术契约；技术批准后另建 development 工作项实施最小连贯切片。

## 目标和预期结果

- 跑通“accepted 设计 + H5 V1 证据共同指导 Unity”的完整流程。
- 建立不依赖 H5 技术结构的 Unity 状态、模块、数据和生命周期契约。
- 规划真假 Peek、情报预瞄、敌人换位、黄色剪影、调瞄和基础射击的实现切片。
- 规划 EditMode、PlayMode、目标手机和 H5/Unity 行为对照证据。
- 为后续 development 工作项提供准确的实现交接。

## 权威上游

- `DESIGN-PRODUCT-DEFINITION`，accepted，设计基线 commit `88d04fd`。
- `DESIGN-CORE-COMBAT`，accepted，设计基线 commit `88d04fd`。
- `DDR-001`，accepted。
- `TECH-COMBAT-FOUNDATION-V1`，accepted；设计者于 2026-09-04 批准。
- `H5-PEEK-CAMERA-001 V1`，tag `h5-peek-camera-v1`，commit `b9c8121`。
- Unity 工程版本 `2022.3.62f2`。

## H5 局部门禁

本工作项明确把 H5 V1 作为行为对照证据，原因是本轮目标本身就是验证设计与 H5 如何共同指导 Unity。门禁只要求：

- V1 tag 和实现 commit 可定位。
- `004-peek-camera/Validation.md` 中的行为与限制可追溯。
- Unity 实现逐项对照可观察行为。

该门禁不允许复制 Three.js/DOM 架构，也不允许 H5 覆盖 accepted 设计语义。

## 范围

- Unity 技术规格、配置、状态、接口、生命周期、失败路径与验证设计。
- 规划 `Assets/_Project/Runtime/Combat` 下的最小战斗模块和 `CombatFoundationV1` 验证场景。
- 定义 UI Toolkit 单指输入、摄像机 Peek、五个敌人点位和真实遮挡的职责。
- 定义 10% 情报阈值、25% 换位、旧位置剪影、一次性预瞄、射击与后坐力契约。
- 定义 EditMode、PlayMode、目标手机和 H5/Unity 对照验证矩阵。

## 非目标

- 弹匣、换弹、伤害、生命、护甲、手雷、左右掩体和敌人 AI。
- 多人网络、地图探索、搜刮、撤离和局外成长。
- 最终角色、武器、动画、美术、音频和渲染质量。
- 修改 accepted 设计来迁就 Unity 实现。
- 编写 Unity 玩法代码、场景、Prefab、Shader、配置资产或测试；这些属于批准后的 development 工作项。

## 实现原则

- Foundation 是规则状态唯一所有者；Unity 表现只消费快照和事件。
- 单场战斗使用场景级组合根，不注册为跨场景全局服务。
- UI 输入转为领域命令，不直接修改 Camera 或 Transform。
- 随机换位和水平后坐力可注入、可固定种子、可测试。
- H5 参数进入 Unity 配置资产，不建立网页运行时依赖。
- 每个切片都可独立验证和回退，不提前加入非目标系统。

## 下游实现验收案例

1. 真架枪点击后 300ms 自动探到底并保持，再次点击以 240ms 返回。
2. 假动作按住探出、松开返回，任何阶段都不产生射击。
3. 真架枪开始即可按住开火，完全探出前射击计数为 0，之后按约 108ms 连续发射。
4. 假 Peek 的真实可见轮廓达到 10% 后记录旧点位，缩回后显示黄色透视剪影。
5. 任一 Peek 完全返回后，用确定性随机源验证 `<25%` 换位且不选择原点位。
6. 敌人换位后，剪影和自动预瞄仍指向旧位置；下一次 Peek 开始时剪影隐藏。
7. fresh 情报只触发一次自动预瞄；没有新情报时保留手动准星。
8. 中央画面和开火指针都可调瞄，按钮不被 Aim Surface 抢占。
9. 后坐力按首发、2～4 发爬升、第 5 发起稳定，并保留可复现的随机水平偏移。
10. 暂停、失焦、PointerCancel 和场景禁用不会留下卡住的假动作或开火。

## 失败与停止判据

- 技术模型需要读取 UI、Camera 或实时敌人 Transform 才能决定规则。
- 黄色剪影跟随当前敌人而不是旧位置。
- 假动作可以射击，或完全探出前产生子弹。
- 随机换位与后坐力无法固定种子重现。
- 为了通过 Unity 测试而修改 accepted 设计语义。
- 技术切片引入非目标系统，导致后续实现无法独立验收或回退。
- 规格没有为持续输入卡死、首发卡顿或移动端性能定义验证方法。

## 状态门禁

- [x] proposed
- [x] discussing
- [x] review
- [x] approved
- [x] applied
- [x] closed

## 关联记录

- 技术规格：`Docs/20_Technical/CombatFoundationV1TechnicalDesign.md`
- 实现任务：`Tasks.md`
- 验证证据：`Validation.md`
- H5 上游证据：`Docs/30_Iterations/004-peek-camera/Validation.md`
