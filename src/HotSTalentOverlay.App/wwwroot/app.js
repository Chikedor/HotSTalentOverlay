const tiers = [1, 4, 7, 10, 13, 16, 20];
const $ = selector => document.querySelector(selector);
const defaultStyle = {
  accentColor: '#b9a8ff', borderColor: '#7869b3', backgroundColor: '#11101c', textColor: '#ffffff',
  borderWidth: 2, borderRadius: 10, borderStyle: 'solid', iconSize: 84, gap: 10,
  showHero: true, showLevels: true, showTalentNames: false, showEmptySlots: true,
  entryAnimation: 'slide', animationSpeed: 100,
};
const presets = {
  classic: { ...defaultStyle },
  clean: { ...defaultStyle, accentColor: '#ffffff', borderColor: '#ffffff', backgroundColor: '#090a10', borderWidth: 1, borderRadius: 6, borderStyle: 'solid', gap: 8, entryAnimation: 'fade' },
  neon: { ...defaultStyle, accentColor: '#00f5c4', borderColor: '#7b61ff', backgroundColor: '#080b18', borderWidth: 2, borderRadius: 12, gap: 10, entryAnimation: 'pop' },
  compact: { ...defaultStyle, iconSize: 58, gap: 5, borderWidth: 1, borderRadius: 7, showHero: false, showTalentNames: false, entryAnimation: 'slide' },
};
const i18n = {
  es: {
    skip: 'Saltar al contenido', localControl: 'CONTROL LOCAL', tagline: 'Tus talentos en OBS, automáticamente y sin salir de tu PC.', language: 'Idioma', interfaceLanguage: 'Idioma de la interfaz', status: 'Estado', overlayPreview: 'Preview del overlay', setupTitle: 'Configuración inicial', firstSetup: 'PRIMERA CONFIGURACIÓN', welcomeTitle: 'Vamos a dejarlo preparado', welcomeText: 'Solo tardaremos un minuto. Primero, elige el idioma de la interfaz.', gameCheck: 'COMPROBACIÓN DEL JUEGO', gameCheckTitle: 'Buscando Heroes of the Storm', gameCheckText: 'Comprobamos automáticamente el juego y la carpeta de partidas.', gameInstalled: 'Juego instalado', matchesFolder: 'Carpeta de partidas', detected: 'Detectado', notFound: 'No encontrado', continue: 'Continuar', obsSetupTitle: 'Añádelo a OBS', obsSetupText: 'En OBS crea una Fuente de navegador, pega esta URL y usa un lienzo transparente.', obsStep1: 'Añadir fuente', obsStep2: 'Elegir Navegador', obsStep3: 'Pegar la URL', finishSetup: 'Terminar configuración', rerunSetup: 'Repetir asistente', setupDone: 'Configuración terminada.',
    localCatalog: 'CATÁLOGO LOCAL', liveDetector: 'DETECTOR EN DIRECTO', currentMatch: 'PARTIDA ACTUAL', appearance: 'APARIENCIA',
    customizeOverlay: 'Personaliza tus talentos', restoreDefaults: 'Restaurar', appearanceHelp: 'Elige un estilo y ajústalo. Los cambios se guardan automáticamente.',
    chooseStyle: 'Elige un estilo', presetClassic: 'Clásico', presetClean: 'Limpio', presetNeon: 'Neón', presetCompact: 'Compacto', essentials: 'Lo esencial', moreOptions: 'Más opciones', moreOptionsHelp: 'Bordes, fondo, espaciado y velocidad',
    colors: 'Colores', accent: 'Acento', borderColor: 'Color de huecos vacíos', backgroundColor: 'Fondo del hueco', textColor: 'Texto', shapeSpacing: 'Forma y espaciado',
    borderStyle: 'Estilo del borde', solid: 'Sólido', double: 'Doble', dashed: 'Discontinuo', none: 'Ninguno', iconSize: 'Tamaño del icono',
    borderWidth: 'Grosor del borde', rounding: 'Redondeado', spacing: 'Separación', motion: 'Movimiento', entryAnimation: 'Al aparecer',
    fade: 'Fundido', slide: 'Deslizamiento', pop: 'Salto', flip: 'Giro', previewAnimation: 'Probar aparición', previewAnimationHelp: 'Comprueba el efecto sin guardar',
    animationSpeed: 'Velocidad de aparición', visibleInfo: 'Información visible', showHero: 'Nombre del héroe', showLevels: 'Niveles', showTalentNames: 'Nombres de talentos', showEmptySlots: 'Mostrar huecos sin elegir',
    saveAppearance: 'Guardar apariencia', appearanceSaved: 'Guardado automáticamente ✓', unsavedChanges: 'Guardando…', saveFailed: 'No se pudo guardar', livePreview: 'PREVIEW EN DIRECTO', obsResult: 'Así se verá en OBS', live: 'En vivo', previewMode: 'Estado de la preview', withTalents: 'Con talentos', withoutTalents: 'Sin talentos', loadDemo: 'Usar partida de demostración', demoHelp: 'Sustituye temporalmente la partida mostrada también en OBS.',
    copyUrl: 'Copiar URL', openPreview: 'Abrir overlay', resetMatch: 'Vaciar partida detectada', advanced: 'AVANZADO', connectionSettings: 'Juego y conexión',
    regenerate: 'Regenerar catálogo', battleTagPlaceholder: 'Nombre#1234', hotsPath: 'Ruta de HotS', accountsFolder: 'Carpeta Accounts', autoDetect: 'Se detecta automáticamente',
    obsPort: 'Puerto OBS', gameDataLanguage: 'Idioma de talentos', saveSettings: 'Guardar configuración', privacyFooter: '100 % local · Sin telemetría · Tus partidas no salen de este equipo',
    ready: 'Todo listo', attention: 'Requiere atención', searching: 'Buscando…', notDetected: 'No detectado', configurePath: 'Configura la ruta abajo', pending: 'Pendiente',
    active: 'Activo', stopped: 'Detenido', waitingMatch: 'Esperando una partida', waitingTalents: 'Los talentos aparecerán aquí al elegirlos.', talentsCount: '{count}/7 talentos',
    catalogDetail: '{heroes} héroes · {talents} talentos · build {build}', saved: 'Cambios guardados.', portRestart: 'Guardado. Reinicia la app para aplicar el nuevo puerto.',
    copied: 'URL copiada.', demoLoaded: 'Demo cargada.', catalogQueued: 'Regeneración iniciada.', matchReset: 'Partida reseteada.', defaultsPreview: 'Valores predeterminados cargados. Guarda para aplicarlos.',
    catalogReady: 'Listo', catalogExtracting: 'Extrayendo desde CASC…', catalogError: 'Error de extracción', catalogNoGame: 'HotS no detectado',
    updateAvailable: 'Nueva versión disponible', updateRestart: 'Actualizar y reiniciar', updating: 'Descargando actualización…', updateVersion: 'Versión {current} → {latest}',
  },
  en: {
    skip: 'Skip to content', localControl: 'LOCAL CONTROL', tagline: 'Your talents in OBS, automatically and entirely on your PC.', language: 'Language', interfaceLanguage: 'Interface language', status: 'Status', overlayPreview: 'Overlay preview', setupTitle: 'Initial setup', firstSetup: 'FIRST-TIME SETUP', welcomeTitle: 'Let’s get everything ready', welcomeText: 'This takes about a minute. First, choose the interface language.', gameCheck: 'GAME CHECK', gameCheckTitle: 'Finding Heroes of the Storm', gameCheckText: 'We automatically check the game and match folder.', gameInstalled: 'Game installed', matchesFolder: 'Match folder', detected: 'Detected', notFound: 'Not found', continue: 'Continue', obsSetupTitle: 'Add it to OBS', obsSetupText: 'In OBS create a Browser Source, paste this URL and use a transparent canvas.', obsStep1: 'Add source', obsStep2: 'Choose Browser', obsStep3: 'Paste the URL', finishSetup: 'Finish setup', rerunSetup: 'Run setup again', setupDone: 'Setup complete.',
    localCatalog: 'LOCAL CATALOG', liveDetector: 'LIVE DETECTOR', currentMatch: 'CURRENT MATCH', appearance: 'APPEARANCE',
    customizeOverlay: 'Customize your talents', restoreDefaults: 'Restore', appearanceHelp: 'Choose a style and adjust it. Changes save automatically.',
    chooseStyle: 'Choose a style', presetClassic: 'Classic', presetClean: 'Clean', presetNeon: 'Neon', presetCompact: 'Compact', essentials: 'Essentials', moreOptions: 'More options', moreOptionsHelp: 'Borders, background, spacing and speed',
    colors: 'Colors', accent: 'Accent', borderColor: 'Empty slot color', backgroundColor: 'Slot background', textColor: 'Text', shapeSpacing: 'Shape and spacing',
    borderStyle: 'Border style', solid: 'Solid', double: 'Double', dashed: 'Dashed', none: 'None', iconSize: 'Icon size', borderWidth: 'Border width',
    rounding: 'Corner radius', spacing: 'Spacing', motion: 'Motion', entryAnimation: 'On appearance', fade: 'Fade', slide: 'Slide',
    pop: 'Pop', flip: 'Flip', previewAnimation: 'Preview appearance', previewAnimationHelp: 'Check the effect without saving', animationSpeed: 'Appearance speed', visibleInfo: 'Visible information',
    showHero: 'Hero name', showLevels: 'Levels', showTalentNames: 'Talent names', showEmptySlots: 'Show unselected slots', saveAppearance: 'Save appearance', appearanceSaved: 'Saved automatically ✓', unsavedChanges: 'Saving…', saveFailed: 'Could not save', livePreview: 'LIVE PREVIEW',
    obsResult: 'How it will look in OBS', live: 'Live', previewMode: 'Preview state', withTalents: 'With talents', withoutTalents: 'Without talents', loadDemo: 'Use demo match', demoHelp: 'Temporarily replaces the match shown in OBS too.', copyUrl: 'Copy URL', openPreview: 'Open overlay', resetMatch: 'Clear detected match',
    advanced: 'ADVANCED', connectionSettings: 'Game and connection', regenerate: 'Regenerate catalog', battleTagPlaceholder: 'Name#1234', hotsPath: 'HotS path',
    accountsFolder: 'Accounts folder', autoDetect: 'Detected automatically', obsPort: 'OBS port', gameDataLanguage: 'Talent language', saveSettings: 'Save settings',
    privacyFooter: '100% local · No telemetry · Your matches never leave this computer', ready: 'Everything ready', attention: 'Needs attention', searching: 'Searching…',
    notDetected: 'Not detected', configurePath: 'Configure the path below', pending: 'Pending', active: 'Active', stopped: 'Stopped', waitingMatch: 'Waiting for a match',
    waitingTalents: 'Talents will appear here when selected.', talentsCount: '{count}/7 talents', catalogDetail: '{heroes} heroes · {talents} talents · build {build}',
    saved: 'Changes saved.', portRestart: 'Saved. Restart the app to apply the new port.', copied: 'URL copied.', demoLoaded: 'Demo loaded.',
    catalogQueued: 'Catalog regeneration started.', matchReset: 'Match reset.', defaultsPreview: 'Default values loaded. Save to apply them.',
    catalogReady: 'Ready', catalogExtracting: 'Extracting from CASC…', catalogError: 'Extraction error', catalogNoGame: 'HotS not detected',
    updateAvailable: 'New version available', updateRestart: 'Update and restart', updating: 'Downloading update…', updateVersion: 'Version {current} → {latest}',
  },
};

