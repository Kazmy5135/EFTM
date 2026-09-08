# Blender 仓库场景交付与验证

日期：2026-09-07。设计者已确认首版效果图，并授权继续建模和接入。

## 基线绑定

- 设计：`DESIGN-CORE-COMBAT`、`DDR-001`，accepted，生效 commit `88d04fd`。
- 技术：`TECH-COMBAT-FOUNDATION-V1`，accepted，生效 commit `60d1c13`。
- 当前实现读取起点：`7af287b14abcae92f516b3e907c057f708bd4b20`。
- 工具：Blender 5.2.1 LTS；Unity 2022.3.62f2。

## 实际交付

- `UnityProject/ArtSource/Warehouse/Warehouse.blend`：真实三维场景源文件，约 7.7 MB，可由 Blender 正常打开并渲染。
- `UnityProject/ArtSource/Warehouse/build_warehouse.py`：确定性几何、材质贴图、FBX 与预览生成脚本。
- `UnityProject/Assets/_Project/Art/Warehouse/`：FBX、10 张 1024 × 1024 材质贴图、9 个 Standard 材质、环境 Prefab 和几何清单。
- 环境规模：21 个 MeshRenderer，41,168 个三角面，13 个简化 BoxCollider。部分材质分配到同一个网格的不同子网格；网格数不代表实际 draw call 数。
- `CombatFoundationV1.unity` 已由 Unity 编辑器 API 替换旧环境对象，加入 `WarehouseEnvironment` Prefab。场景 `.meta` 与现有摄像机、输入、SceneRoot、Settings 引用保持不变。
- `CombatFoundationV1SceneBuilder` 的环境重建入口同步引用新 Prefab；`WarehouseEnvironmentBuilder` 提供导入、材质映射、环境安装、尺度/遮挡校验及摄像机截图入口。
- 新增资产的 `.meta` 均由目标 Unity 编辑器生成；未升级工程或修改渲染管线。
- 已按用户后续要求启动 Blender 图形界面，窗口标题确认打开的是上述 `Warehouse.blend`。

## 检查结果

| 检查 | 结果 |
|---|---|
| Blender 源文件读取与实际渲染 | 通过；预览由 `.blend` 场景渲染 |
| Unity 导入、编译和材质映射 | 通过；场景使用 Standard 材质 |
| FBX 米制尺寸 | 地面包围盒 `(4.00, 0.24, 24.00)` 米 |
| 左右/前后方向 | 远墙中心 Z = 22.00；近端掩体中心 X = 1.14 |
| 隐藏位主要枪线 | 命中真实 `NearCover` Collider |
| 完全探出位主要枪线 | 到远端 `(0, 1.5, 18)` 的射线畅通 |
| 关键墙体网格与 Collider 对齐 | 通过 PlayMode 检查 |
| 原有摄像机位置和连续探出路径 | 通过；21 个采样位置中视线打开后不再次遮挡 |
| EFTM EditMode | 19/19 通过，0 失败 |
| EFTM PlayMode | 7/7 通过，0 失败；含 2 项新增环境测试 |
| 工程框架校验 | `Project framework validation passed` |
| Git 空白检查 | `git diff --check` 通过 |

首次 FBX 导入被朝向校验拦截，定位到 Blender/Unity 左右轴手性差异；已在源脚本坐标转换中修复，最终可见网格与 Collider 对齐。未使用负缩放绕过校验。

## 可定位证据

以下为根目录 `codex-chat-images/` 下的本地临时证据，不提交 Git：

- `warehouse-blender-perspective.png`：最终源场景的透视俯视渲染。
- `warehouse-unity-hidden.png`、`warehouse-unity-peek.png`：当前 Unity 摄像机隐藏位与完全探出位渲染，未叠加 HUD。
- `warehouse-unity-validation.txt`、`warehouse-unity-import.log`：最终导入、尺度、材质、枪线和框架检查结果。
- `warehouse-editmode.xml`、`warehouse-playmode.xml`：目标版本 Unity Test Runner 结果。
- `warehouse-render.log`：最终 `.blend` 读取和预览渲染结果。

## 范围与后续

- 本次完成环境源文件、Unity 资产替换及现有 U2 行为回归；并非逐像素复刻概念图。
- 模型依现有米制通道尺寸搭建，概念图中的透视比例与道具细节按可实现空间调整；摄像机的既有位置和参数未改变。
- 灯光采用当前 Built-in 实时照明，尚未烘焙光照或做真机性能验收。不能据三角面数推断手机帧率。
- 真实敌人、五点位可见性 Rig、黄色情报剪影、预瞄和射击的后续 Unity 适配仍由工作项 006 的 U3 等切片推进。中央枪线检查不等于五个敌人点位的 10% 可见性验收。
- 当前交付状态：verifying；手机竖屏美术、空间体感与性能等待设计者实际体验。
- 保留任务开始前的 H5 等无关改动；未提交、推送或使用用量重置。
