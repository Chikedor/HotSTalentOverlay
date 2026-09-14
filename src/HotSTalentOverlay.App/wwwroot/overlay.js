const tiers = [1, 4, 7, 10, 13, 16, 20];
const slots = document.querySelector('#slots');
const hero = document.querySelector('#hero');
const fallbackStyle = {
  accentColor: '#b9a8ff', borderColor: '#7869b3', backgroundColor: '#11101c', textColor: '#ffffff',
  borderWidth: 2, borderRadius: 10, borderStyle: 'solid', iconSize: 84, gap: 10,
  showHero: true, showLevels: true, showTalentNames: false,
  entryAnimation: 'slide', idleAnimation: 'none', animationSpeed: 100,
};
let activeStyle = fallbackStyle;
let initialized = false;

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
  root.setProperty('--idle-duration', `${2.8 * 100 / activeStyle.animationSpeed}s`);
  hero.hidden = !activeStyle.showHero;
  for (const slot of slots.children) {
    slot.querySelector('.level').hidden = !activeStyle.showLevels;
    slot.querySelector('.talent-name').hidden = !activeStyle.showTalentNames;
    slot.classList.remove('idle-breathe', 'idle-glow', 'idle-float');
    if (activeStyle.idleAnimation !== 'none') slot.classList.add(`idle-${activeStyle.idleAnimation}`);
  }
}

function render(state) {
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
      if (initialized && nextId && !selectedIds.has(previousId) && activeStyle.entryAnimation !== 'none') {
        void slot.offsetWidth;
        slot.classList.add(`entry-${activeStyle.entryAnimation}`);
      }
    }
  }
  initialized = true;
}

window.addEventListener('message', event => {
  if (event.origin === location.origin && event.data?.type === 'overlay-style-preview') applyStyle(event.data.style);
});

fetch('/api/status').then(response => response.json()).then(render);
const events = new EventSource('/events');
events.onmessage = event => render(JSON.parse(event.data));
