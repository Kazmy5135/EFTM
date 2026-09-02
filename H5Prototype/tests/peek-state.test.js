import test from "node:test";
import assert from "node:assert/strict";
import { PeekState, PEEK_CONFIG } from "../src/peek-state.js";

test("camera begins behind cover with a level resting pose", () => {
  const state = new PeekState();
  const snapshot = state.snapshot();
  assert.equal(snapshot.phase, "hidden");
  assert.equal(snapshot.progress, 0);
  assert.equal(snapshot.pose.x, PEEK_CONFIG.hiddenX);
  assert.equal(snapshot.pose.roll, 0);
  assert.ok(PEEK_CONFIG.hiddenZ > PEEK_CONFIG.exposedZ, "hidden camera should sit farther back from cover");
});

test("holding peek reaches the exposed camera pose in 300ms", () => {
  const state = new PeekState();
  state.setHeld(true);
  state.step(150);
  assert.equal(state.phase(), "peeking");
  assert.equal(state.progress, 0.5);
  state.step(150);
  const snapshot = state.snapshot();
  assert.equal(snapshot.phase, "holding");
  assert.equal(snapshot.progress, 1);
  assert.ok(Math.abs(snapshot.pose.x - PEEK_CONFIG.exposedX) < 1e-9);
  assert.ok(Math.abs(snapshot.pose.roll - PEEK_CONFIG.exposedRoll) < 1e-9);
  assert.ok(snapshot.pose.roll > 0, "left peek must lean in the positive roll direction");
});

test("releasing peek returns fully behind cover", () => {
  const state = new PeekState();
  state.setHeld(true);
  state.step(300);
  state.setHeld(false);
  state.step(120);
  assert.equal(state.phase(), "returning");
  assert.equal(state.progress, 0.5);
  state.step(120);
  assert.equal(state.phase(), "hidden");
  assert.equal(state.progress, 0);
});

test("regrabbing during return reverses smoothly without teleporting", () => {
  const state = new PeekState();
  state.setHeld(true);
  state.step(210);
  state.setHeld(false);
  state.step(48);
  const interruptedProgress = state.progress;
  state.setHeld(true);
  state.step(30);
  assert.ok(state.progress > interruptedProgress);
  assert.ok(state.progress < 1);
  assert.equal(state.phase(), "peeking");
});

test("large frame times remain clamped to valid endpoints", () => {
  const state = new PeekState();
  state.setHeld(true);
  state.step(5000);
  assert.equal(state.progress, 1);
  state.setHeld(false);
  state.step(5000);
  assert.equal(state.progress, 0);
});
