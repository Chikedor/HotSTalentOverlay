const tiers = [1, 4, 7, 10, 13, 16, 20];
const $ = selector => document.querySelector(selector);
let state;

function render(next) {
  state = next;
  $('#health').textContent = next.hotsDetected && next.catalogReady && next.watcherRunning ? 'Todo listo' : 'Requiere atención';
  $('#health').classList.toggle('ok', next.hotsDetected && next.catalogReady && next.watcherRunning);
  $('#game').textContent = next.hotsDetected ? `Build ${next.gameBuild}` : 'No detectado';
  $('#path').textContent = next.hotsPath || 'Configura la ruta abajo';
  $('#catalog').textContent = next.catalogStatus;
  $('#catalog-detail').textContent = next.catalogReady ? `${next.heroCount} héroes · ${next.talentCount} talentos · build ${next.catalogBuild}` : '';
  $('#watcher').textContent = next.watcherRunning ? 'Activo' : 'Detenido';
  $('#replay-path').textContent = next.replayPath;
  $('#hero').textContent = next.heroName || 'Esperando una partida';
  $('#player').textContent = next.player ? `${next.player} · ${next.talents.length}/7 talentos` : 'Los talentos aparecerán aquí al elegirlos.';
  $('#error').hidden = !next.lastError;
  $('#error').textContent = next.lastError;
  $('#talents').replaceChildren(...tiers.map(level => {
    const found = (next.talents || []).find(x => x.level === level);
    const box = document.createElement('div'); box.className = 'talent';
    box.innerHTML = found?.iconUrl ? `<img src="${found.iconUrl}?b=${next.catalogBuild}" title="${escapeHtml(found.name)}"><span>${level}</span>` : `<span class="empty"></span><span>${level}</span>`;
    return box;
  }));
}

function escapeHtml(value) { const div = document.createElement('div'); div.textContent = value; return div.innerHTML; }
async function post(url, body) {
  const response = await fetch(url, { method: 'POST', headers: body ? { 'Content-Type': 'application/json' } : {}, body: body ? JSON.stringify(body) : undefined });
  if (!response.ok) throw new Error((await response.json().catch(() => ({}))).error || `HTTP ${response.status}`);
}

async function load() {
  render(await fetch('/api/status').then(x => x.json()));
  const config = await fetch('/api/config').then(x => x.json());
  for (const key of ['battleTag', 'hotsPath', 'replayPath', 'obsPort', 'locale']) $(`#${key}`).value = config[key] ?? '';
  $('#obs-url').textContent = `${location.protocol}//${location.hostname}:${config.obsPort}/overlay.html`;
}

$('#config-form').addEventListener('submit', async event => {
  event.preventDefault();
  const config = Object.fromEntries(['battleTag', 'hotsPath', 'replayPath', 'locale'].map(key => [key, $(`#${key}`).value.trim()]));
  config.obsPort = Number($('#obsPort').value);
  try { await post('/api/config', config); $('#message').textContent = config.obsPort === Number(location.port) ? 'Guardado.' : 'Guardado. Reinicia la app para aplicar el puerto.'; }
  catch (error) { $('#message').textContent = error.message; }
});
$('#copy').onclick = async () => { await navigator.clipboard.writeText($('#obs-url').textContent); $('#message').textContent = 'URL copiada.'; };
$('#demo').onclick = () => post('/api/demo');
$('#regenerate').onclick = () => post('/api/catalog/regenerate');
$('#reset').onclick = () => post('/api/match/reset');

load();
const events = new EventSource('/events');
events.onmessage = event => render(JSON.parse(event.data));
