import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { join } from "node:path";
import { fileURLToPath } from "node:url";

const root = fileURLToPath(new URL("..", import.meta.url));
const html = readFileSync(join(root, "index.html"), "utf8");
const styles = readFileSync(join(root, "styles.css"), "utf8");
const scene = readFileSync(join(root, "src", "peek-scene.js"), "utf8");
const server = readFileSync(join(root, "server.mjs"), "utf8");
const packageJson = JSON.parse(readFileSync(join(root, "package.json"), "utf8"));

test("page keeps the 1080 by 2160 portrait contract", () => {
  assert.match(html, /maximum-scale=1/);
  assert.match(html, /minimum-scale=1/);
  assert.match(html, /user-scalable=no/);
  assert.match(styles, /width:\s*min\(100vw, 50vh\)/);
  assert.match(styles, /height:\s*min\(100vh, 200vw\)/);
  assert.match(styles, /max-width:\s*1080px/);
  assert.match(styles, /max-height:\s*2160px/);
  assert.match(styles, /touch-action:\s*none/);
  assert.match(styles, /\.unsupported\[hidden\]\s*\{\s*display:\s*none/);
});

test("prototype exposes separate true aim, fake action, and contextual fire controls", () => {
  assert.match(html, /id="sceneCanvas"/);
  assert.match(html, /id="trueAimControl"/);
  assert.match(html, /id="fakePeekControl"/);
  assert.match(html, /id="fireControl"/);
  assert.match(html, /<script src="peek-app\.js"><\/script>/);
  assert.match(html, /真架枪点击锁定/);
  assert.match(html, /假动作按住探头/);
  assert.match(html, /掩体后 · 通道边缘可见/);
  assert.doesNotMatch(html, /id="reloadControl"|id="playerActor"|id="ammoCount"/);
  assert.doesNotMatch(scene, /CombatModel|startReload|startCoverSwitch|resolveShots/);
  assert.match(styles, /\.fire-control[^}]*width:\s*16vh[^}]*height:\s*16vh[^}]*border-radius:\s*50%/s);
  assert.match(styles, /\.action-control[^}]*min-height:\s*10\.4vh/);
});

test("three dimensional corridor and real cover geometry are created", () => {
  assert.equal(packageJson.dependencies.three, "^0.185.1");
  assert.match(scene, /from "three"/);
  assert.match(scene, /new THREE\.PerspectiveCamera/);
  assert.match(scene, /new THREE\.WebGLRenderer/);
  assert.match(scene, /"peek-cover"/);
  assert.match(scene, /"left-wall"/);
  assert.match(scene, /"right-wall"/);
  assert.match(scene, /"far-wall"/);
  assert.match(scene, /"dummy-head"/);
  assert.match(scene, /"dummy-cover"/);
});

test("fake action holds peek while true aim latches and unlocks center-ray fire", () => {
  assert.match(scene, /fakePeekButton\.addEventListener\("pointerdown"/);
  assert.match(scene, /trueAimButton\.addEventListener\("click"/);
  assert.match(scene, /toggleCommitted\(\)/);
  assert.match(scene, /setFakeHeld\(true\)/);
  assert.match(scene, /document\.addEventListener\("pointerup", releasePointer/);
  assert.match(scene, /document\.addEventListener\("pointercancel", releasePointer/);
  assert.match(scene, /setFakeHeld\(false\)/);
  assert.match(scene, /window\.addEventListener\("blur"/);
  assert.match(scene, /fireButton\.addEventListener\("pointerdown"/);
  assert.match(scene, /fireButton\.disabled = !committed/);
  assert.match(scene, /if \(!\(aimInteraction\.committed \|\| debugAim\)\) return/);
  assert.match(scene, /isFiring = true/);
  assert.match(scene, /document\.addEventListener\("pointerup", releaseFirePointer/);
  assert.match(scene, /document\.addEventListener\("pointercancel", releaseFirePointer/);
  assert.match(scene, /document\.addEventListener\("pointermove"/);
  assert.match(scene, /aimYaw = THREE\.MathUtils\.clamp/);
  assert.match(scene, /aimPitch = THREE\.MathUtils\.clamp/);
  assert.match(scene, /const SHOT_INTERVAL_MS = 108/);
  assert.match(scene, /const RECOIL_VERTICAL_PER_SHOT = 0\.018/);
  assert.match(scene, /const RECOIL_HORIZONTAL_PER_SHOT = 0\.012/);
  assert.match(scene, /recoilPitch = Math\.min\(0\.09/);
  assert.match(scene, /Math\.random\(\) \* 2 - 1/);
  assert.match(scene, /if \(isFiring\) fireAtReticle\(\)/);
  assert.match(scene, /raycaster\.setFromCamera\(screenCenter, camera\)/);
  assert.match(scene, /isDummyHead/);
});

test("mobile browser selection and zoom gestures remain suppressed", () => {
  assert.match(styles, /-webkit-touch-callout:\s*none/);
  assert.match(styles, /-webkit-user-select:\s*none/);
  assert.match(scene, /"contextmenu"/);
  assert.match(scene, /"dblclick"/);
  assert.match(scene, /"gesturestart"/);
  assert.match(scene, /"gesturechange"/);
});

test("build bundles local three dependency and server exposes LAN access", () => {
  assert.match(packageJson.scripts.build, /esbuild src\/peek-scene\.js --bundle --minify --format=iife/);
  assert.match(packageJson.scripts.serve, /npm run build && node server\.mjs/);
  assert.match(server, /requestedPath\.startsWith\("\/node_modules\/"\)/);
  assert.match(server, /EFTM_H5_HOST \|\| "0\.0\.0\.0"/);
  assert.match(server, /LAN:/);
});
