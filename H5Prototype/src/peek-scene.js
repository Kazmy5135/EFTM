import * as THREE from "three";
import { PEEK_CONFIG, PeekState } from "./peek-state.js";
import { AimInteraction } from "./aim-interaction.js";
import { ENEMY_POSITIONS, ENEMY_VISIBILITY_THRESHOLD, EnemyIntel } from "./enemy-intel.js";
import { RECOIL_PROFILE, recoilImpulseForShot, recoilPhaseForShot } from "./recoil-profile.js";

const app = document.querySelector("#app");
const canvas = document.querySelector("#sceneCanvas");
const aimSurface = document.querySelector("#aimSurface");
const trueAimButton = document.querySelector("#trueAimControl");
const fakePeekButton = document.querySelector("#fakePeekControl");
const fireButton = document.querySelector("#fireControl");
const stateLabel = document.querySelector("#stateLabel");
const progressFill = document.querySelector("#peekProgressFill");
const reticle = document.querySelector("#reticle");
const intelState = document.querySelector("#intelState");
const visibilityLabel = document.querySelector("#visibilityLabel");
const shotFlash = document.querySelector("#shotFlash");
const shotResult = document.querySelector("#shotResult");
const unsupported = document.querySelector("#unsupported");
const debugParams = new URLSearchParams(window.location.search);
const debugPeek = debugParams.get("debugPeek") === "1";
const debugAim = debugParams.get("debugAim") === "1";
const peekState = new PeekState();
const aimInteraction = new AimInteraction();
const enemyIntel = new EnemyIntel();
const debugEnemyPosition = Number(debugParams.get("enemyPos"));
if (Number.isInteger(debugEnemyPosition) && debugEnemyPosition >= 0 && debugEnemyPosition < ENEMY_POSITIONS.length) {
  enemyIntel.currentPositionIndex = debugEnemyPosition;
}
const debugIntelPosition = Number(debugParams.get("intelPos"));
if (Number.isInteger(debugIntelPosition) && debugIntelPosition >= 0 && debugIntelPosition < ENEMY_POSITIONS.length) {
  enemyIntel.rememberedPositionIndex = debugIntelPosition;
  enemyIntel.pendingAutoAim = true;
  enemyIntel.ghostVisible = true;
}

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
  renderer.toneMappingExposure = 1.72;
  renderer.shadowMap.enabled = true;
  renderer.shadowMap.type = THREE.PCFShadowMap;
}

const scene = new THREE.Scene();
scene.background = new THREE.Color(0x121d20);
scene.fog = new THREE.FogExp2(0x152326, 0.027);

const camera = new THREE.PerspectiveCamera(54, 0.5, 0.05, 40);
camera.rotation.order = "YXZ";
const shotTargets = [];

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
  if (options.shotTarget) shotTargets.push(mesh);
  return mesh;
};

// Corridor shell: a narrow industrial lane running away from the player.
addBox("floor", [3.6, 0.18, 22], [0, -1.55, -10.2], 0x3b484a, { roughness: 0.96, shotTarget: true });
addBox("ceiling", [3.6, 0.18, 22], [0, 1.72, -10.2], 0x2a3436, { roughness: 0.95, shotTarget: true });
addBox("left-wall", [0.2, 3.4, 22], [-1.72, 0.05, -10.2], 0x3a484b, { roughness: 0.93, shotTarget: true });
addBox("right-wall", [0.2, 3.4, 22], [1.72, 0.05, -10.2], 0x323f42, { roughness: 0.93, shotTarget: true });
addBox("far-wall", [3.6, 3.4, 0.2], [0, 0.05, -21.1], 0x263235, { shotTarget: true });
addBox("entry-lamp", [1.15, 0.055, 0.3], [-0.7, 1.57, -1.3], 0xb8d8cf, {
  emissive: 0xa7d8ca,
  emissiveIntensity: 2.8,
  roughness: 0.25,
  castShadow: false
});
addBox("entry-floor-guide", [3.05, 0.015, 0.09], [0, -1.445, -1.85], 0x718b84, {
  emissive: 0x405f59,
  emissiveIntensity: 1.25,
  castShadow: false
});

