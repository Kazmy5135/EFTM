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
- 运行时代码另以 `netstandard2.1`、C# 9、关闭 nullable、警告视为错误的兼容配置编译：0 警告、0 错误；该检查覆盖 Unity 2022 LTS 使用的主要语言与 API 边界，但仍不替代目标编辑器导入。
- 设计者随后安装并打开 `E:\Unity 2022.3.62f2\Editor\Unity.exe`；可执行文件、工程 `ProjectVersion.txt` 与编辑器日志均确认为 `2022.3.62f2 (7670c08855a9)`。
- 正式工程完成首次导入和脚本编译，`EFTM.Runtime`、`EFTM.Editor` 与 `EFTM.Tests.EditMode` 均编译成功；Combat 目录、4 个运行时脚本和测试脚本的 `.meta` 已由目标编辑器生成。
- 为不打断设计者当前打开的图形编辑器，使用同一份工程源文件的隔离临时副本运行 Unity 2022.3.62f2 Test Runner：19 项 EditMode 全部通过、0 失败、0 跳过，其中包含原有 3 项 ServiceRegistry 测试和新增 16 项领域测试。
- 使用保留 `Docs/README.md` 同级结构的隔离临时副本执行 `EFTM.Editor.ProjectFrameworkValidator.Validate`，结果为 `[EFTM] Project framework validation passed.`，batchmode 正常退出。

## 当前结论

- development 工作项已启动，交付状态为 developing。
- U1 确定性领域模型已完成：目标编辑器导入、`.meta`、Unity 编译、框架校验和 19 项 EditMode 均通过。
- U1 门禁关闭，可以进入 U2 场景、输入与摄像机实现；工作项整体仍为 developing。
