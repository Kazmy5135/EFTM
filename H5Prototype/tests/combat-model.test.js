"use strict";

const test = require("node:test");
const assert = require("node:assert/strict");
const { CombatModel, STATES } = require("../src/combat-model.js");

function advance(model, milliseconds, stepMs = 20) {
  const events = [];
  for (let elapsed = 0; elapsed < milliseconds; elapsed += stepMs) {
    events.push(...model.step(Math.min(stepMs, milliseconds - elapsed)));
  }
  return events;
}

test("hidden actors cannot shoot or be hit", () => {
  const model = new CombatModel({ random: () => 0 });
  const events = advance(model, 3000);
  const snapshot = model.getSnapshot();
  assert.equal(events.length, 0);
  assert.equal(snapshot.player.hp, 100);
  assert.equal(snapshot.enemy.hp, 100);
  assert.equal(snapshot.player.state, STATES.HIDDEN);
});

test("the actor who establishes the line first fires first", () => {
  const model = new CombatModel({ random: () => 0 });
  model.setPeekIntent("enemy", true);
  advance(model, 1300);
  assert.equal(model.getSnapshot().enemy.aim, 1);
  model.setPeekIntent("player", true);
  const events = advance(model, 600);
  const firstShot = events.find((event) => event.type === "shot");
  assert.ok(firstShot, "expected at least one shot");
  assert.equal(firstShot.shooterId, "enemy");
});

test("a pre-aimed actor fires while the opponent is still exposing", () => {
  const model = new CombatModel({ random: () => 0 });
  model.setPeekIntent("enemy", true);
  advance(model, 1300);
  model.setPeekIntent("player", true);

  const events = advance(model, 20);
  const shot = events.find((event) => event.type === "shot");
  assert.ok(shot);
  assert.equal(shot.shooterId, "enemy");
  assert.ok(shot.targetExposure > 0 && shot.targetExposure < 0.5);
  assert.equal(shot.hitChance, 0);
  assert.equal(shot.hit, false);
  assert.equal(model.getSnapshot().player.shots, 0, "the exposing player must not counterfire before fully exposed");

  const laterEvents = advance(model, 220);
  const vulnerableShot = laterEvents.find((event) => event.type === "shot" && event.hitChance > 0);
  assert.ok(vulnerableShot);
  assert.ok(vulnerableShot.targetExposure >= 0.5 && vulnerableShot.targetExposure < 1);
  assert.ok(vulnerableShot.hitChance >= 0.3 && vulnerableShot.hitChance < 1);
});

test("the first half of peek is safe, then hit chance scales from 30 to 100 percent", () => {
  const model = new CombatModel();
  assert.equal(model.hitChanceForExposure(0), 0);
  assert.equal(model.hitChanceForExposure(0.49), 0);
  assert.equal(model.hitChanceForExposure(0.5), 0.3);
  assert.equal(model.hitChanceForExposure(1), 1);
  assert.ok(Math.abs(model.hitChanceForExposure(0.75) - 0.65) < 0.000001);
});

test("default magazines contain fifteen rounds", () => {
  const model = new CombatModel();
  const snapshot = model.getSnapshot();
  assert.equal(model.config.magazineSize, 15);
  assert.equal(snapshot.player.ammo, 15);
  assert.equal(snapshot.enemy.ammo, 15);
});

test("player and enemy use identical combat parameters", () => {
  const model = new CombatModel();
  model.setPeekIntent("player", true);
  model.setPeekIntent("enemy", true);
  advance(model, 150);
  const snapshot = model.getSnapshot();
  assert.equal(model.config.exposeMs, 300);
  assert.equal(snapshot.player.hp, snapshot.enemy.hp);
  assert.equal(snapshot.player.ammo, snapshot.enemy.ammo);
  assert.equal(snapshot.player.exposure, snapshot.enemy.exposure);
  assert.ok(Math.abs(snapshot.player.exposure - 0.5) < 0.000001);
  assert.equal(snapshot.player.state, snapshot.enemy.state);
  assert.equal(model.hitChanceForExposure(0.5), 0.3);
});

