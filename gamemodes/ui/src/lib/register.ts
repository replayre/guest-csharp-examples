export async function registerServiceWorker() {
  if (!('serviceWorker' in navigator)) return;

  try {
    const base = '/';
    const swUrl = `sw.js`;

    const reg = await navigator.serviceWorker.register(swUrl, {
      scope: base
    });

    console.log('[sw] registered:', reg.scope);
  } catch (err) {
    console.error('[sw] register failed:', err);
  }
}