// Player-side cover. At rest the camera sits behind this slab, so the corridor is genuinely occluded.
addBox("peek-cover", [2.7, 4.1, 0.32], [1.64, 0.08, 0.02], 0x626d6e, { roughness: 0.76, shotTarget: true });
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
addBox("crate-a", [0.78, 0.72, 0.92], [-0.94, -1.1, -8.8], 0x39413c, { roughness: 0.9, shotTarget: true });
addBox("crate-b", [0.62, 0.48, 0.72], [-0.48, -1.22, -9.25], 0x2b3431, { roughness: 0.9, shotTarget: true });
addBox("barrier", [1.2, 0.72, 0.18], [0.65, -1.13, -14.1], 0x4a3026, { metalness: 0.22, shotTarget: true });
addBox("far-door", [1.16, 2.35, 0.12], [-0.34, -0.35, -20.94], 0x243034, { metalness: 0.48, shotTarget: true });

// The opponent surrogate can occupy five far-corridor positions with different
// exposure profiles. Occlusion, not a UI shortcut, determines fake-peek intel.
const dummyCover = addBox("dummy-cover", [1, 1, 1], [0, -0.9, -18], 0x525d60, { metalness: 0.18, shotTarget: true });
dummyCover.userData.blocksShot = true;
const enemyActor = new THREE.Group();
enemyActor.name = "enemy-actor";
scene.add(enemyActor);
const dummyHeadMaterial = material(0xc18e70, { roughness: 0.84 });
dummyHeadMaterial.emissive = new THREE.Color(0x000000);
const dummyHead = new THREE.Mesh(new THREE.SphereGeometry(0.22, 24, 18), dummyHeadMaterial);
dummyHead.name = "dummy-head";
dummyHead.position.set(0, 0, 0);
dummyHead.scale.set(0.88, 1.12, 0.92);
dummyHead.castShadow = true;
dummyHead.userData.isDummyHead = true;
dummyHead.userData.isEnemyPart = true;
enemyActor.add(dummyHead);
shotTargets.push(dummyHead);
const helmetMaterial = material(0x29383a, { roughness: 0.76, metalness: 0.16 });
const dummyHelmet = new THREE.Mesh(new THREE.SphereGeometry(0.235, 24, 18, 0, Math.PI * 2, 0, Math.PI * 0.58), helmetMaterial);
dummyHelmet.name = "dummy-helmet";
dummyHelmet.position.set(0, 0.045, 0);
dummyHelmet.scale.set(0.9, 0.94, 0.94);
dummyHelmet.rotation.z = -0.08;
dummyHelmet.castShadow = true;
dummyHelmet.userData.isDummyHead = true;
dummyHelmet.userData.isEnemyPart = true;
enemyActor.add(dummyHelmet);
shotTargets.push(dummyHelmet);

const uniformMaterial = material(0x405052, { roughness: 0.88 });
const webbingMaterial = material(0x252f30, { roughness: 0.93 });
const addEnemyPart = (name, size, position, partMaterial = uniformMaterial) => {
  const mesh = new THREE.Mesh(new THREE.BoxGeometry(size[0], size[1], size[2]), partMaterial);
  mesh.name = name;
  mesh.position.set(position[0], position[1], position[2]);
  mesh.castShadow = true;
  mesh.receiveShadow = true;
  mesh.userData.isEnemyPart = true;
  enemyActor.add(mesh);
  shotTargets.push(mesh);
  return mesh;
};
addEnemyPart("dummy-neck", [0.16, 0.16, 0.14], [0, -0.22, 0.01], dummyHeadMaterial);
addEnemyPart("dummy-torso", [0.58, 0.68, 0.28], [0, -0.58, 0.03]);
addEnemyPart("dummy-vest", [0.48, 0.42, 0.32], [0, -0.53, 0.01], webbingMaterial);
addEnemyPart("dummy-left-arm", [0.17, 0.68, 0.18], [-0.37, -0.58, 0.04]);
addEnemyPart("dummy-right-arm", [0.17, 0.68, 0.18], [0.37, -0.58, 0.04]);
addEnemyPart("dummy-pelvis", [0.48, 0.3, 0.25], [0, -1.02, 0.04], webbingMaterial);
addEnemyPart("dummy-left-leg", [0.2, 0.64, 0.22], [-0.15, -1.34, 0.04]);
addEnemyPart("dummy-right-leg", [0.2, 0.64, 0.22], [0.15, -1.34, 0.04]);

