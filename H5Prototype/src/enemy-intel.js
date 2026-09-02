export const ENEMY_VISIBILITY_THRESHOLD = 0.1;
export const ENEMY_REPOSITION_CHANCE = 0.25;

export const ENEMY_POSITIONS = Object.freeze([
  Object.freeze({ id: "left-head", label: "左侧高掩体", actor: Object.freeze([-0.9, 0.02, -17.7]), cover: Object.freeze([-0.9, -0.82, -17.42]), coverSize: Object.freeze([1.05, 1.38, 0.34]) }),
  Object.freeze({ id: "left-shoulder", label: "左侧墙角", actor: Object.freeze([-0.42, 0.02, -18.55]), cover: Object.freeze([-0.58, -0.98, -18.27]), coverSize: Object.freeze([0.86, 1.04, 0.34]) }),
  Object.freeze({ id: "center-upper", label: "中央低掩体", actor: Object.freeze([0.35, 0.02, -17.96]), cover: Object.freeze([0.35, -1.15, -17.68]), coverSize: Object.freeze([1.12, 0.7, 0.34]) }),
  Object.freeze({ id: "right-half", label: "右侧设备箱", actor: Object.freeze([0.76, 0.02, -17.15]), cover: Object.freeze([0.93, -1.28, -16.87]), coverSize: Object.freeze([0.72, 0.46, 0.34]) }),
  Object.freeze({ id: "right-full", label: "右侧通道口", actor: Object.freeze([1.02, 0.02, -13.35]), cover: null, coverSize: null })
]);

const clamp01 = (value) => Math.max(0, Math.min(1, Number(value) || 0));

export class EnemyIntel {
  constructor({ initialPositionIndex = 2, visibilityThreshold = ENEMY_VISIBILITY_THRESHOLD } = {}) {
    this.visibilityThreshold = visibilityThreshold;
    this.currentPositionIndex = initialPositionIndex;
    this.rememberedPositionIndex = null;
    this.pendingAutoAim = false;
    this.lastVisibility = 0;
    this.lastMoveChanged = false;
    this.observedThisPeek = false;
    this.ghostVisible = false;
  }

  beginPeek() {
    this.observedThisPeek = false;
    this.ghostVisible = false;
  }

  hideGhost() {
    this.ghostVisible = false;
  }

  observeFakePeek(visibilityRatio) {
    this.lastVisibility = clamp01(visibilityRatio);
    if (this.lastVisibility < this.visibilityThreshold) return false;
    this.rememberedPositionIndex = this.currentPositionIndex;
    this.pendingAutoAim = true;
    this.observedThisPeek = true;
    return true;
  }

  finishPeek() {
    this.ghostVisible = this.observedThisPeek && this.rememberedPositionIndex !== null;
    this.observedThisPeek = false;
    return this.ghostVisible;
  }

  consumePendingAutoAim() {
    if (!this.pendingAutoAim || this.rememberedPositionIndex === null) return null;
    this.pendingAutoAim = false;
    return this.rememberedPositionIndex;
  }

  resolveHiddenReposition(moveRoll = Math.random(), positionRoll = Math.random()) {
    this.lastMoveChanged = clamp01(moveRoll) < ENEMY_REPOSITION_CHANCE;
    if (!this.lastMoveChanged) return this.currentPositionIndex;

    const alternatives = ENEMY_POSITIONS
      .map((_, index) => index)
      .filter((index) => index !== this.currentPositionIndex);
    const selection = Math.min(alternatives.length - 1, Math.floor(clamp01(positionRoll) * alternatives.length));
    this.currentPositionIndex = alternatives[selection];
    return this.currentPositionIndex;
  }

  intelLabel() {
    return this.rememberedPositionIndex === null ? "没有敌人信息" : "已预瞄";
  }

  snapshot() {
    return {
      currentPositionIndex: this.currentPositionIndex,
      rememberedPositionIndex: this.rememberedPositionIndex,
      pendingAutoAim: this.pendingAutoAim,
      lastVisibility: this.lastVisibility,
      lastMoveChanged: this.lastMoveChanged,
      observedThisPeek: this.observedThisPeek,
      ghostVisible: this.ghostVisible,
      intelLabel: this.intelLabel()
    };
  }
}