let appState;
let configState;
let language = 'es';
let toastTimer;
let styleDirty = false;
let styleSaveTimer;
let updateState;

function t(key, values = {}) {
  let value = i18n[language]?.[key] ?? i18n.es[key] ?? key;
  for (const [name, replacement] of Object.entries(values)) value = value.replace(`{${name}}`, replacement);
  return value;
}

function applyLanguage(nextLanguage) {
  language = nextLanguage === 'en' ? 'en' : 'es';
  document.documentElement.lang = language;
  document.querySelectorAll('[data-language]').forEach(button => {
    const active = button.dataset.language === language;
    button.classList.toggle('active', active);
    button.setAttribute('aria-pressed', String(active));
  });
  document.querySelectorAll('[data-i18n]').forEach(element => { element.textContent = t(element.dataset.i18n); });
  document.querySelectorAll('[data-i18n-placeholder]').forEach(element => { element.placeholder = t(element.dataset.i18nPlaceholder); });
  document.querySelectorAll('[data-i18n-aria]').forEach(element => { element.setAttribute('aria-label', t(element.dataset.i18nAria)); });
  document.querySelectorAll('[data-i18n-title]').forEach(element => { element.title = t(element.dataset.i18nTitle); });
  if ($('#style-save-state')) setStyleDirty(styleDirty);
  if (appState) render(appState);
  if (updateState) renderUpdate(updateState);
}

