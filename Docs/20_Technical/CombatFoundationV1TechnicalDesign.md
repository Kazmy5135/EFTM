---
title: 战斗模块 V1 Unity 技术设计
document_id: TECH-COMBAT-FOUNDATION-V1
status: accepted
last_updated: 2026-09-08
---

# 战斗模块 V1 Unity 技术设计

> 当前契约：2026-09-08 设计者批准 008 双侧换边，随后体验确认取消预转头/回看，改为面向通道直接横移 0.80s（见 008/DirectSwitchRevision.md）。增量正文见本文件“008 双侧换边已批准增量契约”，取代基础层中的单掩体、仅假 Peek 采样、移动非目标和统一失焦返回假设；其余基础契约保留。以下“当前事实”是首次基线建立时的历史事实。批准修订尚未提交 Git；原 `60d1c13` 不包含本扩展。

## 负责人和批准

- 技术负责人：Unity Development Agent。
- 当前状态：accepted；设计者于 2026-09-04 批准全部推荐技术选择。
- 下一门禁：建立独立 development 工作项，从 U1 确定性领域模型开始实现。

## 权威上游

- 产品定义：`DESIGN-PRODUCT-DEFINITION`，`Docs/10_Design/ProductDefinition.md`，accepted，2026-09-03，设计基线 commit `88d04fd`。
- 核心战斗：`DESIGN-CORE-COMBAT`，`Docs/10_Design/CoreCombatDesign.md`，accepted，2026-09-03，设计基线 commit `88d04fd`。
- 设计决策：`DDR-001`，`Docs/10_Design/Decisions/DDR-001-peek-intel-and-assisted-aim.md`，accepted。
- H5 行为参照：`H5-PEEK-CAMERA-001 V1`，Git tag `h5-peek-camera-v1`，实现 commit `b9c8121`。
- H5 验证证据：`Docs/30_Iterations/004-peek-camera/Validation.md`。
- 适用工作项：`005-unity-combat-foundation`。

权威顺序如下：

1. accepted 设计决定玩家规则与验收语义。
2. 本技术规格决定 Unity 内部职责、数据、生命周期和验证方式。
3. H5 只提供手感、时序和视觉对照，不提供可移植架构。
4. Unity 实现事实不能静默修改设计；发现冲突时停止相关切片并回报。

## 目标

在 Unity 2022.3.62f2 中建立一个可独立运行、可自动测试的战斗模块 V1 基础切片，跑通以下链路：

```text
accepted 设计规则
→ H5 V1 可观察行为与参数证据
→ Unity 技术契约
→ Unity 最小实现
→ EditMode / PlayMode / 手机行为验证
→ 实现事实回传追踪登记
```

## 范围与非目标

### 范围

- 一处玩家掩体、一条固定通道和五个敌人候选点位。
- 真架枪点击锁定/再次点击返回。
- 假动作按住探出/松开返回，且不能开火。
- 300ms 探出、240ms 缩回和摄像机侧倾的配置化表现。
- 假 Peek 期间基于真实遮挡的 10% 敌人可见轮廓判断。
- 最后观察位置、黄色透视剪影、25% 隐蔽换位和一次性自动预瞄。
- 真架枪开始即可预备开火，完全探出后才连续发射。
- 中央画面和开火指针拖动调瞄、分阶段后坐力和随机水平偏移。
- 1080×2160 竖屏、安全区域和单指输入。

### 非目标

- 弹匣、换弹、生命、伤害、护甲、伤势、死亡和命中概率曲线。
- 玩家左右切换掩体、手雷、趴下、治疗和复杂射角。
- 敌人决策 AI、多人同步、延迟补偿、回放和反作弊。
- 搜索、背包、探索、撤离与局外成长。
- H5 的 DOM、Three.js 对象或 JavaScript 状态结构迁移。
- 最终角色模型、动画、武器资产、材质和渲染质量。

## 已验证的当前事实

- Unity 工程版本为 `2022.3.62f2`。
- 当前使用 Built-in Render Pipeline；`GraphicsSettings` 未绑定 Scriptable Render Pipeline Asset。
- 当前 `EFTM.Runtime` 程序集无第三方程序集引用。
- 当前只存在 `GameBootstrap`、`ServiceRegistry`、`IGameService` 和项目框架验证器。
- `GameBootstrap` 在首场景前创建并跨场景存活；`ServiceRegistry` 按注册顺序初始化、逆序关闭。
- `Bootstrap.unity` 是 Build Settings 中第一个启用场景。
- 当前已有 NUnit EditMode 测试程序集，尚无 PlayMode 测试程序集和战斗场景。
- 当前 manifest 未引入 Input System、URP、Cinemachine 或 uGUI 包；运行时可使用 UnityEngine、物理模块和 UI Toolkit。
- H5 V1 已通过 24 项自动化及目标视口、浏览器、局域网验证，但这些证据不能替代 Unity 测试。

