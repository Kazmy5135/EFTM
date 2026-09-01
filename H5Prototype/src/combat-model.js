(function attachCombatModel(root, factory) {
  const exported = factory();
  if (typeof module !== "undefined" && module.exports) module.exports = exported;
  if (root) root.EFTMCombat = exported;
})(typeof globalThis !== "undefined" ? globalThis : this, function combatModelFactory() {
  "use strict";

  const STATES = Object.freeze({
    HIDDEN: "hidden",
    EXPOSING: "exposing",
    HOLDING: "holding",
    RETREATING: "retreating",
    SWITCHING: "switching",
    RELOADING: "reloading",
    DEAD: "dead"
  });

  const DEFAULTS = Object.freeze({
    exposeMs: 300,
    retreatMs: 280,
    steadyMs: 680,
    aimDecayMs: 420,
    shotCooldownMs: 100,
    magazineSize: 15,
    reloadMs: 2000,
    switchCoverMs: 600,
    switchCoverHitChance: 0.2,
    baseDamage: 5,
    minimumExposureHitChance: 0.3,
    maximumExposureHitChance: 1
  });

  const clamp = (value, minimum, maximum) => Math.max(minimum, Math.min(maximum, value));

  function createActor(id) {
    return {
      id,
      state: STATES.HIDDEN,
      intentPeek: false,
      exposure: 0,
      aim: 0,
      cooldownMs: 0,
      fireHeld: false,
      automaticFire: id === "enemy",
      hp: 100,
      ammo: DEFAULTS.magazineSize,
      needsReload: false,
      reloadRemainingMs: 0,
      reloadReturnState: null,
      coverSide: id === "player" ? "left" : "right",
      switchFromSide: null,
      switchTargetSide: null,
      switchRemainingMs: 0,
      shots: 0,
      hits: 0
    };
  }

  class CombatModel {
    constructor(options) {
      const settings = options || {};
      this.config = Object.assign({}, DEFAULTS, settings.config || {});
      this.random = typeof settings.random === "function" ? settings.random : Math.random;
      this.actors = { player: createActor("player"), enemy: createActor("enemy") };
      this.actors.player.ammo = this.config.magazineSize;
      this.actors.enemy.ammo = this.config.magazineSize;
      this.timeMs = 0;
      this.events = [];
    }

    reset() {
      this.actors.player = createActor("player");
      this.actors.enemy = createActor("enemy");
      this.actors.player.ammo = this.config.magazineSize;
      this.actors.enemy.ammo = this.config.magazineSize;
      this.timeMs = 0;
      this.events.length = 0;
    }

    setPeekIntent(actorId, shouldPeek) {
      const actor = this.actors[actorId];
      if (!actor || actor.state === STATES.DEAD) return;
      if (shouldPeek && (actor.needsReload || actor.state === STATES.RELOADING)) return;
      actor.intentPeek = Boolean(shouldPeek);
    }

    startAim(actorId) {
      const actor = this.actors[actorId];
      if (!actor || actor.state !== STATES.HIDDEN || actor.needsReload) return false;
      actor.intentPeek = true;
      return true;
    }

    startRetreat(actorId) {
      const actor = this.actors[actorId];
      if (!actor || actor.state !== STATES.HOLDING) return false;
      actor.intentPeek = false;
      actor.fireHeld = false;
      return true;
    }

    setFireHeld(actorId, shouldFire) {
      const actor = this.actors[actorId];
      if (!actor) return false;
      if (!shouldFire) {
        actor.fireHeld = false;
        return true;
      }
      if (!this.canFireWeapon(actor)) return false;
      actor.fireHeld = true;
      return true;
    }

    startCoverSwitch(actorId) {
      const actor = this.actors[actorId];
      if (!actor || actor.state !== STATES.HIDDEN) return false;
      actor.intentPeek = false;
      actor.fireHeld = false;
      actor.state = STATES.SWITCHING;
      actor.exposure = 1;
      actor.aim = 0;
      actor.switchFromSide = actor.coverSide;
      actor.switchTargetSide = actor.coverSide === "left" ? "right" : "left";
      actor.switchRemainingMs = this.config.switchCoverMs;
      this.events.push({
        type: "coverSwitchStarted",
        timeMs: this.timeMs,
        actorId: actor.id,
        fromSide: actor.switchFromSide,
        targetSide: actor.switchTargetSide,
        durationMs: this.config.switchCoverMs,
        hitChance: this.config.switchCoverHitChance
      });
      return true;
    }

    startReload(actorId) {
      const actor = this.actors[actorId];
      const canReloadHere = actor && (actor.state === STATES.HIDDEN || actor.state === STATES.HOLDING);
      if (!canReloadHere || actor.ammo >= this.config.magazineSize) return false;
      this.beginReload(actor);
      return true;
    }

    step(deltaMs) {
      const dt = clamp(Number(deltaMs) || 0, 0, 100);
      if (dt <= 0) return [];
      this.timeMs += dt;
      this.updateActor(this.actors.player, dt);
      this.updateActor(this.actors.enemy, dt);
      this.resolveShots();
      return this.drainEvents();
    }

    updateActor(actor, dt) {
      if (actor.state === STATES.DEAD) return;

      actor.cooldownMs = Math.max(0, actor.cooldownMs - dt);

      if (actor.state === STATES.RELOADING) {
        const returnsToAim = actor.reloadReturnState === STATES.HOLDING;
        actor.intentPeek = returnsToAim;
        actor.fireHeld = false;
        actor.exposure = returnsToAim ? 1 : 0;
        actor.aim = returnsToAim ? 1 : 0;
        actor.reloadRemainingMs = Math.max(0, actor.reloadRemainingMs - dt);
        if (actor.reloadRemainingMs <= 0) {
          actor.ammo = this.config.magazineSize;
          actor.needsReload = false;
          actor.state = returnsToAim ? STATES.HOLDING : STATES.HIDDEN;
          actor.intentPeek = returnsToAim;
          actor.exposure = returnsToAim ? 1 : 0;
          actor.aim = returnsToAim ? 1 : 0;
          actor.reloadReturnState = null;
          this.events.push({ type: "reloadCompleted", timeMs: this.timeMs, actorId: actor.id, ammo: actor.ammo });
        }
        return;
      }

      if (actor.state === STATES.SWITCHING) {
        actor.intentPeek = false;
        actor.fireHeld = false;
        actor.exposure = 1;
        actor.aim = 0;
        actor.switchRemainingMs = Math.max(0, actor.switchRemainingMs - dt);
        if (actor.switchRemainingMs <= 0) {
          actor.coverSide = actor.switchTargetSide;
          actor.switchFromSide = null;
          actor.switchTargetSide = null;
          actor.exposure = 0;
          actor.state = STATES.HIDDEN;
          this.events.push({ type: "coverSwitchCompleted", timeMs: this.timeMs, actorId: actor.id, coverSide: actor.coverSide });
        }
        return;
      }

      if (actor.intentPeek) {
        if (!actor.needsReload && (actor.state === STATES.HIDDEN || actor.state === STATES.RETREATING)) actor.state = STATES.EXPOSING;
      } else if (actor.state === STATES.EXPOSING || actor.state === STATES.HOLDING) {
        actor.state = STATES.RETREATING;
      }

      if (actor.state === STATES.EXPOSING) {
        actor.exposure = clamp(actor.exposure + dt / this.config.exposeMs, 0, 1);
        actor.aim = 0;
        if (actor.exposure >= 1) actor.state = STATES.HOLDING;
      } else if (actor.state === STATES.HOLDING) {
        actor.exposure = 1;
        actor.aim = clamp(actor.aim + dt / this.config.steadyMs, 0, 1);
      } else if (actor.state === STATES.RETREATING) {
        actor.exposure = clamp(actor.exposure - dt / this.config.retreatMs, 0, 1);
        actor.aim = clamp(actor.aim - dt / this.config.aimDecayMs, 0, 1);
        if (actor.exposure <= 0) {
          actor.aim = 0;
          actor.state = STATES.HIDDEN;
          if (actor.needsReload) {
            actor.intentPeek = false;
            actor.reloadRemainingMs = 0;
            this.events.push({ type: "reloadAvailable", timeMs: this.timeMs, actorId: actor.id });
          }
        }
      } else if (actor.state === STATES.HIDDEN) {
        actor.exposure = 0;
        actor.aim = 0;
      }
    }

    beginReload(actor) {
      actor.reloadReturnState = actor.state;
      actor.state = STATES.RELOADING;
      actor.fireHeld = false;
      actor.intentPeek = actor.reloadReturnState === STATES.HOLDING;
      actor.exposure = actor.reloadReturnState === STATES.HOLDING ? 1 : 0;
      actor.aim = actor.reloadReturnState === STATES.HOLDING ? 1 : 0;
      actor.reloadRemainingMs = this.config.reloadMs;
      this.events.push({ type: "reloadStarted", timeMs: this.timeMs, actorId: actor.id, durationMs: this.config.reloadMs });
    }

    resolveShots() {
      const player = this.actors.player;
      const enemy = this.actors.enemy;
      const candidates = [];
      if (player.fireHeld && this.canFireWeapon(player) && enemy.state !== STATES.DEAD) {
        candidates.push({ shooter: player, target: enemy });
      }
      if (enemy.automaticFire && this.canAutoShoot(enemy, player)) {
        candidates.push({ shooter: enemy, target: player });
      }

      candidates.sort((left, right) => {
        const aimDifference = right.shooter.aim - left.shooter.aim;
        if (Math.abs(aimDifference) > 0.0001) return aimDifference;
        return left.shooter.id === "player" ? -1 : 1;
      });

      for (const candidate of candidates) {
        if (candidate.shooter.state !== STATES.DEAD && candidate.target.state !== STATES.DEAD) this.fire(candidate.shooter, candidate.target);
      }
    }

    canFireWeapon(shooter) {
      return shooter.state === STATES.HOLDING && shooter.ammo > 0 &&
        !shooter.needsReload && shooter.cooldownMs <= 0;
    }

    canAutoShoot(shooter, target) {
      return this.canFireWeapon(shooter) && target.state !== STATES.DEAD && this.isTargetable(target);
    }

    isTargetable(target) {
      return target.state === STATES.SWITCHING || target.exposure > 0;
    }

    fire(shooter, target) {
      const hitChance = this.hitChanceForTarget(target);
      const hit = hitChance > 0 && this.random() < hitChance;
      shooter.shots += 1;
      shooter.ammo -= 1;
      shooter.cooldownMs = this.config.shotCooldownMs;
      this.events.push({
        type: "shot",
        timeMs: this.timeMs,
        shooterId: shooter.id,
        targetId: target.id,
        targetExposure: target.exposure,
        hitChance,
        ammoRemaining: shooter.ammo,
        hit
      });

      if (hit) {
        shooter.hits += 1;
        target.hp = Math.max(0, target.hp - this.config.baseDamage);
        this.events.push({ type: "hit", timeMs: this.timeMs, shooterId: shooter.id, targetId: target.id, damage: this.config.baseDamage, remainingHp: target.hp, hitChance });
        if (target.hp <= 0) {
          target.state = STATES.DEAD;
          target.intentPeek = false;
          target.fireHeld = false;
          this.events.push({ type: "death", timeMs: this.timeMs, actorId: target.id, killerId: shooter.id });
        }
      }

      if (shooter.ammo <= 0 && shooter.state !== STATES.DEAD) {
        shooter.needsReload = true;
        shooter.fireHeld = false;
        this.events.push({ type: "magazineEmpty", timeMs: this.timeMs, actorId: shooter.id });
      }
    }

    hitChanceForExposure(exposure) {
      const normalizedExposure = clamp(exposure, 0, 1);
      if (normalizedExposure < 0.5) return 0;
      const vulnerableProgress = (normalizedExposure - 0.5) / 0.5;
      return this.config.minimumExposureHitChance +
        (this.config.maximumExposureHitChance - this.config.minimumExposureHitChance) * vulnerableProgress;
    }

    hitChanceForTarget(target) {
      if (target.state === STATES.SWITCHING) return this.config.switchCoverHitChance;
      return this.hitChanceForExposure(target.exposure);
    }

    drainEvents() {
      const events = this.events.slice();
      this.events.length = 0;
      return events;
    }

    getSnapshot() {
      return { timeMs: this.timeMs, player: Object.assign({}, this.actors.player), enemy: Object.assign({}, this.actors.enemy) };
    }
  }

  return { CombatModel, STATES, DEFAULTS };
});
