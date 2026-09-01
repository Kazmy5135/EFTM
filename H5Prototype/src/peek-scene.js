import * as THREE from "three";
import { PeekState } from "./peek-state.js";

const canvas = document.querySelector("#sceneCanvas");
const peekButton = document.querySelector("#peekControl");
const stateLabel = document.querySelector("#stateLabel");
const progressFill = document.querySelector("#peekProgressFill");
const reticle = document.querySelector("#reticle");
const unsupported = document.querySelector("#unsupported");
const debugPeek = new URLSearchParams(window.location.search).get("debugPeek") === "1";
const peekState = new PeekState();

let renderer = null;
let fallbackContext = null;
try {
  renderer = new THREE.WebGLRenderer({ canvas, antialias: true, powerPreference: "high-performance" });
} catch {
  fallbackContext = canvas.getContext("2d");
  document.documentElement.classList.add("canvas-fallback");
  if (!fallbackContext) unsupported.hidden = false;
}

if (renderer) {
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
  renderer.outputColorSpace = THREE.SRGBColorSpace;
  renderer.toneMapping = THREE.ACESFilmicToneMapping;
  renderer.toneMappingExposure = 1.22;
  renderer.shadowMap.enabled = true;
  renderer.shadowMap.type = THREE.PCFShadowMap;
}

const scene = new THREE.Scene();
scene.background = new THREE.Color(0x070c0d);
scene.fog = new THREE.FogExp2(0x081011, 0.045);

const camera = new THREE.PerspectiveCamera(54, 0.5, 0.05, 40);
camera.rotation.order = "YXZ";

const material = (color, options = {}) => new THREE.MeshStandardMaterial({
  color,
  roughness: options.roughness ?? 0.82,
  metalness: options.metalness ?? 0.08,
  emissive: options.emissive ?? 0x000000,
  emissiveIntensity: options.emissiveIntensity ?? 0
});

const addBox = (name, size, position, color, options = {}) => {
  const mesh = new THREE.Mesh(new THREE.BoxGeometry(size[0], size[1], size[2]), material(color, options));
  mesh.name = name;
  mesh.position.set(position[0], position[1], position[2]);
  mesh.castShadow = options.castShadow ?? true;
  mesh.receiveShadow = options.receiveShadow ?? true;
  scene.add(mesh);
  return mesh;
};

// Corridor shell: a narrow industrial lane running away from the player.
addBox("floor", [3.6, 0.18, 22], [0, -1.55, -10.2], 0x273033, { roughness: 0.96 });
addBox("ceiling", [3.6, 0.18, 22], [0, 1.72, -10.2], 0x171d1f, { roughness: 0.95 });
addBox("left-wall", [0.2, 3.4, 22], [-1.72, 0.05, -10.2], 0x263033, { roughness: 0.93 });
addBox("right-wall", [0.2, 3.4, 22], [1.72, 0.05, -10.2], 0x20282a, { roughness: 0.93 });
addBox("far-wall", [3.6, 3.4, 0.2], [0, 0.05, -21.1], 0x141a1c);

// Player-side cover. At rest the camera sits behind this slab, so the corridor is genuinely occluded.
addBox("peek-cover", [2.7, 4.1, 0.32], [1.64, 0.08, 0.02], 0x495051, { roughness: 0.76 });
addBox("door-frame", [0.18, 4.1, 0.46], [0.24, 0.08, -0.02], 0x111719, { metalness: 0.45 });
addBox("door-header", [3.6, 0.22, 0.46], [0, 1.74, -0.02], 0x111719, { metalness: 0.45 });
addBox("cover-panel", [0.76, 0.62, 0.035], [0.86, 0.45, 0.205], 0x171c1d, { metalness: 0.3 });
addBox("cover-handle", [0.08, 0.42, 0.08], [0.38, -0.2, 0.24], 0x0b0d0d, { metalness: 0.8 });

// Repeated beams, lamps, pipes and floor clutter make the depth change readable during the lean.
for (let index = 0; index < 6; index += 1) {
  const z = -2.6 - index * 3.25;
  addBox(`beam-${index}`, [3.25, 0.12, 0.18], [0, 1.48, z], 0x151b1d, { metalness: 0.55 });
  addBox(`stripe-${index}`, [3.12, 0.015, 0.11], [0, -1.445, z], 0x465154, { roughness: 0.7, castShadow: false });
  const lamp = addBox(`lamp-${index}`, [0.72, 0.055, 0.32], [index % 2 ? 0.62 : -0.62, 1.57, z - 0.25], 0x9ab6ad, {
    emissive: index % 3 === 1 ? 0x68452a : 0x8bb8a7,
    emissiveIntensity: index % 3 === 1 ? 2.1 : 1.55,
    roughness: 0.28,
    castShadow: false
  });
  lamp.receiveShadow = false;
}