## 技术原则

1. **规则模型与 Unity 表现分离。** 状态转换、计时、随机换位、情报和后坐力阶段由可测试模型拥有；Transform、Camera、Renderer、Physics 和 UI 只是适配层。
2. **场景级战斗生命周期。** 单次战斗遭遇由场景组合根拥有，不注册为跨场景全局服务；`GameBootstrap` 继续只承担应用级服务组合。
3. **输入转换为命令。** UI Toolkit 指针事件只生成领域命令，不直接移动摄像机、敌人或修改射击状态。
4. **表现消费快照与事件。** 摄像机、HUD、剪影、弹道和目标只读取模型快照或一次性事件，不反向拥有规则。
5. **随机必须可重放。** 敌人换位和水平后坐力通过可注入随机源产生；测试固定种子，运行时记录种子。
6. **配置与运行状态分离。** V1 参数来自只读配置资产，运行状态不写回 ScriptableObject。
7. **无每帧托管分配。** 可见性检测、瞄准和射击热路径复用缓冲区，不在 Update 中创建集合、材质或临时 GameObject。

## 目标目录与依赖方向

首个切片继续使用现有 `EFTM.Runtime` 程序集，不提前拆分新 asmdef；通过目录和 namespace 建立边界。只有当编译时间或依赖隔离出现实际问题时，再单独提出程序集 ADR。

```text
Assets/_Project/Runtime/Combat/Foundation
  纯状态、命令、配置快照、随机接口、事件

Assets/_Project/Runtime/Combat/Input
  UI Toolkit 指针输入 → Foundation 命令

Assets/_Project/Runtime/Combat/Camera
  Foundation 快照 → Unity Camera 位姿

Assets/_Project/Runtime/Combat/Targeting
  可见性采样、点位、旧情报、预瞄适配

Assets/_Project/Runtime/Combat/Weapons
  连射节拍、射线、后坐力、弹道表现

Assets/_Project/Runtime/Combat/Presentation
  场景组合根、HUD、剪影、反馈和对象绑定

Assets/_Project/Tests/EditMode/Combat
  领域状态与确定性规则测试

Assets/_Project/Tests/PlayMode/Combat
  场景输入、摄像机、遮挡、射击和生命周期测试

Assets/_Project/Scenes/CombatFoundationV1.unity
  V1 行为验证场景
```

依赖只能朝向 Foundation：

```text
Input ─┐
Camera ├─→ Foundation ←─ Targeting
Weapons┤
Presentation ────────────┘
```

Foundation 不读取 `Input`, `Camera`, `Renderer`, `Physics.Raycast` 或 UI 元素。Unity 适配层可以依赖 Foundation，适配层之间只通过组合根或显式接口协作。

## 模块、数据与状态归属

| 对象 | 所有者 | 输入 | 输出 | 约束 |
|---|---|---|---|---|
| `CombatFoundationModel` | Foundation | 命令、delta time、可见率、随机值 | `CombatSnapshot`、领域事件 | 唯一规则状态源；不读取场景对象 |
| `CombatFoundationConfig` | Foundation | 配置资产转换结果 | 时长、阈值、概率、射速、后坐力参数 | 构造后只读；启动时验证 |
| `CombatFoundationSettings` | Presentation | Inspector 序列化字段 | `CombatFoundationConfig` | ScriptableObject 仅保存配置，不保存运行状态 |
| `CombatInputCommand` | Foundation | 指针适配层 | 模型状态转换 | 每条命令包含类型、pointer id、归一化坐标和时间 |
| `CombatInputView` | Input | UI Toolkit PointerDown/Move/Up/Cancel | `CombatInputCommand` | 不直接操作 Camera 或模型外对象 |
| `PeekCameraPresenter` | Camera | `CombatSnapshot` | Camera Transform/FOV | 使用快照中的归一化进度插值隐藏/暴露位姿 |
| `VisibilitySampleRig` | Targeting | 敌人身体采样 Transform | 固定采样点缓存 | V1 默认 20 点；权重总和固定 |
| `EnemyVisibilityEvaluator` | Targeting | Camera、采样点、遮挡 LayerMask | 0..1 可见率 | 只在假 Peek 激活；忽略敌人自身碰撞体 |
| `EnemyPositionSet` | Targeting | 五个位置和各自掩体引用 | 当前点位 Pose | 数量不足或重复引用时阻止场景启动 |
| `LastSeenIntel` | Foundation | 有效假 Peek 观察 | 旧点位、旧瞄准点、是否待吸附、剪影状态 | 与敌人当前真实位置分离 |
| `IntelGhostPresenter` | Presentation | `LastSeenIntel` 事件 | 黄色透视剪影 | 只使用记录 Pose；禁止追踪真实敌人 Transform |
| `AimController` | Foundation/Camera | 预瞄命令、归一化拖动量、后坐力 | yaw/pitch 偏移 | 自动预瞄只消费一次；手动偏移受配置范围限制 |
| `BurstFireModel` | Foundation | trigger held、完全暴露、delta time | `ShotRequested` | 完全暴露前只预备；松手重置连射阶段 |
| `ShotRaycaster` | Weapons | Camera 中心射线、`ShotRequested` | 首个命中结果 | 使用 LayerMask；不遍历无关 Collider |
| `RecoilModel` | Foundation | 发数、随机源、delta time、拖动修正 | 垂直/水平偏移 | 第 1 发起跳、2～4 发爬升、5 发起稳定 |
| `CombatFoundationSceneRoot` | Presentation | 场景引用和 Settings | 创建模型、绑定适配层、帧驱动 | 场景级生命周期；OnDisable 必须释放输入 |

