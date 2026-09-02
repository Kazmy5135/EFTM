import Script from 'next/script';

export default function Home() {
  return (
    <>
      <main id="app" className="app" aria-label="EFTM 3D 摄像机 Peek 原型">
        <canvas id="sceneCanvas" aria-label="3D 工业通道" />
        <div className="grain" aria-hidden="true" />
        <div className="edge-shadow" aria-hidden="true" />

        <header className="top-hud">
          <div>
            <p>H5-PEEK-CAMERA-001</p>
            <h1>通道观察位</h1>
          </div>
          <span className="prototype-badge">视角验证</span>
        </header>

        <section className="state-panel" aria-live="polite">
          <span className="state-dot" aria-hidden="true" />
          <div>
            <small>CAMERA STATE</small>
            <strong id="stateLabel">掩体后 · 通道边缘可见</strong>
            <span className="intel-row">
              <b id="intelState">没有敌人信息</b>
              <em id="visibilityLabel" />
            </span>
          </div>
        </section>

        <div id="reticle" className="reticle" aria-hidden="true">
          <i />
          <span />
        </div>
        <div id="aimSurface" className="aim-surface" aria-label="拖动中央画面调整准星" />

        <section id="instructionCard" className="instruction-card">
          <span className="lean-icon" aria-hidden="true">
            <i />
          </span>
          <div>
            <strong>选择你的探头方式</strong>
            <small>真架枪点击锁定 · 假动作按住探头</small>
          </div>
        </section>

        <div className="peek-progress" aria-hidden="true">
          <i id="peekProgressFill" />
        </div>

        <div id="shotFlash" className="shot-flash" aria-hidden="true" />
        <div id="shotResult" className="shot-result" aria-live="polite" />

        <button
          id="fireControl"
          className="fire-control"
          type="button"
          aria-pressed="false"
          disabled
          hidden
        >
          <span>FIRE</span>
          <small>按住射击 · 拖动压枪</small>
        </button>

        <div className="action-row">
          <button
            id="trueAimControl"
            className="action-control true-aim-control"
            type="button"
            aria-pressed="false"
          >
            <span className="action-symbol" aria-hidden="true">◎</span>
            <span className="button-copy">
              <strong>真架枪</strong>
              <small>点击锁定</small>
            </span>
          </button>
          <button
            id="fakePeekControl"
            className="action-control fake-peek-control"
            type="button"
            aria-pressed="false"
          >
            <span className="action-symbol" aria-hidden="true">↗</span>
            <span className="button-copy">
              <strong>假动作</strong>
              <small>按住探头</small>
            </span>
          </button>
        </div>

        <p className="experiment-note">视角 / 假动作 / 准星射击验证 · 无伤害 / 无弹匣</p>

        <div id="unsupported" className="unsupported" role="alert" hidden>
          <strong>设备无法启动 3D 场景</strong>
          <span id="unsupportedDetail">请确认浏览器已开启 WebGL。</span>
        </div>
      </main>

      <Script src="/peek-app.js" strategy="afterInteractive" />
    </>
  );
}
