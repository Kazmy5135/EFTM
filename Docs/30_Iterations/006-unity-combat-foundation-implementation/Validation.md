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

## 当前观察

- U1 已增加 `CombatFoundationConfig`、命令/事件/快照类型、`IRandomSource` 和 `CombatFoundationModel`，以及 16 项领域测试。
- 独立 .NET 8/NUnit 临时工程编译通过，按 `EFTM.Tests.EditMode` 过滤运行 16 项测试：16 通过、0 失败。该结果只证明纯 C# 逻辑与测试结构可执行，不替代 Unity 2022.3.62f2 Test Runner。
- 当前主机只发现 Unity `2019.4.8f1` 和 `6000.0.73f1`，没有项目要求的 `2022.3.62f2`。为避免升级或污染工程，未使用错误版本打开项目。
- 因目标 Unity 未运行，新 Combat 目录和 `.cs` 文件尚未由编辑器生成 `.meta`；在 `.meta`、Unity 编译和正式 EditMode 结果补齐前不得提交 U1 Unity 资产。
- Unity 工程现有 3 个 ServiceRegistry EditMode 测试必须保持通过。

## 当前结论

- development 工作项已启动，交付状态为 developing。
- U1 代码和测试逻辑已完成首轮草案；目标编辑器导入、`.meta`、Unity 编译和 19 项 EditMode（现有 3 项 + U1 16 项）仍待验证。
- 当前只授权 U1，不提前进入场景和表现实现。