function renderUpdate(update) {
  updateState = update;
  const banner = $('#update-banner');
  banner.hidden = !update?.available;
  if (!update?.available) return;
  $('#update-version').textContent = t('updateVersion', { current: update.currentVersion, latest: update.latestVersion });
  const button = $('#install-update');
  if (!button.disabled) button.textContent = t('updateRestart');
}

function translateCatalogStatus(value) {
  const keys = { 'Listo': 'catalogReady', 'Extrayendo desde CASC…': 'catalogExtracting', 'Error de extracción': 'catalogError', 'HotS no detectado': 'catalogNoGame', 'Pendiente': 'pending' };
  return keys[value] ? t(keys[value]) : value;
}

function render(next) {
  appState = next;
  const healthy = next.hotsDetected && next.catalogReady && next.watcherRunning;
  $('#health').textContent = healthy ? t('ready') : t('attention');
  $('#health').classList.toggle('ok', healthy);
  $('#game').textContent = next.hotsDetected ? `Build ${next.gameBuild}` : t('notDetected');
  $('#path').textContent = next.hotsPath || t('configurePath');
  $('#catalog').textContent = translateCatalogStatus(next.catalogStatus || t('pending'));
  $('#catalog-detail').textContent = next.catalogReady ? t('catalogDetail', { heroes: next.heroCount, talents: next.talentCount, build: next.catalogBuild }) : '';
  $('#watcher').textContent = next.watcherRunning ? t('active') : t('stopped');
  $('#replay-path').textContent = next.replayPath;
  $('#hero').textContent = next.heroName || t('waitingMatch');
  $('#player').textContent = next.player ? `${next.player} · ${t('talentsCount', { count: next.talents.length })}` : t('waitingTalents');
  $('#error').hidden = !next.lastError;
  $('#error').textContent = next.lastError;
  $('#talents').replaceChildren(...tiers.map(level => {
    const found = (next.talents || []).find(item => item.level === level);
    const box = document.createElement('div');
    box.className = 'talent';
    if (found?.iconUrl) {
      const image = document.createElement('img');
      image.src = `${found.iconUrl}?b=${next.catalogBuild}`;
      image.alt = found.name;
      image.title = found.name;
      box.append(image);
    } else {
      const empty = document.createElement('span');
      empty.className = 'empty';
      box.append(empty);
    }
    const label = document.createElement('span');
    label.textContent = level;
    box.append(label);
    return box;
  }));
  updateWizardStatus();
  requestAnimationFrame(fitPreview);
}

