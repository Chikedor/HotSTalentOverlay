const tiers = [1, 4, 7, 10, 13, 16, 20];
const slots = document.querySelector('#slots');

for (const level of tiers) {
  const slot = document.createElement('div');
  slot.className = 'slot';
  slot.dataset.level = level;
  slot.innerHTML = `<div class="icon"></div><div class="level">${level}</div>`;
  slots.append(slot);
}

function render(state) {
  document.querySelector('#hero').textContent = state.heroName || '';
  for (const level of tiers) {
    const talent = (state.talents || []).find(x => x.level === level);
    const icon = document.querySelector(`.slot[data-level="${level}"] .icon`);
    icon.classList.toggle('chosen', Boolean(talent));
    icon.title = talent?.name || `Talento de nivel ${level}`;
    icon.replaceChildren();
    if (talent?.iconUrl) {
      const image = new Image();
      image.src = `${talent.iconUrl}?b=${state.catalogBuild || 0}`;
      image.alt = talent.name;
      icon.append(image);
    }
  }
}

fetch('/api/status').then(x => x.json()).then(render);
const events = new EventSource('/events');
events.onmessage = event => render(JSON.parse(event.data));

