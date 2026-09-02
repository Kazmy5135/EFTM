export const PEEK_CONFIG = Object.freeze({
  enterMs: 300,
  returnMs: 240,
  hiddenX: 0.4,
  exposedX: 0.02,
  hiddenY: 0,
  exposedY: -0.055,
  hiddenZ: 2.1,
  exposedZ: 1.1,
  exposedRoll: 0.125,
  exposedYaw: -0.018
});

const clamp01 = (value) => Math.max(0, Math.min(1, value));
const smoothstep = (value) => {
  const t = clamp01(value);
  return t * t * (3 - 2 * t);
};
const lerp = (start, end, progress) => start + (end - start) * progress;

export class PeekState {
  constructor(config = {}) {
    this.config = Object.assign({}, PEEK_CONFIG, config);
    this.progress = 0;
    this.held = false;
  }

  setHeld(held) {
    this.held = Boolean(held);
  }

  step(deltaMs) {
    const dt = Math.max(0, Number(deltaMs) || 0);
    const duration = this.held ? this.config.enterMs : this.config.returnMs;
    const direction = this.held ? 1 : -1;
    this.progress = clamp01(this.progress + direction * dt / duration);
    return this.snapshot();
  }

  phase() {
    if (this.held && this.progress >= 1) return "holding";
    if (this.held) return "peeking";
    if (this.progress <= 0) return "hidden";
    return "returning";
  }

  cameraPose() {
    const eased = smoothstep(this.progress);
    return {
      x: lerp(this.config.hiddenX, this.config.exposedX, eased),
      y: lerp(this.config.hiddenY, this.config.exposedY, eased),
      z: lerp(this.config.hiddenZ, this.config.exposedZ, eased),
      roll: lerp(0, this.config.exposedRoll, eased),
      yaw: lerp(0, this.config.exposedYaw, eased)
    };
  }

  snapshot() {
    return {
      held: this.held,
      phase: this.phase(),
      progress: this.progress,
      pose: this.cameraPose()
    };
  }
}
