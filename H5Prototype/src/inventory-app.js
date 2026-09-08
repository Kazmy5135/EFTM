import { CONTAINERS, cloneItems, commitPose, evaluatePlacement, getDraggedOrigin, rotatePose } from './inventory-model.js';
import { INITIAL_ITEMS } from './inventory-fixture.js';

const ITEM_COLORS = {
  weapon: '#d29b60',
  armor: '#74a89d',
  medical: '#d66f68',
  utility: '#83a7c9',
  supply: '#b4a86a',
};
const DRAG_THRESHOLD = 12;

const app = document.querySelector('#app');
const stateLabel = document.querySelector('#stateLabel');
const statusToast = document.querySelector('#statusToast');
const exitSheet = document.querySelector('#exitSheet');
const grids = new Map(
  [...document.querySelectorAll('.item-grid')].map((grid) => [grid.dataset.container, grid]),
);

let items = cloneItems(INITIAL_ITEMS);
let pendingRotation = null;
let gesture = null;
let toastTimer = 0;

function getCommittedItem(itemId) {
  return items.find((item) => item.id === itemId);
}

function getVisualItem(item) {
  return pendingRotation?.id === item.id ? pendingRotation : item;
}

function renderItem(item, invalidIds) {
  const pose = getVisualItem(item);
  const element = document.createElement('div');
  element.className = 'inventory-item';
  if (invalidIds.has(item.id)) element.classList.add('is-invalid');
  if (pendingRotation?.id === item.id) element.classList.add('is-pending');
  element.dataset.itemId = item.id;
  element.dataset.compact = String(pose.width * pose.height === 1);
  element.dataset.rotation = String(pose.rotation || 0);
  element.style.setProperty('--item-color', ITEM_COLORS[item.kind]);
  element.style.setProperty('--item-rotation', `${pose.rotation || 0}deg`);
  element.innerHTML = `<strong>${item.name}</strong><small>${pose.width} × ${pose.height}</small>`;
  return element;
}

function render() {
  const invalidIds = new Set();
  if (pendingRotation) {
    const result = evaluatePlacement(items, pendingRotation);
    invalidIds.add(pendingRotation.id);
    result.conflicts.forEach((id) => invalidIds.add(id));
  }

  for (const grid of grids.values()) grid.replaceChildren();
  for (const item of items) {
    const pose = getVisualItem(item);
    const grid = grids.get(pose.container);
    if (!grid) continue;
    const element = renderItem(item, invalidIds);
    grid.append(element);
    positionGridElement(element, grid, pose);
  }

  if (pendingRotation) {
    setState('旋转冲突 · 拖到空位或点其他物品撤销', 'danger');
  } else {
    setState('点击旋转 · 拖动移动', 'idle');
  }
}

function setState(message, mode) {
  stateLabel.textContent = message;
  app.dataset.state = mode;
}

function showToast(message, mode = 'success') {
  window.clearTimeout(toastTimer);
  statusToast.textContent = message;
  statusToast.dataset.mode = mode;
  statusToast.hidden = false;
  toastTimer = window.setTimeout(() => { statusToast.hidden = true; }, 1350);
}

function cancelPendingRotation({ announce = false } = {}) {
  if (!pendingRotation) return false;
  pendingRotation = null;
  if (announce) showToast('未完成的旋转已撤销', 'neutral');
  return true;
}

function rotateItem(itemId, pivotCell) {
  const committed = getCommittedItem(itemId);
  const base = pendingRotation?.id === itemId ? pendingRotation : committed;
  const candidate = rotatePose(base, pivotCell);
  const result = evaluatePlacement(items, candidate);

  if (result.valid) {
    items = commitPose(items, candidate);
    pendingRotation = null;
    pulseHaptic(8);
    showToast(`${committed.name} 已顺时针旋转 90°`);
  } else {
    pendingRotation = candidate;
    pulseHaptic([18, 30, 18]);
  }
  render();
}

function handlePointerDown(event) {
  const itemElement = event.target.closest('.inventory-item');
  if (!itemElement || gesture || event.button > 0) return;
  event.preventDefault();

  const itemId = itemElement.dataset.itemId;
  if (pendingRotation && pendingRotation.id !== itemId) {
    cancelPendingRotation();
    render();
  }

  const committed = getCommittedItem(itemId);
  const pose = pendingRotation?.id === itemId ? pendingRotation : committed;
  const freshElement = document.querySelector(`[data-item-id="${itemId}"]`);
  const itemRect = freshElement.getBoundingClientRect();
  const pivotCell = getPressedCell(event.clientX, event.clientY, itemRect, pose);

  gesture = {
    pointerId: event.pointerId,
    itemId,
    basePose: { ...pose },
    pivotCell,
    startX: event.clientX,
    startY: event.clientY,
    offsetX: event.clientX - itemRect.left,
    offsetY: event.clientY - itemRect.top,
    width: itemRect.width,
    height: itemRect.height,
    dragging: false,
    candidate: null,
    evaluation: null,
    sourceElement: freshElement,
    ghost: null,
    preview: null,
  };
  app.setPointerCapture?.(event.pointerId);
}