const createIntelGhost = (name, { opacity, wireframe, scale, renderOrder, blending }) => {
  const ghost = enemyActor.clone(true);
  ghost.name = name;
  ghost.scale.setScalar(scale);
  ghost.visible = false;
  ghost.traverse((object) => {
    if (!object.isMesh) return;
    object.material = new THREE.MeshBasicMaterial({
      color: 0xffd22e,
      transparent: true,
      opacity,
      depthTest: false,
      depthWrite: false,
      wireframe,
      blending,
      toneMapped: false
    });
    object.castShadow = false;
    object.receiveShadow = false;
    object.renderOrder = renderOrder;
  });
  scene.add(ghost);
  return ghost;
};
const intelGhostGlow = createIntelGhost("intel-ghost-glow", { opacity: 0.24, wireframe: true, scale: 1.09, renderOrder: 30, blending: THREE.AdditiveBlending });
const intelGhostFill = createIntelGhost("intel-ghost-fill", { opacity: 0.74, wireframe: false, scale: 1, renderOrder: 31, blending: THREE.NormalBlending });
const intelGhosts = [intelGhostGlow, intelGhostFill];

const ENEMY_VISIBILITY_SAMPLES = Object.freeze([
  [-0.12, 0.08, 0], [0, 0.12, 0], [0.12, 0.08, 0],
  [-0.3, -0.32, 0], [0.3, -0.32, 0],
  [-0.2, -0.52, 0], [0, -0.48, 0], [0.2, -0.52, 0],
  [-0.36, -0.66, 0], [0.36, -0.66, 0],
  [-0.2, -0.82, 0], [0, -0.82, 0], [0.2, -0.82, 0],
  [-0.16, -1.08, 0], [0.16, -1.08, 0],
  [-0.16, -1.28, 0], [0.16, -1.28, 0],
  [-0.16, -1.48, 0], [0, -1.48, 0], [0.16, -1.48, 0]
]);

let activeShotTargets = [];
let activeVisibilityOccluders = [];
const applyEnemyPosition = (positionIndex) => {
  const position = ENEMY_POSITIONS[positionIndex];
  enemyActor.position.set(position.actor[0], position.actor[1], position.actor[2]);
  if (position.cover && position.coverSize) {
    dummyCover.visible = true;
    dummyCover.position.set(position.cover[0], position.cover[1], position.cover[2]);
    dummyCover.scale.set(position.coverSize[0], position.coverSize[1], position.coverSize[2]);
  } else {
    dummyCover.visible = false;
  }
  activeShotTargets = shotTargets.filter((target) => target.visible);
  activeVisibilityOccluders = activeShotTargets.filter((target) => !target.userData.isEnemyPart);
};
applyEnemyPosition(enemyIntel.currentPositionIndex);

const syncIntelGhost = (isFullyHidden) => {
  const positionIndex = enemyIntel.rememberedPositionIndex;
  const visible = isFullyHidden && enemyIntel.ghostVisible && positionIndex !== null;
  const rememberedPosition = visible ? ENEMY_POSITIONS[positionIndex].actor : null;
  for (const ghost of intelGhosts) {
    ghost.visible = visible;
    if (rememberedPosition) ghost.position.set(rememberedPosition[0], rememberedPosition[1], rememberedPosition[2]);
  }
};

scene.add(new THREE.HemisphereLight(0xafd8d1, 0x35403f, 1.95));
scene.add(new THREE.AmbientLight(0x6e8b85, 1.28));
const playerLight = new THREE.PointLight(0xd9eee9, 5.2, 4.2, 1.7);
playerLight.position.set(0.7, 0.65, 0.86);
scene.add(playerLight);
const entranceLight = new THREE.PointLight(0xc8eee2, 8.6, 11, 1.9);
entranceLight.position.set(-0.45, 1.05, -1.8);
entranceLight.castShadow = true;
scene.add(entranceLight);
const warmLight = new THREE.PointLight(0xe4945d, 5.6, 9, 2);
warmLight.position.set(0.75, 0.7, -10.8);
scene.add(warmLight);
const farLight = new THREE.PointLight(0x72c5b9, 8.2, 11, 1.9);
farLight.position.set(0.25, 0.85, -18.1);
scene.add(farLight);

const phaseText = {
  hidden: "掩体后 · 通道边缘可见",
  peeking: "探头中",
  holding: "完全探出 · 通道可见",
  returning: "缩回掩体"
};

const syncPeekTarget = () => {
  peekState.setHeld(aimInteraction.peekRequested() || debugPeek || debugAim);
};

let isFiring = false;
let activeFirePointerId = null;
let burstShotCount = 0;
let recoilReturnMs = RECOIL_PROFILE.climbReturnMs;
let firstShotDeferred = false;

const stopFiring = () => {
  isFiring = false;
  activeFirePointerId = null;
  document.documentElement.dataset.lastBurstShots = String(burstShotCount);
  burstShotCount = 0;
  firstShotDeferred = false;
  fireButton.classList.remove("is-firing");
  fireButton.setAttribute("aria-pressed", "false");
};

