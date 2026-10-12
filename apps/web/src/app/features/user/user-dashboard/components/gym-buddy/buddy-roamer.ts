import type { BuddyPose } from './buddy-scene';

/** What the roamer needs from the 3D scene; all optional so the flat SVG buddy can roam too. */
export interface RoamerPuppet {
  setPose?(pose: BuddyPose): void;
  setFacing?(dir: number): void;
  setSwing?(amount: number): void;
  wave?(): void;
}

interface Point { x: number; y: number; }

/** Where the buddy is parked, kept relative to an element so it survives reflows. */
type Spot =
  | { kind: 'home' }
  | { kind: 'card'; card: HTMLElement; dx: number; pose: 'stand' | 'sit' }
  | { kind: 'free'; dx: number; dy: number };

const WANDER_MIN_MS = 15000;
const WANDER_MAX_MS = 25000;
/** After the member places the buddy, let it stay put for a while. */
const PLACED_REST_MS = 45000;
const DRAG_THRESHOLD_PX = 6;
const LONG_PRESS_MS = 320;
/** How close (px) to a card's top edge a drop has to be to sit on it. */
const PERCH_SNAP_PX = 32;
const CARD_EDGE_MARGIN_PX = 24;
const BUBBLE_MAX_PX = 200;
const BUBBLE_EDGE_MARGIN_PX = 12;

const sleep = (ms: number) => new Promise<void>(resolve => setTimeout(resolve, ms));
const rand = (min: number, max: number) => min + Math.random() * (max - min);

/**
 * Moves the gym buddy around the dashboard: hops between card tops, plays peekaboo behind them, and can be
 * picked up and dropped anywhere (on a card edge it sits). Everything is CSS transforms on one small element,
 * so the page never re-lays out and Angular change detection is never involved.
 */
export class BuddyRoamer {
  private pos: Point = { x: 0, y: 0 };
  private spot: Spot = { kind: 'home' };
  private busy = false;
  private disposed = false;
  private wanderTimer: ReturnType<typeof setTimeout> | null = null;
  private restUntil = 0;
  private motion: Animation[] = [];
  /** Bumped when the member grabs the buddy, so any scripted move in progress stops. */
  private run = 0;

  private drag: {
    pointerId: number; start: Point; startPos: Point; last: number;
    active: boolean; longPress: ReturnType<typeof setTimeout> | null;
  } | null = null;
  /** Swallows the click that follows a drag so it doesn't count as a poke. */
  private suppressClick = false;

  private readonly resizeObserver = new ResizeObserver(() => this.reposition());

  constructor(
    /** Moves as one unit (mascot + speech bubble). */
    private readonly mover: HTMLElement,
    /** The character itself: drag handle and peekaboo clip target. */
    private readonly mascot: HTMLElement,
    private readonly area: HTMLElement,
    private readonly puppet: () => RoamerPuppet | null,
    private readonly canWander: boolean
  ) {
    mascot.addEventListener('pointerdown', this.onPointerDown);
    mascot.addEventListener('pointermove', this.onPointerMove);
    mascot.addEventListener('pointerup', this.onPointerUp);
    mascot.addEventListener('pointercancel', this.onPointerUp);
    mascot.addEventListener('touchmove', this.onTouchMove, { passive: false });
    mascot.addEventListener('contextmenu', this.onContextMenu);
    mascot.addEventListener('dblclick', this.goHome);
    mascot.addEventListener('click', this.onClickCapture, true);
    window.addEventListener('resize', this.reposition, { passive: true });
    this.resizeObserver.observe(area);
    this.scheduleWander();
  }

