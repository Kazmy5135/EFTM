export const CONTAINERS = Object.freeze({
  backpack: Object.freeze({ columns: 6, rows: 6, label: '背包' }),
  chest: Object.freeze({ columns: 4, rows: 5, label: '搜索箱' }),
});

export function cloneItems(items) {
  return items.map((item) => ({ ...item }));
}

export function rotatePose(pose) {
  return {
    ...pose,
    width: pose.height,
    height: pose.width,
    rotation: ((pose.rotation || 0) + 90) % 360,
  };
}

export function evaluatePlacement(items, candidate) {
  const container = CONTAINERS[candidate.container];
  if (!container) {
    return { valid: false, outOfBounds: true, conflicts: [] };
  }

  const outOfBounds = candidate.x < 0
    || candidate.y < 0
    || candidate.x + candidate.width > container.columns
    || candidate.y + candidate.height > container.rows;

  const conflicts = items
    .filter((item) => item.id !== candidate.id && item.container === candidate.container)
    .filter((item) => rectanglesOverlap(item, candidate))
    .map((item) => item.id);

  return {
    valid: !outOfBounds && conflicts.length === 0,
    outOfBounds,
    conflicts,
  };
}

export function commitPose(items, candidate) {
  return items.map((item) => item.id === candidate.id ? { ...item, ...candidate } : item);
}

function rectanglesOverlap(left, right) {
  return left.x < right.x + right.width
    && left.x + left.width > right.x
    && left.y < right.y + right.height
    && left.y + left.height > right.y;
}
