import test from "node:test";
import assert from "node:assert/strict";
import { AimInteraction } from "../src/aim-interaction.js";

test("true aim toggles a latched peek and unlocks fire only when fully exposed", () => {
  const interaction = new AimInteraction();
  interaction.toggleCommitted();
  assert.equal(interaction.mode(), "committed");
  assert.equal(interaction.peekRequested(), true);
  assert.equal(interaction.canFire(0.99), false);
  assert.equal(interaction.canFire(1), true);
  interaction.toggleCommitted();
  assert.equal(interaction.mode(), "idle");
  assert.equal(interaction.peekRequested(), false);
});

test("fake action is hold-to-peek and never unlocks fire", () => {
  const interaction = new AimInteraction();
  assert.equal(interaction.setFakeHeld(true), true);
  assert.equal(interaction.mode(), "fake");
  assert.equal(interaction.canFire(1), false);
  interaction.setFakeHeld(false);
  assert.equal(interaction.mode(), "idle");
});

test("fake action cannot interrupt a committed true aim", () => {
  const interaction = new AimInteraction();
  interaction.toggleCommitted();
  assert.equal(interaction.setFakeHeld(true), false);
  assert.equal(interaction.mode(), "committed");
  assert.equal(interaction.fakeHeld, false);
});
