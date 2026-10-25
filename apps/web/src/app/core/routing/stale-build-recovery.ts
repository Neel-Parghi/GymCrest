import { NavigationError } from '@angular/router';

/**
 * Screens are lazy-loaded from hashed files that change with every deploy. An installed app (or a tab left
 * open) still runs the old build, so after a deploy its next navigation asks for a file that no longer exists
 * (Vercel answers with index.html) and the tap silently does nothing until the user refreshes.
 * When that happens, reload straight to the page they tapped so the new build loads.
 */
const CHUNK_LOAD_FAILURE =
  /Failed to fetch dynamically imported module|error loading dynamically imported module|Importing a module script failed|ChunkLoadError|Loading chunk [\w-]+ failed/i;

const RELOAD_KEY = 'gymcrest_staleBuildReloadAt';
/** Don't reload again within this window, so a real outage can't cause a reload loop. */
const RELOAD_COOLDOWN_MS = 30_000;

export function reloadOnStaleBuild(error: NavigationError): void {
  const message = String((error.error as Error | undefined)?.message ?? error.error ?? '');
  if (!CHUNK_LOAD_FAILURE.test(message)) return;

  try {
    const last = Number(sessionStorage.getItem(RELOAD_KEY) ?? 0);
    if (Date.now() - last < RELOAD_COOLDOWN_MS) return;
    sessionStorage.setItem(RELOAD_KEY, String(Date.now()));
  } catch { /* storage blocked: still reload once */ }

  window.location.assign(error.url);
}