addBox("left-pipe", [0.16, 0.16, 17.5], [-1.48, 1.13, -9.8], 0x515f5c, { metalness: 0.72 });
addBox("right-pipe", [0.11, 0.11, 15.8], [1.47, 1.23, -10.2], 0x564a3a, { metalness: 0.66 });
addBox("crate-a", [0.78, 0.72, 0.92], [-0.94, -1.1, -8.8], 0x39413c, { roughness: 0.9 });
addBox("crate-b", [0.62, 0.48, 0.72], [-0.48, -1.22, -9.25], 0x2b3431, { roughness: 0.9 });
addBox("barrier", [1.2, 0.72, 0.18], [0.65, -1.13, -14.1], 0x4a3026, { metalness: 0.22 });
addBox("far-door", [1.16, 2.35, 0.12], [-0.34, -0.35, -20.94], 0x243034, { metalness: 0.48 });

scene.add(new THREE.HemisphereLight(0x789894, 0x151a1a, 1.12));
scene.add(new THREE.AmbientLight(0x3b4e4b, 0.68));
const playerLight = new THREE.PointLight(0xc4ded7, 3.8, 3.2, 1.8);
playerLight.position.set(0.7, 0.65, 0.86);
scene.add(playerLight);
const entranceLight = new THREE.PointLight(0xaccfc2, 6.2, 9, 2.1);
entranceLight.position.set(-0.45, 1.05, -1.8);
entranceLight.castShadow = true;
scene.add(entranceLight);
const warmLight = new THREE.PointLight(0xd37b45, 4.1, 8, 2.2);
warmLight.position.set(0.75, 0.7, -10.8);
scene.add(warmLight);
const farLight = new THREE.PointLight(0x4e8a83, 5.4, 10, 2.1);
farLight.position.set(-0.55, 0.9, -18.4);
scene.add(farLight);

const phaseText = {
  hidden: "掩体后 · 通道不可见",
  peeking: "探头中",
  holding: "完全探出 · 通道可见",
  returning: "缩回掩体"
};

const setHeld = (held) => {
  peekState.setHeld(held || debugPeek);
  peekButton.classList.toggle("is-held", held || debugPeek);
  peekButton.setAttribute("aria-pressed", String(held || debugPeek));
};

let activePointerId = null;
peekButton.addEventListener("pointerdown", (event) => {
  event.preventDefault();
  activePointerId = event.pointerId;
  setHeld(true);
  try { peekButton.setPointerCapture(event.pointerId); } catch { /* document release remains authoritative */ }
});

const releasePointer = (event) => {
  if (activePointerId === null || event.pointerId !== activePointerId) return;
  activePointerId = null;
  setHeld(false);
};
document.addEventListener("pointerup", releasePointer, { passive: false });
document.addEventListener("pointercancel", releasePointer, { passive: false });
window.addEventListener("blur", () => { activePointerId = null; setHeld(false); });
document.addEventListener("visibilitychange", () => {
  if (document.hidden) { activePointerId = null; setHeld(false); }
});

window.addEventListener("keydown", (event) => {
  if (event.code !== "Space" || event.repeat) return;
  event.preventDefault();
  setHeld(true);
});
window.addEventListener("keyup", (event) => {
  if (event.code !== "Space") return;
  event.preventDefault();
  setHeld(false);
});

const stopBrowserGesture = (event) => { if (event.cancelable) event.preventDefault(); };
for (const eventName of ["contextmenu", "selectstart", "dragstart", "dblclick", "gesturestart", "gesturechange", "gestureend"]) {
  document.addEventListener(eventName, stopBrowserGesture, { passive: false });
}

