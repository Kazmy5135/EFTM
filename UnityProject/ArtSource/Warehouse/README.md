# Warehouse 场景源文件

本目录承接已确认的工业仓库俯视效果图。工作项：`Docs/30_Iterations/007-blender-combat-scene/`。

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

在 Unity 中执行 `Tools > EFTM > Install Blender Warehouse Environment`，更新材质映射、碰撞体、环境 Prefab 与战斗场景。它保留现有摄像机与输入对象；菜单会按 Unity 标准流程处理当前未保存场景。

## 验证约束

- 通道约 4 × 24 米、墙高约 3 米；近端遮挡内缘与当前 U2 摄像机相容。
- FBX 需通过米制尺度和左右方向检查；可见网格与关键墙体 Collider 应对齐。
- 缩回时到通道远端的射线命中近端墙；完全探出后主要枪线畅通。
- 当前交付是环境资产，不包含角色模型或新 U3 玩法模块。
- 俯视和 Unity 摄像机预览保存在仓库根目录 `codex-chat-images/`，不提交 Git。
