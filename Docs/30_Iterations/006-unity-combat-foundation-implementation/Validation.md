# 验证记录

## 基线绑定

- 工作项：`006-unity-combat-foundation-implementation`
- 设计基线：`DESIGN-CORE-COMBAT`、`DDR-001`，accepted，commit `88d04fd`。
- 技术基线：`TECH-COMBAT-FOUNDATION-V1`，accepted，commit `60d1c13`。
- H5：`H5-PEEK-CAMERA-001 V1`，tag `h5-peek-camera-v1`，commit `b9c8121`。
- Unity：`2022.3.62f2`。

## U1 计划验证

- 真架枪 toggle、假动作 hold/release、互斥和返回连续性。
- 300ms 探出、240ms 返回、大 delta clamp 和 0..1 进度。
- 9.9% 不获取情报、10% 获取情报。
- fresh snap 只消费一次，无新情报时保留手动瞄准。
- 随机值 `<0.25` 时换位，且不能选择当前点位；旧情报保持不变。
- 假动作永不射击；真架枪未完全暴露只 armed。
- 108ms 连射、松手重置 burst、第 1/2～4/5+ 发阶段。
- 固定随机序列产生可重复结果。

## U1 历史观察

- U1 已增加 `CombatFoundationConfig`、命令/事件/快照类型、`IRandomSource` 和 `CombatFoundationModel`，以及 16 项领域测试。
- 独立 .NET 8/NUnit 临时工程编译通过，按 `EFTM.Tests.EditMode` 过滤运行 16 项测试：16 通过、0 失败。该结果只证明纯 C# 逻辑与测试结构可执行，不替代 Unity 2022.3.62f2 Test Runner。
- 运行时代码另以 `netstandard2.1`、C# 9、关闭 nullable、警告视为错误的兼容配置编译：0 警告、0 错误；该检查覆盖 Unity 2022 LTS 使用的主要语言与 API 边界，但仍不替代目标编辑器导入。
- 设计者随后安装并打开 `E:\Unity 2022.3.62f2\Editor\Unity.exe`；可执行文件、工程 `ProjectVersion.txt` 与编辑器日志均确认为 `2022.3.62f2 (7670c08855a9)`。
- 正式工程完成首次导入和脚本编译，`EFTM.Runtime`、`EFTM.Editor` 与 `EFTM.Tests.EditMode` 均编译成功；Combat 目录、4 个运行时脚本和测试脚本的 `.meta` 已由目标编辑器生成。
- 为不打断设计者当前打开的图形编辑器，使用同一份工程源文件的隔离临时副本运行 Unity 2022.3.62f2 Test Runner：19 项 EditMode 全部通过、0 失败、0 跳过，其中包含原有 3 项 ServiceRegistry 测试和新增 16 项领域测试。
- 使用保留 `Docs/README.md` 同级结构的隔离临时副本执行 `EFTM.Editor.ProjectFrameworkValidator.Validate`，结果为 `[EFTM] Project framework validation passed.`，batchmode 正常退出。

## U1/U2 历史结论

- development 工作项已启动，交付状态为 developing。
- U1 确定性领域模型已完成：目标编辑器导入、`.meta`、Unity 编译、框架校验和 19 项 EditMode 均通过。
- U2 已建立 `CombatFoundationV1.unity`、第二 Build Settings 场景、场景级组合根、运行时设置资产、UI Toolkit Panel/Theme、指针输入适配、显式 Hidden/Exposed 摄像机 Pose 和 Bootstrap 后续加载器。
- 输入层只发送 `CombatCommand`；Foundation 继续不依赖 UI、Camera、Transform 或 Scene。假动作、开火和 Aim Surface 分别记录 pointer id，其他手指的抬起不会释放当前操作。
- 系统中断通过 `ReleaseAll` 停止假动作和开火，并让锁定的真架枪进入返回路径；`PointerCancel`、Pointer Capture 丢失、失焦、暂停与 `OnDisable` 均接入该路径。
- 首轮 PlayMode 为 4/5，通过测试定位到 `CombatFoundationSceneRoot.Awake` 早于 `UIDocument` 面板根创建；输入视图改为可延迟、幂等构建后复测 5/5 通过。
- 按 Unity 2022.3 官方运行时 UI 规则增加 Panel Settings 与默认主题继承文件；最终 PlayMode 日志中 `No Theme Style Sheet` 警告为 0。
- 当前最终文件在 Unity 2022.3.62f2 隔离副本中完成：EditMode 19/19、PlayMode 5/5、框架校验通过，0 个测试失败。
- U2 自动化门禁关闭；手机竖屏触达、侧倾方向与空间体感将在后续整体验证中由设计者确认。下一实现切片为 U3，工作项整体仍为 developing。

## 2026-09-08：U3/U4 实现与 U5 开始

### 开工快照

