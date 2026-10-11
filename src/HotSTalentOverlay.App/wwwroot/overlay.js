const tiers = [1, 4, 7, 10, 13, 16, 20];
const slots = document.querySelector('#slots');
const hero = document.querySelector('#hero');
const overlay = document.querySelector('#overlay');
const fallbackStyle = {
  accentColor: '#b9a8ff', borderColor: '#7869b3', backgroundColor: '#11101c', textColor: '#ffffff',
  borderWidth: 2, borderRadius: 10, borderStyle: 'solid', iconSize: 84, gap: 10,
  showHero: true, showLevels: true, showTalentNames: false, showEmptySlots: true,
  entryAnimation: 'slide', animationSpeed: 100, exitAnimation: 'fade', exitDurationMs: 450,
  nameFontFamily: 'segoe', nameFontSize: 12, nameBold: true, nameLineHeight: 130, nameShadow: true,
};
const nameFonts = {
  segoe: '"Segoe UI", sans-serif', arial: 'Arial, sans-serif', verdana: 'Verdana, sans-serif',
  tahoma: 'Tahoma, sans-serif', trebuchet: '"Trebuchet MS", sans-serif', georgia: 'Georgia, serif',
};
let activeStyle = fallbackStyle;
let initialized = false;
let currentMatchId = '';
let transitionTimer;
let lastState;
let previewMode = 'live';
let previewSample;
let previewTimer;
let testingExit = false;
let pendingState;

for (const level of tiers) {
  const slot = document.createElement('div');
  slot.className = 'slot';
  slot.dataset.level = level;
  slot.innerHTML = `<div class="icon"></div><div class="level">${level}</div><div class="talent-name" hidden></div>`;
  slot.addEventListener('animationend', event => {
    if (event.target === slot) slot.classList.remove('entry-fade', 'entry-slide', 'entry-pop', 'entry-flip');
  });
  slots.append(slot);
}

function applyStyle(style) {
  activeStyle = { ...fallbackStyle, ...(style || {}) };
  const root = document.documentElement.style;
  root.setProperty('--icon-size', `${activeStyle.iconSize}px`);
  root.setProperty('--gap', `${activeStyle.gap}px`);
  root.setProperty('--accent-color', activeStyle.accentColor);
  root.setProperty('--border-color', activeStyle.borderColor);
  root.setProperty('--background-color', activeStyle.backgroundColor);
  root.setProperty('--text-color', activeStyle.textColor);
  root.setProperty('--border-width', `${activeStyle.borderWidth}px`);
  root.setProperty('--border-radius', `${activeStyle.borderRadius}px`);
  root.setProperty('--border-style', activeStyle.borderStyle);
  root.setProperty('--entry-duration', `${0.45 * 100 / activeStyle.animationSpeed}s`);
  root.setProperty('--exit-duration', `${activeStyle.exitDurationMs}ms`);
  overlay.dataset.exitAnimation = activeStyle.exitAnimation;
  root.setProperty('--name-font-family', nameFonts[activeStyle.nameFontFamily] || nameFonts.segoe);
  root.setProperty('--name-font-size', `${activeStyle.nameFontSize}px`);
  root.setProperty('--name-font-weight', activeStyle.nameBold ? '700' : '400');
  root.setProperty('--name-line-height', String(activeStyle.nameLineHeight / 100));
  root.setProperty('--name-shadow', activeStyle.nameShadow ? '0 2px 4px #000, 0 0 5px #000' : 'none');
  hero.hidden = !activeStyle.showHero;
  for (const slot of slots.children) {
    slot.querySelector('.level').hidden = !activeStyle.showLevels;
    slot.querySelector('.talent-name').hidden = !activeStyle.showTalentNames;
    slot.classList.toggle('empty-slot-hidden', !activeStyle.showEmptySlots && !slot.dataset.talentId);
  }
}

function renderState(state, suppressEntry = false) {
  applyStyle(state.overlayStyle);
  document.documentElement.lang = state.uiLanguage === 'en' ? 'en' : 'es';
  hero.textContent = state.heroName || '';
  const selectedIds = new Set((state.talents || []).map(talent => talent.talentTreeId));

  for (const level of tiers) {
    const talent = (state.talents || []).find(item => item.level === level);
    const slot = document.querySelector(`.slot[data-level="${level}"]`);
    const icon = slot.querySelector('.icon');
    const name = slot.querySelector('.talent-name');
    const previousId = slot.dataset.talentId || '';
    const nextId = talent?.talentTreeId || '';

    icon.classList.toggle('chosen', Boolean(talent));
    icon.title = talent?.name || (state.uiLanguage === 'en' ? `Level ${level} talent` : `Talento de nivel ${level}`);
    name.textContent = talent?.name || '';
    if (nextId !== previousId) {
      icon.replaceChildren();
      slot.dataset.talentId = nextId;
      if (talent?.iconUrl) {
        const image = new Image();
        image.src = `${talent.iconUrl}?b=${state.catalogBuild || 0}`;
        image.alt = talent.name;
        icon.append(image);
      }
      slot.classList.remove('entry-fade', 'entry-slide', 'entry-pop', 'entry-flip');
      if (initialized && !suppressEntry && nextId && !selectedIds.has(previousId) && activeStyle.entryAnimation !== 'none') {
        void slot.offsetWidth;
        slot.classList.add(`entry-${activeStyle.entryAnimation}`);
      }
    }
    slot.classList.toggle('empty-slot-hidden', !activeStyle.showEmptySlots && !nextId);
  }
  initialized = true;
}

