(function startPrototype() {
  "use strict";

  const { CombatModel, STATES } = window.EFTMCombat;
  const model = new CombatModel();
  const byId = (id) => document.getElementById(id);
  const elements = {
    playerActor: byId("playerActor"), enemyActor: byId("enemyActor"), enemyLabel: byId("enemyLabel"),
    playerCoverLeft: byId("playerCoverLeft"), playerCoverRight: byId("playerCoverRight"),
    playerStateText: byId("playerStateText"), enemyStateText: byId("enemyStateText"),
    playerHealth: byId("playerHealth"), enemyHealth: byId("enemyHealth"),
    aimFill: byId("aimFill"), aimPercent: byId("aimPercent"),
    ammoReadout: byId("ammoReadout"), ammoCount: byId("ammoCount"), ammoStatus: byId("ammoStatus"), reloadFill: byId("reloadFill"),
    battlefield: byId("battlefield"), gestureGuide: byId("gestureGuide"),
    gestureGuideTitle: byId("gestureGuideTitle"), gestureGuideHint: byId("gestureGuideHint"),
    fireControl: byId("fireControl"), fireControlTitle: byId("fireControlTitle"), fireControlHint: byId("fireControlHint"),
    reloadControl: byId("reloadControl"), reloadControlTitle: byId("reloadControlTitle"),
    reloadControlHint: byId("reloadControlHint"), reloadProgressFill: byId("reloadProgressFill"),
    emptyWarning: byId("emptyWarning"), emptyWarningHint: byId("emptyWarningHint"), emptyFlash: byId("emptyFlash"),
    safetyBadge: byId("safetyBadge"), combatFeed: byId("combatFeed"), roundStatus: byId("roundStatus"),
    tracer: byId("tracer"), impact: byId("impact"), damageVignette: byId("damageVignette"), combatTextLayer: byId("combatTextLayer"),
    resultPanel: byId("resultPanel"), resultEyebrow: byId("resultEyebrow"), resultTitle: byId("resultTitle"),
    resetButton: byId("resetButton"), restartButton: byId("restartButton")
  };

  const stateLabels = {
    [STATES.HIDDEN]: "掩体内", [STATES.EXPOSING]: "探身中", [STATES.HOLDING]: "架枪中",
    [STATES.RETREATING]: "回撤中", [STATES.SWITCHING]: "换掩体中", [STATES.RELOADING]: "换弹中", [STATES.DEAD]: "失去战斗力"
  };

  let lastFrame = performance.now();
  let enemyDecisionAt = 900;
  let enemyPeeking = false;
  let enemyReloadAt = 0;
  let gesturePointerId = null;
  let gestureStartX = 0;
  let gestureStartY = 0;
  let gestureTracking = false;
  let firePointerId = null;
  let fireGestureActive = false;
  let gameOver = false;
  let audioContext = null;
  let lastTouchEndAt = 0;

  const randomBetween = (minimum, maximum) => minimum + Math.random() * (maximum - minimum);

  function preventBrowserGesture(event) {
    if (event.cancelable) event.preventDefault();
  }

  document.addEventListener("contextmenu", preventBrowserGesture, { passive: false });
  document.addEventListener("selectstart", preventBrowserGesture, { passive: false });
  document.addEventListener("dragstart", preventBrowserGesture, { passive: false });
  document.addEventListener("dblclick", preventBrowserGesture, { passive: false });
  document.addEventListener("gesturestart", preventBrowserGesture, { passive: false });
  document.addEventListener("gesturechange", preventBrowserGesture, { passive: false });
  document.addEventListener("gestureend", preventBrowserGesture, { passive: false });
  document.addEventListener("touchend", (event) => {
    const now = Date.now();
    if (now - lastTouchEndAt <= 350) preventBrowserGesture(event);
    lastTouchEndAt = now;
  }, { passive: false });

  function ensureAudio() {
    if (!audioContext) {
      const AudioContext = window.AudioContext || window.webkitAudioContext;
      if (AudioContext) audioContext = new AudioContext();
    }
    if (audioContext && audioContext.state === "suspended") audioContext.resume();
  }

  function playShotSound(shooterId) {
    if (!audioContext) return;
    const now = audioContext.currentTime;
    const oscillator = audioContext.createOscillator();
    const gain = audioContext.createGain();
    oscillator.type = "sawtooth";
    oscillator.frequency.setValueAtTime(shooterId === "player" ? 112 : 92, now);
    oscillator.frequency.exponentialRampToValueAtTime(48, now + 0.07);
    gain.gain.setValueAtTime(0.0001, now);
    gain.gain.exponentialRampToValueAtTime(0.16, now + 0.006);
    gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.09);
    oscillator.connect(gain).connect(audioContext.destination);
    oscillator.start(now);
    oscillator.stop(now + 0.095);
  }

  function pulseClass(element, className, duration) {
    element.classList.remove(className);
    void element.offsetWidth;
    element.classList.add(className);
    window.setTimeout(() => element.classList.remove(className), duration);
  }

  function spawnCombatText(targetId, text, kind) {
    const popup = document.createElement("span");
    popup.className = `combat-float ${kind} target-${targetId}`;
    popup.textContent = text;
    popup.style.setProperty("--drift", `${randomBetween(-4.5, 4.5).toFixed(2)}vh`);
    elements.combatTextLayer.appendChild(popup);
    window.setTimeout(() => popup.remove(), 760);
  }

  function triggerAim() {
    if (gameOver) return false;
    ensureAudio();
    if (!model.startAim("player")) return false;
    elements.combatFeed.textContent = "上滑架枪 · 开始探身";
    return true;
  }

  function triggerRetreat() {
    if (gameOver || !model.startRetreat("player")) return false;
    ensureAudio();
    elements.combatFeed.textContent = "下滑 · 主动返回掩体";
    return true;
  }

  function triggerCoverSwitch() {
    if (gameOver || !model.startCoverSwitch("player")) return false;
    ensureAudio();
    elements.combatFeed.textContent = "横穿通道 · 被命中概率 20%";
    if (navigator.vibrate) navigator.vibrate(24);
    return true;
  }

  function setPlayerFireHeld(shouldFire) {
    if (gameOver && shouldFire) return false;
    const changed = model.setFireHeld("player", shouldFire);
    fireGestureActive = Boolean(shouldFire && changed);
    elements.fireControl.classList.toggle("firing-held", fireGestureActive);
    if (fireGestureActive) {
      ensureAudio();
      // The first round leaves immediately on press; subsequent rounds are paced by the frame loop.
      for (const event of model.step(1)) processEvent(event);
      render(model.getSnapshot());
    }
    return changed;
  }

  function beginGesture(clientX, clientY, pointerId) {
    if (gestureTracking || gameOver) return false;
    gestureTracking = true;
    gesturePointerId = pointerId;
    gestureStartX = clientX;
    gestureStartY = clientY;
    elements.battlefield.classList.add("gesture-active");
    elements.gestureGuide.classList.add("active");
    return true;
  }

  function finishGesture(clientX, clientY) {
    if (!gestureTracking) return;
    const deltaX = clientX - gestureStartX;
    const deltaY = clientY - gestureStartY;
    const threshold = Math.max(42, elements.battlefield.clientWidth * 0.09);
    gestureTracking = false;
    gesturePointerId = null;
    elements.battlefield.classList.remove("gesture-active");
    elements.gestureGuide.classList.remove("active");
    if (Math.max(Math.abs(deltaX), Math.abs(deltaY)) < threshold) return;

    const player = model.getSnapshot().player;
    if (Math.abs(deltaX) > Math.abs(deltaY) * 1.15) {
      const correctDirection = (player.coverSide === "left" && deltaX > 0) ||
        (player.coverSide === "right" && deltaX < 0);
      if (correctDirection) triggerCoverSwitch();
      else elements.combatFeed.textContent = player.coverSide === "left" ? "请向右滑动切换掩体" : "请向左滑动切换掩体";
      return;
    }
    if (deltaY < 0) triggerAim();
    else triggerRetreat();
  }

  elements.battlefield.addEventListener("pointerdown", (event) => {
    if (!beginGesture(event.clientX, event.clientY, event.pointerId)) return;
    event.preventDefault();
    try { elements.battlefield.setPointerCapture(event.pointerId); } catch { /* document fallback */ }
  });
  document.addEventListener("pointerup", (event) => {
    if (gesturePointerId !== null && event.pointerId === gesturePointerId) finishGesture(event.clientX, event.clientY);
    if (firePointerId !== null && event.pointerId === firePointerId) {
      firePointerId = null;
      setPlayerFireHeld(false);
    }
  }, { passive: false });
  document.addEventListener("pointercancel", (event) => {
    if (gesturePointerId !== null && event.pointerId === gesturePointerId) finishGesture(gestureStartX, gestureStartY);
    if (firePointerId !== null && event.pointerId === firePointerId) {
      firePointerId = null;
      setPlayerFireHeld(false);
    }
  }, { passive: false });

  elements.battlefield.addEventListener("touchstart", (event) => {
    const touch = event.touches[0];
    if (touch && beginGesture(touch.clientX, touch.clientY, "touch")) event.preventDefault();
  }, { passive: false });
  document.addEventListener("touchend", (event) => {
    const touch = event.changedTouches[0];
    if (gesturePointerId === "touch" && touch) finishGesture(touch.clientX, touch.clientY);
    if (fireGestureActive) setPlayerFireHeld(false);
  }, { passive: false });
  document.addEventListener("touchcancel", () => {
    if (gesturePointerId === "touch") finishGesture(gestureStartX, gestureStartY);
    if (fireGestureActive) setPlayerFireHeld(false);
  }, { passive: false });

  elements.fireControl.addEventListener("pointerdown", (event) => {
    event.preventDefault();
    event.stopPropagation();
    if (!setPlayerFireHeld(true)) return;
    firePointerId = event.pointerId;
    try { elements.fireControl.setPointerCapture(event.pointerId); } catch { /* document fallback */ }
  });
  elements.fireControl.addEventListener("touchstart", (event) => {
    event.stopPropagation();
    if (setPlayerFireHeld(true)) event.preventDefault();
  }, { passive: false });
  elements.fireControl.addEventListener("contextmenu", (event) => event.preventDefault());

  function triggerReload() {
    const player = model.getSnapshot().player;
    const canReloadHere = player.state === STATES.HIDDEN || player.state === STATES.HOLDING;
    if (!canReloadHere || player.ammo >= model.config.magazineSize) return false;
    if (!model.startReload("player")) return false;
    setPlayerFireHeld(false);
    elements.combatFeed.textContent = player.state === STATES.HOLDING ? "暴露换弹 · 仍会被攻击" : "掩体内换弹 · 2.0s";
    ensureAudio();
    if (navigator.vibrate) navigator.vibrate(25);
    return true;
  }

  elements.reloadControl.addEventListener("click", (event) => {
    event.preventDefault();
    triggerReload();
  });
  elements.reloadControl.addEventListener("contextmenu", (event) => event.preventDefault());

  window.addEventListener("keydown", (event) => {
    if (event.code === "KeyF") { event.preventDefault(); setPlayerFireHeld(true); return; }
    if (event.repeat) return;
    if (event.code === "ArrowUp") { event.preventDefault(); triggerAim(); }
    if (event.code === "ArrowDown") { event.preventDefault(); triggerRetreat(); }
    if (event.code === "KeyQ") { event.preventDefault(); triggerCoverSwitch(); }
  });
  window.addEventListener("keyup", (event) => {
    if (event.code === "KeyF") { event.preventDefault(); setPlayerFireHeld(false); }
  });
  window.addEventListener("blur", () => setPlayerFireHeld(false));

  elements.resetButton.addEventListener("click", resetGame);
  elements.restartButton.addEventListener("click", resetGame);

  function updateEnemyIntent(now, snapshot) {
    if (gameOver || snapshot.enemy.state === STATES.DEAD) return;
    if (snapshot.enemy.state === STATES.RELOADING) return;
    if (snapshot.enemy.needsReload) {
      enemyPeeking = false;
      model.setPeekIntent("enemy", false);
      if (snapshot.enemy.state === STATES.HIDDEN) {
        if (enemyReloadAt === 0) enemyReloadAt = now + randomBetween(250, 600);
        if (now >= enemyReloadAt && model.startReload("enemy")) enemyReloadAt = 0;
      }
      return;
    }

    const enemyCanTopUp = snapshot.enemy.state === STATES.HIDDEN &&
      snapshot.enemy.ammo > 0 && snapshot.enemy.ammo <= 5;
    if (enemyCanTopUp) {
      if (enemyReloadAt === 0) enemyReloadAt = now + randomBetween(450, 900);
      if (now >= enemyReloadAt && model.startReload("enemy")) enemyReloadAt = 0;
      return;
    }

    enemyReloadAt = 0;
    if (now < enemyDecisionAt) return;
    enemyPeeking = !enemyPeeking;
    model.setPeekIntent("enemy", enemyPeeking);
    if (enemyPeeking) {
      const playerIsHolding = snapshot.player.state === STATES.HOLDING;
      enemyDecisionAt = now + randomBetween(playerIsHolding ? 700 : 1250, playerIsHolding ? 1350 : 2350);
      elements.combatFeed.textContent = "侦测到对方探身动作";
    } else {
      enemyDecisionAt = now + randomBetween(650, 1500);
      elements.combatFeed.textContent = "对方退出枪线";
    }
  }

  function processEvent(event) {
    if (event.type === "shot") {
      const shooterElement = event.shooterId === "player" ? elements.playerActor : elements.enemyActor;
      pulseClass(shooterElement, "firing", 190);
      elements.tracer.classList.toggle("enemy-shot", event.shooterId === "enemy");
      pulseClass(elements.tracer, "active", 130);
      playShotSound(event.shooterId);
      elements.combatFeed.textContent = event.shooterId === "player"
        ? `连续开火 · 余弹 ${event.ammoRemaining}` : "敌方枪线开火";
      if (!event.hit) {
        spawnCombatText(event.targetId, "MISS", "miss");
        elements.combatFeed.textContent = event.shooterId === "player" ? "MISS · 未命中敌方" : "MISS · 敌方未命中";
      }
    }

    if (event.type === "hit") {
      const targetElement = event.targetId === "player" ? elements.playerActor : elements.enemyActor;
      spawnCombatText(event.targetId, `-${event.damage}`, "damage");
      pulseClass(targetElement, "hit", 220);
      elements.impact.classList.toggle("enemy-impact", event.targetId === "enemy");
      pulseClass(elements.impact, "active", 210);
      if (event.targetId === "player") {
        pulseClass(elements.damageVignette, "active", 290);
        if (navigator.vibrate) navigator.vibrate(38);
        elements.combatFeed.textContent = `你被命中 · 概率 ${Math.round(event.hitChance * 100)}%`;
      } else {
        elements.combatFeed.textContent = "命中敌方 PMC";
      }
    }

    if (event.type === "magazineEmpty") {
      if (event.actorId === "player") {
        elements.combatFeed.textContent = "弹匣已空 · 右下换弹或下滑回掩体";
        pulseClass(elements.emptyFlash, "active", 720);
        if (navigator.vibrate) navigator.vibrate([85, 45, 85]);
      } else {
        enemyPeeking = false;
        model.setPeekIntent("enemy", false);
      }
    }

    if (event.type === "coverSwitchStarted" && event.actorId === "player") {
      elements.combatFeed.textContent = "切换掩体中 · 每发 20% 命中概率";
    }

    if (event.type === "coverSwitchCompleted" && event.actorId === "player") {
      elements.combatFeed.textContent = `已进入${event.coverSide === "left" ? "左侧" : "右侧"}掩体`;
    }

    if (event.type === "reloadAvailable") {
      if (event.actorId === "player") elements.combatFeed.textContent = "点击右下角换弹";
      else enemyReloadAt = performance.now() + randomBetween(250, 600);
    }

    if (event.type === "reloadStarted" && event.actorId === "player") {
      const player = model.getSnapshot().player;
      elements.combatFeed.textContent = player.reloadReturnState === STATES.HOLDING
        ? "架枪换弹 · 身体仍然暴露" : "掩体内换弹 · 2.0s";
    }

    if (event.type === "reloadCompleted") {
      if (event.actorId === "player") {
        const player = model.getSnapshot().player;
        elements.combatFeed.textContent = player.state === STATES.HOLDING
          ? "换弹完成 · 继续架枪" : "换弹完成 · 15 发就绪";
        if (navigator.vibrate) navigator.vibrate(45);
      }
      else enemyDecisionAt = performance.now() + randomBetween(350, 900);
    }

    if (event.type === "death") finishGame(event.actorId === "enemy");
  }

  function finishGame(playerWon) {
    gameOver = true;
    setPlayerFireHeld(false);
    model.setPeekIntent("enemy", false);
    elements.battlefield.classList.remove("gesture-active");
    elements.gestureGuide.classList.remove("active");
    elements.fireControl.hidden = true;
    elements.reloadControl.disabled = true;
    elements.resultEyebrow.textContent = playerWon ? "枪线控制成功" : "交战失败";
    elements.resultTitle.textContent = playerWon ? "敌方失去战斗力" : "你被压制击倒";
    elements.resultPanel.hidden = false;
  }

  function resetGame() {
    model.reset();
    gameOver = false;
    enemyPeeking = false;
    enemyReloadAt = 0;
    enemyDecisionAt = performance.now() + 900;
    gestureTracking = false;
    gesturePointerId = null;
    firePointerId = null;
    fireGestureActive = false;
    elements.battlefield.classList.remove("gesture-active");
    elements.gestureGuide.classList.remove("active");
    elements.fireControl.classList.remove("firing-held", "empty-alert");
    elements.fireControl.hidden = true;
    elements.resultPanel.hidden = true;
    elements.playerActor.classList.remove("dead", "hit", "firing");
    elements.enemyActor.classList.remove("dead", "hit", "firing");
    elements.combatFeed.textContent = "等待交战";
    render(model.getSnapshot());
  }

  function renderActor(element, actor) {
    const hiddenOffset = element.classList.contains("enemy") ? 46 : 52;
    element.style.setProperty("--actor-shift", `${((1 - actor.exposure) * hiddenOffset).toFixed(2)}%`);
    if (actor.id === "player") {
      const sidePosition = (side) => side === "left" ? 23 : 77;
      let actorLeft = sidePosition(actor.coverSide);
      if (actor.state === STATES.SWITCHING) {
        const progress = 1 - actor.switchRemainingMs / model.config.switchCoverMs;
        const from = sidePosition(actor.switchFromSide);
        const target = sidePosition(actor.switchTargetSide);
        actorLeft = from + (target - from) * Math.max(0, Math.min(1, progress));
      }
      element.style.setProperty("--actor-left", `${actorLeft.toFixed(2)}%`);
      element.classList.toggle("switching", actor.state === STATES.SWITCHING);
      const peeking = actor.exposure > 0 && actor.state !== STATES.SWITCHING;
      const direction = actor.coverSide === "left" ? 1 : -1;
      element.classList.toggle("peeking", peeking);
      element.style.setProperty("--peek-lean", `${direction * 8}deg`);
      element.style.setProperty("--peek-slide", `${direction * 8}%`);
    } else {
      element.style.setProperty("--actor-left", `${(50 + actor.exposure * 21).toFixed(2)}%`);
      element.classList.toggle("concealed", actor.exposure <= 0 && actor.state !== STATES.DEAD);
    }
    element.classList.toggle("dead", actor.state === STATES.DEAD);
  }

  function render(snapshot) {
    const { player, enemy } = snapshot;
    renderActor(elements.playerActor, player);
    renderActor(elements.enemyActor, enemy);
    elements.playerHealth.style.transform = `scaleX(${player.hp / 100})`;
    elements.enemyHealth.style.transform = `scaleX(${enemy.hp / 100})`;
    const exposureHitChance = model.hitChanceForTarget(player);
    elements.aimFill.style.transform = `scaleX(${exposureHitChance})`;
    elements.aimPercent.textContent = exposureHitChance > 0 ? `${Math.round(exposureHitChance * 100)}%` : "安全";
    elements.ammoCount.textContent = String(player.ammo).padStart(2, "0");
    const reloadProgress = player.state === STATES.RELOADING
      ? 1 - player.reloadRemainingMs / model.config.reloadMs
      : 0;
    elements.reloadFill.style.transform = `scaleX(${Math.max(0, Math.min(1, reloadProgress))})`;
    elements.ammoReadout.classList.toggle("reloading", player.state === STATES.RELOADING);
    elements.ammoReadout.classList.toggle("empty", player.ammo <= 0);
    if (player.state === STATES.RELOADING) elements.ammoStatus.textContent = `${(player.reloadRemainingMs / 1000).toFixed(1)}s`;
    else if (player.ammo <= 0) elements.ammoStatus.textContent = "空仓";
    else if (player.ammo < model.config.magazineSize) elements.ammoStatus.textContent = "可换";
    else elements.ammoStatus.textContent = "就绪";
    const coverName = player.coverSide === "left" ? "左侧" : "右侧";
    const exposedReload = player.state === STATES.RELOADING && player.reloadReturnState === STATES.HOLDING;
    elements.playerStateText.textContent = player.state === STATES.HIDDEN
      ? `${coverName}掩体 · ${player.needsReload ? "弹匣已空" : "安全"}`
      : (player.state === STATES.SWITCHING
        ? "横穿通道 · 20%"
        : (exposedReload ? "架枪换弹中 · 暴露" : stateLabels[player.state]));
    const enemyConcealed = enemy.exposure <= 0 && enemy.state !== STATES.DEAD;
    elements.enemyLabel.classList.toggle("concealed", enemyConcealed);
    elements.enemyStateText.textContent = enemyConcealed ? "目标丢失" : stateLabels[enemy.state];
    elements.playerCoverLeft.classList.toggle("active", player.state !== STATES.SWITCHING && player.coverSide === "left");
    elements.playerCoverRight.classList.toggle("active", player.state !== STATES.SWITCHING && player.coverSide === "right");

    if (player.state === STATES.RELOADING) {
      if (exposedReload) {
        elements.safetyBadge.className = "safety-badge danger";
        elements.safetyBadge.textContent = "架枪换弹 · 身体暴露";
        elements.gestureGuideTitle.textContent = "换弹中";
        elements.gestureGuideHint.textContent = "保持架枪暴露 · 完成后继续瞄准";
      } else {
        elements.safetyBadge.className = "safety-badge safe";
        elements.safetyBadge.textContent = "掩体内换弹";
        elements.gestureGuideTitle.textContent = "换弹中";
        elements.gestureGuideHint.textContent = "掩体保护 · 等待完成";
      }
    } else if (player.state === STATES.SWITCHING) {
      elements.safetyBadge.className = "safety-badge danger";
      elements.safetyBadge.textContent = "每发 20% 命中";
      elements.gestureGuideTitle.textContent = "切换中";
      elements.gestureGuideHint.textContent = `前往${player.switchTargetSide === "left" ? "左侧" : "右侧"}掩体 · 被命中 20%`;
    } else if (player.needsReload) {
      if (player.state === STATES.HOLDING) {
        elements.safetyBadge.className = "safety-badge danger";
        elements.safetyBadge.textContent = "空弹暴露 · 无法攻击";
        elements.gestureGuideTitle.textContent = "弹匣已空";
        elements.gestureGuideHint.textContent = "右下换弹 · 或下滑回掩体";
      } else if (player.state === STATES.RETREATING) {
        elements.safetyBadge.className = "safety-badge danger";
        elements.safetyBadge.textContent = "回撤中可被命中";
        elements.gestureGuideTitle.textContent = "空弹回撤中";
        elements.gestureGuideHint.textContent = "回到掩体后点击右下换弹";
      } else {
        elements.safetyBadge.className = "safety-badge safe";
        elements.safetyBadge.textContent = "掩体保护 · 待换弹";
        elements.gestureGuideTitle.textContent = "弹匣已空";
        elements.gestureGuideHint.textContent = "点击右下角换弹";
      }
    } else if (player.state === STATES.HIDDEN) {
      elements.safetyBadge.className = "safety-badge safe";
      elements.safetyBadge.textContent = "掩体保护";
      elements.gestureGuideTitle.textContent = `${coverName}掩体`;
      elements.gestureGuideHint.textContent = player.coverSide === "left"
        ? "向右滑切换 · 上滑架枪" : "向左滑切换 · 上滑架枪";
    } else if (player.state === STATES.RETREATING) {
      elements.safetyBadge.className = "safety-badge danger";
      elements.safetyBadge.textContent = "仍可被命中";
      elements.gestureGuideTitle.textContent = "正在回撤";
      elements.gestureGuideHint.textContent = "回到掩体后恢复安全";
    } else if (player.state === STATES.HOLDING) {
      elements.safetyBadge.className = "safety-badge exposed";
      elements.safetyBadge.textContent = "身体暴露";
      elements.gestureGuideTitle.textContent = "歪头架枪中";
      elements.gestureGuideHint.textContent = "按住中央开火 · 下滑回掩体";
    } else {
      elements.safetyBadge.className = "safety-badge danger";
      elements.safetyBadge.textContent = "暴露增加";
      elements.gestureGuideTitle.textContent = "歪头架枪中";
      elements.gestureGuideHint.textContent = player.exposure < 0.5
        ? "建立动作不可取消 · 前150ms安全" : "进入火力区 · 完成后可开火";
    }
    elements.gestureGuide.classList.toggle("danger", player.exposure > 0 || player.state === STATES.SWITCHING);

    const inCover = player.state === STATES.HIDDEN;
    const reloadInProgress = player.state === STATES.RELOADING;
    const canStartReload = (inCover || player.state === STATES.HOLDING) && player.ammo < model.config.magazineSize;
    elements.reloadControl.disabled = gameOver || !canStartReload;
    elements.reloadProgressFill.style.transform = `scaleX(${Math.max(0, Math.min(1, reloadProgress))})`;

    const showEmptyAlert = !gameOver && player.needsReload && !reloadInProgress;
    elements.emptyWarning.hidden = !showEmptyAlert;
    elements.fireControl.classList.toggle("empty-alert", showEmptyAlert && player.state === STATES.HOLDING);
    elements.reloadControl.classList.toggle("empty-alert", showEmptyAlert && (inCover || player.state === STATES.HOLDING));
    if (showEmptyAlert) {
      if (player.state === STATES.HOLDING) elements.emptyWarningHint.textContent = "右下换弹 · 或下滑回掩体";
      else if (player.state === STATES.RETREATING) elements.emptyWarningHint.textContent = "回撤中 · 准备换弹";
      else elements.emptyWarningHint.textContent = "点击右下角 · 立即换弹";
    }

    const fireVisible = !gameOver && player.state === STATES.HOLDING;
    elements.fireControl.hidden = !fireVisible;
    elements.fireControl.disabled = !fireVisible || player.ammo <= 0;
    elements.fireControl.classList.toggle("firing-held", fireVisible && player.fireHeld);
    if (player.ammo <= 0) {
      elements.fireControlTitle.textContent = "空弹";
      elements.fireControlHint.textContent = "点击右下换弹";
    } else if (player.fireHeld) {
      elements.fireControlTitle.textContent = "连续开火";
      elements.fireControlHint.textContent = "松开停止";
    } else {
      elements.fireControlTitle.textContent = "按住开火";
      elements.fireControlHint.textContent = "松开停止";
    }

    if (reloadInProgress) {
      elements.reloadControlTitle.textContent = `换弹 ${(player.reloadRemainingMs / 1000).toFixed(1)}s`;
      elements.reloadControlHint.textContent = exposedReload ? "架枪暴露中" : "掩体内进行";
    } else if (player.ammo >= model.config.magazineSize) {
      elements.reloadControlTitle.textContent = "换弹";
      elements.reloadControlHint.textContent = "弹匣已满";
    } else if (!inCover && player.state !== STATES.HOLDING) {
      elements.reloadControlTitle.textContent = "换弹";
      elements.reloadControlHint.textContent = "当前不可用";
    } else if (player.needsReload) {
      elements.reloadControlTitle.textContent = "换弹";
      elements.reloadControlHint.textContent = "空匣 · 点击";
    } else {
      elements.reloadControlTitle.textContent = "换弹";
      elements.reloadControlHint.textContent = player.state === STATES.HOLDING ? "架枪中可用" : "掩体内可用";
    }

    if (!gameOver) {
      if (player.state === STATES.SWITCHING) elements.roundStatus.textContent = "横穿通道 · 每发 20% 命中概率";
      else if (player.exposure <= 0 && enemy.exposure <= 0) elements.roundStatus.textContent = "通道安静 · 敌人不可见";
      else if (enemy.state === STATES.HOLDING && player.exposure <= 0) elements.roundStatus.textContent = "敌方已经建立枪线";
      else if ((player.state === STATES.HOLDING || exposedReload) && enemy.exposure <= 0) elements.roundStatus.textContent = exposedReload ? "架枪换弹 · 仍在枪线" : "你正在提前架枪";
      else if (player.exposure > 0 && enemy.exposure > 0) elements.roundStatus.textContent = "双方进入同一枪线";
      else elements.roundStatus.textContent = "枪线争夺中";
    }
  }

  function frame(now) {
    const deltaMs = Math.min(50, now - lastFrame);
    lastFrame = now;
    updateEnemyIntent(now, model.getSnapshot());
    for (const event of model.step(deltaMs)) processEvent(event);
    render(model.getSnapshot());
    requestAnimationFrame(frame);
  }

  render(model.getSnapshot());
  requestAnimationFrame(frame);
})();