## 配置契约

`CombatFoundationSettings` 至少提供：

| 字段 | V1 初始值 | 验证 |
|---|---:|---|
| `peekOutDurationMs` | 300 | 必须大于 0 |
| `peekReturnDurationMs` | 240 | 必须大于 0 |
| `peekRollDegrees` | 约 7 | 限制在可配置安全范围 |
| `visibilityThreshold` | 0.10 | 0..1 |
| `enemyRepositionChance` | 0.25 | 0..1 |
| `enemyPositionCount` | 5 | 场景必须恰好提供 5 个有效点位 |
| `shotIntervalMs` | 108 | 必须大于 0 |
| `stabilizeFromShot` | 5 | 必须大于 1 |
| `aimYawLimit` / `aimPitchLimit` | 由 H5 对照调入 | 必须大于 0 |
| 后坐力参数 | 由 H5 V1 对照调入 | 不允许负时长或 NaN |

H5 中的弧度常量不是 Unity 权威数据。Unity 使用角度或归一化输入配置，并通过行为对照校准到相同体验。

## 领域状态与不变量

### 主状态

```text
Hidden
├─ HoldFakePeek → FakePeeking → FakeHolding
│                    └─ Release → FakeReturning → Hidden
└─ ToggleTrueAim → TruePeeking → TrueAiming
                     └─ ToggleTrueAim → TrueReturning → Hidden
```

实现可用 `PeekMode` 与 `PeekPhase` 两个正交枚举避免状态爆炸：

- `PeekMode`: `None`, `Fake`, `TrueAim`。
- `PeekPhase`: `Hidden`, `Peeking`, `Holding`, `Returning`。

### 必须保持的不变量

- `Fake` 模式永远不能产生 `ShotRequested`。
- `TrueAim` 未达到完全暴露时，trigger held 只能进入 armed，不得产生射击。
- `Hidden` 时真实敌人不因情报剪影变成可见目标。
- 敌人换位只在一次 Peek 已开始且最终重新达到完全隐藏时结算一次。
- 换位概率为 25%，发生换位时不能选择当前点位。
- `LastSeenIntel` 保存观察时的点位与瞄准 Pose，不能引用实时敌人 Transform。
- 剪影只在有效假 Peek 完全返回后显示；任何下一次 Peek 开始时立即隐藏。
- 自动预瞄只消费 fresh/pending 标记，不清除最后观察信息本身。
- 没有 fresh 情报时再次真架枪不得重置手动瞄准偏移。
- pointer cancel、窗口失焦、场景禁用和应用暂停不能留下卡住的假动作或开火状态。

## 可见性与情报算法

### V1 Unity 实现

1. `VisibilitySampleRig` 在敌人局部空间缓存 20 个身体采样点，覆盖头、肩、躯干、手臂、骨盆和腿部。
2. 假 Peek 激活时，将采样点投影到 Camera viewport。
3. viewport 外或摄像机后方的采样点计为不可见。
4. 对 viewport 内采样点执行无分配遮挡射线；如果环境遮挡在采样点前被命中，则该点不可见。
5. `visibleWeight / totalWeight` 得到可见率；V1 等权时达到 2/20 即满足 10% 阈值。
6. 达到阈值时记录当帧敌人点位索引、瞄准锚点世界 Pose 和观察时间，发出 `IntelAcquired`。

### Layer 约束

- `CombatOccluder`：墙、掩体和可阻挡视线的环境。
- `CombatTarget`：敌人命中 Collider。
- `CombatPresentation`：剪影、弹道和纯表现对象，不参与可见性或命中。

如果正式 Layer 尚未建立，首个实现必须在工作项内新增并由框架验证器检查，不能依赖默认 Layer 混用。

## 黄色透视剪影

- `IntelGhostPresenter` 使用独立 Ghost Prefab 或独立 Renderer 集合，不克隆并持续绑定真实敌人对象。
- 创建/显示时只拷贝 `LastSeenIntel` 中记录的 Pose。
- Built-in Render Pipeline 下使用项目自有 `EFTM/IntelGhost` Shader，至少包含黄色半透明填充和发光轮廓。
- Shader 需要 `ZWrite Off` 并使用可透视的深度比较；剪影所在 Layer 不参与射击和可见性检测。
- 剪影对象在场景加载时预创建，运行时只切换状态和 Pose，不重复 Instantiate/Destroy 或创建 Material。
- 任何新 Peek 开始时先隐藏剪影；只有新的有效假 Peek 完全返回后才重新显示。

