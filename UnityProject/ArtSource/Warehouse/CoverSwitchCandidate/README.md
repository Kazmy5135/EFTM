# 008 双侧掩体已确认资产

由 Blender 5.2.1 后台打开已保存的原 `../Warehouse.blend`，运行 `../upgrade_cover_switch.py` 生成。不是 Unity 中拼装的模型。

- `Warehouse.blend`：可编辑双石板源模型，纹理已打包。
- `Warehouse.fbx`：双石板导出，2026-09-08 已替换 Unity 正式模型，保留原 FBX .meta。
- `warehouse-manifest.json`：schemaVersion=2，包含左右掩体、碰撞记录、玩家/镜头姿态、双向路径及源文件哈希。初版 0.15s+0.8s / lookYawDegrees 元数据保留历史，已不被 Unity 读取；最新批准修订为直接横移 0.80s、固定通道朝向，由 CombatFoundationSettings 控制，无几何修改。
- 共 21 个环境网格；删除旧 NearCover / DoorFrame，增加 NearCoverRight / NearCoverLeft，其他环境保留。

设计者已确认模型效果。Blender 端遮挡/路径、FBX 回读与 Unity ModelImporter 检查均通过；双侧连续换边、移动观察、UI 与跨侧预瞄已接入。具体自动化和体验门禁见工作项 008 的 Validation.md，不以源资产确认代替手机体验确认。

此目录 `.blend` 是当前双侧布局的编辑源；`../Warehouse.blend` 保留为旧单侧升级输入，未被覆盖。迁移入口为 `CoverSwitchSceneBuilder.UpgradeCoverSwitch`，从版本化清单绑定场景，不能在 Unity 中手工移动石板替代 Blender 修改。

可重复生成：在独立后台 Blender 加载原 Warehouse.blend，运行 upgrade_cover_switch.py，参数 `--output-root` 指向新的隔离目录，`--preview-root` 指向仓库 codex-chat-images。脚本不会读取未保存的交互窗口内容；人工改动须先保护工作副本。