function handlePointerMove(event) {
  if (!gesture || gesture.pointerId !== event.pointerId) return;
  event.preventDefault();
  const distance = Math.hypot(event.clientX - gesture.startX, event.clientY - gesture.startY);
  if (!gesture.dragging && distance >= DRAG_THRESHOLD) startDrag();
  if (gesture.dragging) updateDrag(event.clientX, event.clientY);
}

function startDrag() {
  gesture.dragging = true;
  gesture.sourceElement.classList.add('is-drag-source');
  gesture.ghost = gesture.sourceElement.cloneNode(true);
  gesture.ghost.className = 'inventory-item drag-ghost';
  gesture.ghost.style.gridColumn = '';
  gesture.ghost.style.gridRow = '';
  gesture.ghost.style.width = `${gesture.width}px`;
  gesture.ghost.style.height = `${gesture.height}px`;
  app.append(gesture.ghost);
  setState('拖到高亮格后松手', 'dragging');
}

function updateDrag(clientX, clientY) {
  const appRect = app.getBoundingClientRect();
  const origin = getDraggedOrigin(
    { x: clientX, y: clientY },
    { x: gesture.offsetX, y: gesture.offsetY },
  );
  const ghostLeft = origin.x;
  const ghostTop = origin.y;
  gesture.ghost.style.left = `${ghostLeft - appRect.left}px`;
  gesture.ghost.style.top = `${ghostTop - appRect.top}px`;

  clearPlacementPreview();
  const centerX = ghostLeft + gesture.width / 2;
  const centerY = ghostTop + gesture.height / 2;
  const target = findTargetGrid(centerX, centerY);
  if (!target) {
    gesture.candidate = null;
    gesture.evaluation = null;
    gesture.ghost.classList.add('is-invalid');
    setState('移入背包或搜索箱范围', 'danger');
    return;
  }

  const snapped = snapToGrid(target.name, target.grid, ghostLeft, ghostTop, gesture.basePose);
  gesture.candidate = snapped;
  gesture.evaluation = evaluatePlacement(items, snapped);
  gesture.ghost.classList.toggle('is-invalid', !gesture.evaluation.valid);
  createPlacementPreview(target.grid, snapped, gesture.evaluation);
  markConflicts(gesture.evaluation.conflicts);
  setState(gesture.evaluation.valid ? `松手放入${CONTAINERS[target.name].label}` : '此处无法放下', gesture.evaluation.valid ? 'valid' : 'danger');
}

function findTargetGrid(x, y) {
  let nearest = null;
  for (const [name, grid] of grids) {
    const rect = grid.getBoundingClientRect();
    const inside = x >= rect.left - 18 && x <= rect.right + 18 && y >= rect.top - 18 && y <= rect.bottom + 18;
    if (!inside) continue;
    const distance = Math.abs(x - Math.max(rect.left, Math.min(x, rect.right)))
      + Math.abs(y - Math.max(rect.top, Math.min(y, rect.bottom)));
    if (!nearest || distance < nearest.distance) nearest = { name, grid, distance };
  }
  return nearest;
}

function snapToGrid(container, grid, left, top, pose) {
  const metrics = getGridMetrics(grid, container);

  return {
    ...pose,
    container,
    x: Math.round((left - metrics.rect.left - metrics.paddingLeft) / metrics.strideX),
    y: Math.round((top - metrics.rect.top - metrics.paddingTop) / metrics.strideY),
  };
}

function createPlacementPreview(grid, candidate, evaluation) {
  const preview = document.createElement('div');
  preview.className = `placement-preview ${evaluation.valid ? 'is-valid' : 'is-invalid'}`;
  grid.append(preview);
  positionGridElement(preview, grid, candidate);
  gesture.preview = preview;
}