## 自动预瞄与手动调瞄

- `LastSeenIntel` 分离 `HasIntel`、`HasPendingSnap` 和 `GhostVisible` 三个状态。
- 真架枪开始时，如果 `HasPendingSnap` 为 true，使用记录的瞄准锚点计算 Camera yaw/pitch，应用一次后清除 pending。
- 计算只使用旧位置 Pose；不得在吸附时读取当前敌人位置。
- `HasIntel` 可以继续驱动 HUD 文案，但不意味着下一次仍会重复吸附。
- 输入适配层把像素拖动转换为相对当前安全区域的归一化 delta，再交给 `AimController`。
- 中央 Aim Surface 只在真架枪状态接收拖动；三个操作按钮拥有更高输入优先级。
- Fire Surface 同时产生 trigger 和 aim delta；释放必须同时结束当前 trigger。
- yaw/pitch 限制、灵敏度和反向选项来自 Settings，不写死在 UI 代码中。

## 连射与后坐力

- `BurstFireModel` 累积时间并按约 108ms 间隔产生 `ShotRequested`；大帧间隔必须有单帧最大补发限制，避免卡顿后一次产生无限发。
- 首发预备不能在 Peek 完成同一帧执行昂贵的首次资源创建。音频、弹道和必要缓冲必须在场景初始化或第一次交互前准备。
- `RecoilModel` 使用 burst shot index 决定阶段：第 1 发 kick，第 2～4 发 climb，第 5 发起 stable。
- 水平冲量使用可注入随机源；运行时随机种子记录到验证日志，测试使用固定种子。
- 相机回正与玩家拖动修正分别计算后再合成，避免输入被回正逻辑覆盖。
- 弹道 LineRenderer、命中反馈和枪声源采用预创建或对象池；射击热路径不创建 Material、Mesh 或 AudioClip。

## 场景与生命周期

### Bootstrap

- 保持 `Bootstrap.unity` 为第一个启用场景。
- 首个实现可以由 Bootstrap 场景加载 `CombatFoundationV1.unity`，也可以在开发阶段由测试直接打开战斗场景；最终 Build Settings 路径必须在技术评审时固定。
- `ServiceRegistry` 不持有单场战斗的 mutable state，不把 `CombatFoundationModel` 注册为全局服务。

### CombatFoundationSceneRoot

1. `Awake` 验证 Camera、UIDocument、Settings、五个点位、采样 Rig、LayerMask 和表现引用。
2. 引用无效时记录单一聚合错误并禁用场景驱动，不以 NullReferenceException 继续运行。
3. `OnEnable` 创建模型、缓存缓冲区、准备音频/弹道资源并绑定输入事件。
4. `Update` 收集输入命令、执行模型 tick、处理领域事件、应用快照。
5. `OnApplicationPause(true)`、`OnApplicationFocus(false)` 与 `OnDisable` 释放所有 pointer、停止开火并让模型进入安全返回路径。
6. `OnDestroy` 解除事件并释放场景拥有资源；ScriptableObject 配置不被修改。

## 输入与移动端约束

- V1 使用 UI Toolkit runtime `UIDocument` 和 Pointer 事件，不在首个切片引入 Input System 或 uGUI 包。
- 真架枪使用 Click/PointerUp 完成一次 toggle；假动作使用 PointerDown 开始、PointerUp/Cancel 结束；开火使用 PointerDown/Up/Cancel。
- 必须保留 pointer id，禁止任意手指的 PointerUp 结束另一根手指的输入。
- 所有核心控件放在 Screen.safeArea 内；战斗画面仍按完整竖屏渲染。
- 设计坐标以 1080×2160 为基准，UI 使用相对布局和参考分辨率，不依赖设备像素等于设计像素。
- Android/iOS 暂停、系统手势打断和屏幕旋转请求必须触发安全释放；运行时锁定 Portrait。

## 事件契约

Foundation 至少输出以下一次性事件：

| 事件 | 触发条件 | 主要消费者 |
|---|---|---|
| `PeekStarted` | 从 Hidden 进入任一 Peek | 剪影隐藏、摄像机/HUD |
| `PeekFullyExposed` | 进度首次达到 1 | 开火模型、HUD |
| `PeekFullyHidden` | 已开始的 Peek 返回 0 | 敌人换位、剪影、HUD |
| `IntelAcquired` | 假 Peek 可见率跨过或保持阈值且位置需更新 | 情报模型、HUD |
| `PreAimConsumed` | 真架枪消费 fresh 情报 | Camera/HUD/调试日志 |
| `EnemyRelocated` | 隐藏后 25% 换位成功 | 敌人表现、调试日志 |
| `ShotRequested` | 真架枪完全暴露且达到射击节拍 | 射线、后坐力、音画反馈 |
| `BurstEnded` | trigger 释放或被系统取消 | 后坐力阶段重置 |