  dispose(): void {
    this.disposed = true;
    if (this.wanderTimer) clearTimeout(this.wanderTimer);
    this.stopMotion();
    this.mascot.removeEventListener('pointerdown', this.onPointerDown);
    this.mascot.removeEventListener('pointermove', this.onPointerMove);
    this.mascot.removeEventListener('pointerup', this.onPointerUp);
    this.mascot.removeEventListener('pointercancel', this.onPointerUp);
    this.mascot.removeEventListener('touchmove', this.onTouchMove);
    this.mascot.removeEventListener('contextmenu', this.onContextMenu);
    this.mascot.removeEventListener('dblclick', this.goHome);
    this.mascot.removeEventListener('click', this.onClickCapture, true);
    window.removeEventListener('resize', this.reposition);
    this.resizeObserver.disconnect();
  }

  // ---------- geometry ----------

  /** Where the feet and the seat are inside the mascot box (the 3D render has room around the character). */
  private anchors(): { cx: number; feet: number; seat: number; height: number } {
    const { width, height } = this.mascot.getBoundingClientRect();
    const is3d = this.mascot.classList.contains('is-3d');
    return {
      cx: width / 2,
      feet: height * (is3d ? 0.88 : 0.94),
      seat: height * (is3d ? 0.75 : 0.94),
      height
    };
  }

  /** Mascot's top-left in the viewport when it is at home (no translation). */
  private homeOrigin(): Point {
    const r = this.mascot.getBoundingClientRect();
    return { x: r.left - this.pos.x, y: r.top - this.pos.y };
  }

  /** Translation that puts the feet (or seat) on a viewport point. */
  private posFor(point: Point, pose: 'stand' | 'sit'): Point {
    const home = this.homeOrigin();
    const a = this.anchors();
    return { x: point.x - a.cx - home.x, y: point.y - (pose === 'sit' ? a.seat : a.feet) - home.y };
  }

  private feetPoint(): Point {
    const r = this.mascot.getBoundingClientRect();
    const a = this.anchors();
    return { x: r.left + a.cx, y: r.top + a.feet };
  }

  private cards(): HTMLElement[] {
    return Array.from(this.area.querySelectorAll<HTMLElement>('.card, .hero'))
      .filter(el => el.offsetParent !== null && !el.contains(this.mover));
  }

  /** Recomputes the translation for the current spot (cards move when the layout reflows). */
  private readonly reposition = () => {
    if (this.busy || this.drag?.active) return;
    const spot = this.spot;
    if (spot.kind === 'home') {
      this.place({ x: 0, y: 0 });
    } else if (spot.kind === 'card') {
      if (!spot.card.isConnected) return this.setSpot({ kind: 'home' }, 'stand');
      const r = spot.card.getBoundingClientRect();
      this.place(this.posFor({ x: r.left + Math.min(spot.dx, r.width - CARD_EDGE_MARGIN_PX), y: r.top }, spot.pose));
    } else {
      const r = this.area.getBoundingClientRect();
      this.place(this.posFor({ x: r.left + spot.dx, y: r.top + spot.dy }, 'stand'));
    }
  };

  private place(p: Point, settle = true): void {
    this.pos = p;
    this.mover.style.transform = p.x || p.y ? `translate(${p.x}px, ${p.y}px)` : '';
    this.mover.classList.toggle('roaming', !!(p.x || p.y));
    if (settle) this.fitBubble();
  }

  // Keep the speech bubble on screen: open it on the roomier side and cap its width to that room.
  private fitBubble(): void {
    const r = this.mascot.getBoundingClientRect();
    const viewport = document.documentElement.clientWidth;
    const tuck = this.mover.classList.contains('is-3d') ? 16 : -10;
    const left = r.left + tuck - BUBBLE_EDGE_MARGIN_PX;
    const right = viewport - r.right + tuck - BUBBLE_EDGE_MARGIN_PX;
    const toRight = right > left && left < BUBBLE_MAX_PX;
    this.mover.classList.toggle('bubble-right', toRight);
    this.mover.style.setProperty('--bubble-room', `${Math.max(Math.floor(toRight ? right : left), 120)}px`);
  }

