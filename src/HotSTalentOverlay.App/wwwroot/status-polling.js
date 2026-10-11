// Short, sequential requests leave HTTP/1.1 connections free for talent images.
function watchStatus(onStatus) {
  let stopped = false;
  let pending = false;
  let controller;
  let timer;
  let previousJson;

  async function poll() {
    if (stopped || pending) return;
    pending = true;
    controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 5000);
    try {
      const response = await fetch('/api/status', { cache: 'no-store', signal: controller.signal });
      if (!response.ok) throw new Error(`Status unavailable: ${response.status}`);
      const state = await response.json();
      const json = JSON.stringify(state);
      if (!stopped && json !== previousJson) {
        onStatus(state);
        previousJson = json;
      }
    } catch {
      // Keep the last frame during an update/restart and reconnect automatically.
    } finally {
      clearTimeout(timeout);
      pending = false;
      if (!stopped) timer = setTimeout(poll, 1000);
    }
  }

  window.addEventListener('pagehide', () => {
    stopped = true;
    clearTimeout(timer);
    controller?.abort();
  });
  window.addEventListener('pageshow', () => {
    if (!stopped) return;
    stopped = false;
    poll();
  });
  poll();
}