事件必须在单个 model tick 内有稳定顺序。建议顺序为状态转换 → 情报/换位 → 射击 → 快照，测试需要锁定关键次序。

## 失败路径

| 失败 | 行为 |
|---|---|
| Settings 缺失或字段非法 | 场景启动失败，输出包含全部非法字段的错误；不使用隐式默认值继续 |
| 五个敌人点位缺失或重复 | 场景启动失败；不降级为随机 Transform |
| 可见性 Rig 少于约定采样点 | 场景启动失败并指出敌人对象路径 |
| LayerMask 未配置 | 场景启动失败，避免射线把剪影或自身当作遮挡 |
| PointerUp/Cancel 丢失 | OnDisable/暂停/失焦安全释放并停止开火 |
| 目标被销毁 | 停止可见性与射击解析，清除当前真实目标；历史情报按设计状态保留或由上层结束遭遇 |
| 单帧 delta 过大 | 模型 clamp delta；连射限制单帧补发数量；状态进度保持 0..1 |
| 音频不可用 | 射击和状态继续，记录一次警告，不重复刷屏 |
| Ghost Shader 不可用 | 隐藏剪影并报告验证失败，不能用实时敌人高亮代替 |

## 性能、平台和工具约束

- 目标设备与最低性能档位尚未确定；首个切片以 60Hz 输入/相机更新为体验目标，并记录 30Hz 下的降级表现。
- 可见性检测上限为 20 个采样射线/渲染帧，只在假 Peek 激活时执行；需要以 Profiler 验证后再决定是否降频。
- 射击和可见性使用 LayerMask 与 NonAlloc API；Update、pointer move 和连续射击不产生持续 GC Alloc。
- Built-in Render Pipeline 是当前工程事实，不在本切片迁移 URP。
- 不新增 Input System、Cinemachine、Tween 或依赖注入框架；出现明确需求后单独评审。
- Unity 资产必须由编辑器生成并保留 `.meta`；不得手写或伪造 GUID。

## 迁移、兼容与回退

- H5 与 Unity 并行保留；Unity 不覆盖 `H5Prototype/`，H5 继续作为 V1 行为参照。
- H5 参数人工录入 `CombatFoundationSettings`，不建立运行时读取网页文件的依赖。
- 新实现只放入 `Assets/_Project/Runtime/Combat`、对应 Tests 和独立场景，避免修改现有 Core 骨架。
- 未通过技术评审前只允许创建文档，不创建正式玩家行为代码。
- 实现失败或方向被否决时，可删除新增 Combat 目录、场景和测试并从 Build Settings 移除，不影响 `GameBootstrap` 与现有服务框架。
- 若 Unity 行为与 accepted 设计冲突，将追踪状态标记为 `diverged`，不得通过修改设计规避失败。

## 实现切片

| 切片 | 依赖 | 交付 | 完成条件 |
|---|---|---|---|
| U0 技术门禁 | accepted 设计、H5 V1 | 本技术规格、005 工作项、测试矩阵 | 技术规格 accepted，范围和回退获批准 |
| U1 确定性领域模型 | U0 | Foundation 状态、配置、随机源、事件；EditMode 测试 | 真/假 Peek、计时、预备开火、25% 换位、情报和连射阶段测试通过 |
| U2 场景与输入/摄像机 | U1 | CombatFoundationV1 场景、SceneRoot、UI Toolkit 输入、Camera Presenter | 竖屏下真假 Peek、300/240ms、侧倾、取消与失焦行为通过 PlayMode |
| U3 可见性与情报 | U2 | 五点位、20 点 Rig、遮挡评估、旧位置、黄色剪影、自动预瞄 | 10% 阈值、旧位置不跟踪、剪影生命周期和 fresh snap 测试通过 |
| U4 射击与后坐力 | U2、U3 | 中心射线、预备开火、108ms 连射、调瞄、分阶段后坐力、反馈池 | 完全探出前零射击；阶段、随机种子和无首发资源卡顿测试通过 |
| U5 移动端验收 | U1～U4 | Android 开发构建、目标手机记录、H5/Unity 对照 | 单手可操作、无卡指针、无明显首发卡顿、差异有记录 |

任何切片不得提前加入弹匣、伤害、换弹、手雷或敌人 AI。

## 验证策略

### EditMode

