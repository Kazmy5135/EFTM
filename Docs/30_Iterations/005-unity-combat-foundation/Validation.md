# 验证记录

## 基线绑定

- 工作项：`005-unity-combat-foundation`
- 设计：`DESIGN-PRODUCT-DEFINITION`、`DESIGN-CORE-COMBAT`、`DDR-001`，accepted，设计基线 commit `88d04fd`。
- 技术：`TECH-COMBAT-FOUNDATION-V1`，accepted；设计者于 2026-09-04 批准。
- H5：`H5-PEEK-CAMERA-001 V1`，tag `h5-peek-camera-v1`，commit `b9c8121`。
- Unity：`2022.3.62f2`。

## 当前事实

- Unity 工程当前只有 Bootstrap、ServiceRegistry、框架验证器和 3 个 EditMode 服务测试。
- 当前不存在 Combat 运行时代码、PlayMode 测试程序集或战斗验证场景。
- Built-in Render Pipeline、物理模块和 UI Toolkit 可用；未引入 Input System、URP、Cinemachine 或 uGUI 包。
- 本轮尚未修改 Unity 代码或资产。

## 计划自动化证据

- U1：确定性状态、时间、情报、随机、连射和后坐力 EditMode 测试。
- U2：输入、摄像机、PointerCancel、暂停与场景生命周期 PlayMode 测试。
- U3：五点位真实遮挡、10% 阈值、旧位置剪影和预瞄 PlayMode 测试。
- U4：预备开火、108ms 连射、拖动调瞄、后坐力阶段和热路径测试。
- U5：项目框架校验、全部测试、Android 开发构建和目标设备记录。

## H5—Unity 对照要求

- 使用同一组动作脚本分别验证真架枪、假动作、开火预备、情报获取、剪影、预瞄、调瞄和后坐力。
- H5 提供视觉与体感参照；Unity 是否通过由 accepted 设计验收标准决定。
- 所有差异必须分类为允许的平台差异、技术缺陷、设计歧义或实现偏差。
- 设计歧义返回 Planning Design；技术缺陷留在本工作项；实现偏差在追踪登记标记 `diverged`。

## 当前结论

- 技术设计已获批准，005 技术工作项关闭。
- 下一步建立独立 development 工作项，并从 U1 确定性领域模型开始正式实现。