async function post(url, body) {
  const response = await fetch(url, { method: 'POST', headers: body ? { 'Content-Type': 'application/json' } : {}, body: body ? JSON.stringify(body) : undefined });
  if (!response.ok) throw new Error((await response.json().catch(() => ({}))).error || `HTTP ${response.status}`);
  return response.status === 204 ? null : response.json().catch(() => null);
}

function showToast(message, bad = false) {
  clearTimeout(toastTimer);
  const toast = $('#message');
  toast.textContent = message;
  toast.classList.toggle('bad', bad);
  toast.hidden = false;
  toastTimer = setTimeout(() => { toast.hidden = true; }, 3600);
}

function setBusy(button, busy) { button.disabled = busy; button.setAttribute('aria-busy', String(busy)); }

function fillGeneralForm(config) {
  for (const key of ['battleTag', 'hotsPath', 'replayPath', 'obsPort', 'locale']) $(`#${key}`).value = config[key] ?? '';
  $('#obs-url').textContent = `${location.protocol}//${location.hostname}:${config.obsPort}/overlay.html`;
  $('#wizard-obs-url').textContent = $('#obs-url').textContent;
}

function showWizardStep(step) {
  $('#setup-wizard').hidden = false;
  document.querySelectorAll('[data-wizard-step]').forEach(panel => { panel.hidden = Number(panel.dataset.wizardStep) !== step; });
  document.querySelectorAll('.wizard-progress i').forEach((item, index) => item.classList.toggle('active', index < step));
  updateWizardStatus();
}

function updateWizardStatus() {
  if (!appState) return;
  const gameOk = Boolean(appState.hotsDetected);
  const replayOk = Boolean(appState.replayPath);
  $('#wizard-game-dot').classList.toggle('ok', gameOk);
  $('#wizard-replay-dot').classList.toggle('ok', replayOk);
  $('#wizard-game-result').textContent = gameOk ? (appState.hotsPath || t('detected')) : t('notFound');
  $('#wizard-replay-result').textContent = replayOk ? appState.replayPath : t('notFound');
}

