# Warehouse 场景源文件

本目录承接已确认的工业仓库俯视效果图。工作项：`Docs/30_Iterations/007-blender-combat-scene/`。

## 008 双侧换边源与安装

2026-09-08 直视通道换边修订不改变几何，因此沿用本次 Blender 导出。清单中的 `lookYawDegrees`、`motion.turnSeconds/lookBackSeconds` 仅保留初版历史元数据，Unity 已不读取；当前运行时节奏以 `CombatFoundationSettings` 的直接横移 0.80s 为准。

双石板候选模型与导出见 [CoverSwitchCandidate](CoverSwitchCandidate/README.md)。共享几何合同在 `cover_switch_layout.py`；`upgrade_cover_switch.py` 从已保存旧源进行定向升级，不重生成贴图，`validate_cover_switch.py` 检查 Blender 几何，`verify_cover_switch_export.py` 检查 FBX 回读轴向/尺度。

设计者已于 2026-09-08 确认双石板效果；正式 Unity FBX/清单已安装该版本。**当前双侧模型权威源为 `CoverSwitchCandidate/Warehouse.blend`**；根部旧 `Warehouse.blend` 保留为定向升级输入，未覆盖。完整生成器 `build_warehouse.py` 已改用同一合同，但仍会重生成贴图和覆盖旧源，不能直接运行来覆盖人工修改。

后续修改先在 Blender 的双侧源/共享脚本完成，导出 FBX 与 schema v2 清单到隔离目录并验证，再成组复制到 `Assets/_Project/Art/Warehouse/Models/Warehouse.fbx` 和 `warehouse-manifest.json`（保留 .meta）。退出 Play、处理未保存场景后，通过 FakeUnityCLI 调用 `EFTM.Editor.CoverSwitchSceneBuilder.UpgradeCoverSwitch()`；它更新环境 Prefab、双侧 Rig 与层级，验证后保存，保留已有遭遇引用。不要只替换 FBX 或重跑全部 CombatFoundationAdaptersBuilder.Install。

Blender MCP 配置与后续制作流程见 [BlenderWorkflow.md](../BlenderWorkflow.md)。

- `Warehouse.blend`：Blender 5.2.1 生成的可编辑真实三维场景，贴图打包在文件内。
- `build_warehouse.py`：确定性建模、贴图生成、FBX 导出与俯视渲染脚本。脚本中的位置/尺寸采用 Unity 米制坐标；导出时处理 Blender 与 Unity 的坐标差异。
- 导入产物：`UnityProject/Assets/_Project/Art/Warehouse/`，包括模型、纹理、材质、环境 Prefab 和几何清单。

## 编辑与再生成

打开 `Warehouse.blend`，环境构件在 `Warehouse - export meshes` 集合中。墙、地面、门、箱、柜、管道等按可编辑构件合并。顶棚与横梁在俯视渲染中隐藏，但包含在 FBX 和 Unity 场景中；查看室内时可恢复这两个对象的渲染可见性。

脚本重跑会重建本目录的 `.blend`、FBX、贴图和清单，覆盖对这些生成产物的手工修改。手工建模时先另存工作副本，或同步修改生成脚本。不要直接重跑脚本覆盖尚未保存到来源的人工工作。

```powershell
& 'E:/Blender/blender.exe' --background --factory-startup --python 'UnityProject/ArtSource/Warehouse/build_warehouse.py'
```

双侧版本使用 `Tools > EFTM > Upgrade Cover Switch 008`。旧 `Install Blender Warehouse Environment` 仅处理环境，不完成双侧镜头/玩家绑定；不要把它当成 008 完整迁移入口。

## 验证约束

- 通道约 4 × 24 米、墙高约 3 米；近端遮挡内缘与当前 U2 摄像机相容。
- FBX 需通过米制尺度和左右方向检查；可见网格与关键墙体 Collider 应对齐。
- 缩回时到通道远端的射线命中近端墙；完全探出后主要枪线畅通。
- 当前交付是环境资产，不包含角色模型或新 U3 玩法模块。
- 俯视和 Unity 摄像机预览保存在仓库根目录 `codex-chat-images/`，不提交 Git。