function replayEntryAnimation() {
  const animation = activeStyle.entryAnimation;
  if (animation === 'none') return;
  const chosen = [...slots.children].filter(slot => slot.dataset.talentId);
  const targets = chosen.length ? chosen : [...slots.children];
  for (const slot of targets) slot.classList.remove('entry-fade', 'entry-slide', 'entry-pop', 'entry-flip');
  void slots.offsetWidth;
  targets.forEach((slot, index) => setTimeout(() => slot.classList.add(`entry-${animation}`), index * 45));
}

function previewState(state) {
  if (new URLSearchParams(location.search).has('preview')) {
    if (previewMode === 'empty') return { ...state, matchId: '', heroName: '', talents: [] };
    if (previewMode === 'filled' && previewSample) return { ...state, ...previewSample, overlayStyle: state.overlayStyle, uiLanguage: state.uiLanguage };
  }
  return state;
}

function render(state) {
  const previousStyle = lastState?.overlayStyle;
  lastState = state;
  if (testingExit) {
    if (JSON.stringify(state.overlayStyle) !== JSON.stringify(previousStyle)) applyStyle(state.overlayStyle);
    return;
  }
  state = previewState(state);
  const nextMatchId = state.matchId || '';
  if (pendingState && (pendingState.matchId || '') === nextMatchId) {
    pendingState = state;
    return;
  }
  const isNewMatch = initialized && currentMatchId && nextMatchId !== currentMatchId;
  clearTimeout(transitionTimer);
  pendingState = undefined;
  overlay.classList.remove('match-leaving', 'match-arriving');
  if (!isNewMatch) {
    renderState(state);
    currentMatchId = nextMatchId;
    return;
  }

  if (activeStyle.exitAnimation === 'none') {
    renderState(state, true);
    currentMatchId = nextMatchId;
    return;
  }
  void overlay.offsetWidth;
  overlay.classList.add('match-leaving');
  pendingState = state;
  const delay = activeStyle.exitDurationMs;
  transitionTimer = setTimeout(() => {
    renderState(pendingState, true);
    pendingState = undefined;
    currentMatchId = nextMatchId;
    overlay.classList.remove('match-leaving');
    if (!nextMatchId) return;
    void overlay.offsetWidth;
    overlay.classList.add('match-arriving');
    transitionTimer = setTimeout(() => overlay.classList.remove('match-arriving'), 500 * 100 / activeStyle.animationSpeed);
  }, delay);
}

function replayExitAnimation(sample) {
  if (!lastState || activeStyle.exitAnimation === 'none') return;
  clearTimeout(transitionTimer);
  pendingState = undefined;
  clearTimeout(previewTimer);
  testingExit = true;
  const style = activeStyle;
  const shown = previewState(lastState);
  renderState({ ...shown, ...((shown.talents || []).length ? {} : sample), overlayStyle: style }, true);
  overlay.classList.remove('match-leaving', 'match-arriving');
  void overlay.offsetWidth;
  overlay.classList.add('match-leaving');
  previewTimer = setTimeout(() => {
    renderState({ ...lastState, heroName: '', talents: [], overlayStyle: activeStyle }, true);
    overlay.classList.remove('match-leaving');
    previewTimer = setTimeout(() => {
      testingExit = false;
      const restored = previewState(lastState);
      renderState({ ...restored, overlayStyle: activeStyle }, true);
      currentMatchId = restored.matchId || '';
    }, 300);
  }, style.exitDurationMs);
}

window.addEventListener('message', event => {
  if (event.origin !== location.origin) return;
  if (event.data?.type === 'overlay-style-preview') applyStyle(event.data.style);
  if (event.data?.type === 'overlay-entry-preview') replayEntryAnimation();
  if (event.data?.type === 'overlay-exit-preview' && new URLSearchParams(location.search).has('preview')) replayExitAnimation(event.data.sample);
  if (event.data?.type === 'overlay-preview-mode') {
    clearTimeout(previewTimer);
    testingExit = false;
    previewSample = event.data.sample;
    previewMode = ['live', 'empty', 'filled'].includes(event.data.mode) ? event.data.mode : 'live';
    if (lastState) render(lastState);
  }
});

watchStatus(render);
