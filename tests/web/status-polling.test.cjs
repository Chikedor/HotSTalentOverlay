const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const webRoot = path.resolve(__dirname, '../../src/HotSTalentOverlay.App/wwwroot');
const settle = async () => { for (let i = 0; i < 12; i++) await Promise.resolve(); };

function environment(fetch) {
  const timers = new Map();
  const listeners = {};
  let id = 0;
  const context = vm.createContext({
    fetch, AbortController, console,
    setTimeout: (callback, delay) => { timers.set(++id, { callback, delay }); return id; },
    clearTimeout: id => timers.delete(id),
    window: { addEventListener: (name, callback) => { listeners[name] = callback; } },
  });
  const script = path.join(webRoot, 'status-polling.js');
  if (fs.existsSync(script)) vm.runInContext(fs.readFileSync(script, 'utf8'), context);
  return { context, timers, listeners, run(delay) {
    const entry = [...timers].find(([, timer]) => timer.delay === delay);
    assert.ok(entry, `Expected a scheduled timer for ${delay}ms`);
    timers.delete(entry[0]); entry[1].callback();
  } };
}

function start(env, callback = () => {}) {
  assert.equal(typeof env.context.watchStatus, 'function', 'A finite status polling client is required');
  env.context.watchStatus(callback);
}

test('standalone overlay does not reserve a permanent HTTP connection', () => {
  let streams = 0;
  const env = environment(() => new Promise(() => {}));
  const element = () => ({ dataset: {}, addEventListener() {}, append() {}, children: [] });
  Object.assign(env.context, {
    document: { querySelector: element, createElement: element },
    EventSource: function () { streams++; },
    location: { search: '', origin: 'http://localhost' },
  });
  vm.runInContext(fs.readFileSync(path.join(webRoot, 'overlay.js'), 'utf8'), env.context);
  assert.equal(streams, 0, 'Overlay must release connections instead of keeping SSE open');
});

test('slow requests never overlap; changed states are delivered without cached responses', async () => {
  let resolve;
  const calls = [];
  const received = [];
  const env = environment((url, options) => {
    calls.push({ url, options });
    return new Promise(done => { resolve = done; });
  });
  start(env, state => received.push(state));
  assert.equal(calls.length, 1);
  assert.equal(calls[0].url, '/api/status');
  assert.equal(calls[0].options.cache, 'no-store');
  assert.ok(![...env.timers.values()].some(timer => timer.delay === 1000));
  resolve({ ok: true, json: async () => ({ matchId: 'live', talents: [1] }) });
  await settle();
  assert.equal(received.length, 1);
  env.run(1000);
  assert.equal(calls.length, 2);
  resolve({ ok: true, json: async () => ({ matchId: 'live', talents: [1, 4] }) });
  await settle();
  assert.deepEqual(received[1].talents, [1, 4]);
});

test('identical snapshots do not restart rendering or entry animations', async () => {
  const env = environment(async () => ({ ok: true, json: async () => ({ updatedAt: 'same', talents: [1] }) }));
  let renders = 0;
  start(env, () => renders++);
  await settle();
  env.run(1000); await settle();
  assert.equal(renders, 1);
});

test('temporary server errors preserve the displayed state and retry', async () => {
  let request = 0;
  const received = [];
  const env = environment(async () => {
    if (++request === 1) throw new Error('App restarting');
    if (request === 2) return { ok: false, status: 503 };
    return { ok: true, json: async () => ({ matchId: 'reconnected' }) };
  });
  start(env, state => received.push(state));
  await settle(); env.run(1000); await settle();
  assert.equal(received.length, 0);
  env.run(1000); await settle();
  assert.equal(received[0].matchId, 'reconnected');
});

test('hung requests time out and release their connection before retrying', async () => {
  let signal;
  const env = environment((_, options) => {
    signal = options.signal;
    return new Promise((_, reject) => signal.addEventListener('abort', () => reject(new Error('aborted'))));
  });
  start(env);
  env.run(5000); await settle();
  assert.equal(signal.aborted, true);
  assert.ok([...env.timers.values()].some(timer => timer.delay === 1000));
});

test('navigation aborts the pending request; back-forward restoration restarts polling', async () => {
  const signals = [];
  const env = environment((_, options) => {
    signals.push(options.signal);
    return new Promise((_, reject) => options.signal.addEventListener('abort', () => reject(new Error('aborted'))));
  });
  start(env);
  env.listeners.pagehide(); await settle();
  assert.equal(signals[0].aborted, true);
  assert.equal(env.timers.size, 0);
  env.listeners.pageshow(); await settle();
  assert.equal(signals.length, 2);
  env.listeners.pageshow();
  assert.equal(signals.length, 2, 'Restoration must not create a second polling loop');
});

test('dashboard displays local status without waiting for a stalled GitHub update check', async () => {
  const rendered = [];
  const env = environment(async url => {
    if (url === '/api/update') return new Promise(() => {});
    return { json: async () => url === '/api/config'
      ? { setupCompleted: true, uiLanguage: 'es', overlayStyle: {} }
      : { heroName: 'Greymane', talents: [] } };
  });
  Object.assign(env.context, {
    defaultStyle: {},
    applyLanguage() {}, fillGeneralForm() {}, fillStyleForm() {}, setStyleDirty() {},
    render: state => rendered.push(state), renderUpdate() {}, showWizardStep() {},
  });
  // Exercise the production initializer with only its UI callbacks stubbed.
  const initializer = fs.readFileSync(path.join(webRoot, 'app.js'), 'utf8')
    .match(/async function load\(\) \{[\s\S]*?\n\}/)[0];
  vm.runInContext(initializer, env.context);
  env.context.load();
  await settle();
  assert.equal(rendered[0]?.heroName, 'Greymane', 'Local match must render even when GitHub never responds');
});