- 按设计者要求先提交当时所有工作区变更：`378afdb`，`feat: integrate warehouse environment and refine inventory gestures`。包含仓库 Blender/FBX/材质、场景、背包旋转/拖动修正及文档。
- 开工时 H5 Node 测试 32/32、`git diff --check` 通过。没有执行 push。
- 本轮继续绑定上述 accepted 设计与技术基线，不把背包或后续战斗草案合入 V1。

### 实现事实

- `CombatTargetingPresenter` 组合五点位、20 个等权局部采样点、视锥投影和环境遮挡射线。使用仅含 `CombatOccluder` 的 mask，最多每个有效假 Peek 帧 20 条射线，不采样真实目标自身和剪影。
- `LastSeenIntel.WorldPose` 为纯数值快照，记录世界位置/旋转、旧瞄准锚点和观察时间，不持有 Transform。25% 隐蔽换位不修改旧快照；`HasPendingSnap` 只消费一次。
- 黄色独立 Ghost 在场景中预创建，无碰撞体，使用自有 `EFTM/IntelGhost` 双 Pass Shader、`ZWrite Off` 和 `ZTest Always`。每次 Peek 开始隐藏，只在新有效假 Peek 完全返回后重显。
- `CombatShotPresenter` 使用包含环境与目标的中心射线。ShotRequested 增加每一发冲量施加前的 yaw/pitch；即使同帧补发，也不把末发镜头方向用于之前所有子弹。
- 四组预创建弹道、落点与 AudioSource，启动时生成一次短枪声音频；没有弹匣、伤害、换弹或敌人决策。
- 修正 Unity 角度符号：后坐力向上，手指下拖压枪、右拖向右。完全隐藏时使用 Hidden Pose；手动瞄准状态保留，在 Peek 过程中合成，避免保留偏移破坏掩体遮挡。
- UI 增加准星、命中颜色反馈、“已预瞄 / 没有敌人信息”和旧位置可能过期提示。去除 Button 自带 Clickable 对自管指针的竞争；安全释放同时释放捕获，缓存未改变的 HUD 状态。
- 使用仓库已存在的 OFL 中文字体之运行时副本，预生成静态 SDF 字形并随构建附许可证；无运行时编辑器依赖，无系统字体依赖。
- Bootstrap loader 增加 SubsystemRegistration 重置，避免禁用 Domain Reload 后第二次启动丢失战斗场景加载。

### 真实几何结果

| 点位 | 完全隐藏可见率 | 完全探出采样可见率 |
|---|---:|---:|
| 0 | 0% | 20% |
| 1 | 0% | 50% |
| 2 | 0% | 70% |
| 3 | 0% | 65% |
| 4 | 0% | 100% |

- 独立真实 Collider 阈值夹具验证 1/20=5% 与 2/20=10%；不是以屏幕矩形交叠替代遮挡。
- 上述档位与 H5 V1 的 15/35/65/65/100% 不同，来自已替换的仓库空间与采样布局。保留 10% 设计阈值，差异明确登记供 U5 体验对照，不修改 accepted 设计。

### 自动化与构建证据