- 状态迁移：真架枪 toggle、假动作 hold/release、互斥和返回反向连续性。
- 时间：300ms 探出、240ms 返回、大 delta clamp、进度 0..1。
- 情报：9.9% 不获取、10% 获取；fresh snap 只消费一次；手动 aim 保留。
- 换位：随机值 `<0.25` 才换位；新位置不等于旧位置；旧情报不被覆盖。
- 剪影状态：有效假 Peek 返回后显示；任一新 Peek 开始隐藏；无新观察不重现。
- 开火：假动作永不射击；真架枪未完全暴露只 armed；108ms 节拍；松手重置 burst。
- 后坐力：第 1 发 kick、2～4 climb、第 5 发 stable；固定随机种子可复现水平偏移。

### PlayMode

- UI Toolkit pointer id、capture、cancel、失焦和 OnDisable 行为。
- 摄像机从隐藏到暴露和返回的 Transform/roll 终点与连续性。
- 真实 Collider 遮挡下五个点位的可见率档位和 10% 阈值。
- 黄色剪影透过近墙显示但不进入 `CombatTarget`/`CombatOccluder` 射线。
- 当前敌人换位后，剪影和预瞄仍停留在旧位置。
- 真架枪立即按住开火，完全探出前 0 发，完成后的下一可用更新开始射击。
- 中央拖动与 Fire 拖动均改变瞄准，按钮输入不被 Aim Surface 抢占。
- 连续射击热路径的 GC Alloc 和对象池行为。

### 设备与人工对照

- 1080×2160 与至少一种不同长宽比 Android 设备。
- 单手触达、真假 Peek 区分、开火预备、拖动压枪、系统打断恢复。
- 同一测试脚本分别操作 H5 V1 与 Unity，记录行为一致、允许差异和失败差异。
- H5 只作为感知对照；Unity 验收最终以 accepted 设计标准判断。

## H5—设计—Unity 对照矩阵

| 行为 | 设计权威 | H5 V1 证据 | Unity 验证 |
|---|---|---|---|
| 真架枪点击锁定/再次返回 | CoreCombat 单手输入 | 点击后自动探出并锁定 | EditMode 状态 + PlayMode Pointer/Camera |
| 假动作按住/松回且不能开火 | CoreCombat 单手输入 | hold/release 状态与页面契约 | EditMode 不变量 + PlayMode PointerCancel |
| 300ms/240ms 与侧倾 | CoreCombat 视角约束 | PeekState + 视觉截图 | EditMode 时间 + PlayMode Camera Pose |
| 10% 可见轮廓获取情报 | CoreCombat 情报获取 | 五点位 15/35/65/65/100% | PlayMode 20 点真实遮挡 |
| 25% 隐藏换位 | CoreCombat 情报过期 | EnemyIntel 确定性测试 | EditMode 注入随机源 |
| 黄色旧位置剪影 | CoreCombat 黄色剪影 | depthTest false 视觉验证 | PlayMode Shader、Layer 与旧 Pose |
| fresh 情报一次性预瞄 | CoreCombat 自动预瞄 | pending/remembered 测试 | EditMode intel + PlayMode Camera aim |
| 无新情报保留手动 aim | CoreCombat 自动预瞄 | aim dataset 浏览器验证 | EditMode aim state + PlayMode drag |
| 完全探出前只预备开火 | CoreCombat 开火 | 首发热路径测试 | EditMode shot count + PlayMode 帧次序 |
| 108ms 连射与分阶段后坐力 | CoreCombat 射击/后坐力 | RecoilProfile 测试 | EditMode 节拍/阶段 + PlayMode表现 |

## 已批准技术选择

1. 场景加载：建议保持 `Bootstrap.unity` 为第一场景，把 `CombatFoundationV1.unity` 放在 Build Settings 第二位，由一个最小场景加载器在应用级服务初始化后进入战斗场景。
2. PlayMode 程序集：建议在 U2 开始时建立独立 `Assets/_Project/Tests/PlayMode/EFTM.Tests.PlayMode.asmdef`，不把场景测试混入 EditMode 程序集。
3. 黄色剪影 Shader：建议首版使用黄色半透明填充加发光轮廓双 Pass，保持与 H5 V1 的信息层级一致；低端降级可以关闭轮廓但不能改变旧位置语义。
4. Camera 位姿：建议首版由场景中的显式 Hidden/Exposed Pose 配置，模型只输出 0..1 进度，避免规则层计算 Transform。
5. VisibilitySampleRig：建议首版 20 个采样点全部等权，与 H5 的 2/20 = 10% 阈值证据一致。
6. 目标设备：建议 U5 先以当前 Windows 开发环境可直接构建的 Android 真机为首个门禁；iOS 触控与构建作为接入 macOS/Xcode 环境后的第二平台验证。最低帧率、图形 API 和性能预算仍需设计者指定。

## 追踪与批准记录

- 技术规格状态：accepted。
- 设计基线：accepted，commit `88d04fd`。
- H5 V1：tag `h5-peek-camera-v1`，commit `b9c8121`。
- Unity 当前实现：developing；从 U1 确定性领域模型开始。
- 适用工作项：`Docs/30_Iterations/005-unity-combat-foundation/IterationBrief.md`。
- 批准记录：设计者于 2026-09-04 确认技术文档通过。
- 生效 Git commit：`60d1c13`。

