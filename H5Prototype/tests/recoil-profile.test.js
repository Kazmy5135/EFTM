import test from "node:test";
import assert from "node:assert/strict";
import { RECOIL_PROFILE, recoilImpulseForShot, recoilPhaseForShot } from "../src/recoil-profile.js";

test("recoil climbs through shots two to four before stabilization", () => {
  const impulses = [1, 2, 3, 4, 5].map((shot) => recoilImpulseForShot(shot, 0.5));
  assert.deepEqual(impulses.map(({ phase }) => phase), ["kick", "climb", "climb", "climb", "stable"]);
  assert.ok(impulses[1].vertical > impulses[0].vertical);
  assert.ok(impulses[2].vertical > impulses[1].vertical);
  assert.ok(impulses[3].vertical > impulses[2].vertical);
  assert.ok(impulses[4].vertical < impulses[0].vertical);
});

test("stabilized fire returns faster but keeps horizontal uncertainty", () => {
  const left = recoilImpulseForShot(8, 0);
  const right = recoilImpulseForShot(8, 1);
  assert.equal(left.phase, "stable");
  assert.equal(left.returnMs, RECOIL_PROFILE.stableReturnMs);
  assert.ok(left.returnMs < RECOIL_PROFILE.climbReturnMs);
  assert.ok(left.horizontal < 0);
  assert.ok(right.horizontal > 0);
  assert.equal(Math.abs(left.horizontal), Math.abs(right.horizontal));
});

test("shot phase boundaries are deterministic", () => {
  assert.equal(recoilPhaseForShot(1), "kick");
  assert.equal(recoilPhaseForShot(4), "climb");
  assert.equal(recoilPhaseForShot(5), "stable");
  assert.equal(recoilPhaseForShot(100), "stable");
});
