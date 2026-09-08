---
title: 双侧掩体换边 Unity 技术修订稿
document_id: TECH-PROPOSAL-COVER-SWITCH-008
status: accepted
last_updated: 2026-09-08
authority: approved-change-record
---

# 双侧掩体换边 Unity 技术修订稿

> 2026-09-08：设计者确认“换边时间同意，文档通过，开始下一步”。本稿（含 0.95s 初值）整体批准，作为历史变更记录保留；现行增量契约已合入正式技术规格。后续体验确认的[直视通道换边修订](DirectSwitchRevision.md)取代下文 Turning、预转头/回看及 0.95s 总时长：当前直接横移 0.80s，固定通道朝向。下文待批措辞保留评审时原文，不再构成审批阻断。批准不等于测试通过，不授权提交或推送。

## 1. 审批范围与基线

- 设计依据：`DESIGN-CORE-COMBAT`、`DDR-001`，2026-09-08 已应用 008 换边扩展；设计者明确批准，见 [批准记录](IterationBrief.md)。本次设计修订尚未提交 Git，不能使用旧 `88d04fd` 指代换边规则。
- 既有技术：`TECH-COMBAT-FOUNDATION-V1`，accepted，commit `60d1c13`。保留模型/表现分离、UI Toolkit、Built-in、20 点遮挡采样、指针安全释放和射击预热等原则。
- 已检查实现快照：`5d0e3cb`；本稿新增类型、字段、接口、参数均为待实施方案，不是当前实现事实。
- 本稿通过后，将增量契约合入 `../../20_Technical/CombatFoundationV1TechnicalDesign.md`，更新其单掩体、仅假 Peek 采样和移动非目标等条目，再进入开发。审批前不覆盖其 accepted 正文，不创建第二套长期权威架构。
- 不要求新 H5；只继承原 V1 未变化行为的对照证据。008 尚无模型、运行结果或设备验收证据。

## 2. 不可改变的行为

- 初始右侧；右侧向左 Peek、左侧向右 Peek。两侧均以面向通道尽头的世界方向命名。
- 只在完全隐藏时单击换边；过程不可取消、反向或排队，不允许真假 Peek、开火、预备开火及手动调瞄。
- 先转向目的点，再连续平移并回看通道；玩家空间位置与镜头都要移动。
- 抵达后直接隐藏，无自动假 Peek、观察停留或额外缩回阶段。
- 跑动回看/抵达按实际轮廓 `10%` 获取旧情报；完成隐藏落位仅结算一次 `25%` 敌人换位，并显示本次有效情报剪影。
- 普通 Peek、射击节拍和后坐力规则不变；无伤害、20% 受击概率、弹匣、AI、自由导航或多人。
- 所有模型/环境布局修改先在 Blender 完成，Unity 不拼装几何替代。

## 3. 当前实现事实与必须修订处

以下路径相对于 `UnityProject/`，均为本轮直接读取的事实。

| 当前位置 | 已存在内容 | 008 必须修订 |
|---|---|---|
| `Assets/_Project/Runtime/Combat/Camera/PeekCameraPresenter.cs` | 一组 Hidden/Exposed；预瞄与射线都使用唯一 exposedPose | 双侧姿态、移动镜头唯一写入者、当前侧射线/预瞄 |
| `Assets/_Project/Runtime/Combat/Foundation/CombatFoundationModel.cs` | 仅 PeekMode/PeekPhase；ObserveEnemy 只接收 Fake；保存旧侧 yaw/pitch；FinishReturn 结算换位 | 独立换边状态、来源明确的观察、落位一次结算、跨侧预瞄输入 |
| `Assets/_Project/Runtime/Combat/Targeting/CombatTargetingPresenter.cs` | 20 点采样、旧世界 Pose 与 AimAnchor 已保存，真实目标和 Ghost 分离 | 扩展观察许可，复用旧世界锚点而非旧侧角度 |
| `Assets/_Project/Runtime/Combat/Presentation/CombatFoundationSceneRoot.cs` | Tick → 目标表现 → 镜头 → 事件 → 观察；失焦走安全返回 | 移动/相机同步、终点确认、预瞄求解前置、暂停锁存 |
| `Assets/_Project/Runtime/Combat/Input/CombatInputView.cs` / `CombatInputController.cs` | 三按钮、自管捕获、HUD 状态缓存、单指拖动 | 第四按钮、全层互斥、捕获清理与缓存键增加换边/侧别 |
| `ArtSource/Warehouse/build_warehouse.py` / `Warehouse.blend` | Blender 模型、贴图、FBX、碰撞清单由脚本生成 | 在源头修改双石板和关联空间，补充双侧锚点/路径清单 |
| `Assets/_Project/Editor/WarehouseEnvironmentBuilder.cs` | 导入 FBX，清单生成 BoxCollider/Prefab；检查 NearCover.x≈1.14、21 网格及单侧姿态 | 版本化清单和双侧几何验证；移除旧单侧硬编码假设 |

