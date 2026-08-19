# 验证记录

## 基线绑定

- 日期：2026-08-18
- 工作项：001
- 设计基线：不适用；本工作不定义产品或玩法规则。
- 技术基线：初始项目框架。
- 实现标识：工作区未提交状态。

## 验收

| 验收项 | 方法 | 结果 | 证据 |
|---|---|---|---|
| Unity 版本固定 | 检查 `ProjectVersion.txt` | 通过 | `2022.3.62f2 (7670c08855a9)` |
| JSON 格式 | PowerShell JSON 解析 | 通过 | manifest 与三个 asmdef |
| 场景登记 | 对比 Build Settings 与 `.meta` | 通过 | 路径与 GUID 一致 |
| 服务生命周期 | C# 编译、行为断言与 EditMode 测试 | 通过 | 注册顺序初始化、逆序关闭、失败回滚 |
| Unity 脚本编译 | Unity 2022.3.62f2 批处理导入 | 通过 | 编辑器退出码 0，无编译错误 |
| 框架结构 | `ProjectFrameworkValidator.Validate` | 通过 | 编辑器日志输出 validation passed |
| EditMode 测试 | Unity Test Runner | 通过 | 3 passed、0 failed、0 skipped |

## 未通过项

- 无。
