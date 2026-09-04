---
title: 战斗模块 V1 Unity 技术设计
document_id: TECH-COMBAT-FOUNDATION-V1
status: accepted
last_updated: 2026-09-04
---

# 战斗模块 V1 Unity 技术设计

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
- 生效 Git commit：待本次技术批准提交后登记。