重要接入风险：环境重建本身不会保证 `CombatOccluder` Layer；当前是在 `CombatFoundationAdaptersBuilder.Install` 给环境赋层，而该 Install 遇到已有 CombatEncounter 会拒绝执行。008 不能重跑全部适配器安装来修补 Layer，也不能删除重建已有遭遇。应独立、幂等更新环境 Layer 和双侧绑定，保留射击/目标引用。

## 4. 模块职责与数据契约

继续使用现有程序集和 Foundation 依赖方向，不引入 Cinemachine、Input System、Tween、NavMesh 或新的全局服务。

| 对象（拟定名） | 职责 | 不得负责 |
|---|---|---|
| `CombatFoundationModel` | 侧别、换边阶段/计时、输入许可、情报版本、一次性结算、随机源 | Transform、物理、模型导入 |
| `CoverSideRig`（新增场景配置组件） | 两侧 PlayerAnchor、Hidden/Exposed Camera Pose、双向路径和预转头朝向；启动验证 | 自己计时或结算敌人换位 |
| `CoverTransitionPresenter`（新增） | 由快照进度求玩家位置、身体朝向和移动相机基础 Pose；更新 PlayerRoot | 直接写 Camera Transform、产生战斗命令 |
| `PeekCameraPresenter` | Camera Transform 唯一写入者；合成原地 Peek 或换边 Pose，按当前侧求射线和旧世界点预瞄 | 自己维护换边进度或跟踪真实敌人 |
| `CombatTargetingPresenter` | 对最终本帧相机采样、生成数值 Observation、应用目标/剪影 | 由可见性丢失次数触发随机换位 |
| `CombatInputView/Controller` | 换边 PointerUp 转命令、按钮状态、指针安全释放 | 通过隐藏按钮替代模型互斥 |
| `SceneRoot` | 原子命令适配、应用顺序、暂停/失焦、聚合配置错误 | 把移动状态藏在协程中与模型并行 |

### Foundation 新增数据

- `CoverSide { Right, Left }`；显式初始 Right，不依赖数组默认顺序。
- `CoverSwitchPhase { Idle, Turning, Traversing, Landing }`。Landing 是路径末段减速，**不是抵达后的观察状态**。
- `CoverSwitchSnapshot`：SourceSide、TargetSide、CurrentSide、Phase、Elapsed/Progress、ActionId、AwaitingArrivalConfirmation、Suspended。CurrentSide 只在确认落位时变为目标侧。
- `CanSwitchCover` 要求：Idle、未暂停、PeekMode=None、PeekPhase=Hidden、无待确认落位。所有入口统一使用模型许可，不能仅检查 PeekPhase=Hidden（换边期间 Peek 本身也未探出）。
- `ObservationSource { FakePeek, CoverSwitch }`，数值 Observation 含 VisibilityRatio、EnemyPositionIndex、旧 `IntelWorldPose`、ActionId；模型拒绝阶段不符或过期 ActionId 的观察。
- `IntelRevision`、`ObservedThisAction` 与已有 HasIntel / HasPendingSnap / GhostVisible 分离。新鲜预瞄只能消费一次；开始新动作清本轮观察标记，不清全部历史情报。
- 新事件：CoverSwitchStarted、CoverSwitchPhaseChanged、CoverSwitchArrived；沿用 IntelAcquired、IntelGhostHidden/Shown、EnemyRelocated。不要伪造 FakePeekPressed/Released 或 PeekFullyHidden 来复用移动结算。

