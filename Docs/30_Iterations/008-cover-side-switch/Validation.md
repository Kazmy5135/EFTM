# 008 实施与验证记录

## 基线与授权

- 2026-09-08 设计者批准设计、技术文档及 0.95s 换边初值，并授权开始实施；电脑重启后明确允许重新打开对应 Blender 文件。
- 设计/技术已同步正式文档；这些修订尚未提交 Git。最近实现提交为 `5d0e3cb`，不是换边功能提交。
- 第一轮执行 S0 与 S1；设计者随后确认“模型效果没问题，进行下一步”，本轮继续 S2～S4 和 S5 自动化。尚未宣称手机体验验收完成。

## 工具与文件保护

- 先检查 Git，保留已有设计改动。原仓库源 SHA256：`841d3b7ff35fd2a39faa7841f1a5139f893adfaa32ce53026e723447a9c63546`；本轮结束仍一致。
- 重启前 Blender MCP 不可用；电脑重启后打开正确 Warehouse.blend，MCP 已恢复，读取到 Blender 5.2.1 LTS、29 对象。
- 候选通过独立后台 Blender 生成，不覆盖已安装 Unity 资产，不操作另一个 Unity 工程。当前 Blender 会话脏标记为 true，打开候选前通过保存副本保护会话：`C:/Users/castl/AppData/Local/Temp/EFTM-cover-switch-75841315/open-session-backup.blend`。
- 当前 Blender 已打开 `UnityProject/ArtSource/Warehouse/CoverSwitchCandidate/Warehouse.blend`，可直接查看双石板。

## S1 产物

- 新共享 `cover_switch_layout.py`；定向升级/检查脚本 `upgrade_cover_switch.py`、`validate_cover_switch.py`、`verify_cover_switch_export.py`。
- 完整 `build_warehouse.py` 同步使用双石板合同；本轮没有全量重跑该脚本覆盖正式输出。候选定向修改保留已有环境与打包贴图。
- 候选包含 .blend / FBX / schema v2 清单，位于 `UnityProject/ArtSource/Warehouse/CoverSwitchCandidate/`，未导入正式 Unity Assets。
- 两块石板中心为 X=±1.25m，尺寸 1.26×3×0.32m，中央开口约 1.24m；隐藏位置 X=±0.73m、Z=-0.85m，Peek 位置 X=±0.22m、Z=-0.65m。方向以通道 +Z 为前方。
- 首轮隐藏位置 X=±0.79m 只留下极窄通道边缘，查看预览后调整至 ±0.73m；未改变 10% 阈值或添加自动探头。

## 已执行检查

1. Blender 近端真实网格射线：左右隐藏位 × 五个既有敌人测试点位 × 20 个身体采样，全部由近端石板遮住（各点位可见 0/20）。不是靠目标出屏代替遮挡。
2. 只包含 Blender 环境的完全探出几何射线：右侧 18/20、20/20、20/20、13/20、20/20；左侧 18/20、20/20、20/20、14/20、20/20。**不含 Unity 生成的敌方挡板、完整渲染角色与视口投影，不能作为最终可见率。**
3. 双向平直路径以包围半径 0.25m、高 1.8m 胶囊的保守扫掠 AABB 检查；对全部导出碰撞记录无交叠，地面仅正常接触。相机 near=0.05m、54° 竖直 FOV 下的保守近裁面移动包络无环境交叠。
4. 独立 Blender 重新导入候选 FBX：左右中心与尺寸均在 0.02m 容差内，名称/左右方向正确，无旧 NearCover、DoorFrame 残留，网格数量 21 与清单一致。**这不是 Unity ModelImporter 验证。**
5. 已查看近端双石板布局、左右隐藏预览；隐藏画面两侧分别留出有限通道，探出预览有通道纵深。没有增加新贴图或外部资产。

本地证据（临时、不提交）：`codex-chat-images/switch-geometry-validation.json`、`switch-export-validation.json`、`switch-layout.png`、左右 hidden/peek 及 `switch-path-middle.png`。它们是 Blender 预览，不是运行时截图。

## S1 完成时的交接（历史）

- S0 已完成，S1 Blender 候选模型与导出检查已完成；技术/设计任务整体仍 developing。
- 下一步 S2：独立换边状态、时序、观察许可及一次落位结算；随后 S3/S4 升级 Unity 导入器与双侧 Rig、连续镜头、UI 和跨侧预瞄。
- 正式 Unity 场景仍是旧单侧版本，目前没有换边按钮。既有 006 测试不能代替新功能验证。
- Android/手机触控、镜头舒适度、Unity 导入与完整双侧遭遇验证尚未完成。不以候选建模成功提前关闭这些门禁。

## 2026-09-08：S2～S4 Unity 接入与回归

### 实现事实