const syncControls = (snapshot = peekState.snapshot()) => {
  const interaction = aimInteraction.snapshot();
  const committed = interaction.committed || debugAim;
  const fakeHeld = interaction.fakeHeld || debugPeek;
  trueAimButton.classList.toggle("is-committed", committed);
  trueAimButton.setAttribute("aria-pressed", String(committed));
  trueAimButton.querySelector("small").textContent = committed ? "点击缩回" : "点击锁定";
  fakePeekButton.classList.toggle("is-held", fakeHeld);
  fakePeekButton.setAttribute("aria-pressed", String(fakeHeld));
  fakePeekButton.disabled = committed;
  fireButton.hidden = !committed;
  fireButton.disabled = !committed;
  fireButton.classList.toggle("is-armed", isFiring && snapshot.progress < 1);
  app.classList.toggle("is-aiming", committed);
  intelState.textContent = enemyIntel.intelLabel();
  intelState.classList.toggle("has-intel", enemyIntel.rememberedPositionIndex !== null);
  reticle.classList.toggle("is-preaimed", enemyIntel.rememberedPositionIndex !== null);
};

trueAimButton.addEventListener("click", (event) => {
  event.preventDefault();
  ensureAudioReady();
  if (debugAim) return;
  aimInteraction.toggleCommitted();
  if (aimInteraction.committed) {
    enemyIntel.hideGhost();
    const preAimPositionIndex = enemyIntel.consumePendingAutoAim();
    if (preAimPositionIndex !== null) applyPreAimToPosition(preAimPositionIndex);
  } else {
    stopFiring();
  }
  syncPeekTarget();
  syncControls();
});

let activePointerId = null;
fakePeekButton.addEventListener("pointerdown", (event) => {
  event.preventDefault();
  if (!aimInteraction.setFakeHeld(true)) return;
  enemyIntel.hideGhost();
  activePointerId = event.pointerId;
  syncPeekTarget();
  syncControls();
  try { fakePeekButton.setPointerCapture(event.pointerId); } catch { /* document release remains authoritative */ }
});

const releasePointer = (event) => {
  if (activePointerId === null || event.pointerId !== activePointerId) return;
  activePointerId = null;
  aimInteraction.setFakeHeld(false);
  syncPeekTarget();
  syncControls();
};
document.addEventListener("pointerup", releasePointer, { passive: false });
document.addEventListener("pointercancel", releasePointer, { passive: false });
window.addEventListener("blur", () => {
  activePointerId = null;
  activeAimPointerId = null;
  aimInteraction.releaseAll();
  stopFiring();
  syncPeekTarget();
  syncControls();
});
document.addEventListener("visibilitychange", () => {
  if (!document.hidden) return;
  activePointerId = null;
  activeAimPointerId = null;
  aimInteraction.releaseAll();
  stopFiring();
  syncPeekTarget();
  syncControls();
});

window.addEventListener("keydown", (event) => {
  if (event.repeat) return;
  if (event.code === "Space") {
    event.preventDefault();
    if (aimInteraction.setFakeHeld(true)) {
      enemyIntel.hideGhost();
      syncPeekTarget();
      syncControls();
    }
  }
  if (event.code === "KeyE") {
    event.preventDefault();
    aimInteraction.toggleCommitted();
    if (aimInteraction.committed) {
      enemyIntel.hideGhost();
      const preAimPositionIndex = enemyIntel.consumePendingAutoAim();
      if (preAimPositionIndex !== null) applyPreAimToPosition(preAimPositionIndex);
    } else {
      stopFiring();
    }
    syncPeekTarget();
    syncControls();
  }
});
window.addEventListener("keyup", (event) => {
  if (event.code !== "Space") return;
  event.preventDefault();
  aimInteraction.setFakeHeld(false);
  syncPeekTarget();
  syncControls();
});

const stopBrowserGesture = (event) => { if (event.cancelable) event.preventDefault(); };
for (const eventName of ["contextmenu", "selectstart", "dragstart", "dblclick", "gesturestart", "gesturechange", "gestureend"]) {
  document.addEventListener(eventName, stopBrowserGesture, { passive: false });
}