### 命令与互斥

- `SwitchCoverRequested` 在合法 PointerUp 触发一次；方向由模型当前侧决定，不接受 UI 任意指定位置。
- Turning/Traversing/Landing 与等待确认期间，模型拒绝 ToggleTrueAim、FakePeekPressed、FirePressed、AimDelta 和再次换边；Release/Cancel 保持幂等。
- 只有成功接受换边才隐藏旧 Ghost、增加 ActionId 并锁定目标侧；非法点击不清情报、不消耗随机数。
- 完成动作只释放操作锁，不自动发送任何 Peek/Fire 命令。

## 5. 运动、镜头与建议初值

### 空间方案

- 采用预定义、经过碰撞检查的双向三维路径；不用瞬移、负缩放镜像或运行时寻路。
- 引入场景级 `PlayerRoot` 作为玩家空间位置，Camera 保持独立写入；运动相机点由同一移动进度和相对视点偏移求得。无需本轮新增完整角色皮肤/动画，但不能只换镜头而没有玩家位置。
- 两侧使用独立 Hidden/Exposed Pose。它们以稳定的 CoverRig 空间存储，不能挂在正在移动的 PlayerRoot 下造成二次位移。
- 两侧共用通道前向参考。静止时姿态按该侧 Peek 参数求解；左右 roll 与横移分别验收。无新情报的手动瞄准方向以共用前向/水平轴保存，在侧别姿态合成时转换，避免相反 roll 让同一 yaw/pitch 改变世界指向。
- 换边路径优先为连续曲线及预计算弧长表；位置单调沿路径前进。减速只改变行进速度，不增加观察停留。终点 Pose 与目标 HiddenPose 一致，误差阈值见验证。
- Turning 阶段位置不动，朝目标路径方向预转头；Traversing 开始即向通道方向回看，最迟在 Landing 开始前回正。回看目标为固定通道方向，不是敌人 Transform。
- 镜头运动中不合成手动瞄准拖动；保存原手动方向，恢复普通 Peek 时使用。换边开始前已无射击；残余后坐力按原规则衰减，不重置手动瞄准。

### 技术评审建议值（未批准、非最终平衡）

| 参数 | 建议初值 | 约束 |
|---|---:|---|
| 预转头时长 | 0.15s | 位置不动；大于 0 |
| 总移动时长 | 0.80s | 含末段 Landing，不另加停留 |
| Landing 时长 | 最后 0.20s | 属于上述 0.80s，持续移动至终点 |
| 回看时长 | 移动前 0.55s 内完成 | 与移动重叠；不延迟至落位后 |
| 预转头最大偏航 | 60° | 左右符号相反，实际角由目的点方向限幅 |
| 额外自动观察/跑动晃动 | 0s / 默认关闭 | 不增加自动 Peek；先验收平移/转头 |
| 几何位置/方向容差 | 0.02m / 0.5° | 终点和导入检查用，不是瞬移许可 |

推荐单次约 0.95s。路径距离以 Blender 审核结果为准，记录实际速度；若该时长让移动过快或产生眩晕，调整配置并提交体验记录，不放宽遮挡或穿墙约束。

## 6. 同帧时序、观察与单次结算

### 正常换边帧

