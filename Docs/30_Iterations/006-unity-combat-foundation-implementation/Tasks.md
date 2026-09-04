# 任务

## U1 确定性领域模型

- [x] 建立 Combat/Foundation 目录、配置和类型代码。
- [x] 实现命令驱动的 Peek 状态与 300ms/240ms 计时。
- [x] 实现假 Peek 10% 情报阈值、旧位置和 fresh snap。
- [x] 实现 25% 隐蔽换位与可注入随机源。
- [x] 实现开火预备、108ms 连射和 burst 阶段。
- [x] 实现瞄准偏移限制和可复现水平后坐力输入。
- [x] 完成 U1 EditMode 测试并运行现有框架测试。

## U2 场景、输入与摄像机

- [ ] 建立独立 PlayMode 测试程序集。
- [ ] 建立 `CombatFoundationV1.unity` 与场景组合根。
- [ ] 实现 UI Toolkit 真架枪、假动作、开火和 Aim Surface。
- [ ] 实现显式 Hidden/Exposed Pose 的摄像机插值与侧倾。
- [ ] 实现暂停、失焦、PointerCancel 和 OnDisable 安全释放。

## U3 可见性与情报

- [ ] 建立五点位、掩体和 20 点等权 VisibilitySampleRig。
- [ ] 实现无分配遮挡评估和 10% 阈值。
- [ ] 实现旧位置快照、黄色双 Pass 剪影和独立 Layer。
- [ ] 实现一次性旧位置自动预瞄。

## U4 射击与后坐力

- [ ] 实现中心射线、提前 armed 与 108ms 连射。
- [ ] 实现中央/开火指针调瞄。
- [ ] 实现首发、爬升、稳定和随机水平后坐力。
- [ ] 预热并池化弹道、音频和命中反馈。
- [ ] 验证热路径无持续 GC Alloc。

## U5 验证与回传

- [ ] 运行框架校验、EditMode、PlayMode 和构建验证。
- [ ] 生成 Android 开发构建并完成真机验证。
- [ ] 按统一脚本完成 H5/Unity 行为对照。
- [ ] 更新验证、项目状态和追踪登记。
- [ ] 经设计者验收后关闭工作项。
