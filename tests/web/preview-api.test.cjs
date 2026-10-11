const { test } = require('node:test');
const assert = require('node:assert/strict');
test('preview sample supplies real icons without changing the live match', { skip: !process.env.OVERLAY_TEST_URL }, async () => {
  const url = process.env.OVERLAY_TEST_URL;
  const before = await fetch(`${url}/api/status`).then(r => r.json());
  const response = await fetch(`${url}/api/preview`);
  assert.equal(response.status, 200);
  const preview = await response.json();
  assert.equal(preview.talents.length, 7);
  assert.ok(preview.talents.every(t => t.iconUrl.startsWith('/assets/talents/')));
  const after = await fetch(`${url}/api/status`).then(r => r.json());
  assert.equal(after.matchId, before.matchId);
  assert.deepEqual(after.talents, before.talents);
});
