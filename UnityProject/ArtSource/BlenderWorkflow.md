# Blender MCP 配置与工作流程

配置日期：2026-09-07。此文记录本机工具接入和资产制作方法，不修改玩法设计基线。

## 方法来源

OpenAI 于 2026-09-04 发布的 [Architectural visualization with Astra](https://developers.openai.com/blog/architectural-visualization-with-astra) 展示了通过 `bpy` 建立真实几何、命令行渲染、查看图像和 Blender 界面并迭代的流程。本项目采用这一流程，并用社区 [ahujasid/blender-mcp](https://github.com/ahujasid/blender-mcp) 连接正在运行的 Blender。该 MCP 是独立社区工具，不是 OpenAI 发布的 Astra 专属插件。

## 本机配置

- Blender：`E:/Blender/blender.exe`，5.2.1 LTS。
- MCP 服务：固定 `blender-mcp==1.9.1`，使用独立 Python 3.11.16 和 uv 0.12.10。
- Codex 配置：`C:/Users/castl/.codex/config.toml` 中的 `mcp_servers.blender`，STDIO 方式启动服务，连接 `127.0.0.1:9876`。
- 插件：`C:/Users/castl/AppData/Roaming/Blender Foundation/Blender/5.2/scripts/addons/blender_mcp.py`，已启用并保存用户偏好，当前自动启动为开启。
- 启动超时 60 秒；工具超时 180 秒。长时间渲染通过独立后台 Blender 进程执行并检查退出状态和输出。
- MCP 环境设置 `DISABLE_TELEMETRY=true`，Blender 插件遥测偏好也已关闭。
- uv 与 Python 缓存位于 `C:/Users/castl/.codex/tools/blender-mcp/`；安装未改系统 PATH。
- 原 Codex 配置备份：`C:/Users/castl/.codex/tools/blender-mcp/setup/config-before-blender-20260907-105704.toml`。

## 制作流程

1. 读取目标资产说明、当前场景文件路径、未保存状态、对象集合、相机与单位设置，确认正在处理的场景。
2. 根据已确认的参考图分阶段修改几何、材质、灯光与相机。通过 MCP 的 `execute_blender_code` 调用 `bpy`；每次改动范围明确，并读取结果确认。
3. 先渲染低成本预览，实际查看构图、遮挡、比例、材质和光照。图片保存到仓库根目录 `codex-chat-images/`，按反馈迭代。
4. 需要批量重建或长时间渲染时使用 Blender CLI 和明确的工作副本；不要覆盖当前窗口尚未保存的内容。
5. 导出可编辑 `.blend` 和 Unity 导入资产，检查米制尺度、轴向、材质、关键碰撞体及游戏摄像机下的遮挡。执行与变更相称的 Unity 验证。

模型构件应为真实三维网格。素材或纹理下载应保留来源与授权信息；外部生成服务需要单独配置，不能把安装 MCP 当成付费服务授权。

## 验证与重连

2026-09-07 使用独立 MCP SDK 客户端，按 Codex 配置中的同一启动命令完成 initialize、list_tools、get_scene_info、execute_blender_code 验证：发现 28 个工具，成功读取当前 `Warehouse.blend` 的 29 个对象和 21 个网格。当前窗口未保存标记仍为 true；本次未保存或重建场景文件。

本机验证记录：`C:/Users/castl/.codex/tools/blender-mcp/setup/verification.json`。安装包版本是 1.9.1；MCP initialize 返回的 1.29.1 是服务报告字段，不能当作 blender-mcp 包版本。

配置写入时，当前 Codex 对话的原生工具清单尚未加载 `blender`。新任务仍看不到工具时，重新启动 Codex 后检查；无需重启 Blender。不能把独立客户端验证通过表述为当前对话已原生加载工具。

如果 Blender 是安装插件前启动的，出现 `No module named blender_mcp`，在 Blender Python 控制台调用 `bpy.utils.refresh_script_paths()` 后启用插件即可，无需丢弃未保存场景。其他连接故障先检查当前 Blender 窗口的 MCP 运行状态、端口与插件版本。

用量重置必须取得用户针对本次重置的明确批准；本次配置未使用重置次数。
