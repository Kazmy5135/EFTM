import test from 'node:test';
import assert from 'node:assert/strict';

import { cloneItems, commitPose, evaluatePlacement, rotatePose } from '../src/inventory-model.js';
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

test('rotation is clockwise and preserves the anchor cell', () => {
  const item = { id: 'item', container: 'backpack', x: 2, y: 3, width: 1, height: 3, rotation: 0 };
  assert.deepEqual(rotatePose(item), {
    id: 'item', container: 'backpack', x: 2, y: 3, width: 3, height: 1, rotation: 90,
  });
});

test('out-of-bounds rotations are reported without changing committed items', () => {
  const items = [{ id: 'item', container: 'backpack', x: 5, y: 0, width: 1, height: 3, rotation: 0 }];
  const candidate = rotatePose(items[0]);
  assert.equal(evaluatePlacement(items, candidate).valid, false);
  assert.equal(evaluatePlacement(items, candidate).outOfBounds, true);
  assert.equal(items[0].width, 1);
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
