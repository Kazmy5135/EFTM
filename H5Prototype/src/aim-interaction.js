export class AimInteraction {
  constructor() {
    this.committed = false;
    this.fakeHeld = false;
  }

  toggleCommitted() {
    this.committed = !this.committed;
    if (this.committed) this.fakeHeld = false;
    return this.snapshot();
  }

  setFakeHeld(held) {
    if (this.committed && held) return false;
    this.fakeHeld = Boolean(held);
    return true;
  }

  releaseAll() {
    this.fakeHeld = false;
  }

  peekRequested() {
    return this.committed || this.fakeHeld;
  }

  canFire(progress) {
    return this.committed && progress >= 1;
  }

  mode() {
    if (this.committed) return "committed";
    if (this.fakeHeld) return "fake";
    return "idle";
  }

  snapshot() {
    return {
      committed: this.committed,
      fakeHeld: this.fakeHeld,
      peekRequested: this.peekRequested(),
      mode: this.mode()
    };
  }
}