- 通过项目内 FakeUnityCLI 检查当前 Editor：2022.3.62f2、Bootstrap、退出 Play、无未保存场景。未使用桌面控制，也未操作另一 Unity 工程。
- 已批准候选 FBX/清单成组导入原 Assets 路径，保留 .meta；环境是 Blender 导出网格，未在 Unity 创建或移动石板。原根部 Warehouse.blend 保持不变，当前双侧编辑源为 CoverSwitchCandidate/Warehouse.blend。
- 导入前环境/场景备份：`C:/Users/castl/AppData/Local/Temp/EFTM-008-import-a8577daa68e24848ac836edf721337a1/`。正式 FBX SHA256 与候选相同：`0A97844F194226AD88B0C5190E87A7703DAF51F184C13732CC5D0C87F989B4B3`。
- Foundation 新增 CoverSide、独立换边阶段、ActionId、观察来源、IntelRevision、带当前侧与版本的原子 PreAimSolution。仍为 0.15s 转头 + 0.8s 移动，无自动 Peek 或抵达后观察停留。
- CoverSideRig 保存 Blender 双侧姿态与路径；CoverTransitionPresenter 更新真实 PlayerRoot 并提供镜头 Pose；PeekCameraPresenter 是唯一 Camera Transform 写入者。共同通道方向保存手动 yaw/pitch，侧倾只绕观察方向合成，换边不会镜像已保存的手动方向。
- 只有完全隐藏可换边；过程不可取消、反向、排队或开火/拖动。移动/末段按实际本帧 20 点采样获取 10% 情报，最终物理隐藏确认后仅结算一轮 25% 换位；目标消失仍能完成安全落位。
- 新按钮在假动作上方，PointerUp 单次触发；字幕包含侧别与换边/暂停状态。旧世界点从当前侧解算预瞄；不读取新敌人点位，非法解算明确停遭遇而非静默消耗情报。
- pause/focus 独立锁存，清捕获和预备火力；换边冻结并原处继续，普通 Peek 仍安全返回。运行时包络阻塞时保持最后有效位置、停遭遇并报错，不隐形传送。

### 测试与资源证据

| 检查 | 结果与边界 |
|---|---|
| EditMode | **37/37** 项目测试通过；含原基础模型回归与新增全阶段互斥、10% 边界、旧 ActionId、确认幂等、随机次数、暂停、原子预瞄、无新情报保留手调、30/60Hz |
| PlayMode | **24/24** 项目测试通过；含双向 PlayerRoot 连续移动、转头/回看、实际移动可见窗口、落位隐藏、无自动 Peek、跨侧旧锚点/准星射线、暂停交错、阻塞失败、目标丢失、UI 捕获及原射击回归 |
| 性能自动化 | 移动 Presenter + 相机 + 观察的预热后连续采样无托管分配；原连续射击/SceneRoot 稳态无托管分配回归通过。不是手机整帧性能报告 |
| Unity 几何 | 21 网格、13 环境 BoxCollider、米制与左右轴向符合清单；两侧×五点隐藏采样全部由对应 NearCover 实际阻挡；全部点位至少一侧达到 10%；路径胶囊、近裁面包络与 Peek 路线检查通过 |
| 幂等迁移 | 再次 Upgrade 后旧 Targeting/ShotPresenter GlobalObjectId 不变，只有一套 CoverSideRig/PlayerRoot；未重装 CombatEncounter |
| 从源重建 | 新增 BuildForValidation：从空场景生成完整双侧遭遇，校验后关闭、不覆盖正式场景；框架校验通过 |
| 静态中文字体 | 首次重载后的重复迁移暴露字典未初始化误判；已显式 ReadFontAssetDefinition、按实际字形检查并保证恢复 Static。重载后迁移与新旧文本字形检查均通过，竖屏 UI 截图已查看 |
| Windows 开发包 | 通过 FakeUnityCLI 调用既有 BuildWindowsDevelopment 成功，首次约 29.5s、最终代码增量重建约 6.5s；输出 `C:/Users/castl/AppData/Local/Temp/EFTM-008-Windows-20260908-164347/EFTM.exe`，记录 `008-build.json`。构建成功不等于真实鼠标或手机验收 |
| 输入证据边界 | 自动化 PointerDown/Up/取消通过；本轮没有接管鼠标执行真实硬件点击，仍需设计者 Game/设备体验 |

证据在根目录临时 `codex-chat-images/`，不提交：`008-editmode.xml`、`008-playmode.xml`、`008-migration.json`、`008-Right-hidden.png`、`008-Left-peek.png` 等双侧预览，以及 `unity-v1-ui.png`（本轮 1080×2160 UI 渲染，不是旧 H5 图片）。

### 下一门禁

- S2～S4 已接入并通过上述自动化；S5 的 Windows 构建通过，真实输入与完整生命周期组合仍单列，S6 手机体验未完成。
- 初版待体验项为 0.95s 节奏、预转头/回看；体验反馈已触发下述直视通道修订，该初版镜头流程不再是当前验收标准。
- Android 环境、手机型号/触控、系统打断与舒适度仍属于 006 U5 / 008 S6，不自动安装 SDK 或以 Windows 结果替代。
- 当前代码、资产和文档尚未提交/推送；最近提交仍为 `5d0e3cb`，不能把它称为换边实现版本。

