---
title: 设计—技术—实现追踪登记
document_id: TRACEABILITY-REGISTER
status: accepted
last_updated: 2026-09-08
---

# 设计—技术—实现追踪登记

| 主题 | 设计基线 | 技术基线 | 实现目标 | 适用工作项 | 交付状态 | 验证证据 |
|---|---|---|---|---|---|---|
| 项目框架 | 不适用 | `ADR-001`、初始框架约定 | `UnityProject/Assets/_Project`、`UnityProject/Packages`、`UnityProject/ProjectSettings` | 001、004 | verifying | `Docs/30_Iterations/001-project-bootstrap/Validation.md`、`Docs/30_Iterations/004-repository-restructure/Validation.md` |
| 产品定义与完整 Demo 范围 | `DESIGN-PRODUCT-DEFINITION`（accepted） | 尚未建立 | 尚未进入实现 | 002、004 | not_assessed | 战斗模块 V1 已同步并于 2026-09-03 获设计者批准 |
| 核心掩体战斗 V1 基础方向 | `DESIGN-CORE-COMBAT`、`DDR-001`（accepted） | `TECH-COMBAT-FOUNDATION-V1`（accepted） | `H5-PEEK-CAMERA-001 V1`（已验证 H5）；Unity U1～U4 `Assets/_Project/Runtime/Combat` 与 `CombatFoundationV1.unity` | 003、004、005、006 | verifying | H5：`Docs/30_Iterations/004-peek-camera/Validation.md`；Unity：`Docs/30_Iterations/006-unity-combat-foundation-implementation/Validation.md`；项目 EditMode 22/22、PlayMode 16/16、Windows 开发包启动，Android/真机待验收 |
| Blender 仓库场景美术与 Unity 接入 | `DESIGN-CORE-COMBAT`（accepted）、工作项 007 已批准美术提案 | `TECH-COMBAT-FOUNDATION-V1`（accepted） | `UnityProject/ArtSource/Warehouse`、`Assets/_Project/Art/Warehouse`、`CombatFoundationV1.unity` | 007 | verifying | `Docs/30_Iterations/007-blender-combat-scene/Validation.md`；26/26 测试通过，手机视觉与体感待验收 |

登记表只维护对应关系，不拥有设计规则、技术契约或测试正文。
