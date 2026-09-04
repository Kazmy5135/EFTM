import Script from 'next/script';

export default function InventoryPrototype() {
  return (
    <>
      <main id="app" className="app inventory-app" aria-label="EFTM 搜索与背包整理原型">
        <div className="grain" aria-hidden="true" />
        <div className="edge-shadow" aria-hidden="true" />

        <header className="top-hud">
          <div>
            <p>H5-SEARCH-INVENTORY-001</p>
            <h1>战术整理</h1>
          </div>
          <div className="header-actions">
            <button id="resetControl" className="icon-control" type="button" aria-label="重置物品布局">↺</button>
            <button id="exitControl" className="icon-control" type="button" aria-label="退出整理界面">×</button>
          </div>
        </header>

        <section className="interaction-hint" aria-live="polite">
          <span className="state-dot" aria-hidden="true" />
          <div>
            <small>ONE-HAND CONTROL</small>
            <strong id="stateLabel">点击旋转 · 拖动移动</strong>
          </div>
        </section>

        <section className="inventory-workspace" aria-label="背包和搜索箱">
          <article className="container-panel backpack-panel">
            <header className="container-heading">
              <div><small>LOADOUT</small><h2>背包</h2></div>
              <span>6 × 6</span>
            </header>
            <div id="backpackGrid" className="item-grid backpack-grid" data-container="backpack" aria-label="六乘六背包" />
          </article>

          <div className="transfer-marker" aria-hidden="true"><i /><span>双向移动</span><i /></div>

          <article className="container-panel chest-panel">
            <header className="container-heading">
              <div><small>SEARCH RESULT</small><h2>搜索箱</h2></div>
              <span>4 × 5</span>
            </header>
            <div id="chestGrid" className="item-grid chest-grid" data-container="chest" aria-label="四乘五搜索箱" />
          </article>
        </section>

        <div id="statusToast" className="status-toast" role="status" aria-live="polite" hidden />
        <p className="experiment-note">单手整理验证 · 点击旋转 90° · 拖动跨容器</p>

        <section id="exitSheet" className="exit-sheet" aria-modal="true" role="dialog" aria-labelledby="exitTitle" hidden>
          <div>
            <small>SEARCH PAUSED</small>
            <h2 id="exitTitle">已退出整理</h2>
            <p>未能放下的旋转已撤销。</p>
            <button id="resumeControl" type="button">继续整理</button>
          </div>
        </section>
      </main>

      <link rel="stylesheet" href="/inventory-styles.css" />
      <Script src="/inventory-app.js" strategy="afterInteractive" />
    </>
  );
}
