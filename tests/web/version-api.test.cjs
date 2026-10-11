const { test } = require('node:test');
const assert = require('node:assert/strict');
test('local status reports the installed version independently of GitHub', { skip: !process.env.OVERLAY_TEST_URL }, async () => {
  const response = await fetch(`${process.env.OVERLAY_TEST_URL}/api/status`, { signal: AbortSignal.timeout(1500) });
  assert.equal(response.status, 200);
  const status = await response.json();
  assert.match(status.appVersion || '', /^\d+\.\d+\.\d+$/);
  if (process.env.OVERLAY_TEST_VERSION) assert.equal(status.appVersion, process.env.OVERLAY_TEST_VERSION);
});
