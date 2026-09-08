import test from 'node:test';
import assert from 'node:assert/strict';

import { cloneItems, commitPose, evaluatePlacement, getDraggedOrigin, rotatePose } from '../src/inventory-model.js';
import { INITIAL_ITEMS } from '../src/inventory-fixture.js';

test('the fixed scenario starts with every item in a legal position', () => {
  const items = cloneItems(INITIAL_ITEMS);
  for (const item of items) {
    assert.deepEqual(evaluatePlacement(items, item), {
      valid: true,
      outOfBounds: false,
      conflicts: [],
    }, item.id);
  }
});

test('rotation is clockwise around the pressed one-by-one cell', () => {
  const item = { id: 'item', container: 'backpack', x: 2, y: 3, width: 1, height: 3, rotation: 0 };
  assert.deepEqual(rotatePose(item, { column: 0, row: 2 }), {
    id: 'item', container: 'backpack', x: 2, y: 5, width: 3, height: 1, rotation: 90,
  });
});

test('every item shape advances exactly ninety degrees and returns after four presses', () => {
  const shapes = [[1, 1], [1, 2], [1, 3], [2, 2], [2, 3]];
  for (const [width, height] of shapes) {
    const original = { id: `${width}x${height}`, container: 'backpack', x: 10, y: 10, width, height, rotation: 0 };
    const fixedGlobalCell = { x: original.x + width - 1, y: original.y + height - 1 };
    let pose = original;
    let pivot = { column: width - 1, row: height - 1 };

    for (let step = 1; step <= 4; step += 1) {
      pose = rotatePose(pose, pivot);
      pivot = { column: pose.width - 1 - pivot.row, row: pivot.column };
      assert.equal(pose.rotation, (step * 90) % 360, `${width}x${height} step ${step}`);
      assert.deepEqual(
        { x: pose.x + pivot.column, y: pose.y + pivot.row },
        fixedGlobalCell,
        `${width}x${height} pivot step ${step}`,
      );
    }

    assert.deepEqual(pose, original, `${width}x${height} full rotation`);
  }
});

test('out-of-bounds pivot rotations are reported without changing committed items', () => {
  const items = [{ id: 'item', container: 'backpack', x: 0, y: 0, width: 1, height: 3, rotation: 0 }];
  const candidate = rotatePose(items[0]);
  assert.equal(evaluatePlacement(items, candidate).valid, false);
  assert.equal(evaluatePlacement(items, candidate).outOfBounds, true);
  assert.equal(items[0].width, 1);
});

test('drag origin preserves the exact pointer-to-item offset', () => {
  const origin = getDraggedOrigin({ x: 218, y: 464 }, { x: 17, y: 39 });
  assert.deepEqual(origin, { x: 201, y: 425 });
  assert.deepEqual(
    { x: 218 - origin.x, y: 464 - origin.y },
    { x: 17, y: 39 },
  );
});

test('colliding items are identified for red outline feedback', () => {
  const items = [
    { id: 'moving', container: 'backpack', x: 0, y: 0, width: 1, height: 2 },
    { id: 'blocker', container: 'backpack', x: 2, y: 1, width: 2, height: 2 },
  ];
  const candidate = { ...items[0], x: 2, y: 1 };
  assert.deepEqual(evaluatePlacement(items, candidate).conflicts, ['blocker']);
});

test('an item can commit across containers only at a legal location', () => {
  const items = [{ id: 'moving', container: 'backpack', x: 0, y: 0, width: 2, height: 3, rotation: 0 }];
  const candidate = { ...items[0], container: 'chest', x: 2, y: 2 };
  assert.equal(evaluatePlacement(items, candidate).valid, true);
  const committed = commitPose(items, candidate);
  assert.equal(committed[0].container, 'chest');
  assert.equal(items[0].container, 'backpack');
});

test('the four-by-five chest rejects a pose that extends past its edge', () => {
  const candidate = { id: 'moving', container: 'chest', x: 3, y: 3, width: 2, height: 2 };
  assert.deepEqual(evaluatePlacement([], candidate), {
    valid: false,
    outOfBounds: true,
    conflicts: [],
  });
});