function fillStyleForm(style) {
  const resolved = { ...defaultStyle, ...(style || {}) };
  for (const key of ['accentColor', 'borderColor', 'backgroundColor', 'textColor', 'borderWidth', 'borderRadius', 'borderStyle', 'iconSize', 'gap', 'entryAnimation', 'animationSpeed']) {
    $(`#${key}`).value = resolved[key];
  }
  for (const key of ['showHero', 'showLevels', 'showTalentNames', 'showEmptySlots']) $(`#${key}`).checked = resolved[key];
  updateOutputs();
  updateDependentControls();
  previewStyle();
}

function readStyleForm() {
  return {
    accentColor: $('#accentColor').value, borderColor: $('#borderColor').value, backgroundColor: $('#backgroundColor').value, textColor: $('#textColor').value,
    borderWidth: Number($('#borderWidth').value), borderRadius: Number($('#borderRadius').value), borderStyle: $('#borderStyle').value,
    iconSize: Number($('#iconSize').value), gap: Number($('#gap').value), showHero: $('#showHero').checked,
    showLevels: $('#showLevels').checked, showTalentNames: $('#showTalentNames').checked, showEmptySlots: $('#showEmptySlots').checked,
    entryAnimation: $('#entryAnimation').value, animationSpeed: Number($('#animationSpeed').value),
  };
}

function updateOutputs() {
  for (const id of ['accentColor', 'borderColor', 'backgroundColor', 'textColor']) $(`output[for="${id}"]`).textContent = $(`#${id}`).value;
  for (const id of ['iconSize', 'borderWidth', 'borderRadius', 'gap']) $(`output[for="${id}"]`).textContent = `${$(`#${id}`).value}px`;
  $('output[for="animationSpeed"]').textContent = `${$('#animationSpeed').value}%`;
}

function setStyleDirty(dirty) {
  styleDirty = dirty;
  const state = $('#style-save-state');
  state.textContent = t(dirty ? 'unsavedChanges' : 'appearanceSaved');
  state.classList.toggle('dirty', dirty);
  state.classList.remove('bad');
}

function scheduleStyleSave() {
  clearTimeout(styleSaveTimer);
  setStyleDirty(true);
  styleSaveTimer = setTimeout(async () => {
    if (!configState) return;
    try {
      await saveConfig({ ...configState, overlayStyle: readStyleForm() }, '', false);
      setStyleDirty(false);
    } catch (error) {
      const state = $('#style-save-state');
      state.textContent = t('saveFailed');
      state.classList.add('bad');
      showToast(error.message, true);
    }
  }, 550);
}

function updateDependentControls() {
  const borderDisabled = $('#borderStyle').value === 'none';
  $('#borderWidth').disabled = borderDisabled;
  $('#borderWidthControl').classList.toggle('control-disabled', borderDisabled);
  const animationDisabled = $('#entryAnimation').value === 'none';
  $('#animationSpeed').disabled = animationDisabled;
  $('#previewEntry').disabled = animationDisabled;
  $('#animationSpeedControl').classList.toggle('control-disabled', animationDisabled);
  $('#previewAnimationControl').classList.toggle('control-disabled', animationDisabled);
}

function previewStyle() {
  const frame = $('#overlay-preview');
  frame.contentWindow?.postMessage({ type: 'overlay-style-preview', style: readStyleForm() }, location.origin);
  requestAnimationFrame(fitPreview);
}

function fitPreview() {
  const frame = $('#overlay-preview');
  const stage = frame.closest('.preview-stage');
  const overlay = frame.contentDocument?.querySelector('#overlay');
  if (!stage || !overlay) return;
  const contentWidth = Math.max(overlay.getBoundingClientRect().width + 36, 1);
  const availableWidth = Math.max(stage.clientWidth - 36, 1);
  frame.style.setProperty('--preview-scale', Math.min(1, availableWidth / contentWidth).toFixed(3));
}

async function saveConfig(next, successMessage = t('saved'), notify = true) {
  await post('/api/config', next);
  configState = next;
  fillGeneralForm(configState);
  if (notify && successMessage) showToast(successMessage);
}

async function load() {
  const [status, config, update] = await Promise.all([
    fetch('/api/status').then(response => response.json()),
    fetch('/api/config').then(response => response.json()),
    fetch('/api/update').then(response => response.json()).catch(() => null),
  ]);
  configState = { ...config, overlayStyle: { ...defaultStyle, ...(config.overlayStyle || {}) } };
  applyLanguage(configState.uiLanguage);
  fillGeneralForm(configState);
  fillStyleForm(configState.overlayStyle);
  setStyleDirty(false);
  render(status);
  renderUpdate(update);
  if (!configState.setupCompleted) showWizardStep(1);
}

