(function startPrototype() {
  "use strict";

  const { CombatModel, STATES } = window.EFTMCombat;
  const model = new CombatModel();
  const byId = (id) => document.getElementById(id);
  const elements = {
    playerActor: byId("playerActor"), enemyActor: byId("enemyActor"),
    playerStateText: byId("playerStateText"), enemyStateText: byId("enemyStateText"),
    playerHealth: byId("playerHealth"), enemyHealth: byId("enemyHealth"),
    aimFill: byId("aimFill"), aimPercent: byId("aimPercent"),
    ammoReadout: byId("ammoReadout"), ammoCount: byId("ammoCount"), ammoStatus: byId("ammoStatus"), reloadFill: byId("reloadFill"),
    holdControl: byId("holdControl"), controlTitle: byId("controlTitle"), controlHint: byId("controlHint"),
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
    [STATES.RETREATING]: "回撤中", [STATES.RELOADING]: "换弹中", [STATES.DEAD]: "失去战斗力"
  };

  let lastFrame = performance.now();
  let enemyDecisionAt = 900;
  let enemyPeeking = false;
  let enemyReloadAt = 0;
  let activePointerId = null;
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

  function setPlayerIntent(shouldPeek) {
    if (gameOver) return;
    const player = model.getSnapshot().player;
    if (shouldPeek && (player.needsReload || player.state === STATES.RELOADING)) return;
    ensureAudio();
    model.setPeekIntent("player", shouldPeek);
    elements.holdControl.classList.toggle("holding", shouldPeek);
  }

  function releasePointer(event) {
    if (activePointerId !== null && (!event || event.pointerId === activePointerId)) {
      activePointerId = null;
      setPlayerIntent(false);
    }
  }

  elements.holdControl.addEventListener("pointerdown", (event) => {
    event.preventDefault();
    activePointerId = event.pointerId;
    try {
      elements.holdControl.setPointerCapture(event.pointerId);
    } catch {
      // The local release listener remains available when capture is unsupported.
    }
    setPlayerIntent(true);
  });
  elements.holdControl.addEventListener("pointerup", releasePointer);
  elements.holdControl.addEventListener("pointercancel", releasePointer);
  elements.holdControl.addEventListener("lostpointercapture", releasePointer);
  elements.holdControl.addEventListener("contextmenu", (event) => event.preventDefault());

  function triggerReload() {
    const player = model.getSnapshot().player;
    if (player.state !== STATES.HIDDEN || player.ammo >= model.config.magazineSize) return false;
    if (!model.startReload("player")) return false;
    elements.combatFeed.textContent = "换弹已触发 · 2.0s";
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
    if (event.code === "Space" && !event.repeat) { event.preventDefault(); setPlayerIntent(true); }
  });
  window.addEventListener("keyup", (event) => {
    if (event.code === "Space") { event.preventDefault(); setPlayerIntent(false); }
  });
  window.addEventListener("blur", () => {
    activePointerId = null;
    setPlayerIntent(false);
  });
  document.addEventListener("visibilitychange", () => {
    if (document.hidden) {
      activePointerId = null;
      setPlayerIntent(false);
    }
  });

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
        ? `自动开火 · 余弹 ${event.ammoRemaining}` : "敌方枪线开火";
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
        elements.combatFeed.textContent = "返回掩体，子弹夹已空";
        pulseClass(elements.emptyFlash, "active", 720);
        if (navigator.vibrate) navigator.vibrate([85, 45, 85]);
      } else {
        enemyPeeking = false;
        model.setPeekIntent("enemy", false);
      }
    }

    if (event.type === "reloadAvailable") {
      if (event.actorId === "player") elements.combatFeed.textContent = "点击右侧按钮换弹";
      else enemyReloadAt = performance.now() + randomBetween(250, 600);
    }

    if (event.type === "reloadStarted" && event.actorId === "player") {
      elements.combatFeed.textContent = "换弹已触发 · 2.0s";
    }

    if (event.type === "reloadCompleted") {
      if (event.actorId === "player") {
        elements.combatFeed.textContent = "换弹完成 · 15 发就绪";
        if (navigator.vibrate) navigator.vibrate(45);
      }
      else enemyDecisionAt = performance.now() + randomBetween(350, 900);
    }

    if (event.type === "death") finishGame(event.actorId === "enemy");
  }

  function finishGame(playerWon) {
    gameOver = true;
    activePointerId = null;
    model.setPeekIntent("player", false);
    model.setPeekIntent("enemy", false);
    elements.holdControl.classList.remove("holding");
    elements.holdControl.disabled = true;
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
    activePointerId = null;
    enemyDecisionAt = performance.now() + 900;
    elements.holdControl.disabled = false;
    elements.holdControl.classList.remove("holding");
    elements.resultPanel.hidden = true;
    elements.playerActor.classList.remove("dead", "hit", "firing");
    elements.enemyActor.classList.remove("dead", "hit", "firing");
    elements.combatFeed.textContent = "等待交战";
    render(model.getSnapshot());
  }

  function renderActor(element, actor) {
    const hiddenOffset = element.classList.contains("enemy") ? 46 : 52;
    element.style.setProperty("--actor-shift", `${((1 - actor.exposure) * hiddenOffset).toFixed(2)}%`);
    element.classList.toggle("dead", actor.state === STATES.DEAD);
  }

  function render(snapshot) {
    const { player, enemy } = snapshot;
    renderActor(elements.playerActor, player);
    renderActor(elements.enemyActor, enemy);
    elements.playerHealth.style.transform = `scaleX(${player.hp / 100})`;
    elements.enemyHealth.style.transform = `scaleX(${enemy.hp / 100})`;
    const exposureHitChance = player.exposure > 0 ? model.hitChanceForExposure(player.exposure) : 0;
    elements.aimFill.style.transform = `scaleX(${exposureHitChance})`;
    elements.aimPercent.textContent = player.exposure > 0 ? `${Math.round(exposureHitChance * 100)}%` : "安全";
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
    elements.playerStateText.textContent = player.state === STATES.HIDDEN
      ? (player.needsReload ? "掩体内 · 子弹夹已空" : "掩体内 · 安全")
      : stateLabels[player.state];
    elements.enemyStateText.textContent = enemy.state === STATES.RELOADING ? "掩体内" : stateLabels[enemy.state];

    const inputLockedByReload = player.state === STATES.RELOADING || (player.needsReload && player.state !== STATES.HOLDING);
    elements.holdControl.disabled = gameOver || inputLockedByReload;
    if (inputLockedByReload && player.state !== STATES.HOLDING) elements.holdControl.classList.remove("holding");

    if (player.state === STATES.RELOADING) {
      elements.controlTitle.textContent = "换弹中";
      elements.controlHint.textContent = "等待 2 秒完成";
      elements.safetyBadge.className = "safety-badge safe";
      elements.safetyBadge.textContent = "掩体内换弹";
    } else if (player.needsReload) {
      if (player.state === STATES.HOLDING) {
        elements.controlTitle.textContent = "弹匣已空";
        elements.controlHint.textContent = "松开 · 立即回掩体";
        elements.safetyBadge.className = "safety-badge danger";
        elements.safetyBadge.textContent = "空弹暴露 · 无法攻击";
      } else if (player.state === STATES.RETREATING) {
        elements.controlTitle.textContent = "空弹 · 回撤中";
        elements.controlHint.textContent = "准备点击换弹";
        elements.safetyBadge.className = "safety-badge danger";
        elements.safetyBadge.textContent = "回撤中可被命中";
      } else {
        elements.controlTitle.textContent = "禁止探身";
        elements.controlHint.textContent = "点击右侧换弹";
        elements.safetyBadge.className = "safety-badge safe";
        elements.safetyBadge.textContent = "掩体保护 · 待换弹";
      }
    } else if (player.state === STATES.HIDDEN) {
      elements.controlTitle.textContent = "按住 · 探身";
      elements.controlHint.textContent = "松开回掩体";
      elements.safetyBadge.className = "safety-badge safe";
      elements.safetyBadge.textContent = "掩体保护";
    } else if (player.state === STATES.RETREATING) {
      elements.controlTitle.textContent = "正在回撤";
      elements.controlHint.textContent = "回掩体后安全";
      elements.safetyBadge.className = "safety-badge danger";
      elements.safetyBadge.textContent = "仍可被命中";
    } else if (player.state === STATES.HOLDING) {
      elements.controlTitle.textContent = "保持 · 架枪";
      elements.controlHint.textContent = "敌人探身即开火";
      elements.safetyBadge.className = "safety-badge exposed";
      elements.safetyBadge.textContent = "身体暴露";
    } else {
      elements.controlTitle.textContent = "探身中 · 按住";
      elements.controlHint.textContent = player.exposure < 0.5 ? "前 150ms 安全" : "进入敌方火力区";
      elements.safetyBadge.className = "safety-badge danger";
      elements.safetyBadge.textContent = "暴露增加";
    }

    const inCover = player.state === STATES.HIDDEN;
    const reloadInProgress = player.state === STATES.RELOADING;
    const canStartReload = inCover && player.ammo < model.config.magazineSize;
    elements.reloadControl.disabled = gameOver || !canStartReload;
    elements.reloadProgressFill.style.transform = `scaleX(${Math.max(0, Math.min(1, reloadProgress))})`;

    const showEmptyAlert = !gameOver && player.needsReload && !reloadInProgress;
    elements.emptyWarning.hidden = !showEmptyAlert;
    elements.holdControl.classList.toggle("empty-alert", showEmptyAlert);
    elements.reloadControl.classList.toggle("empty-alert", showEmptyAlert && inCover);
    if (showEmptyAlert) {
      if (player.state === STATES.HOLDING) elements.emptyWarningHint.textContent = "松开探身键 · 立即回掩体";
      else if (player.state === STATES.RETREATING) elements.emptyWarningHint.textContent = "回撤中 · 准备换弹";
      else elements.emptyWarningHint.textContent = "点击右侧按钮 · 立即换弹";
    }

    if (reloadInProgress) {
      elements.reloadControlTitle.textContent = `换弹 ${(player.reloadRemainingMs / 1000).toFixed(1)}s`;
      elements.reloadControlHint.textContent = "自动进行中";
    } else if (!inCover) {
      elements.reloadControlTitle.textContent = "掩体外不可用";
      elements.reloadControlHint.textContent = "先回到掩体";
    } else if (player.ammo >= model.config.magazineSize) {
      elements.reloadControlTitle.textContent = "弹匣已满";
      elements.reloadControlHint.textContent = `${model.config.magazineSize} / ${model.config.magazineSize}`;
    } else if (player.needsReload) {
      elements.reloadControlTitle.textContent = "立即换弹";
      elements.reloadControlHint.textContent = "点击一次";
    } else {
      elements.reloadControlTitle.textContent = "点击 · 换弹";
      elements.reloadControlHint.textContent = "松手后开始";
    }

    if (!gameOver) {
      if (player.exposure <= 0 && enemy.exposure <= 0) elements.roundStatus.textContent = "双方均在掩体内";
      else if (enemy.state === STATES.HOLDING && player.exposure <= 0) elements.roundStatus.textContent = "敌方已经建立枪线";
      else if (player.state === STATES.HOLDING && enemy.exposure <= 0) elements.roundStatus.textContent = "你正在提前架枪";
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
