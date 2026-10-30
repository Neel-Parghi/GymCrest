import {
  ChangeDetectionStrategy, Component, DestroyRef, ElementRef, afterNextRender, computed, effect, inject, input,
  signal, untracked, viewChild
} from '@angular/core';
import { BuddyEvent, BuddyFacts } from '../../../../../core/models/user-dashboard.model';
import { BuddyLine, buddyLines } from './buddy-lines';
import type { BuddyScene } from './buddy-scene';
import { DANCE_SECONDS, LIFT_SECONDS } from './buddy-timings';
import { BuddyRoamer } from './buddy-roamer';

/** How long each speech bubble stays up. */
const BUBBLE_MS = 3800;
const WARMING_UP: BuddyLine = { text: 'Warming up...', icon: 'fa-person-running' };
/** Reactions wait until the hello is over; this is the latest they wait if it never plays. */
const REACTIONS_FALLBACK_MS = 12000;
const POINT_MS = 3800;
const BETWEEN_REACTIONS_MS = 1500;
const MAX_QUEUED = 3;

const sleep = (ms: number) => new Promise<void>(resolve => setTimeout(resolve, ms));

const prefersReducedMotion = () =>
  typeof window !== 'undefined' && !!window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;

/** WebGL is available and the member hasn't asked for reduced motion. */
function canRender3d(): boolean {
  if (typeof window === 'undefined' || prefersReducedMotion()) return false;
  try {
    return !!document.createElement('canvas').getContext('webgl2');
  } catch {
    return false;
  }
}

/**
 * "Bolt", the dashboard's gym buddy: a 3D chibi coach that pops up, waves hello and chats about
 * the member's day. Falls back to a flat SVG buddy without WebGL or with reduced motion.
 */
