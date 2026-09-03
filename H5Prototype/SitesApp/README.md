# EFTM H5 Sites 部署项目

此目录只发布当前活跃原型 `H5-PEEK-CAMERA-001` 的 `V1` 快照。

这里的 V1 仅指战斗模块基础原型版本，不代表完整战斗系统或完整游戏 V1。

V1 可部署实现快照对应主仓库 Git commit `b9c8121`。

废弃的 `H5-COMBAT-001` 仅保存在主仓库 Git 提交 `de45dd2`，不进入本部署项目。

更新部署快照：

1. 在上一级 `H5Prototype` 运行 `npm run check`，生成最新的 `peek-app.js`。
2. 在本目录运行 `npm run sync:prototype`。
3. 运行 `npm run build` 验证 Sites 部署产物。

此目录中的 `public/peek-app.js` 和 `public/peek-styles.css` 是当前活跃原型的可部署快照。