## 2026-09-08 直视通道修订验证（前次）

依据设计者最新确认及 [DirectSwitchRevision.md](DirectSwitchRevision.md)：取消预转头/回看，立即横移 0.80s，全程固定通道朝向，不自动跟踪敌人。未改 Blender 几何、FBX 或路径。

- FakeUnityCLI 在线刷新后编译 generation 17：0 error、0 warning；退出测试后正式 CombatFoundationV1 场景恢复并保存。
- EditMode **37/37**：两方向 30/60Hz 下精确 0.80s，立即进入 Traversing，原阈值、互斥、暂停及一次落位结算通过。
- PlayMode **25/25**：首个推进帧产生位移，两方向镜头和 PlayerRoot 始终固定朝向；更换敌人点位不改变镜头方向；移动实际可见、剪影、跨侧预瞄、阻塞保护、UI 捕获及原射击回归通过。
- 更新现有场景 Rig 与状态文本所需静态字形“面”，场景几何验证通过；没有重新制作模型或覆盖遭遇组件。
- Windows 开发包增量构建成功，约 5.8s；同一临时输出 `C:/Users/castl/AppData/Local/Temp/EFTM-008-Windows-20260908-164347/EFTM.exe` 已更新为本修订。
- 新证据位于忽略目录 `codex-chat-images/`：`008-direct-editmode.xml`、`008-direct-playmode.xml`、`008-direct-build.json`；初版 PlayMode 留存为 `008-preturn-playmode.xml`。旧截图/初版证据不作为当前节奏证明。
- 待设计者体验当前横移节奏、真实按钮输入与舒适度；手机触控/系统打断和 S6 仍未验收。无新增 H5、无 Git 提交或推送。

## 2026-09-08 移动侧探修订验证（当前）

- 授权：设计者要求先完整提交推送工作区，再按推荐方案实施。全部 61 个工作区变更及此前积累的提交已推送到 origin/master；实施前本地 HEAD 与远端均为 `49ce351281ecec3f17ba551222a4d6b92d71c51f`，工作区干净。
- 基线：`MovingLeanProposal.md` 获批，`DESIGN-CORE-COMBAT` 与 `TECH-COMBAT-FOUNDATION-V1` 同步。侧探 0.08m、压低 0.02m、来源侧侧倾 4°；头部在 0.12s 达峰，侧倾在 0.16s 达峰，0.34s 后逐步回收、0.80s 完全归零。无额外移动阶段或观察停留。
- 实现：Settings 保存独立 AnimationCurve；CoverTransitionPresenter 从 Foundation 的同一 MoveProgress 求合成相机 Pose；SceneRoot 与 Editor 统一使用该解算器验证 128 段路径、保守近裁面球包络、扫掠及相机前进单调性。相机仍由 PeekCameraPresenter 唯一写入。运行时热路径无新托管分配。
- 资产：通过 FakeUnityCLI 在线保存现有 CombatFoundationSettings.asset，保留 GUID；没有修改 Blender、FBX、石板、身体路径、正式场景布局或普通 Peek 参数。
- 编译：在线刷新，generation 21 clean，0 error / 0 warning。
- EditMode：**37/37 passed**。原换边时序、互斥、阈值、原子预瞄与一次结算回归通过。
- PlayMode：**28/28 passed**。覆盖 30/60Hz 双向首帧移动、前向稳定、侧倾符号/峰值/归零、头部偏移/压低、无倒退、同进度跨帧率一致、头部先于侧倾达峰、敌人位置独立性、暂停冻结相机、两方向×五个敌人点位的移动观察与近裁面角点、隐藏落位、原射击/UI/情报/热路径分配回归。
- 渲染采样：通过在线 Editor 实际 Camera.Render 采样两方向 0/0.12/0.16/0.34/0.60/0.80s。0.12s 头部已偏移 8cm、roll 约 3.375°；0.16s 达 4°；0.60s 为 3.2cm/1.6°；0.80s 偏移与侧倾均为零。已查看左右侧探画面，通道可见、侧倾方向相反，无明显裁剪穿模；这些是场景相机渲染，不是硬件操作录像。
- Windows 开发包：成功，输出 `UnityProject/Builds/MovingLean/EFTM.exe`（Git 忽略的本地构建产物）。可直接用于体验新版动作。
- 本地验证证据：`codex-chat-images/008-lean-editmode.xml`、`008-lean-playmode.xml`、`008-lean-capture.json`、`008-lean-build.json`、`lean-right-*.png`、`lean-left-*.png`；临时目录不提交。
- 剩余门禁：设计者常速操作的歪头体感、真实输入与手机舒适度；没有宣称塔科夫逐帧复刻或手机验收完成。