1. 从唯一时间源取已限幅 delta；模型推进运动阶段和候选进度，末端先标记待确认，不立即切侧/换敌人。
2. 目标表现保持当前敌人点位；TransitionPresenter 更新 PlayerRoot 并计算移动视点；CameraPresenter 写入最终相机 Pose。
3. 只有 Traversing/Landing 允许采样，使用**已经应用的本帧相机**与真实 Collider，20 等权点，2/20 达阈值。Turning 与落位后不采样情报。
4. 模型接收带当前 ActionId 的 Observation；Ghost 位置只来自旧 Pose 数值，不能读取新敌人位置。
5. 候选进度达到终点时，确认玩家/相机终点、路径未阻塞及最终敌人完全不可见，再提交 `ConfirmCoverArrival(ActionId)`。
6. 模型一次性写 TargetSide→CurrentSide，退出移动，调用原有随机源结算一次换位；随后按 ObservedThisAction 决定 GhostVisible，再发布 CoverSwitchArrived / EnemyRelocated（如发生）/ IntelGhostShown（如有效）。
7. 应用新目标位置、历史 Ghost 与最终 UI。次帧回到普通隐藏状态，不产生自动观察窗口。

结束确认是同帧逻辑屏障，不是新增等待动画；正常情况在终点帧完成。失败则报告配置/路径错误并禁止继续推进该遭遇，不把仍可见的落点当作安全隐藏，不反复随机结算。重复 ActionId 确认无效。

### 采样与性能

- 一次 SceneRoot.Step 最多一次 20 点采样，普通假 Peek 与换边互斥；终点确认复用本帧可见性结果。非观察阶段不读取目标信息给模型。
- 不补算“未呈现帧”的移动观察来凭空获取情报；30Hz 与 60Hz 均须有足够路径可见窗口达到阈值。过窄窗口先调整 Blender 空间/动作参数，不悄悄降阈值。
- 终点几何安全通过启动检查覆盖两侧×五点位；运行时“可见率 0”不能代替源场景的真实遮挡验收，单纯看向地面或目标出屏不视为合格隐藏落点。
- 热路径缓存路径、数组和事件缓冲，不用 LINQ、GetComponents 或每帧新建曲线；沿用原模型 delta 上限，不积累暂停时长。

## 7. 跨侧预瞄与射击

当前模型保存并直接使用观察时计算的 yaw/pitch，跨侧会错位。改为在真架枪命令被接受时基于旧世界锚点求解：

1. SceneRoot 检查命令合法、HasPendingSnap 和 IntelRevision；仅从 `LastSeenIntel.WorldPose.AnchorX/Y/Z` 读点。
2. CameraPresenter 使用**当前侧** ExposedPose 与该旧点求出可用 yaw/pitch；不读取真实 Enemy/AimAnchor。
3. 将含解算值、侧别及 IntelRevision 的命令原子交给模型；模型验证版本与状态，接受真架枪并一次消费 pending。不能先消费，再在下一帧补瞄准。
4. 无新情报的命令不求解、不吸附，保留手动方向。无效数值或未绑定正确侧姿态报告错误，不回退为实时敌人位置。
5. ShotRay 使用当前侧 ExposedPose、该发施加冲量前的瞄准/后坐力；保持完全暴露后次更新首发与 108ms 节拍。

资产验收必须让旧世界锚点在两侧约定瞄准范围内；允许真实掩体挡住该点，不保证命中。不能用扩大瞄准范围或读实时位置掩盖跨侧空间错误。

## 8. UI、暂停与生命周期

- 新按钮建议名 `switch-cover`，置于假动作正上方、圆形开火右侧可触达位置。布局以 Screen.safeArea 和 1080×2160 参考坐标为准，复验 9:18、9:19.5；不固定死角使按钮出屏。
- PointerDown 仅记录/捕获；同一指针合法 PointerUp 发一次命令；PointerCancel/CaptureOut 清待触发，不取消已启动换边。避免 Button Clickable 与自管捕获双重触发。
- 按钮范围不属于 AimSurface，即使置灰也不把点击穿透成拖动。更新 HUD 缓存键加入 CurrentSide、SwitchPhase、暂停/可操作状态，不能只沿用现有 PeekMode/Phase 位掩码。
- 新中文文本加入静态 SDF 字形范围，重新生成字体资产并检查无缺字；不为按钮安装另一套 UI/输入框架。
- 输入适配层、模型、射击入口三层都必须拒绝移动中射击，不能仅把开火键隐藏。
- SceneRoot 分别记录应用 pause 和 focus 标志，二者都恢复才推进换边。失焦/暂停释放捕获、清待触发指针，冻结移动阶段/进度；恢复不重放旧点击、不积累后台 delta。
- 普通 Peek 失焦仍按原安全返回处理；换边不调用会修改位置的通用“回原掩体”逻辑。OnDisable/场景卸载终止本次遭遇并释放资源，不提交未完成落位或随机换位；重新加载初始右侧。