$('#style-form').addEventListener('input', () => { updateOutputs(); updateDependentControls(); previewStyle(); scheduleStyleSave(); });
$('#style-form').addEventListener('submit', event => event.preventDefault());
$('#defaults').addEventListener('click', () => { fillStyleForm(defaultStyle); scheduleStyleSave(); });
document.querySelectorAll('[data-preset]').forEach(button => button.addEventListener('click', () => {
  fillStyleForm(presets[button.dataset.preset]);
  document.querySelectorAll('[data-preset]').forEach(item => item.classList.toggle('active', item === button));
  scheduleStyleSave();
}));

$('#config-form').addEventListener('submit', async event => {
  event.preventDefault();
  const button = event.submitter;
  const next = {
    ...configState,
    battleTag: $('#battleTag').value.trim(), hotsPath: $('#hotsPath').value.trim(), replayPath: $('#replayPath').value.trim(),
    obsPort: Number($('#obsPort').value), locale: $('#locale').value, uiLanguage: language,
  };
  setBusy(button, true);
  try { await saveConfig(next, next.obsPort === Number(location.port) ? t('saved') : t('portRestart')); }
  catch (error) { showToast(error.message, true); }
  finally { setBusy(button, false); }
});

document.querySelectorAll('[data-language]').forEach(button => button.addEventListener('click', async () => {
  if (button.dataset.language === language) return;
  applyLanguage(button.dataset.language);
  if (!configState) return;
  try { await saveConfig({ ...configState, uiLanguage: language }); }
  catch (error) { showToast(error.message, true); }
}));
document.querySelectorAll('[data-wizard-language]').forEach(button => button.addEventListener('click', async () => {
  applyLanguage(button.dataset.wizardLanguage);
  if (configState) await saveConfig({ ...configState, uiLanguage: language }, '', false).catch(error => showToast(error.message, true));
  showWizardStep(2);
}));
document.querySelectorAll('[data-wizard-next]').forEach(button => button.addEventListener('click', () => showWizardStep(Number(button.dataset.wizardNext))));
$('#wizard-copy').addEventListener('click', async () => { try { await navigator.clipboard.writeText($('#wizard-obs-url').textContent); showToast(t('copied')); } catch (error) { showToast(error.message, true); } });
$('#wizard-finish').addEventListener('click', async () => {
  try {
    await saveConfig({ ...configState, uiLanguage: language, setupCompleted: true }, t('setupDone'));
    $('#setup-wizard').hidden = true;
  } catch (error) { showToast(error.message, true); }
});
$('#rerun-setup').addEventListener('click', () => showWizardStep(1));
$('#previewEntry').addEventListener('click', () => {
  previewStyle();
  $('#overlay-preview').contentWindow?.postMessage({ type: 'overlay-entry-preview' }, location.origin);
});
document.querySelectorAll('[data-preview-mode]').forEach(button => button.addEventListener('click', () => {
  document.querySelectorAll('[data-preview-mode]').forEach(item => item.classList.toggle('active', item === button));
  $('#overlay-preview').contentWindow?.postMessage({ type: 'overlay-preview-mode', mode: button.dataset.previewMode }, location.origin);
}));
$('#copy').addEventListener('click', async () => { try { await navigator.clipboard.writeText($('#obs-url').textContent); showToast(t('copied')); } catch (error) { showToast(error.message, true); } });
$('#regenerate').addEventListener('click', async () => { try { await post('/api/catalog/regenerate'); showToast(t('catalogQueued')); } catch (error) { showToast(error.message, true); } });
$('#reset').addEventListener('click', async () => { try { await post('/api/match/reset'); showToast(t('matchReset')); } catch (error) { showToast(error.message, true); } });
$('#install-update').addEventListener('click', async event => {
  const button = event.currentTarget;
  setBusy(button, true);
  button.textContent = t('updating');
  try { await post('/api/update/install'); }
  catch (error) { setBusy(button, false); button.textContent = t('updateRestart'); showToast(error.message, true); }
});
$('#overlay-preview').addEventListener('load', () => { previewStyle(); fitPreview(); });
new ResizeObserver(fitPreview).observe($('.preview-stage'));

load().catch(error => showToast(error.message, true));
setInterval(() => fetch('/api/update').then(response => response.json()).then(renderUpdate).catch(() => {}), 60 * 60 * 1000);
const events = new EventSource('/events');
events.onmessage = event => {
  const next = JSON.parse(event.data);
  if (next.uiLanguage && next.uiLanguage !== language) applyLanguage(next.uiLanguage);
  render(next);
};