  private setSpot(spot: Spot, pose: BuddyPose): void {
    this.spot = spot;
    this.puppet()?.setPose?.(pose);
    this.puppet()?.setFacing?.(0);
    this.reposition();
  }

  // ---------- motion ----------

  private stopMotion(): void {
    this.motion.forEach(a => a.cancel());
    this.motion = [];
    this.mascot.style.clipPath = '';
  }

  private async animate(el: HTMLElement, frames: Keyframe[], options: KeyframeAnimationOptions): Promise<void> {
    const anim = el.animate(frames, { fill: 'forwards', ...options });
    this.motion.push(anim);
    try {
      await anim.finished;
    } catch {
      // Cancelled (drag or teardown).
    }
  }

  /** Walks along an edge or hops in an arc to `to`. Resolves false if the member grabbed him on the way. */
  private async travel(to: Point): Promise<boolean> {
    const run = this.run;
    const from = this.pos;
    const dx = to.x - from.x;
    const dy = to.y - from.y;
    const dist = Math.hypot(dx, dy);
    if (dist < 2) return true;
    const puppet = this.puppet();
    puppet?.setFacing?.(dx);
    const walking = Math.abs(dy) < 24;
    puppet?.setPose?.(walking ? 'walk' : 'jump');

    const t = (p: Point) => `translate(${p.x}px, ${p.y}px)`;
    if (walking) {
      await this.animate(this.mover, [{ transform: t(from) }, { transform: t(to) }],
        { duration: Math.min(Math.max(dist * 7, 500), 2600), easing: 'linear' });
    } else {
      const peak = { x: from.x + dx / 2, y: Math.min(from.y, to.y) - (50 + dist * 0.12) };
      await this.animate(this.mover, [
        { transform: t(from), easing: 'cubic-bezier(0.2, 0.6, 0.4, 1)' },
        { transform: t(peak), offset: 0.5, easing: 'cubic-bezier(0.6, 0, 0.8, 0.4)' },
        { transform: t(to) }
      ], { duration: Math.min(Math.max(dist * 1.8, 650), 1500) });
    }
    if (run !== this.run) return false;
    this.stopMotion();
    this.place(to);
    puppet?.setFacing?.(0);
    puppet?.setPose?.('stand');
    return true;
  }

  /** Ducks behind the card edge his feet are on, peeks out twice, then climbs back up. */
  private async peekaboo(): Promise<boolean> {
    const run = this.run;
    const a = this.anchors();
    const below = a.height - a.feet;
    const down = a.feet - a.height * 0.2;
    const peek = a.feet - a.height * 0.45;
    const base = this.pos;
    const step = async (sink: number, duration: number) => {
      const from = this.currentSink;
      this.currentSink = sink;
      const frames = (s: number) => ({
        transform: `translate(${base.x}px, ${base.y + s}px)`
      });
      const clip = (s: number) => ({ clipPath: `inset(0 0 ${below + s}px 0)` });
      await Promise.all([
        this.animate(this.mover, [frames(from), frames(sink)], { duration, easing: 'cubic-bezier(0.34, 1.4, 0.64, 1)' }),
        this.animate(this.mascot, [clip(from), clip(sink)], { duration, easing: 'cubic-bezier(0.34, 1.4, 0.64, 1)' })
      ]);
    };

    this.currentSink = 0;
    const script: [number, number, number][] = [
      // [sink, duration, pause after]
      [down, 450, rand(900, 1600)],
      [peek, 380, 1300],
      [down, 300, rand(700, 1200)],
      [peek, 320, 900],
      [0, 420, 0]
    ];
    for (const [sink, duration, pause] of script) {
      await step(sink, duration);
      if (run !== this.run) return false;
      await sleep(pause);
      if (run !== this.run) return false;
    }
    this.stopMotion();
    this.place(base);
    this.puppet()?.wave?.();
    return true;
  }
  private currentSink = 0;

  // ---------- wandering ----------