const raycaster = new THREE.Raycaster();
const visibilityRaycaster = new THREE.Raycaster();
const screenCenter = new THREE.Vector2(0, 0);
const sampleWorldPoint = new THREE.Vector3();
const sampleProjectedPoint = new THREE.Vector3();
const sampleDirection = new THREE.Vector3();
const AIM_YAW_LIMIT = 0.12;
const AIM_PITCH_LIMIT = 0.14;
let lastShotTime = -Infinity;
let aimYaw = 0;
let aimPitch = 0;
let recoilYaw = 0;
let recoilPitch = 0;
let lastFirePointerX = 0;
let lastFirePointerY = 0;
let activeAimPointerId = null;
let lastAimSurfaceX = 0;
let lastAimSurfaceY = 0;

const applyAimDelta = (deltaX, deltaY) => {
  aimYaw = THREE.MathUtils.clamp(aimYaw - deltaX / Math.max(1, window.innerWidth) * 0.48, -AIM_YAW_LIMIT, AIM_YAW_LIMIT);
  aimPitch = THREE.MathUtils.clamp(aimPitch - deltaY / Math.max(1, window.innerHeight) * 0.9, -AIM_PITCH_LIMIT, AIM_PITCH_LIMIT);
};

const applyPreAimToPosition = (positionIndex) => {
  const position = ENEMY_POSITIONS[positionIndex];
  const deltaX = position.actor[0] - PEEK_CONFIG.exposedX;
  const deltaY = position.actor[1] - PEEK_CONFIG.exposedY;
  const deltaZ = position.actor[2] - PEEK_CONFIG.exposedZ;
  const horizontalDistance = Math.hypot(deltaX, deltaZ);
  const desiredYaw = Math.atan2(-deltaX, -deltaZ);
  const desiredPitch = Math.atan2(deltaY, horizontalDistance);
  aimYaw = THREE.MathUtils.clamp(desiredYaw - PEEK_CONFIG.exposedYaw, -AIM_YAW_LIMIT, AIM_YAW_LIMIT);
  aimPitch = THREE.MathUtils.clamp(desiredPitch, -AIM_PITCH_LIMIT, AIM_PITCH_LIMIT);
  recoilYaw = 0;
  recoilPitch = 0;
};

const calculateEnemyVisibility = () => {
  camera.updateMatrixWorld();
  enemyActor.updateWorldMatrix(true, false);
  let visibleSamples = 0;

  for (const sample of ENEMY_VISIBILITY_SAMPLES) {
    sampleWorldPoint.set(sample[0], sample[1], sample[2]);
    enemyActor.localToWorld(sampleWorldPoint);
    sampleProjectedPoint.copy(sampleWorldPoint).project(camera);
    if (Math.abs(sampleProjectedPoint.x) > 1 || Math.abs(sampleProjectedPoint.y) > 1 || sampleProjectedPoint.z < -1 || sampleProjectedPoint.z > 1) continue;

    sampleDirection.copy(sampleWorldPoint).sub(camera.position);
    const sampleDistance = sampleDirection.length();
    sampleDirection.normalize();
    visibilityRaycaster.set(camera.position, sampleDirection);
    visibilityRaycaster.far = Math.max(0.01, sampleDistance - 0.025);
    if (visibilityRaycaster.intersectObjects(activeVisibilityOccluders, false).length === 0) visibleSamples += 1;
  }

  return visibleSamples / ENEMY_VISIBILITY_SAMPLES.length;
};

const AudioContextClass = window.AudioContext || window.webkitAudioContext;
let shotAudioContext = null;
let shotAudioBuffer = null;

const buildShotAudioBuffer = (audioContext) => {
  const durationSeconds = 0.075;
  const frameCount = Math.ceil(audioContext.sampleRate * durationSeconds);
  const buffer = audioContext.createBuffer(1, frameCount, audioContext.sampleRate);
  const channel = buffer.getChannelData(0);
  for (let index = 0; index < frameCount; index += 1) {
    const time = index / audioContext.sampleRate;
    const envelope = Math.pow(1 - index / frameCount, 3.4);
    const lowPulse = Math.sin(time * Math.PI * 2 * 78);
    const noise = Math.random() * 2 - 1;
    channel[index] = (lowPulse * 0.58 + noise * 0.42) * envelope * 0.16;
  }
  return buffer;
};

const prepareShotAudio = () => {
  if (!AudioContextClass) return null;
  try {
    if (!shotAudioContext || shotAudioContext.state === "closed") {
      shotAudioContext = new AudioContextClass({ latencyHint: "interactive" });
      shotAudioBuffer = buildShotAudioBuffer(shotAudioContext);
    }
    return shotAudioContext;
  } catch {
    return null;
  }
};