## 9. Blender 源模型与 Unity 接入合同

### 9.1 已有路径与工具

- 源：`UnityProject/ArtSource/Warehouse/Warehouse.blend`、`build_warehouse.py`、`README.md`。
- 导出：`UnityProject/Assets/_Project/Art/Warehouse/Models/Warehouse.fbx`、`warehouse-manifest.json`、Textures。
- 接入：同目录 Materials、`WarehouseEnvironment.prefab`；`Assets/_Project/Scenes/CombatFoundationV1.unity`。
- 现有流程：[BlenderWorkflow.md](../../../UnityProject/ArtSource/BlenderWorkflow.md)。其中工具版本/路径为历史记录，开发前重新只读确认可用性，不重装工具来代替检查。
- Blender 当前脚本会先清场并重建、覆盖 .blend/FBX/贴图。**本轮不运行**；开发时先检查打开场景、路径和未保存状态，保存受保护工作副本并核对手工变更，不能直接执行覆盖。

### 9.2 制作与导出

1. 在 Blender 编辑源模型/生成脚本，将近端布局改为左右石板，保留真实厚度、米制尺度和现有风格；关联地面、墙裙、门框或挡路道具一并在 Blender 处理。
2. 本轮优先保留现有材质和贴图；脚本支持受控输出/工作副本或定向导出，避免为两块石板无关重生成全套纹理。Blender 内手工修改需同步到生成来源，不留下下一次重跑即丢失的差异。
3. 在 Blender 校验并渲染俯视、左右隐藏/Peek 及路径中点。图片保存 `codex-chat-images/`，不提交；图像不是碰撞验证替代品。
4. 统一导出 FBX 与清单。源脚本当前 helper 使用 Unity 米制坐标 `(X,Y,Z)`，转 Blender 为 `(-X,-Z,Y)`，FBX `-Z forward / Y up`；沿用并用非对称命名锚点验证，禁止单独再做一次轴翻转。
5. 新清单建议 `schemaVersion=2`，包含两个命名 Cover 记录（Left/Right）、Collider 位置/尺寸、双侧 PlayerAnchor/CameraPose、双向路径控制点及几何/生成器 revision。它们由 Blender 导出，不手写 Unity 环境布局补丁；材质、灯光和玩法参数与几何来源区分。
6. 源 .blend、生成脚本、FBX 与清单的 source revision 一致后才接入。保留既有资产路径和 .meta GUID；新资产 .meta 由 Unity 生成。

### 9.3 Unity 接入与迁移

- 通过 FakeUnityCLI 先检查版本/编辑器状态；仅在已批准开发阶段、确认不覆盖未保存场景和退出 Play 后更新资产。CLI 不可用先说明限制，桌面控制最后考虑。
- 修改 WarehouseEnvironmentBuilder 的清单解析与验证：不再硬编码 NearCover 单侧中心或 21 网格；要求 Left/Right 构件、米制方向、源 revision 和清单网格/Collider 对应。schema 缺失或仍为单侧时拒绝迁移，不静默补一个石板。
- 环境更新按清单创建材质映射/碰撞，递归正确设置 CombatOccluder；预览/路径锚点无 Renderer/Collider，不参与射线。Unity 不改环境子网格的位置/形状来通过测试。
- 新增幂等的 `UpgradeCoverSwitch` 编辑器迁移入口：先验证全部来源，再更新环境引用和 CoverSideRig/PlayerRoot/UI 配置。保留 CombatEncounter、目标/剪影、音频池、Settings、场景 GUID，不调用拒绝已有遭遇的全量 Install。
- 扩展原 SceneBuilder 的全新场景生成分支，既要验证升级旧场景，也要验证从源重建；旧单侧姿态仅在引用完全迁移后移除，不能留下双份相机控制器。
- 五个敌人原点位优先保留。若某个模型/挡物需要改造以满足双侧视野，必须回 Blender 制作；当前 Unity 生成的敌方临时挡板不得被当作绕过 Blender 的调整入口。若必须调整点位语义，先回设计评审。
- 最终保存前验证几何、引用、Layer 和相机/玩家路径；失败保留原场景资产，不提交半迁移输出。需要修空间时回 Blender 迭代。

