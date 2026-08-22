"use strict";

const test = require("node:test");
const assert = require("node:assert/strict");
const { readFileSync } = require("node:fs");
const { join } = require("node:path");

const root = join(__dirname, "..");
const html = readFileSync(join(root, "index.html"), "utf8");
const game = readFileSync(join(root, "src", "game.js"), "utf8");
const styles = readFileSync(join(root, "styles.css"), "utf8");

test("every DOM id requested by the controller exists in the page", () => {
  const requestedIds = [...game.matchAll(/byId\("([^"]+)"\)/g)].map((match) => match[1]);
  assert.ok(requestedIds.length > 0);
  for (const id of requestedIds) {
    assert.match(html, new RegExp(`id=["']${id}["']`), `missing #${id}`);
  }
});

test("page declares the fixed portrait design contract", () => {
  assert.match(html, /maximum-scale=1/);
  assert.match(html, /minimum-scale=1/);
  assert.match(html, /user-scalable=no/);
  assert.match(styles, /width:\s*min\(100vw, 50vh\)/);
  assert.match(styles, /height:\s*min\(100vh, 200vw\)/);
  assert.match(styles, /touch-action:\s*none/);
  assert.match(styles, /overflow:\s*hidden/);
});

test("iOS long press callout and browser zoom gestures are disabled", () => {
  assert.match(styles, /-webkit-touch-callout:\s*none/);
  assert.match(styles, /-webkit-user-select:\s*none/);
  assert.match(game, /addEventListener\("contextmenu", preventBrowserGesture/);
  assert.match(game, /addEventListener\("dblclick", preventBrowserGesture/);
  assert.match(game, /addEventListener\("gesturestart", preventBrowserGesture/);
  assert.match(game, /addEventListener\("gesturechange", preventBrowserGesture/);
  assert.match(game, /addEventListener\("touchend"/);
  assert.match(game, /now - lastTouchEndAt <= 350/);
});

test("prototype assets referenced by the page exist", () => {
  for (const asset of ["styles.css", "src/combat-model.js", "src/game.js"]) {
    assert.match(html, new RegExp(asset.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")));
    assert.doesNotThrow(() => readFileSync(join(root, asset)));
  }
});

test("player sees own magazine while enemy reload countdown stays hidden", () => {
  assert.match(html, /id="ammoCount"/);
  assert.match(html, /id="ammoCount">15</);
  assert.match(html, /<small>\/15<\/small>/);
  assert.match(html, /id="reloadFill"/);
  assert.doesNotMatch(html, /id="enemyAmmo/);
  assert.match(game, /enemy\.state === STATES\.RELOADING \? "掩体内"/);
});

test("bottom controls reserve left space, keep peek in the center, and put reload on the right", () => {
  assert.match(html, /class="action-row"[\s\S]*class="action-placeholder"[\s\S]*id="holdControl"[\s\S]*id="reloadControl"/);
  assert.doesNotMatch(html, /id="reloadSlider"/);
  assert.match(html, /id="reloadControlTitle"/);
  assert.match(html, /id="reloadProgressFill"/);
  assert.match(game, /返回掩体，子弹夹已空/);
  assert.match(game, /model\.startReload\("player"\)/);
  assert.doesNotMatch(game, /cancelReload/);
  assert.match(game, /player\.state !== STATES\.HIDDEN/);
  assert.match(game, /player\.ammo >= model\.config\.magazineSize/);
  assert.match(game, /reloadControl\.addEventListener\("click"/);
  assert.match(game, /换弹已触发 · 2\.0s/);
  assert.match(game, /自动进行中/);
  assert.match(styles, /\.action-row\s*\{[\s\S]*grid-template-columns:\s*repeat\(3/);
  assert.match(styles, /\.action-row > \.hold-control/);
  assert.match(styles, /\.action-row > \.reload-control/);
  assert.match(styles, /\.reload-progress-fill/);
});

test("an empty player magazine raises a persistent high-priority warning", () => {
  assert.match(html, /id="emptyWarning"[^>]*role="alert"/);
  assert.match(html, /id="emptyWarningHint"/);
  assert.match(html, /id="emptyFlash"/);
  assert.match(game, /pulseClass\(elements\.emptyFlash, "active", 720\)/);
  assert.match(game, /navigator\.vibrate\(\[85, 45, 85\]\)/);
  assert.match(game, /elements\.emptyWarning\.hidden = !showEmptyAlert/);
  assert.match(game, /holdControl\.classList\.toggle\("empty-alert", showEmptyAlert\)/);
  assert.match(game, /reloadControl\.classList\.toggle\("empty-alert", showEmptyAlert && inCover\)/);
  assert.match(game, /点击右侧按钮 · 立即换弹/);
  assert.match(styles, /\.empty-warning\s*\{/);
  assert.match(styles, /@keyframes emptyWarningPulse/);
  assert.match(styles, /\.hold-control\.empty-alert/);
  assert.match(styles, /\.reload-control\.empty-alert/);
});

test("misses and damage create floating combat text", () => {
  assert.match(html, /id="combatTextLayer"/);
  assert.match(game, /spawnCombatText\(event\.targetId, "MISS", "miss"\)/);
  assert.match(game, /spawnCombatText\(event\.targetId, `-\$\{event\.damage\}`, "damage"\)/);
});

test("server listens on the LAN by default", () => {
  const server = readFileSync(join(root, "server.mjs"), "utf8");
  assert.match(server, /EFTM_H5_HOST \|\| "0\.0\.0\.0"/);
  assert.match(server, /LAN:/);
});