const ensureAudioReady = () => {
  const audioContext = prepareShotAudio();
  if (!audioContext) return null;
  try {
    if (shotAudioContext.state === "suspended") {
      void shotAudioContext.resume().catch(() => { /* visual feedback remains available */ });
    }
    return shotAudioContext;
  } catch {
    return null;
  }
};
prepareShotAudio();

const playShotSound = () => {
  const audioContext = ensureAudioReady();
  if (!audioContext || !shotAudioBuffer || audioContext.state !== "running") return;
  const source = audioContext.createBufferSource();
  source.buffer = shotAudioBuffer;
  source.connect(audioContext.destination);
  source.start();
};

let flashAnimation = null;
let resultAnimation = null;
const showShotFeedback = (label) => {
  shotResult.textContent = label;
  flashAnimation?.cancel();
  resultAnimation?.cancel();
  flashAnimation = shotFlash.animate([
    { opacity: 1, boxShadow: "0 0 2vh 1vh #fff4b7, 0 0 8vh 3vh rgba(255,139,55,.85)" },
    { opacity: 0, boxShadow: "0 0 9vh 3vh transparent" }
  ], { duration: 120, easing: "ease-out" });
  resultAnimation = shotResult.animate([
    { opacity: 0, transform: "translate(-50%, 1.2vh) scale(.86)" },
    { opacity: 1, transform: "translate(-50%, 0) scale(1)", offset: 0.14 },
    { opacity: 1, transform: "translate(-50%, 0) scale(1)", offset: 0.65 },
    { opacity: 0, transform: "translate(-50%, -2vh) scale(.96)" }
  ], { duration: 720, easing: "ease-out" });
};

const tracerPositions = new Float32Array(6);
const tracerGeometry = new THREE.BufferGeometry();
tracerGeometry.setAttribute("position", new THREE.BufferAttribute(tracerPositions, 3));
const tracerMaterial = new THREE.LineBasicMaterial({ color: 0xffe49b, transparent: true, opacity: 0.95 });
const tracer = new THREE.Line(tracerGeometry, tracerMaterial);
tracer.name = "shot-tracer";
tracer.renderOrder = 20;
tracer.visible = false;
scene.add(tracer);
let tracerHideTimer = null;

const addTracer = (start, end, color) => {
  tracerPositions.set([start.x, start.y, start.z, end.x, end.y, end.z]);
  tracerGeometry.attributes.position.needsUpdate = true;
  tracerMaterial.color.setHex(color);
  tracer.visible = true;
  if (tracerHideTimer !== null) window.clearTimeout(tracerHideTimer);
  tracerHideTimer = window.setTimeout(() => { tracer.visible = false; }, 120);
};

const fireAtReticle = () => {
  const snapshot = peekState.snapshot();
  if (!(aimInteraction.canFire(snapshot.progress) || (debugAim && snapshot.progress >= 1))) return;
  const now = performance.now();
  if (now - lastShotTime < RECOIL_PROFILE.shotIntervalMs) return;
  lastShotTime = now;

  burstShotCount += 1;
  const recoilImpulse = recoilImpulseForShot(burstShotCount, Math.random());
  recoilReturnMs = recoilImpulse.returnMs;
  recoilPitch = Math.min(RECOIL_PROFILE.maxVerticalRadians, recoilPitch + recoilImpulse.vertical);
  recoilYaw = THREE.MathUtils.clamp(
    recoilYaw + recoilImpulse.horizontal,
    -RECOIL_PROFILE.maxHorizontalRadians,
    RECOIL_PROFILE.maxHorizontalRadians
  );

  raycaster.setFromCamera(screenCenter, camera);
  const intersections = raycaster.intersectObjects(activeShotTargets, false);
  const firstHit = intersections[0];
  const origin = raycaster.ray.origin.clone().add(raycaster.ray.direction.clone().multiplyScalar(0.35));
  const end = firstHit ? firstHit.point.clone() : raycaster.ray.at(24, new THREE.Vector3());
  const isHeadshot = Boolean(firstHit?.object.userData.isDummyHead);
  const isBodyshot = Boolean(firstHit?.object.userData.isEnemyPart && !isHeadshot);

  addTracer(origin, end, isHeadshot ? 0xffe49b : isBodyshot ? 0xffc071 : 0xff9c55);
  showShotFeedback(isHeadshot ? "命中假人头部" : isBodyshot ? "命中假人身体" : "射线落点");
  playShotSound();

  if (isHeadshot) {
    const hitMaterial = firstHit.object.material;
    hitMaterial.emissive.setHex(0xff571f);
    hitMaterial.emissiveIntensity = 2.6;
    window.setTimeout(() => {
      hitMaterial.emissive.setHex(0x000000);
      hitMaterial.emissiveIntensity = 0;
    }, 110);
  }
};

