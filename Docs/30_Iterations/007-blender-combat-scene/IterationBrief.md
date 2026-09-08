---
title: 007 - Blender 战斗场景美术与 Unity 接入
work_id: "007"
work_type: art
work_status: applied
last_updated: 2026-09-07
---

# Blender 战斗场景美术与 Unity 接入

## 目标与顺序

1. 生成俯视角场景效果图，等待设计者确认。
2. 效果图通过后，用 Blender 搭建真实三维场景。
3. 将模型接入 Unity，替换当前简化场景并验证摄像机控制的核心玩法效果。

## 当前提案

- 首版方向：写实工业仓库通道，混凝土、旧绿墙裙、木箱、金属柜与贴墙管线。
- 俯视剖切图用于美术与空间布局评审；正式玩法仍使用第三人称掩体摄像机。
- 参考现有场景构建器的约 4 米宽、24 米长、3 米墙高比例，以及近端右侧高墙和左侧开口。
- 保留一处近端掩体、一条主要枪线、五个敌人候选位置的设计约束。图中道具只表达美术意向，位置、高度和遮挡比例须在实际三维场景中校验。
- 本提案不修改 accepted 玩法、摄像机参数或技术基线。美术批准不自动证明手机端性能或遮挡验收通过。

## 上游依据

- `DESIGN-CORE-COMBAT`：`Docs/10_Design/CoreCombatDesign.md`，accepted，2026-09-03。
- 技术参考：`Docs/20_Technical/CombatFoundationV1TechnicalDesign.md`，accepted。
- 当前实现参考：`UnityProject/Assets/_Project/Editor/CombatFoundationV1SceneBuilder.cs`。
- 本次读取时 Git HEAD：`7af287b14abcae92f516b3e907c057f708bd4b20`。

## 交付与验证

- 使用内置 image_gen 生成首版效果图。
- 本地临时图片：`codex-chat-images/warehouse-topdown-v1.png`，PNG，1024 × 1536，已验证可读取；图片不提交 Git。
- 完整生成提示词：`codex-chat-images/warehouse-topdown-v1-prompt.txt`，本地临时记录。
- 已检查画面中的近端高墙、单一通道和不同高度远端道具。精确米制尺寸、五个候选点的射线可见性尚未验证。
- 2026-09-07：设计者回复“好，进行下一步”，确认首版效果图并授权继续建模与接入。
- Blender 建模、FBX 导出和 Unity 环境替换已完成；19 项 EditMode、7 项 PlayMode 与框架、尺度、枪线检查通过。玩法与摄像机参数保持现有基线。证据见 `Validation.md`，手机视觉与体感待设计者验收。

## 接入边界与回退

- 可编辑源文件和生成脚本位于 `UnityProject/ArtSource/Warehouse/`；FBX、贴图、材质与环境 Prefab 位于 `UnityProject/Assets/_Project/Art/Warehouse/`。
- 以当前 Built-in 渲染管线的 Standard 材质接入，不升级 Unity 或改变渲染管线。
- 替换现有环境根对象，保留 SceneRoot、UI、摄像机 Pose、设置资产与场景 GUID；场景重建入口同步引用新环境 Prefab。
- 环境使用独立的简化 BoxCollider，真实渲染网格保持可编辑构件；不通过运行时图像、billboard 或相机贴图伪造场景。
- U3 真实敌人、五点位可见性 Rig、情报剪影与完整射击适配仍由工作项 006 承担，本次环境接入不宣称这些模块已完成。
- 回退可恢复本次前的 `CombatFoundationV1.unity` 与场景构建器；新增美术资产不影响旧场景引用。不执行自动回退或删除。

## 后续验收

- 模型构件具有真实厚度、明确尺度，并为地面、墙体和掩体配置适当碰撞体。
- 实际隐藏摄像机遮住敌人，探出过程产生连续视差与遮挡变化；校验五点位在竖屏中的可见性。
- 保留现有真假 Peek、情报、预瞄和射击行为；按实际接入范围执行 Unity 检查与设计者视觉验收。