## 008 双侧换边已批准增量契约

批准日期：2026-09-08；适用工作项 008。设计依据为同日已批准的 `DESIGN-CORE-COMBAT` / `DDR-001` 扩展。下列拟定类型名可在不改变职责与行为的前提下细化；初值已批准，设备体验仍需验证。历史提案见工作项，当前权威仅为本文。

## 2. 不可改变的行为

- 初始右侧；右侧向左 Peek、左侧向右 Peek。两侧均以面向通道尽头的世界方向命名。
- 只在完全隐藏时单击换边；过程不可取消、反向或排队，不允许真假 Peek、开火、预备开火及手动调瞄。
- 点击后直接面朝固定通道方向横移，无预转头、回看或敌人追踪；玩家空间位置与镜头都要移动。
- 抵达后直接隐藏，无自动假 Peek、观察停留或额外缩回阶段。
- 横移/抵达按实际轮廓 `10%` 获取旧情报；完成隐藏落位仅结算一次 `25%` 敌人换位，并显示本次有效情报剪影。
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
| `CoverSideRig`（新增场景配置组件） | 两侧 PlayerAnchor、Hidden/Exposed Camera Pose、双向路径和固定通道朝向；启动验证 | 自己计时或结算敌人换位 |
| `CoverTransitionPresenter`（新增） | 由快照进度求玩家位置、身体朝向和移动相机基础 Pose；更新 PlayerRoot | 直接写 Camera Transform、产生战斗命令 |
| `PeekCameraPresenter` | Camera Transform 唯一写入者；合成原地 Peek 或换边 Pose，按当前侧求射线和旧世界点预瞄 | 自己维护换边进度或跟踪真实敌人 |
| `CombatTargetingPresenter` | 对最终本帧相机采样、生成数值 Observation、应用目标/剪影 | 由可见性丢失次数触发随机换位 |
| `CombatInputView/Controller` | 换边 PointerUp 转命令、按钮状态、指针安全释放 | 通过隐藏按钮替代模型互斥 |
| `SceneRoot` | 原子命令适配、应用顺序、暂停/失焦、聚合配置错误 | 把移动状态藏在协程中与模型并行 |

### Foundation 新增数据

- `CoverSide { Right, Left }`；显式初始 Right，不依赖数组默认顺序。
- `CoverSwitchPhase { Idle, Traversing, Landing }`。点击直接进入 Traversing；删除 Turning。Landing 是路径末段减速，**不是抵达后的观察状态**。
- `CoverSwitchSnapshot`：SourceSide、TargetSide、CurrentSide、Phase、Elapsed/Progress、ActionId、AwaitingArrivalConfirmation、Suspended。CurrentSide 只在确认落位时变为目标侧。
- `CanSwitchCover` 要求：Idle、未暂停、PeekMode=None、PeekPhase=Hidden、无待确认落位。所有入口统一使用模型许可，不能仅检查 PeekPhase=Hidden（换边期间 Peek 本身也未探出）。
- `ObservationSource { FakePeek, CoverSwitch }`，数值 Observation 含 VisibilityRatio、EnemyPositionIndex、旧 `IntelWorldPose`、ActionId；模型拒绝阶段不符或过期 ActionId 的观察。
- `IntelRevision`、`ObservedThisAction` 与已有 HasIntel / HasPendingSnap / GhostVisible 分离。新鲜预瞄只能消费一次；开始新动作清本轮观察标记，不清全部历史情报。
- 新事件：CoverSwitchStarted、CoverSwitchPhaseChanged、CoverSwitchArrived；沿用 IntelAcquired、IntelGhostHidden/Shown、EnemyRelocated。不要伪造 FakePeekPressed/Released 或 PeekFullyHidden 来复用移动结算。

### 命令与互斥

- `SwitchCoverRequested` 在合法 PointerUp 触发一次；方向由模型当前侧决定，不接受 UI 任意指定位置。
- Traversing/Landing 与等待确认期间，模型拒绝 ToggleTrueAim、FakePeekPressed、FirePressed、AimDelta 和再次换边；Release/Cancel 保持幂等。
- 只有成功接受换边才隐藏旧 Ghost、增加 ActionId 并锁定目标侧；非法点击不清情报、不消耗随机数。
- 完成动作只释放操作锁，不自动发送任何 Peek/Fire 命令。

## 5. 运动、镜头与建议初值

### 空间方案