- 使用 Unity `2022.3.62f2` 隔离副本：`C:/Users/castl/AppData/Local/Temp/EFTM-U34-b0066ab6/UnityProject`。未关闭或替换用户已打开的 Unity 工程。
- EditMode：项目测试 22/22，另有现有 UnitySkills 插件测试 130/130，总计 152/152。
- PlayMode：16/16，含原有输入/仓库测试和新场景集成测试。覆盖五点位、真实遮挡阈值、旧位置不跟踪、一次性预瞄、首发帧门禁、上跳/下拖、池化、目标销毁、真实 UI Toolkit PointerDown/Up 和捕获释放。
- 新增 30Hz / 60Hz 测试：完全探出前零发射，首发在暴露后更新，108ms 节拍允许一帧量化误差；连续 3 秒发数在预期范围。
- 热路径测量采用 `GC.GetAllocatedBytesForCurrentThread`：预热后 200 次可见性+射击池调用，以及 200 次持续射击 SceneRoot.Step，各为 0 字节增量。仅证明这些调用的托管分配，不代表设备整帧 GC、GPU 或首发耗时已经验收。
- Windows x64 Development Build 成功，完整包约 95.4 MB。实际启动通过 Bootstrap 进入战斗；打包后的中文字体与竖屏双按钮显示已直接检查。
- 启动实测发现旧 ProjectSettings 未序列化输入后端，Unity 读为 `-1`，警告 UI Toolkit 输入源不可用。通过编辑器显式设置 Input Manager (Old)=0 后重新构建，警告消失；框架验证器新增该门禁。未引入新 Input System 包。[Unity 2022.3 运行时事件系统说明](https://docs.unity3d.com/2022.3/Documentation/Manual/UIE-Runtime-Event-System.html)。
- UI 捕获断言曾在同一调用内检查释放而失败；UI Toolkit 在面板更新处理队列。模型停火的同步断言与面板更新后捕获清除的断言均通过，未将停火延迟到下一帧。
- `codex-chat-images/` 保存本地 XML、日志与截图，不提交 Git。`unity-v1-0/1/2.png` 是 1080×2160 场景相机证据；测试运行器的 UI RenderTexture 为空，不计为 UI 视觉通过。真正 UI 证据为独立程序 `unity-player-hidden.png` / `unity-player-current.png`。
- computer-use 检查中检测到用户正在操作独立窗口，随后停止输入以避免争用；没有把尚未完成的脚本化桌面交互记为全部通过。
- 设计者补充优先使用 FakeUnityCLI 后，从安装任务“迁移 Unity 工具与收藏功能”定位到工程内 `Tools/FakeUnityCLI/runtime/windows-x64/fuc.exe`，并写入 Unity AGENTS 入口。`unity probe` 确认 full support；`editor status` 为 ready；Bridge compile watermark 为 clean、0 error、0 warning。
- FakeUnityCLI 在线读回发现当前编辑器内存中的 Layer 8～10 尚为空，而磁盘与隔离构建已正确。普通 refresh 未更新原生 TagManager；通过 `editor exec` 在确认槽位无冲突后使用 SerializedObject 同步，读回 CombatOccluder=8、CombatTarget=9、CombatPresentation=10，当前编辑器框架检查通过。没有切换用户场景或进入/退出 Play。
- CLI `diag duplicates`：1495 个 GUID，重复、缺失 meta、孤儿 meta 均为 0。场景 `diag missing-refs` 仅将 Unity 原生未烘焙 `OcclusionCullingSettings.m_SceneGUID=000...000` 误报为丢失 GUID；该值由编辑器生成，且加载/构建通过，保持原样，不为消除工具误报伪造 GUID。56 条 type-3 跨文件内部 fileID 不在 CLI 离线校验范围，继续以编辑器导入和 PlayMode 测试覆盖。

### 当前结论与停止边界

- U1～U4 已实现并完成当前自动化门禁，工作项转 verifying，不标记 implemented/closed，不代替设计者验收。
- 现有编辑器 `PlaybackEngines` 仅含 `windowsstandalonesupport`，缺 Android Build Support；当前 PATH 也无 adb。Android 包、触控系统中断、真机整帧性能和 H5/Unity 手感对照尚未完成。
- 继续 U5 需要用户安装对应版本 Android 模块（含 SDK/NDK/OpenJDK）、指定并接入手机、授权调试与确认体验。操作脚本见 `DeviceValidation.md`。
- iOS 需要独立 macOS/Xcode 环境。不能因为 Windows 构建成功而声明手机可运行。
- 弹匣、伤害、换弹、左右掩体、手雷、敌人 AI、多人和完整搜打撤不在本工作项 accepted 范围；没有新的批准基线，不继续扩展。

## 2026-09-08：Game 视图鼠标输入恢复

- 现象：设计者在 Play 中点击真架枪、按住假动作均无响应。
- FakeUnityCLI 在线读回：GameView 获得焦点、场景初始化成功、输入控制器已绑定，UI 根仍挂载于当前面板，按钮启用且面板拾取命中 `true-aim`；没有发现 UI 遮挡或失效绑定。
- 输入层异常：Windows 正常识别鼠标，但旧编辑器进程中的 `Input.mousePresent=false`；UI Toolkit 默认事件系统 `m_MouseProcessedAtLeastOnce=false`。其鼠标处理在设备不存在时直接返回，未将鼠标点击传给按钮。当前输入后端配置已为 Input Manager (Old)=0，配置值正确不代表当前进程设备状态正常。
- 经设计者批准，通过 CLI 停止 Play、确认场景无未保存修改、保存资产并完整退出/重启该工程；未关闭另一个 Unity 工程。新进程在 Edit Mode 与 Play Mode 均读回 `Input.mousePresent=true`。未修改战斗规则或添加输入系统依赖。
- 重启恢复证明本次阻断位于旧编辑器进程的输入状态；底层为何误报设备缺失尚未确认，不能将其直接归因为按钮代码或特定硬件故障。
- 复发检查顺序：确认实际 GameView 焦点、初始化与 UI 拾取 → 检查输入后端及 `Input.mousePresent` → 保存后完整重启并读回 → 如仍失败，再检查实际指针事件与按钮回调。不要仅依据直接 `SendEvent` 的自动化通过就宣称硬件输入正常。
- 重启后 Play 中默认事件系统 `m_MouseProcessedAtLeastOnce=true`，控制台仅有战斗初始化记录，无新错误；此前被跳过的鼠标处理已启动。临时只读事件观察器在结束时解除注册，未写入运行时代码。
- 本次真实点击复验：观察窗口内未收到用户点击，仍待设计者手动确认真假 Peek；不以输入状态恢复或既有 PlayMode 合成事件测试替代硬件点击验收。
