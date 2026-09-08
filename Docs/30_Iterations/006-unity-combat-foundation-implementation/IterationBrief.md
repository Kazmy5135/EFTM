---
title: 006 - Unity 战斗模块 V1 实现
work_id: "006"
work_type: development
work_status: verifying
change_id: null
target_document_ids:
  - DESIGN-CORE-COMBAT
  - TECH-COMBAT-FOUNDATION-V1
  - PROJECT-STATUS
  - TRACEABILITY-REGISTER
last_updated: 2026-09-08
---

# 006 - Unity 战斗模块 V1 实现

## 问题与背景

战斗模块 V1 已完成设计批准、H5 V1 验证和 Unity 技术设计批准。Unity 当前仍只有项目框架，没有玩法实现。本工作项按 U1～U5 的顺序完成最小战斗基础切片，并验证 H5 与 accepted 设计能够共同指导 Unity 开发。

## 权威基线

- 设计：`DESIGN-CORE-COMBAT`、`DDR-001`，accepted，commit `88d04fd`。
- 技术：`TECH-COMBAT-FOUNDATION-V1`，accepted，commit `60d1c13`。
- H5 行为参照：`H5-PEEK-CAMERA-001 V1`，tag `h5-peek-camera-v1`，commit `b9c8121`。
- Unity：`2022.3.62f2`。

## 目标

- U1：建立确定性战斗领域模型和 EditMode 测试。
- U2：建立战斗场景、UI Toolkit 输入、摄像机和 PlayMode 测试。
- U3：实现五点位、真实遮挡、情报、黄色剪影和旧位置预瞄。
- U4：实现预备开火、连续射击、调瞄和分阶段后坐力。
- U5：完成 Android 真机与 H5/Unity 行为对照。

## 当前切片

U1 与 U2 已完成自动化门禁：

- `CombatFoundationV1.unity`、场景级组合根与独立 PlayMode 测试程序集已经建立。
- UI Toolkit 已接入真架枪、假动作、开火与中央 Aim Surface，并按 pointer id 隔离输入。
- 摄像机由显式 Hidden/Exposed Pose 驱动位置、旋转、侧倾和瞄准偏移。
- PointerCancel、捕获丢失、窗口失焦、应用暂停和组件禁用均进入安全释放与返回路径。

U3/U4 已接入工作项 007 的仓库场景：五点位、20 点真实遮挡采样、旧世界位姿情报、黄色双 Pass 剪影、一次性预瞄、中心射线、连续射击和预热反馈池。摄像机角度符号已校正为上跳、下拖压枪；完全隐藏时镜头回到固定掩体 Pose，手动瞄准数值仍保留。

当前进入 U5 verifying：自动化、Windows 开发构建和桌面交互检查可在现有环境推进；Android Build Support 未安装，且尚未指定/接入验收手机。不得用桌面测试替代设计者真机体验验收。详见 `Validation.md` 与 `DeviceValidation.md`。

## H5 局部门禁

- 每项 Unity 行为必须在任务和验证记录中关联 accepted 设计条目与 H5 V1 证据。
- H5 是感知和时序参照，不是代码或架构来源。
- Unity 无法复现 H5 行为时必须记录差异，不能静默改写设计。

## 失败与停止判据

- Foundation 依赖 MonoBehaviour、UI、Camera、Physics 或场景 Transform 才能运行测试。
- 假动作能够射击，或真架枪完全暴露前产生 ShotRequested。
- 敌人随机换位和水平后坐力无法固定种子重现。
- fresh 预瞄重复消费，或敌人换位覆盖旧情报。
- U1 引入 U2～U5 的场景与表现内容。

## 状态门禁

- [x] proposed
- [x] developing
- [x] verifying
- [ ] implemented
- [ ] closed

## 关联记录

- 任务：`Tasks.md`
- 验证：`Validation.md`
- 技术基线：`Docs/20_Technical/CombatFoundationV1TechnicalDesign.md`