  private scheduleWander(): void {
    if (!this.canWander || this.disposed) return;
    this.wanderTimer = setTimeout(() => this.wander(), rand(WANDER_MIN_MS, WANDER_MAX_MS));
  }

  private async wander(): Promise<void> {
    if (this.disposed) return;
    const idle = !this.busy && !this.drag && !document.hidden && Date.now() > this.restUntil;
    const choices = idle ? this.visibleCards() : [];
    if (choices.length) {
      this.busy = true;
      try {
        const card = choices[Math.floor(Math.random() * choices.length)];
        const r = card.getBoundingClientRect();
        const dx = rand(CARD_EDGE_MARGIN_PX + 30, Math.max(CARD_EDGE_MARGIN_PX + 31, r.width - CARD_EDGE_MARGIN_PX - 30));
        const roll = Math.random();
        // Now and then just go home.
        if (roll < 0.15 && this.spot.kind !== 'home') {
          if (await this.travel({ x: 0, y: 0 })) this.spot = { kind: 'home' };
        } else if (await this.travel(this.posFor({ x: r.left + dx, y: r.top }, 'stand')) && !this.disposed) {
          const pose = roll < 0.55 ? 'peek' : roll < 0.8 ? 'sit' : 'stand';
          if (pose === 'peek' && !(await this.peekaboo())) return this.scheduleWander();
          this.spot = { kind: 'card', card, dx, pose: pose === 'sit' ? 'sit' : 'stand' };
          if (pose === 'sit') {
            this.puppet()?.setPose?.('sit');
            this.reposition();
          }
        }
      } finally {
        this.busy = false;
      }
    }
    this.scheduleWander();
  }

  /** Cards whose top edge is comfortably on screen and not where the buddy already is. */
  private visibleCards(): HTMLElement[] {
    const current = this.spot.kind === 'card' ? this.spot.card : null;
    return this.cards().filter(card => {
      const r = card.getBoundingClientRect();
      return card !== current && r.width > 160 && r.top > 90 && r.top < window.innerHeight - 80;
    });
  }

  /**
   * Interrupts any wandering and goes to stand on `card`'s top edge, a little left of `focus` when given,
   * to react to something there. Resolves false (staying put) when he can't or shouldn't go.
   */
  async visit(card: HTMLElement, focus?: HTMLElement | null): Promise<boolean> {
    if (!this.canWander || this.drag || this.disposed) return false;
    const r = card.getBoundingClientRect();
    // Only go somewhere the member can actually see.
    if (r.top < 60 || r.top > window.innerHeight - 80) return false;

    this.run++;
    const m = new DOMMatrixReadOnly(getComputedStyle(this.mover).transform);
    this.stopMotion();
    this.place({ x: m.m41, y: m.m42 }, false);
    this.busy = true;
    try {
      const f = focus?.getBoundingClientRect();
      const want = f ? f.left + f.width / 2 - 70 : r.left + r.width / 2;
      const margin = CARD_EDGE_MARGIN_PX + 30;
      const dx = Math.min(Math.max(want - r.left, margin), Math.max(margin, r.width - margin));
      const arrived = await this.travel(this.posFor({ x: r.left + dx, y: r.top }, 'stand'));
      if (arrived) {
        this.spot = { kind: 'card', card, dx, pose: 'stand' };
        this.restUntil = Date.now() + PLACED_REST_MS;
      }
      return arrived;
    } finally {
      this.busy = false;
    }
  }

  private readonly goHome = () => {
    if (this.spot.kind === 'home' && !this.pos.x && !this.pos.y) return;
    this.busy = true;
    this.stopMotion();
    this.run++;
    this.travel({ x: 0, y: 0 }).then(arrived => {
      this.busy = false;
      if (arrived) this.setSpot({ kind: 'home' }, 'stand');
    });
  };

  // ---------- drag ----------