## 10. 路径安全与错误处理

- 启动/编辑器验证两侧×五点位：隐藏位由近端掩体真实遮挡；暴露位记录实际可见率；每个点位至少一侧达到 10%，左右各自存在有效射击机会。
- 玩家包络建议为半径 0.25m、高 1.8m 的配置胶囊（不新增可见模型）；沿完整路线检查胶囊扫掠，不能仅测试中心线。
- 镜头以 near clip 平面角点和路径段扫掠检查与墙体的间距；清单变更后重新验证，不能依靠提高 near clip 隐藏穿模。
- 路径运行时若碰撞/落点确认失败：停在最后有效位置、停火、输出一次明确错误并禁用该遭遇输入；不穿过去、不瞬移、不循环消费随机数。当前无动态障碍玩法，出现这种情况视为资源/配置缺陷，不另做导航绕行。
- 非法配置聚合报告：两侧缺失/重复、姿态无效、路径断裂、时长非法、Layer 错误、模型/清单版本不符、UI 字形缺失。
- 目标销毁时不再观察；仍可完成安全换边，但不生成新情报，不因为空目标抛异常或让按钮永久锁定。

## 11. 验证与交付

详见 [任务分解](Tasks.md)，至少完成：

- EditMode：全阶段命令许可、状态与 ActionId、不可取消/排队、暂停冻结、9.9%/10%、本轮 Ghost、一次随机结算与无新情报不吸附。
- PlayMode：左右两侧、五点位、完整连续路径、相机侧倾、移动回看方向、跨侧旧世界锚点与两侧射线、无自动 Peek、30/60Hz 行为和终点帧时序。
- 输入：实际 UI 捕获/释放、多指干扰、按钮排除 AimSurface、失焦恢复；另用真实 Game 鼠标/手机触控验证，不能只向 Button.SendEvent 就宣布硬件输入通过。复发鼠标缺失参考 006 恢复记录。
- 资源：Blender 源与导出一致、原始轴向/尺度、双石板/Collider、清单拒绝旧格式、幂等迁移及全新重建；无丢引用/重复控制器。
- 性能：预热后持续换边采样与呈现无持续托管分配；记录实际帧时间、20 点上限、路径验证开销；Windows 结果不替代真机。
- 设备：Windows 开发包用于输入/空间检查，Android 模块与指定手机仍是独立 U5 条件；安装/授权不由文档批准自动执行。
- 回退：在开发前记录实际干净 commit；未来需要回退时成组恢复 Blender 源/脚本、导出/清单、环境/场景、代码和契约，保留 `.meta`。不自动执行 reset/delete，不只回退 FBX 留下不匹配场景。

## 12. 本轮待技术审批

1. 独立换边状态与双侧 Rig；固定连续路径、不引入导航框架，Camera 单一写入者。
2. 约 0.95s 初始节奏，移动内回看、末段减速，无自动观察；参数可调并需体验验收。
3. 终点确认后一次结算；旧世界锚点在当前侧原子预瞄，暂停/失焦冻结而非瞬移。
4. Blender 源/脚本 → FBX/版本化清单 → CLI 幂等接入；不在 Unity 改模型。
5. S1～S6 开发切片及测试门禁；通过前只保留文档，不执行建模、代码或场景迁移。