const renderFallback = (progress, roll) => {
  if (!fallbackContext) return;
  const context = fallbackContext;
  const width = canvas.width;
  const height = canvas.height;
  const reveal = width * Math.pow(progress, 1.45) * 0.83;
  const horizonY = height * 0.43;
  const floorY = height * 0.81;
  context.save();
  context.clearRect(0, 0, width, height);
  context.translate(width / 2, height / 2);
  context.rotate(roll);
  context.translate(-width / 2, -height / 2);

  context.save();
  context.beginPath();
  context.rect(-width * 0.1, 0, reveal + width * 0.1, height);
  context.clip();
  const corridor = context.createLinearGradient(0, 0, 0, height);
  corridor.addColorStop(0, "#142123");
  corridor.addColorStop(0.52, "#0c1618");
  corridor.addColorStop(1, "#1d292a");
  context.fillStyle = corridor;
  context.fillRect(0, 0, width, height);

  context.fillStyle = "#1f2b2d";
  context.beginPath();
  context.moveTo(0, 0);
  context.lineTo(width * 0.49, horizonY);
  context.lineTo(width * 0.49, floorY);
  context.lineTo(0, height);
  context.closePath();
  context.fill();
  context.fillStyle = "#11191b";
  context.beginPath();
  context.moveTo(width, 0);
  context.lineTo(width * 0.55, horizonY);
  context.lineTo(width * 0.55, floorY);
  context.lineTo(width, height);
  context.closePath();
  context.fill();
  context.fillStyle = "#0a1112";
  context.fillRect(width * 0.49, horizonY, width * 0.06, floorY - horizonY);

  for (let index = 0; index < 7; index += 1) {
    const depth = index / 6;
    const y = horizonY + Math.pow(depth, 1.75) * (floorY - horizonY);
    context.strokeStyle = `rgba(116, 151, 144, ${0.1 + depth * 0.22})`;
    context.lineWidth = Math.max(1, depth * width * 0.006);
    context.beginPath();
    context.moveTo(width * (0.49 - depth * 0.48), y);
    context.lineTo(width * (0.55 + depth * 0.45), y);
    context.stroke();
  }

  for (let index = 0; index < 5; index += 1) {
    const depth = (index + 1) / 6;
    const y = horizonY - depth * height * 0.31;
    const lampWidth = width * (0.04 + depth * 0.1);
    context.fillStyle = index === 2 ? "rgba(255,151,84,.7)" : "rgba(154,222,204,.68)";
    context.shadowColor = context.fillStyle;
    context.shadowBlur = width * 0.035;
    context.fillRect(width * 0.52 - lampWidth / 2, y, lampWidth, Math.max(2, depth * height * 0.007));
  }
  context.shadowBlur = 0;
  context.fillStyle = "#27332f";
  context.fillRect(width * 0.2, height * 0.62, width * 0.15, height * 0.1);
  context.fillStyle = "#53372a";
  context.fillRect(width * 0.59, height * 0.57, width * 0.18, height * 0.09);
  context.restore();

  const wallGradient = context.createLinearGradient(reveal, 0, width, height);
  wallGradient.addColorStop(0, "#171c1d");
  wallGradient.addColorStop(0.08, "#303638");
  wallGradient.addColorStop(1, "#202526");
  context.fillStyle = wallGradient;
  context.fillRect(reveal, -height * 0.1, width * 1.2, height * 1.2);
  context.fillStyle = "rgba(7,10,10,.72)";
  context.fillRect(reveal + width * 0.08, height * 0.28, width * 0.34, height * 0.13);
  context.fillStyle = "rgba(116,138,132,.18)";
  context.fillRect(reveal + width * 0.11, height * 0.31, width * 0.22, height * 0.012);
  context.fillStyle = "#0a0d0d";
  context.fillRect(reveal + width * 0.025, height * 0.49, width * 0.026, height * 0.13);
  context.restore();
};

const resize = () => {
  const width = canvas.clientWidth;
  const height = canvas.clientHeight;
  if (renderer) renderer.setSize(width, height, false);
  else {
    const ratio = Math.min(window.devicePixelRatio || 1, 2);
    canvas.width = Math.max(1, Math.round(width * ratio));
    canvas.height = Math.max(1, Math.round(height * ratio));
  }
  camera.aspect = width / Math.max(1, height);
  camera.updateProjectionMatrix();
};
window.addEventListener("resize", resize, { passive: true });
resize();

if (debugPeek) setHeld(true);

let previousTime = performance.now();
const animate = (time) => {
  const deltaMs = Math.min(250, time - previousTime);
  previousTime = time;
  const snapshot = peekState.step(deltaMs);
  const breathing = snapshot.progress > 0.98 ? Math.sin(time * 0.0025) * 0.006 : 0;
  camera.position.set(snapshot.pose.x, snapshot.pose.y + breathing, snapshot.pose.z);
  camera.rotation.set(0, snapshot.pose.yaw, snapshot.pose.roll);

  document.documentElement.style.setProperty("--peek-progress", snapshot.progress.toFixed(4));
  stateLabel.textContent = phaseText[snapshot.phase];
  progressFill.style.transform = `scaleX(${snapshot.progress.toFixed(4)})`;
  reticle.style.opacity = String(Math.max(0, (snapshot.progress - 0.55) / 0.45));
  peekButton.querySelector("small").textContent = snapshot.held ? "松开缩回" : "按住不放";

  if (renderer) renderer.render(scene, camera);
  else renderFallback(snapshot.progress, snapshot.pose.roll);
  requestAnimationFrame(animate);
};
requestAnimationFrame(animate);