fireButton.addEventListener("pointerdown", (event) => {
  event.preventDefault();
  if (!(aimInteraction.committed || debugAim)) return;
  activeFirePointerId = event.pointerId;
  lastFirePointerX = event.clientX;
  lastFirePointerY = event.clientY;
  isFiring = true;
  fireButton.classList.add("is-firing");
  fireButton.setAttribute("aria-pressed", "true");
  try { fireButton.setPointerCapture(event.pointerId); } catch { /* document release remains authoritative */ }
});
const releaseFirePointer = (event) => {
  if (activeFirePointerId === null || event.pointerId !== activeFirePointerId) return;
  stopFiring();
};
document.addEventListener("pointerup", releaseFirePointer, { passive: false });
document.addEventListener("pointercancel", releaseFirePointer, { passive: false });

aimSurface.addEventListener("pointerdown", (event) => {
  if (!aimInteraction.committed) return;
  event.preventDefault();
  activeAimPointerId = event.pointerId;
  lastAimSurfaceX = event.clientX;
  lastAimSurfaceY = event.clientY;
  try { aimSurface.setPointerCapture(event.pointerId); } catch { /* document release remains authoritative */ }
});

const releaseAimPointer = (event) => {
  if (activeAimPointerId === null || event.pointerId !== activeAimPointerId) return;
  activeAimPointerId = null;
};
document.addEventListener("pointerup", releaseAimPointer, { passive: false });
document.addEventListener("pointercancel", releaseAimPointer, { passive: false });

document.addEventListener("pointermove", (event) => {
  if (activeFirePointerId !== null && event.pointerId === activeFirePointerId) {
    event.preventDefault();
    applyAimDelta(event.clientX - lastFirePointerX, event.clientY - lastFirePointerY);
    lastFirePointerX = event.clientX;
    lastFirePointerY = event.clientY;
    return;
  }
  if (activeAimPointerId !== null && event.pointerId === activeAimPointerId) {
    event.preventDefault();
    applyAimDelta(event.clientX - lastAimSurfaceX, event.clientY - lastAimSurfaceY);
    lastAimSurfaceX = event.clientX;
    lastAimSurfaceY = event.clientY;
  }
}, { passive: false });
window.addEventListener("keydown", (event) => {
  if (event.code !== "KeyF" || event.repeat) return;
  event.preventDefault();
  if (!(aimInteraction.committed || debugAim)) return;
  isFiring = true;
  fireButton.classList.add("is-firing");
  fireButton.setAttribute("aria-pressed", "true");
});
window.addEventListener("keyup", (event) => {
  if (event.code !== "KeyF") return;
  event.preventDefault();
  stopFiring();
});

