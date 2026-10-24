import { Injectable, PLATFORM_ID, computed, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

/** Chrome/Edge/Samsung's install event; not in the DOM typings yet. */
interface BeforeInstallPromptEvent extends Event {
  prompt(): Promise<void>;
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>;
}

/**
 * - `prompt`: the browser can show its own install dialog
 * - `ios`: iPhone/iPad, installed through Share → Add to Home Screen
 * - `installed`: already running as the installed app
 * - `manual`: anything else (Firefox, or Chrome before it offers the install)
 */
export type InstallMode = 'prompt' | 'ios' | 'installed' | 'manual';

@Injectable({ providedIn: 'root' })
export class PwaInstallService {
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  // index.html stashes the event in case it fires before Angular is up
  private readonly deferred = signal<BeforeInstallPromptEvent | null>(
    this.isBrowser ? (window as any).__installPrompt ?? null : null);
  private readonly installed = signal(this.isBrowser && document.documentElement.classList.contains('installed-app'));
  private readonly isIos = this.isBrowser &&
    (/iPad|iPhone|iPod/.test(navigator.userAgent) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1));

  readonly mode = computed<InstallMode>(() => {
    if (this.installed()) return 'installed';
    if (this.deferred()) return 'prompt';
    return this.isIos ? 'ios' : 'manual';
  });

  constructor() {
    if (!this.isBrowser) return;
    window.addEventListener('beforeinstallprompt', e => this.deferred.set(e as BeforeInstallPromptEvent));
    window.addEventListener('appinstalled', () => {
      this.installed.set(true);
      this.deferred.set(null);
    });
  }

  /** Opens the browser's install dialog. Only works in `prompt` mode, from a click. */
  async install(): Promise<void> {
    const event = this.deferred();
    if (!event) return;
    await event.prompt();
    const { outcome } = await event.userChoice;
    // the event can only be used once; the browser fires a new one if the visitor dismissed it
    this.deferred.set(null);
    if (outcome === 'accepted') this.installed.set(true);
  }
}