function getGridMetrics(grid, containerName = grid.dataset.container) {
  const rect = grid.getBoundingClientRect();
  const style = getComputedStyle(grid);
  const gapX = Number.parseFloat(style.columnGap) || 0;
  const gapY = Number.parseFloat(style.rowGap) || 0;
  const paddingLeft = Number.parseFloat(style.paddingLeft) || 0;
  const paddingTop = Number.parseFloat(style.paddingTop) || 0;
  const paddingRight = Number.parseFloat(style.paddingRight) || 0;
  const paddingBottom = Number.parseFloat(style.paddingBottom) || 0;
  const definition = CONTAINERS[containerName];
  const cellWidth = (rect.width - paddingLeft - paddingRight - gapX * (definition.columns - 1)) / definition.columns;
  const cellHeight = (rect.height - paddingTop - paddingBottom - gapY * (definition.rows - 1)) / definition.rows;
  return {
    rect,
    paddingLeft,
    paddingTop,
    cellWidth,
    cellHeight,
    gapX,
    gapY,
    strideX: cellWidth + gapX,
    strideY: cellHeight + gapY,
  };
}

function positionGridElement(element, grid, pose) {
  const metrics = getGridMetrics(grid);
  element.style.left = `${metrics.paddingLeft + pose.x * metrics.strideX}px`;
  element.style.top = `${metrics.paddingTop + pose.y * metrics.strideY}px`;
  element.style.width = `${pose.width * metrics.cellWidth + (pose.width - 1) * metrics.gapX}px`;
  element.style.height = `${pose.height * metrics.cellHeight + (pose.height - 1) * metrics.gapY}px`;
}

function getPressedCell(clientX, clientY, itemRect, pose) {
  return {
    column: Math.min(pose.width - 1, Math.max(0, Math.floor((clientX - itemRect.left) / (itemRect.width / pose.width)))),
    row: Math.min(pose.height - 1, Math.max(0, Math.floor((clientY - itemRect.top) / (itemRect.height / pose.height)))),
  };
}

function markConflicts(conflictIds) {
  for (const itemId of conflictIds) {
    document.querySelector(`[data-item-id="${itemId}"]`)?.classList.add('is-drag-conflict');
  }
}

function clearPlacementPreview() {
  gesture?.preview?.remove();
  if (gesture) gesture.preview = null;
  document.querySelectorAll('.is-drag-conflict').forEach((element) => element.classList.remove('is-drag-conflict'));
}

function handlePointerEnd(event, cancelled = false) {
  if (!gesture || gesture.pointerId !== event.pointerId) return;
  event.preventDefault();
  const completedGesture = gesture;

  if (completedGesture.dragging) {
    const canCommit = !cancelled && completedGesture.candidate && completedGesture.evaluation?.valid;
    if (canCommit) {
      const from = completedGesture.basePose.container;
      items = commitPose(items, completedGesture.candidate);
      pendingRotation = null;
      const destination = completedGesture.candidate.container;
      const itemName = getCommittedItem(completedGesture.itemId).name;
      const message = from === destination
        ? `${itemName} 已重新放置`
        : `${itemName} 已移入${CONTAINERS[destination].label}`;
      pulseHaptic(10);
      showToast(message);
    } else if (!cancelled) {
      pulseHaptic([18, 25, 18]);
      showToast('目标位置放不下，已返回', 'danger');
    }
    cleanupGesture();
    render();
    return;
  }

  cleanupGesture();
  if (!cancelled) rotateItem(completedGesture.itemId, completedGesture.pivotCell);
}

function cleanupGesture() {
  clearPlacementPreview();
  gesture?.ghost?.remove();
  gesture?.sourceElement?.classList.remove('is-drag-source');
  gesture = null;
}

function pulseHaptic(pattern) {
  try { navigator.vibrate?.(pattern); } catch { /* Optional mobile feedback. */ }
}

app.addEventListener('pointerdown', handlePointerDown);
app.addEventListener('pointermove', handlePointerMove);
app.addEventListener('pointerup', (event) => handlePointerEnd(event));
app.addEventListener('pointercancel', (event) => handlePointerEnd(event, true));
app.addEventListener('contextmenu', (event) => event.preventDefault());

document.querySelector('#resetControl').addEventListener('click', () => {
  cleanupGesture();
  pendingRotation = null;
  items = cloneItems(INITIAL_ITEMS);
  render();
  showToast('物品布局已重置', 'neutral');
});

document.querySelector('#exitControl').addEventListener('click', () => {
  cleanupGesture();
  const reverted = cancelPendingRotation();
  render();
  exitSheet.hidden = false;
  exitSheet.querySelector('p').textContent = reverted ? '未能放下的旋转已撤销。' : '当前合法布局已保留。';
});

document.querySelector('#resumeControl').addEventListener('click', () => {
  exitSheet.hidden = true;
  setState('点击旋转 · 拖动移动', 'idle');
});

render();