  private readonly onPointerDown = (e: PointerEvent) => {
    if (e.button !== 0 || this.drag) return;
    this.drag = {
      pointerId: e.pointerId,
      start: { x: e.clientX, y: e.clientY },
      startPos: this.pos,
      last: e.clientX,
      active: false,
      // Touch needs a long-press so a normal swipe still scrolls the page.
      longPress: e.pointerType === 'touch' ? setTimeout(() => this.beginDrag(), LONG_PRESS_MS) : null
    };
  };

  private beginDrag(): void {
    const drag = this.drag;
    if (!drag || drag.active) return;
    drag.active = true;
    this.run++;
    this.busy = false;
    // Pick him up from wherever he visibly is, even mid-hop.
    const m = new DOMMatrixReadOnly(getComputedStyle(this.mover).transform);
    this.stopMotion();
    this.place({ x: m.m41, y: m.m42 }, false);
    this.currentSink = 0;
    drag.startPos = this.pos;
    try {
      this.mascot.setPointerCapture(drag.pointerId);
    } catch {
      // Pointer already gone.
    }
    this.mover.classList.add('dragging');
    this.puppet()?.setPose?.('held');
  }

  private readonly onPointerMove = (e: PointerEvent) => {
    const drag = this.drag;
    if (!drag || e.pointerId !== drag.pointerId) return;
    const dx = e.clientX - drag.start.x;
    const dy = e.clientY - drag.start.y;
    if (!drag.active) {
      if (drag.longPress) {
        // Finger moved before the long-press: it's a scroll, not a pick-up.
        if (Math.hypot(dx, dy) > 10) this.cancelDrag();
        return;
      }
      if (Math.hypot(dx, dy) < DRAG_THRESHOLD_PX) return;
      this.beginDrag();
    }
    e.preventDefault();
    this.puppet()?.setSwing?.((e.clientX - drag.last) / 12);
    drag.last = e.clientX;
    this.place({ x: drag.startPos.x + dx, y: drag.startPos.y + dy }, false);
  };

  private readonly onPointerUp = (e: PointerEvent) => {
    const drag = this.drag;
    if (!drag || e.pointerId !== drag.pointerId) return;
    const wasActive = drag.active;
    this.cancelDrag();
    if (!wasActive) return;
    this.suppressClick = true;
    setTimeout(() => (this.suppressClick = false));
    this.drop();
  };

  private cancelDrag(): void {
    if (this.drag?.longPress) clearTimeout(this.drag.longPress);
    if (this.drag?.active) {
      try {
        this.mascot.releasePointerCapture(this.drag.pointerId);
      } catch {
        // Already released.
      }
    }
    this.drag = null;
    this.mover.classList.remove('dragging');
  }

  /** Near a card's top edge: sit on it. Near the greeting: go home. Anywhere else: stand right there. */
  private drop(): void {
    this.restUntil = Date.now() + PLACED_REST_MS;
    const feet = this.feetPoint();

    if (Math.hypot(this.pos.x, this.pos.y) < 40) return this.setSpot({ kind: 'home' }, 'stand');

    for (const card of this.cards()) {
      const r = card.getBoundingClientRect();
      if (feet.x > r.left + CARD_EDGE_MARGIN_PX && feet.x < r.right - CARD_EDGE_MARGIN_PX && Math.abs(feet.y - r.top) < PERCH_SNAP_PX) {
        return this.setSpot({ kind: 'card', card, dx: feet.x - r.left, pose: 'sit' }, 'sit');
      }
    }

    const area = this.area.getBoundingClientRect();
    this.setSpot({ kind: 'free', dx: feet.x - area.left, dy: feet.y - area.top }, 'stand');
  }

  private readonly onTouchMove = (e: TouchEvent) => {
    if (this.drag?.active) e.preventDefault();
  };

  private readonly onContextMenu = (e: Event) => {
    if (this.drag) e.preventDefault();
  };

  private readonly onClickCapture = (e: Event) => {
    if (this.suppressClick) {
      e.stopImmediatePropagation();
      e.preventDefault();
    }
  };
}