test("releasing begins a vulnerable retreat before cover protection", () => {
  const model = new CombatModel({ random: () => 0 });
  model.setPeekIntent("player", true);
  advance(model, 280);
  assert.ok(model.getSnapshot().player.exposure > 0.63);
  model.setPeekIntent("player", false);
  advance(model, 20);
  const retreating = model.getSnapshot().player;
  assert.equal(retreating.state, STATES.RETREATING);
  assert.ok(retreating.exposure > 0);
  advance(model, 400);
  assert.equal(model.getSnapshot().player.state, STATES.HIDDEN);
});

test("five press and release loops return to a clean hidden state", () => {
  const model = new CombatModel({ random: () => 1 });
  for (let loop = 0; loop < 5; loop += 1) {
    model.setPeekIntent("player", true);
    advance(model, 180);
    model.setPeekIntent("player", false);
    advance(model, 360);
  }
  const player = model.getSnapshot().player;
  assert.equal(player.state, STATES.HIDDEN);
  assert.equal(player.intentPeek, false);
  assert.equal(player.exposure, 0);
  assert.equal(player.aim, 0);
});

test("a fully hidden target is protected from a pre-aimed opponent", () => {
  const model = new CombatModel({ random: () => 0 });
  model.setPeekIntent("enemy", true);
  const events = advance(model, 4000);
  assert.equal(events.filter((event) => event.type === "shot").length, 0);
  assert.equal(model.getSnapshot().player.hp, 100);
});

test("an empty magazine holds the line until release, then reloads after a single trigger", () => {
  const model = new CombatModel({
    random: () => 0,
    config: { magazineSize: 3, shotCooldownMs: 1, baseDamage: 0, reloadMs: 2000 }
  });
  model.setPeekIntent("player", true);
  model.setPeekIntent("enemy", true);
  advance(model, 420);

  let player = model.getSnapshot().player;
  assert.equal(player.ammo, 0);
  assert.equal(player.needsReload, true);
  assert.equal(player.state, STATES.HOLDING);
  assert.equal(player.exposure, 1);
  assert.equal(player.intentPeek, true);

  advance(model, 600);
  player = model.getSnapshot().player;
  assert.equal(player.state, STATES.HOLDING, "empty actors stay exposed until their controller releases");
  assert.equal(player.shots, 3, "empty actors cannot keep attacking");

  model.setPeekIntent("player", false);
  advance(model, 20);
  assert.equal(model.getSnapshot().player.state, STATES.RETREATING);

  for (let index = 0; index < 30 && model.getSnapshot().player.state !== STATES.HIDDEN; index += 1) {
    model.step(20);
  }
  player = model.getSnapshot().player;
  assert.equal(player.state, STATES.HIDDEN);
  assert.equal(player.exposure, 0);
  assert.equal(player.reloadRemainingMs, 0);

  model.setPeekIntent("player", true);
  assert.equal(model.getSnapshot().player.intentPeek, false, "peek input is ignored while the magazine is empty");
  advance(model, 500);
  assert.equal(model.getSnapshot().player.state, STATES.HIDDEN, "reload never starts automatically");

  assert.equal(model.startReload("player"), true);
  assert.equal(model.getSnapshot().player.state, STATES.RELOADING);
  assert.equal(model.getSnapshot().player.reloadRemainingMs, 2000);
  advance(model, 1980);
  assert.equal(model.getSnapshot().player.state, STATES.RELOADING);
  advance(model, 40);
  player = model.getSnapshot().player;
  assert.equal(player.state, STATES.HIDDEN);
  assert.equal(player.ammo, 3);
  assert.equal(player.needsReload, false);
});

test("a partially used magazine can be actively reloaded only while fully in cover", () => {
  const model = new CombatModel({ config: { reloadMs: 2000 } });
  model.actors.player.ammo = 7;

  model.setPeekIntent("player", true);
  advance(model, 100);
  assert.equal(model.startReload("player"), false, "reload is unavailable outside cover");

  model.setPeekIntent("player", false);
  advance(model, 400);
  assert.equal(model.getSnapshot().player.state, STATES.HIDDEN);
  assert.equal(model.startReload("player"), true, "a non-empty partial magazine can reload in cover");
  advance(model, 2020);
  const player = model.getSnapshot().player;
  assert.equal(player.state, STATES.HIDDEN);
  assert.equal(player.ammo, 15);
});
