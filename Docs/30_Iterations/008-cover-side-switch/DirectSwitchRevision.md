---
title: 008 换边体验修订：面向通道直接横移
status: accepted
authority: approved-change-record
last_updated: 2026-09-08
---

# 面向通道直接横移

设计者体验后要求取消起步前朝目标掩体转头，并确认“始终面朝通道、不自动跟踪敌人”。本次确认替代 008 初版的预转头与回看流程，继续在同一工作项实施，不增加新 H5 或模型制作任务。

- 点击换边立即进入移动，没有 0.15s 起步等待。保留 0.80s 横移及其中最后 0.20s Landing，单次总时长由 0.95s 改为 0.80s。
- 玩家与镜头持续面向固定通道方向，不看目标掩体、不回看、不读取实时敌人位置或旧情报来转动镜头；移动依然产生真实视差。
- 完全隐藏可发起、输入互斥、暂停冻结、10% 实际可见轮廓采样、落位后一次 25% 换位、黄色旧位置剪影与下一次真架枪的原子预瞄不变。
- 技术采用 Idle → Traversing → Landing → Idle；移除 Turning、转头/回看进度及相应时长配置。镜头和 PlayerRoot 朝向采用 CoverSideRig.ForwardReference；两侧 HiddenPose 必须与该方向一致，避免起止跳转。
- 双石板、路径、隐藏/暴露位置不变，无几何修改，因此本轮不重建 Blender/FBX。既有 schema v2 导出的 lookYawDegrees 和 motion 中 turnSeconds/lookBackSeconds 是初版历史元数据，Unity 不再读取；运行时运动参数以 CombatFoundationSettings 为准。
- 回归覆盖双向首个更新即位移、全程恒定朝向、敌人位置变化不改变镜头方向、30/60Hz 的 0.80s 时序及原情报/射击/暂停/阻塞流程。

生效文档：CoreCombatDesign.md、CombatFoundationV1TechnicalDesign.md。原 DesignProposal.md / TechnicalDesign.md 保留初次批准内容，后续引用以本修订及正式规格为准；旧测试/开发包证据不自动代表新节奏。

实施结果：Unity 代码、场景绑定、静态状态字形与正式文档已同步；本修订 EditMode 37/37、PlayMode 25/25、Windows 构建通过。真实输入和手机舒适度仍待体验，详见 [验证记录](Validation.md)。本轮未提交或推送 Git。

后续修订：2026-09-08 设计者批准 [移动侧探方案](MovingLeanProposal.md)。本记录的全程相机旋转恒定由前向固定、受控侧倾与头部偏移替代；立即移动、0.80s、无追踪及隐藏落位规则继续有效。
