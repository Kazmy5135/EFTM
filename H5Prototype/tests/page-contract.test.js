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

test("prototype contains only the camera peek interaction", () => {
  assert.match(html, /id="sceneCanvas"/);
  assert.match(html, /id="peekControl"/);
  assert.match(html, /<script src="peek-app\.js"><\/script>/);
  assert.match(html, /按住探头观察/);
  assert.doesNotMatch(html, /id="fireControl"|id="reloadControl"|id="playerActor"|id="enemyActor"|id="ammoCount"/);
  assert.doesNotMatch(scene, /CombatModel|startReload|startCoverSwitch|resolveShots/);
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
});

test("pointer press holds peek and document release returns to cover", () => {
  assert.match(scene, /peekButton\.addEventListener\("pointerdown"/);
  assert.match(scene, /setHeld\(true\)/);
  assert.match(scene, /document\.addEventListener\("pointerup", releasePointer/);
  assert.match(scene, /document\.addEventListener\("pointercancel", releasePointer/);
  assert.match(scene, /setHeld\(false\)/);
  assert.match(scene, /window\.addEventListener\("blur"/);
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
