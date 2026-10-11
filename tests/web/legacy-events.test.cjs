const { test } = require('node:test');
const assert = require('node:assert/strict');

// Run against an isolated packaged app using OVERLAY_TEST_URL=http://127.0.0.1:3988.
test('old dashboard tabs receive a complete event response without holding a connection',
  { skip: !process.env.OVERLAY_TEST_URL }, async () => {
    const response = await fetch(`${process.env.OVERLAY_TEST_URL}/events`, { signal: AbortSignal.timeout(1500) });
    assert.equal(response.status, 200);
    assert.match(response.headers.get('content-type'), /text\/event-stream/);
    let text;
    try { text = await response.text(); }
    catch { assert.fail('Legacy event response must finish instead of blocking image downloads'); }
    assert.match(text, /retry: 1000\n/);
    const line = text.split('\n').find(line => line.startsWith('data: '));
    assert.ok(line, 'Legacy tabs still need the current state');
    const state = JSON.parse(line.slice(6));
    assert.ok(Array.isArray(state.talents));
  });