const renderFallback = (progress, roll) => {
  if (!fallbackContext) return;
  const context = fallbackContext;
  const width = canvas.width;
  const height = canvas.height;
  const reveal = width * (0.07 + Math.pow(progress, 1.45) * 0.76);
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
  corridor.addColorStop(0, "#2a3b3e");
  corridor.addColorStop(0.52, "#1a2a2d");
  corridor.addColorStop(1, "#35474a");
  context.fillStyle = corridor;
  context.fillRect(0, 0, width, height);

  context.fillStyle = "#344548";
  context.beginPath();
  context.moveTo(0, 0);
  context.lineTo(width * 0.49, horizonY);
  context.lineTo(width * 0.49, floorY);
  context.lineTo(0, height);
  context.closePath();
  context.fill();
  context.fillStyle = "#233235";
  context.beginPath();
  context.moveTo(width, 0);
  context.lineTo(width * 0.55, horizonY);
  context.lineTo(width * 0.55, floorY);
  context.lineTo(width, height);
  context.closePath();
  context.fill();
  context.fillStyle = "#172326";
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
  context.fillStyle = "#596568";
  context.fillRect(width * 0.48, height * 0.48, width * 0.1, height * 0.08);
  context.fillStyle = "#c18e70";
  context.beginPath();
  context.arc(width * 0.53, height * 0.47, width * 0.018, 0, Math.PI * 2);
  context.fill();
  context.restore();

  const wallGradient = context.createLinearGradient(reveal, 0, width, height);
  wallGradient.addColorStop(0, "#293133");
  wallGradient.addColorStop(0.08, "#4a5355");
  wallGradient.addColorStop(1, "#343c3e");
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

if (debugAim) aimInteraction.toggleCommitted();
if (debugPeek && !debugAim) aimInteraction.setFakeHeld(true);
syncPeekTarget();
syncControls();

let previousTime = performance.now();
let peekCycleActive = false;
const animate = (time) => {
  const deltaMs = Math.min(250, time - previousTime);
  previousTime = time;
  const snapshot = peekState.step(deltaMs);
  const recoilDecay = Math.exp(-deltaMs / recoilReturnMs);
  recoilPitch *= recoilDecay;
  recoilYaw *= recoilDecay;
  const breathing = snapshot.progress > 0.98 ? Math.sin(time * 0.0025) * 0.006 : 0;
  camera.position.set(snapshot.pose.x, snapshot.pose.y + breathing, snapshot.pose.z);
  camera.rotation.set(aimPitch + recoilPitch, snapshot.pose.yaw + aimYaw + recoilYaw, snapshot.pose.roll);

  document.documentElement.style.setProperty("--peek-progress", snapshot.progress.toFixed(4));
  const mode = debugAim ? "committed" : debugPeek ? "fake" : aimInteraction.mode();
  if (!peekCycleActive && snapshot.progress > 0.001) {
    enemyIntel.beginPeek();
    peekCycleActive = true;
  }
  let enemyVisibility = 0;
  if (mode === "fake" && snapshot.progress > 0) {
    enemyVisibility = calculateEnemyVisibility();
    enemyIntel.observeFakePeek(enemyVisibility);
  }
  visibilityLabel.textContent = mode === "fake"
    ? `可见 ${Math.round(enemyVisibility * 100)}% / 阈值 ${Math.round(ENEMY_VISIBILITY_THRESHOLD * 100)}%`
    : "";

  if (peekCycleActive && snapshot.progress <= 0) {
    const nextPositionIndex = enemyIntel.resolveHiddenReposition();
    applyEnemyPosition(nextPositionIndex);
    enemyIntel.finishPeek();
    peekCycleActive = false;
  }
  syncIntelGhost(snapshot.progress <= 0);

  if (mode === "committed") {
    if (snapshot.progress < 1) {
      stateLabel.textContent = isFiring ? "真架枪 · 已预备开火" : "真架枪 · 自动探出中";
    } else if (!isFiring || burstShotCount === 0) {
      stateLabel.textContent = "真架枪 · 按住射击 / 拖动压枪";
    } else {
      const recoilPhase = recoilPhaseForShot(burstShotCount);
      stateLabel.textContent = recoilPhase === "kick"
        ? "首发起跳 · 立即向下压"
        : recoilPhase === "climb"
          ? `连射爬升 · 第 ${burstShotCount} 发`
          : "角色自动稳枪 · 减少下压";
    }
  } else if (mode === "fake") {
    stateLabel.textContent = snapshot.progress >= 1 ? "假动作 · 完全探出" : "假动作 · 探头中";
  } else {
    stateLabel.textContent = phaseText[snapshot.phase];
  }
  progressFill.style.transform = `scaleX(${snapshot.progress.toFixed(4)})`;
  reticle.style.opacity = String(Math.max(0, (snapshot.progress - 0.55) / 0.45));
  document.documentElement.dataset.recoilPhase = burstShotCount === 0 ? "idle" : recoilPhaseForShot(burstShotCount);
  document.documentElement.dataset.burstShots = String(burstShotCount);
  document.documentElement.dataset.enemyVisibility = enemyVisibility.toFixed(3);
  document.documentElement.dataset.enemyPosition = String(enemyIntel.currentPositionIndex);
  document.documentElement.dataset.rememberedEnemyPosition = enemyIntel.rememberedPositionIndex === null ? "none" : String(enemyIntel.rememberedPositionIndex);
  document.documentElement.dataset.pendingAutoAim = String(enemyIntel.pendingAutoAim);
  document.documentElement.dataset.enemyMoved = String(enemyIntel.lastMoveChanged);
  document.documentElement.dataset.intelGhostVisible = String(enemyIntel.ghostVisible && snapshot.progress <= 0);
  document.documentElement.dataset.aimYaw = aimYaw.toFixed(5);
  document.documentElement.dataset.aimPitch = aimPitch.toFixed(5);
  syncControls(snapshot);

  if (renderer) renderer.render(scene, camera);
  else renderFallback(snapshot.progress, snapshot.pose.roll);

  if (isFiring && snapshot.progress >= 1) {
    if (burstShotCount === 0 && !firstShotDeferred) firstShotDeferred = true;
    else fireAtReticle();
  }
  requestAnimationFrame(animate);
};
requestAnimationFrame(animate);