@Component({
  selector: 'app-gym-buddy',
  standalone: true,
  templateUrl: './gym-buddy.component.html',
  styleUrl: './gym-buddy.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class GymBuddyComponent {
  readonly facts = input<BuddyFacts | null>(null);
  /** Element whose cards the buddy can climb onto and be dragged around; without it he stays home. */
  readonly roamArea = input<HTMLElement | null>(null);

  readonly use3d = signal(canRender3d());
  readonly bubbleOpen = signal(false);
  readonly hopping = signal(false);
  private readonly lineIndex = signal(0);
  /** A one-off line for a reaction, shown instead of the regular script. */
  private readonly reactionLine = signal<BuddyLine | null>(null);

  private readonly canvas = viewChild<ElementRef<HTMLCanvasElement>>('canvas');
  private readonly mover = viewChild<ElementRef<HTMLElement>>('mover');
  private readonly mascot = viewChild<ElementRef<HTMLElement>>('mascot');
  private readonly lines = computed(() => {
    const f = this.facts();
    return f ? buddyLines(f) : [WARMING_UP];
  });
  readonly line = computed(() => this.reactionLine() ?? this.lines()[this.lineIndex() % this.lines().length]);
  readonly state = computed(() => this.facts()?.state ?? null);

  private scene: BuddyScene | null = null;
  private roamer: BuddyRoamer | null = null;
  private introPlayed = false;
  private timers: ReturnType<typeof setTimeout>[] = [];
  private queue: BuddyEvent[] = [];
  private playing = false;
  private reactionsReady = false;
  private destroyed = false;

  constructor() {
    afterNextRender(() => {
      this.start3d();
      this.startRoaming();
      setTimeout(() => this.allowReactions(), REACTIONS_FALLBACK_MS);
    });

    effect(() => {
      const state = this.state();
      this.scene?.setMood(state);
      // Once today's workout is known, say hello and then what's on for today.
      if (state && !this.introPlayed) {
        this.introPlayed = true;
        untracked(() => this.intro());
      }
    });

    inject(DestroyRef).onDestroy(() => {
      this.destroyed = true;
      this.clearTimers();
      this.roamer?.dispose();
      this.scene?.dispose();
    });
  }

  private async start3d(): Promise<void> {
    const canvas = this.canvas()?.nativeElement;
    if (!this.use3d() || !canvas) return;
    try {
      const { createBuddyScene } = await import('./buddy-scene');
      this.scene = createBuddyScene(canvas);
      this.scene.setMood(this.state());
    } catch {
      this.use3d.set(false);
    }
  }

  /** Drag-anywhere works for every buddy; wandering and peekaboo only for the animated 3D one. */
  private startRoaming(): void {
    const area = this.roamArea();
    const mover = this.mover()?.nativeElement;
    const mascot = this.mascot()?.nativeElement;
    if (!area || !mover || !mascot) return;
    this.roamer = new BuddyRoamer(mover, mascot, area, () => this.scene, this.use3d());
  }

  private intro(): void {
    this.clearTimers();
    this.later(() => this.show(0), 450);
    this.later(() => this.show(1), 450 + BUBBLE_MS);
    this.later(() => this.bubbleOpen.set(false), 450 + BUBBLE_MS * 2);
    setTimeout(() => this.allowReactions(), 450 + BUBBLE_MS * 2 + 400);
  }

  /**
   * Something happened on the dashboard worth a reaction. Reactions play one at a time; a newer one for
   * the same card replaces a queued one (e.g. ticking several routines quickly).
   */
  react(event: BuddyEvent): void {
    this.queue = [...this.queue.filter(e => e.target !== event.target), event].slice(-MAX_QUEUED);
    void this.playQueue();
  }

  private allowReactions(): void {
    if (this.reactionsReady) return;
    this.reactionsReady = true;
    void this.playQueue();
  }

  private async playQueue(): Promise<void> {
    if (this.playing || !this.reactionsReady) return;
    this.playing = true;
    while (this.queue.length && !this.destroyed) {
      await this.play(this.queue.shift()!);
      if (this.queue.length) await sleep(BETWEEN_REACTIONS_MS);
    }
    this.playing = false;
  }

  /** Goes to the card (when it can roam), says the line and acts it out. */
  private async play(event: BuddyEvent): Promise<void> {
    const area = this.roamArea();
    const card = area?.querySelector<HTMLElement>(event.target) ?? null;
    const focus = event.focus ? area?.querySelector<HTMLElement>(event.focus) ?? null : null;
    const arrived = card && this.roamer ? await this.roamer.visit(card, focus) : false;
    if (this.destroyed) return;

    this.say({ text: event.text, icon: event.icon });
    const scene = this.scene;
    if (!arrived || !scene) {
      // Can't go there (reduced motion, off screen, being held): just cheer from where he is.
      scene?.cheer();
      await sleep(BUBBLE_MS);
      return;
    }

    if (event.act === 'dance') {
      scene.dance();
      await sleep(DANCE_SECONDS * 1000);
    } else if (event.act === 'lift') {
      scene.lift();
      await sleep(LIFT_SECONDS * 1000);
    } else {
      const mascot = this.mascot()?.nativeElement.getBoundingClientRect();
      const target = focus?.getBoundingClientRect();
      const dir = mascot && target ? Math.sign(target.left + target.width / 2 - (mascot.left + mascot.width / 2)) : 1;
      scene.point(dir || 1);
      await sleep(POINT_MS);
      scene.point(0);
    }
  }

  private say(line: BuddyLine): void {
    this.clearTimers();
    this.reactionLine.set(line);
    this.bubbleOpen.set(true);
    this.later(() => {
      this.bubbleOpen.set(false);
      this.later(() => this.reactionLine.set(null), 300);
    }, BUBBLE_MS);
  }

  /** Tap: hop, wave and say the next thing it knows. */
  poke(): void {
    this.clearTimers();
    this.reactionLine.set(null);
    this.scene?.cheer();
    this.hopping.set(false);
    requestAnimationFrame(() => this.hopping.set(true));
    this.later(() => this.hopping.set(false), 600);

    const next = this.bubbleOpen() || this.introPlayed ? this.lineIndex() + 1 : 0;
    this.introPlayed = true;
    this.show(next);
    this.later(() => this.bubbleOpen.set(false), BUBBLE_MS);
  }

  private show(index: number): void {
    this.lineIndex.set(index);
    this.bubbleOpen.set(true);
    if (index === 0) this.scene?.wave();
  }

  private later(fn: () => void, ms: number): void {
    this.timers.push(setTimeout(fn, ms));
  }

  private clearTimers(): void {
    this.timers.forEach(clearTimeout);
    this.timers = [];
  }
}
