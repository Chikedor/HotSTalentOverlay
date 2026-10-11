const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');

// Browser primitives only: rendering and transition decisions run the production script.
function harness(search = '?preview=1') {
  class Element {
    constructor() {
      this.dataset = {}; this.children = []; this.parts = {};
      const classes = new Set();
      this.classList = { add: (...names) => names.forEach(n => classes.add(n)), remove: (...names) => names.forEach(n => classes.delete(n)), contains: n => classes.has(n), toggle: (n, on) => on ? classes.add(n) : classes.delete(n) };
    }
    set innerHTML(_) { for (const name of ['.icon', '.level', '.talent-name']) this.parts[name] = new Element(); }
    querySelector(name) { return this.parts[name]; }
    append(item) { this.children.push(item); }
    replaceChildren() { this.children = []; }
    addEventListener() {}
  }
  const slots = new Element(), hero = new Element(), overlay = new Element();
  const properties = new Map(), timers = new Map(), listeners = {};
  let onStatus, id = 0;
  const context = { document: {
    documentElement: { style: { setProperty: (k, v) => properties.set(k, v) } },
    createElement: () => new Element(),
    querySelector: q => ({ '#slots': slots, '#hero': hero, '#overlay': overlay }[q] || slots.children.find(s => q === `.slot[data-level="${s.dataset.level}"]`)),
  }, window: { addEventListener: (type, fn) => listeners[type] = fn },
  location: { search, origin: 'http://localhost' }, URLSearchParams, Image: Element,
  watchStatus: fn => onStatus = fn,
  setTimeout: (fn, ms) => { timers.set(++id, { fn, ms }); return id; }, clearTimeout: n => timers.delete(n),
  };
  vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../../src/HotSTalentOverlay.App/wwwroot/overlay.js'), 'utf8'), context);
  return { slots, overlay, properties, timers, feed: state => onStatus(state),
    message: data => listeners.message({ origin: 'http://localhost', data }),
    advance: () => { const [key, timer] = timers.entries().next().value; timers.delete(key); timer.fn(); },
  };
}
const empty = { matchId: '', talents: [], overlayStyle: { showEmptySlots: false, showHero: false }, uiLanguage: 'es' };
const playing = { ...empty, matchId: 'game-a', heroName: 'Valla', talents: [{ level: 1, talentTreeId: 'VallaTalent', name: 'Talent', iconUrl: '/assets/talent.png' }] };

test('live preview never invents chosen slots after a match', () => {
  const h = harness(); h.feed(empty);
  assert.equal(h.slots.children.filter(s => s.querySelector('.icon').classList.contains('chosen')).length, 0);
  assert.ok(h.slots.children.every(s => s.classList.contains('empty-slot-hidden')));
});
test('match end fades for an independent default duration then hides empty slots', () => {
  const h = harness(''); h.feed(playing); h.feed(empty);
  assert.equal(h.overlay.dataset.exitAnimation, 'fade');
  assert.equal(h.properties.get('--exit-duration'), '450ms');
  assert.equal([...h.timers.values()][0].ms, 450);
  assert.equal(h.slots.children[0].dataset.talentId, 'VallaTalent');
  h.advance();
  assert.ok(h.slots.children.every(s => s.classList.contains('empty-slot-hidden')));
  assert.equal(h.overlay.classList.contains('match-leaving'), false);
});
test('none clears immediately without an exit timer', () => {
  const h = harness(''); const style = { ...empty.overlayStyle, exitAnimation: 'none' };
  h.feed({ ...playing, overlayStyle: style }); h.feed({ ...empty, overlayStyle: style });
  assert.equal(h.timers.size, 0);
  assert.equal(h.slots.children[0].dataset.talentId, '');
});
test('exit duration does not depend on appearance speed', () => {
  const h = harness(''); const style = { ...empty.overlayStyle, exitAnimation: 'left', exitDurationMs: 1100, animationSpeed: 200 };
  h.feed({ ...playing, overlayStyle: style }); h.feed({ ...empty, overlayStyle: style });
  assert.equal(h.overlay.dataset.exitAnimation, 'left');
  assert.equal([...h.timers.values()][0].ms, 1100);
});
test('new match arriving during exit replaces the pending clear safely', () => {
  const h = harness(''); h.feed(playing); h.feed(empty);
  h.feed({ ...playing, matchId: 'game-b' }); h.advance();
  assert.equal(h.slots.children[0].dataset.talentId, 'VallaTalent');
  assert.equal(h.overlay.classList.contains('match-leaving'), false);
});
test('exit preview restores live talents without mutating incoming match data', () => {
  const h = harness(); h.feed(playing);
  h.message({ type: 'overlay-exit-preview' });
  assert.equal(h.overlay.classList.contains('match-leaving'), true);
  h.advance(); assert.equal(h.slots.children[0].dataset.talentId, '');
  h.advance(); assert.equal(h.slots.children[0].dataset.talentId, 'VallaTalent');
  assert.equal(playing.talents.length, 1);
});
test('style changes during exit preview survive restoration', () => {
  const h = harness(); h.feed(playing);
  h.message({ type: 'overlay-exit-preview' });
  h.message({ type: 'overlay-style-preview', style: { ...empty.overlayStyle, iconSize: 120 } });
  h.feed({ ...playing, overlayStyle: { ...empty.overlayStyle, iconSize: 120 } });
  h.advance(); h.advance();
  assert.equal(h.properties.get('--icon-size'), '120px');
});
test('status updates during match exit do not restart its countdown', () => {
  const h = harness(''); h.feed(playing); h.feed(empty);
  const timer = [...h.timers.values()][0];
  h.feed({ ...empty, updatedAt: 'later', overlayStyle: { ...empty.overlayStyle, iconSize: 120 } });
  assert.equal([...h.timers.values()][0], timer);
  h.advance();
  assert.equal(h.properties.get('--icon-size'), '120px');
});
