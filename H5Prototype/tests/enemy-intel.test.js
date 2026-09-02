import test from "node:test";
import assert from "node:assert/strict";
import { ENEMY_POSITIONS, ENEMY_VISIBILITY_THRESHOLD, EnemyIntel } from "../src/enemy-intel.js";

test("fake peek records enemy position at the ten percent visibility threshold", () => {
  const intel = new EnemyIntel({ initialPositionIndex: 2 });
  assert.equal(ENEMY_VISIBILITY_THRESHOLD, 0.1);
  assert.equal(intel.observeFakePeek(0.099), false);
  assert.equal(intel.intelLabel(), "没有敌人信息");
  assert.equal(intel.observeFakePeek(0.1), true);
  assert.equal(intel.intelLabel(), "已预瞄");
  assert.equal(intel.snapshot().rememberedPositionIndex, 2);
});

test("pre-aim consumes only fresh fake-peek intel and keeps remembered location", () => {
  const intel = new EnemyIntel({ initialPositionIndex: 1 });
  intel.observeFakePeek(0.4);
  assert.equal(intel.consumePendingAutoAim(), 1);
  assert.equal(intel.consumePendingAutoAim(), null);
  assert.equal(intel.snapshot().rememberedPositionIndex, 1);
  assert.equal(intel.intelLabel(), "已预瞄");
});

test("hidden enemy has a fifty percent chance to move to one of four other positions", () => {
  const intel = new EnemyIntel({ initialPositionIndex: 2 });
  assert.equal(ENEMY_POSITIONS.length, 5);
  assert.equal(intel.resolveHiddenReposition(0.5, 0), 2, "50% or higher keeps the position");
  const moved = intel.resolveHiddenReposition(0.49, 0.99);
  assert.notEqual(moved, 2);
  assert.ok(moved >= 0 && moved < 5);
});

test("enemy relocation does not overwrite the old remembered pre-aim point", () => {
  const intel = new EnemyIntel({ initialPositionIndex: 0 });
  intel.observeFakePeek(0.2);
  intel.resolveHiddenReposition(0, 0.75);
  assert.notEqual(intel.snapshot().currentPositionIndex, 0);
  assert.equal(intel.snapshot().rememberedPositionIndex, 0);
  assert.equal(intel.consumePendingAutoAim(), 0);
});