- 采用预定义、经过碰撞检查的双向三维路径；不用瞬移、负缩放镜像或运行时寻路。
- 引入场景级 `PlayerRoot` 作为玩家空间位置，Camera 保持独立写入；运动相机点由同一移动进度和相对视点偏移求得。无需本轮新增完整角色皮肤/动画，但不能只换镜头而没有玩家位置。
- 两侧使用独立 Hidden/Exposed Pose。它们以稳定的 CoverRig 空间存储，不能挂在正在移动的 PlayerRoot 下造成二次位移。
- 两侧共用通道前向参考。静止时姿态按该侧 Peek 参数求解；左右 roll 与横移分别验收。无新情报的手动瞄准方向以共用前向/水平轴保存，在侧别姿态合成时转换，避免相反 roll 让同一 yaw/pitch 改变世界指向。
- 换边路径优先为连续曲线及预计算弧长表；位置单调沿路径前进。减速只改变行进速度，不增加观察停留。终点 Pose 与目标 HiddenPose 一致，误差阈值见验证。
- 点击后首个有效更新即产生位移；移动全程 Camera/PlayerRoot 使用 CoverSideRig.ForwardReference，朝向不受敌人或旧情报影响。两侧 HiddenPose 与 PlayerAnchor 朝向须与通道参考一致，启动校验不一致则报错，避免起止跳转。
- 镜头运动中不合成手动瞄准拖动；保存原手动方向，恢复普通 Peek 时使用。换边开始前已无射击；残余后坐力按原规则衰减，不重置手动瞄准。

### 技术初值（008 已批准初值、非最终平衡）

| 参数 | 建议初值 | 约束 |
|---|---:|---|
| 预转头/回看 | 无 | 移除相应阶段、进度与时长配置，不用静止等待代替 |
| 总移动时长 | 0.80s | 含末段 Landing，不另加停留 |
| Landing 时长 | 最后 0.20s | 属于上述 0.80s，持续移动至终点 |
| 移动镜头朝向 | 固定通道方向 | 不跟踪真实敌人，也不转向旧情报点 |
| 额外自动观察/跑动晃动 | 0s / 默认关闭 | 不增加自动 Peek；验收连续横移 |
| 几何位置/方向容差 | 0.02m / 0.5° | 终点和导入检查用，不是瞬移许可 |

单次总时长为 0.80s，取消初版 0.15s 预转头，不把该时间加到移动中。路径与几何保持已确认 Blender 版本；旧 schema v2 的 lookYawDegrees、turnSeconds/lookBackSeconds 为历史元数据，导入器不再读取，运行时运动参数由 CombatFoundationSettings 管理。若继续调优需提交体验记录，不放宽遮挡或穿墙约束。

## 6. 同帧时序、观察与单次结算

### 正常换边帧

1. 从唯一时间源取已限幅 delta；模型推进运动阶段和候选进度，末端先标记待确认，不立即切侧/换敌人。
2. 目标表现保持当前敌人点位；TransitionPresenter 更新 PlayerRoot 并计算移动视点；CameraPresenter 写入最终相机 Pose。
3. 只有 Traversing/Landing 允许采样，使用**已经应用的本帧相机**与真实 Collider，20 等权点，2/20 达阈值。完全隐藏和落位后不采样情报；不存在额外起步观察窗口。
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
- 现有流程：[BlenderWorkflow.md](../../UnityProject/ArtSource/BlenderWorkflow.md)。其中工具版本/路径为历史记录，开发前重新只读确认可用性，不重装工具来代替检查。
- Blender 当前脚本会先清场并重建、覆盖 .blend/FBX/贴图。**开发前不得直接运行**；开发时先检查打开场景、路径和未保存状态，保存受保护工作副本并核对手工变更，不能直接执行覆盖。

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

详见 [任务分解](../30_Iterations/008-cover-side-switch/Tasks.md)，至少完成：

- EditMode：全阶段命令许可、状态与 ActionId、不可取消/排队、暂停冻结、9.9%/10%、本轮 Ghost、一次随机结算与无新情报不吸附。
- PlayMode：左右两侧、五点位、完整连续路径、相机侧倾、首帧横移与全程固定朝向、跨侧旧世界锚点与两侧射线、无自动 Peek、30/60Hz 的 0.80s 行为和终点帧时序。
- 输入：实际 UI 捕获/释放、多指干扰、按钮排除 AimSurface、失焦恢复；另用真实 Game 鼠标/手机触控验证，不能只向 Button.SendEvent 就宣布硬件输入通过。复发鼠标缺失参考 006 恢复记录。
- 资源：Blender 源与导出一致、原始轴向/尺度、双石板/Collider、清单拒绝旧格式、幂等迁移及全新重建；无丢引用/重复控制器。
- 性能：预热后持续换边采样与呈现无持续托管分配；记录实际帧时间、20 点上限、路径验证开销；Windows 结果不替代真机。
- 设备：Windows 开发包用于输入/空间检查，Android 模块与指定手机仍是独立 U5 条件；安装/授权不由文档批准自动执行。
- 回退：在开发前记录实际干净 commit；未来需要回退时成组恢复 Blender 源/脚本、导出/清单、环境/场景、代码和契约，保留 `.meta`。不自动执行 reset/delete，不只回退 FBX 留下不匹配场景。
