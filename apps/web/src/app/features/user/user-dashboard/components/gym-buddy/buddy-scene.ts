import {
  NeutralToneMapping, CanvasTexture, CapsuleGeometry, CircleGeometry, Color, ConeGeometry, CylinderGeometry,
  DirectionalLight, Group, HemisphereLight, Material, Mesh, MeshBasicMaterial, MeshPhysicalMaterial, PMREMGenerator,
  PerspectiveCamera, Scene, SphereGeometry, TorusGeometry, WebGLRenderer
} from 'three';
import { RoomEnvironment } from 'three/examples/jsm/environments/RoomEnvironment.js';
import { WorkoutState } from '../../../../../core/models/user-dashboard.model';

/** Handle the Angular component uses to drive the 3D buddy. */
export interface BuddyScene {
  wave(): void;
  /** Tap reaction: a little hop and a bicep flex. */
  cheer(): void;
  /** Happy dance: hops with alternating arm pumps. */
  dance(): void;
  /** Point toward a side (-1 left, 1 right) until cleared with 0. */
  point(dir: number): void;
  /** PR celebration: pulls a barbell up to the chest, holds it shaking, drops it and throws both arms up. */
  lift(): void;
  setMood(state: WorkoutState | null): void;
  /** Body pose while roaming the dashboard; poses blend smoothly into each other. */
  setPose(pose: BuddyPose): void;
  /** Turn toward the direction of travel (-1 left, 1 right) or face the viewer (0). */
  setFacing(dir: number): void;
  /** Sideways swing while being carried, from the drag velocity. */
  setSwing(amount: number): void;
  dispose(): void;
}

export type BuddyPose = 'stand' | 'walk' | 'jump' | 'sit' | 'held';

const COLORS = {
  skin: '#f6c39b',
  hair: '#3a2417',
  top: '#fb7a24',
  shorts: '#0a1f44',
  shoe: '#ffffff',
  sole: '#0a1f44',
  band: '#ffffff',
  eye: '#ffffff',
  pupil: '#0a1f44',
  blush: '#ff8fa3',
  mouth: '#5b1020',
  tongue: '#ff6b81',
  whistle: '#facc15',
  sweat: '#7dd3fc'
};

const POP_SECONDS = 0.65;
const WAVE_SECONDS = 1.4;
const CHEER_SECONDS = 1;
export const DANCE_SECONDS = 2.6;
export const LIFT_SECONDS = 3.4;
/** Speeds up the idle loop (breathing, sway, head bob). */
const IDLE_SPEED = 1.3;

const clamp01 = (v: number) => Math.min(Math.max(v, 0), 1);
const smooth = (v: number) => {
  const t = clamp01(v);
  return t * t * (3 - 2 * t);
};
const lerp = (a: number, b: number, t: number) => a + (b - a) * t;
const easeOutBack = (t: number) => {
  const c = 1.9;
  return 1 + (c + 1) * Math.pow(t - 1, 3) + c * Math.pow(t - 1, 2);
};
/** 0 → 1 → 0 over a normalised time window, with eased ends. */
const pulse = (t: number) => (t < 0 || t > 1 ? 0 : Math.sin(Math.PI * clamp01(t)));

/** Soft radial blob used as the floor shadow. */
function shadowTexture(): CanvasTexture {
  const size = 128;
  const c = document.createElement('canvas');
  c.width = c.height = size;
  const ctx = c.getContext('2d')!;
  const g = ctx.createRadialGradient(size / 2, size / 2, 0, size / 2, size / 2, size / 2);
  g.addColorStop(0, 'rgba(10, 31, 68, 0.45)');
  g.addColorStop(0.5, 'rgba(10, 31, 68, 0.18)');
  g.addColorStop(1, 'rgba(10, 31, 68, 0)');
  ctx.fillStyle = g;
  ctx.fillRect(0, 0, size, size);
  return new CanvasTexture(c);
}

/** Matte vinyl-toy finish, or a glossy one for eyes, whistle and shoes. */
type Finish = 'soft' | 'gloss';

/**
 * Builds "Bolt", a chibi gym coach (big head, headband, tank top, whistle), in a transparent canvas.
 * It rises out of the background, waves hello, follows the pointer with its eyes, blinks, and flexes when tapped.
 */
export function createBuddyScene(canvas: HTMLCanvasElement): BuddyScene {
  const renderer = new WebGLRenderer({ canvas, alpha: true, antialias: true });
  // The canvas is small, so supersample (2x on standard screens, 3x on retina) for crisp, smooth edges.
  renderer.setPixelRatio(Math.min((window.devicePixelRatio || 1) * 2, 3));
  renderer.setClearColor(0x000000, 0);
  renderer.toneMapping = NeutralToneMapping;
  renderer.toneMappingExposure = 0.95;

  const scene = new Scene();
  const camera = new PerspectiveCamera(28, 1, 0.1, 50);
  camera.position.set(0, 0.2, 6.2);
  camera.lookAt(0, 0.15, 0);

  // Studio lighting: soft reflections from a room environment, a warm key, a fill, and a cool rim from behind
  // that separates the silhouette from the page.
  const pmrem = new PMREMGenerator(renderer);
  const envMap = pmrem.fromScene(new RoomEnvironment(), 0.04).texture;
  scene.environment = envMap;
  scene.environmentIntensity = 0.35;
  scene.add(new HemisphereLight(0xffffff, 0x8899bb, 0.35));
  const key = new DirectionalLight(0xfff1e0, 2.1);
  key.position.set(-2.5, 3.5, 4);
  const fill = new DirectionalLight(0xdbe7ff, 0.5);
  fill.position.set(3, 0.5, 3);
  const rim = new DirectionalLight(0x9cc3ff, 1.8);
  rim.position.set(2, 2.5, -4);
  scene.add(key, fill, rim);

  const mat = (color: string, finish: Finish = 'soft') => new MeshPhysicalMaterial({
    color: new Color(color),
    roughness: finish === 'gloss' ? 0.18 : 0.5,
    clearcoat: finish === 'gloss' ? 1 : 0.25,
    clearcoatRoughness: finish === 'gloss' ? 0.08 : 0.45
  });
  const flat = (color: string, opacity = 1) =>
    new MeshBasicMaterial({ color: new Color(color), transparent: opacity < 1, opacity });

  const part = (geo: SphereGeometry | CapsuleGeometry | CylinderGeometry, color: string, finish: Finish = 'soft') =>
    new Mesh(geo, mat(color, finish));

  // root: placement + pop; rig: hop/breathing; head and limbs hang off rig.
  const root = new Group();
  const rig = new Group();
  root.add(rig);
  scene.add(root);

  // ---- body ----
  const torso = part(new CapsuleGeometry(0.33, 0.26, 8, 20), COLORS.top);
  torso.position.y = -0.3;
  torso.scale.set(1, 1, 0.82);
  rig.add(torso);

  const neckline = new Mesh(new TorusGeometry(0.15, 0.03, 8, 24), flat(COLORS.shorts));
  neckline.rotation.x = Math.PI / 2 - 0.35;
  neckline.position.set(0, -0.02, 0.06);
  rig.add(neckline);

  // Coach's whistle on a lanyard.
  const lanyard = new Mesh(new TorusGeometry(0.2, 0.013, 6, 32), flat('#ffffff'));
  lanyard.rotation.x = Math.PI / 2 - 1.05;
  lanyard.position.set(0, -0.14, 0.17);
  const whistle = new Mesh(new CapsuleGeometry(0.035, 0.07, 6, 14), mat(COLORS.whistle, 'gloss'));
  whistle.rotation.z = Math.PI / 2;
  whistle.position.set(0.03, -0.31, 0.3);
  rig.add(lanyard, whistle);

  const shorts = part(new CylinderGeometry(0.31, 0.35, 0.22, 24), COLORS.shorts);
  shorts.position.y = -0.62;
  shorts.scale.z = 0.85;
  rig.add(shorts);

  // Legs hang from hip joints so they can walk, dangle and tuck.
  const hips: Group[] = [];
  for (const side of [-1, 1]) {
    const hip = new Group();
    hip.position.set(0.15 * side, -0.7, 0);
    const leg = part(new CapsuleGeometry(0.085, 0.1, 8, 20), COLORS.skin);
    leg.position.y = -0.1;
    const shoe = part(new SphereGeometry(0.13, 24, 16), COLORS.shoe, 'gloss');
    shoe.scale.set(1.05, 0.62, 1.45);
    shoe.position.set(0.01 * side, -0.24, 0.07);
    hip.add(leg, shoe);
    rig.add(hip);
    hips.push(hip);
  }

  // ---- arms: shoulder → upper arm → elbow → forearm + hand ----
  const makeArm = (side: number) => {
    const shoulder = new Group();
    shoulder.position.set(0.42 * side, -0.1, 0.02);
    const upper = part(new CapsuleGeometry(0.085, 0.18, 8, 20), COLORS.skin);
    upper.position.y = -0.13;
    // Bicep that swells during a flex.
    const bicep = new Mesh(new SphereGeometry(0.09, 16, 12), mat(COLORS.skin));
    bicep.position.set(0, -0.12, 0.05);
    bicep.scale.setScalar(0.01);
    const elbow = new Group();
    elbow.position.y = -0.28;
    const fore = part(new CapsuleGeometry(0.08, 0.14, 8, 20), COLORS.skin);
    fore.position.y = -0.1;
    const hand = part(new SphereGeometry(0.105, 18, 12), COLORS.skin);
    hand.position.y = -0.25;
    elbow.add(fore, hand);
    shoulder.add(upper, bicep, elbow);
    rig.add(shoulder);
    return { shoulder, elbow, bicep };
  };
  const leftArm = makeArm(-1);
  const rightArm = makeArm(1);

  // ---- head ----
  const head = new Group();
  head.position.y = 0.6;
  rig.add(head);

  const skull = part(new SphereGeometry(0.62, 40, 28), COLORS.skin);
  skull.scale.set(1.04, 0.96, 0.96);
  head.add(skull);

  for (const side of [-1, 1]) {
    const ear = part(new SphereGeometry(0.11, 14, 10), COLORS.skin);
    ear.scale.set(0.55, 1, 0.8);
    ear.position.set(0.63 * side, -0.04, 0);
    head.add(ear);
  }

  // Hair: a cap tilted back so the forehead shows, plus a cheeky tuft.
  const hair = new Mesh(new SphereGeometry(0.67, 36, 18, 0, Math.PI * 2, 0, 1.38), mat(COLORS.hair));
  hair.scale.set(1.05, 0.98, 0.99);
  hair.rotation.x = -0.5;
  head.add(hair);
  const tuft = new Mesh(new ConeGeometry(0.11, 0.26, 12), mat(COLORS.hair));
  tuft.position.set(0.08, 0.68, 0.02);
  tuft.rotation.set(-0.35, 0, -0.45);
  head.add(tuft);

  const headband = new Mesh(new TorusGeometry(0.615, 0.06, 10, 64), mat(COLORS.band));
  headband.rotation.x = Math.PI / 2 - 0.32;
  headband.position.set(0, 0.24, 0.02);
  head.add(headband);
  const stripe = new Mesh(new TorusGeometry(0.62, 0.02, 6, 64), flat(COLORS.top));
  stripe.rotation.copy(headband.rotation);
  stripe.position.copy(headband.position);
  head.add(stripe);

  // Face.
  const eyes: Group[] = [];
  const pupils: Group[] = [];
  const brows: Mesh[] = [];
  for (const side of [-1, 1]) {
    const eye = new Group();
    eye.position.set(0.23 * side, -0.03, 0.52);
    const white = new Mesh(new SphereGeometry(0.16, 28, 20), mat(COLORS.eye, 'gloss'));
    white.scale.z = 0.5;
    const pupil = new Group();
    const iris = new Mesh(new SphereGeometry(0.105, 24, 16), mat(COLORS.pupil, 'gloss'));
    iris.scale.z = 0.5;
    const sparkle = new Mesh(new SphereGeometry(0.03, 8, 6), flat('#ffffff'));
    sparkle.position.set(-0.03, 0.035, 0.05);
    pupil.add(iris, sparkle);
    pupil.position.z = 0.06;
    eye.add(white, pupil);
    head.add(eye);
    eyes.push(eye);
    pupils.push(pupil);

    const brow = new Mesh(new CapsuleGeometry(0.022, 0.1, 4, 10), mat(COLORS.hair));
    brow.rotation.z = Math.PI / 2;
    brow.position.set(0.23 * side, 0.17, 0.54);
    head.add(brow);
    brows.push(brow);

    const blush = new Mesh(new CircleGeometry(0.08, 18), flat(COLORS.blush, 0.6));
    blush.position.set(0.37 * side, -0.17, 0.46);
    blush.rotation.y = 0.65 * side;
    blush.scale.y = 0.6;
    head.add(blush);
  }

  const nose = new Mesh(new SphereGeometry(0.045, 12, 8), mat(COLORS.skin));
  nose.position.set(0, -0.1, 0.6);
  head.add(nose);

  const smile = new Mesh(new TorusGeometry(0.09, 0.022, 8, 20, Math.PI), flat(COLORS.pupil));
  smile.rotation.z = Math.PI;
  smile.position.set(0, -0.2, 0.575);
  const grin = new Group();
  const grinHole = new Mesh(new CircleGeometry(0.12, 24, Math.PI, Math.PI), flat(COLORS.mouth));
  const tongue = new Mesh(new CircleGeometry(0.06, 16, Math.PI, Math.PI), flat(COLORS.tongue));
  tongue.position.set(0, -0.055, 0.004);
  grin.add(grinHole, tongue);
  grin.position.set(0, -0.18, 0.585);
  grin.rotation.x = -0.3;
  head.add(smile, grin);

  // Sweat drop, shown once the day's workout is done.
  const sweat = new Group();
  const drop = new Mesh(new SphereGeometry(0.06, 16, 12), mat(COLORS.sweat, 'gloss'));
  const tip = new Mesh(new ConeGeometry(0.058, 0.1, 16), mat(COLORS.sweat, 'gloss'));
  tip.position.y = 0.07;
  sweat.add(drop, tip);
  sweat.position.set(0.5, 0.12, 0.38);
  sweat.visible = false;

  // Tiny barbell, only shown during a PR lift.
  const barbell = new Group();
  const bar = new Mesh(new CylinderGeometry(0.025, 0.025, 1.5, 12), mat('#cbd5e1', 'gloss'));
  bar.rotation.z = Math.PI / 2;
  barbell.add(bar);
  for (const side of [-1, 1]) {
    for (const [x, r] of [[0.6, 0.21], [0.68, 0.16]] as const) {
      const plate = new Mesh(new CylinderGeometry(r, r, 0.07, 24), mat(side > 0 && x === 0.6 ? COLORS.top : COLORS.shorts));
      plate.rotation.z = Math.PI / 2;
      plate.position.x = x * side;
      barbell.add(plate);
    }
  }
  barbell.position.set(0, -0.72, 0.42);
  barbell.visible = false;
  rig.add(barbell);
  head.add(sweat);

  // Soft contact shadow that shrinks as the buddy jumps.
  const shadowMap = shadowTexture();
  const shadow = new Mesh(new CircleGeometry(0.85, 32), new MeshBasicMaterial({ map: shadowMap, transparent: true, depthWrite: false }));
  shadow.rotation.x = -Math.PI / 2;
  shadow.position.y = -1.03;
  scene.add(shadow);

  // ---- animation state ----
  const start = performance.now() / 1000;
  let waveAt = start + POP_SECONDS * 0.6;
  let cheerAt = -10;
  let danceAt = -10;
  let liftAt = -10;
  let pointDir = 0;
  let pointWeight = 0;
  let nextBlink = start + 2.5;
  let mood: WorkoutState | null = null;
  const look = { x: 0, y: 0, tx: 0, ty: 0 };
  let pose: BuddyPose = 'stand';
  const weight = { walk: 0, jump: 0, sit: 0, held: 0 };
  const turn = { value: 0, target: 0 };
  const swing = { value: 0, target: 0 };

  const onPointer = (e: PointerEvent) => {
    const r = canvas.getBoundingClientRect();
    look.tx = Math.max(-1, Math.min(1, (e.clientX - (r.left + r.width / 2)) / 600));
    look.ty = Math.max(-1, Math.min(1, (e.clientY - (r.top + r.height / 2)) / 400));
  };
  window.addEventListener('pointermove', onPointer, { passive: true });

  const resize = () => {
    const { clientWidth: w, clientHeight: h } = canvas;
    if (!w || !h) return;
    renderer.setSize(w, h, false);
    camera.aspect = w / h;
    camera.updateProjectionMatrix();
  };
  resize();
  const resizeObserver = new ResizeObserver(resize);
  resizeObserver.observe(canvas);

  /** Layers the roaming pose over the idle/wave/flex animation already set for this frame. */
  const applyPose = (now: number) => {
    for (const key of Object.keys(weight) as (keyof typeof weight)[]) {
      weight[key] = lerp(weight[key], pose === key ? 1 : 0, 0.18);
    }
    turn.value = lerp(turn.value, turn.target, 0.12);
    swing.value = lerp(swing.value, swing.target, 0.15);
    swing.target *= 0.85;

    const stride = Math.sin(now * 11);
    const airborne = Math.max(weight.jump, weight.held);

    hips.forEach((hip, i) => {
      const side = i === 0 ? -1 : 1;
      hip.rotation.x =
        weight.walk * stride * 0.55 * side +
        weight.jump * -0.55 +
        weight.sit * (-1.3 + Math.sin(now * 2.4 + i * Math.PI) * 0.22) +
        weight.held * (Math.sin(now * 5 + i * 2) * 0.25 + swing.value * 0.5 * side);
    });

    // Arms swing opposite the legs when walking, and go up when jumping or carried.
    leftArm.shoulder.rotation.x = weight.walk * -stride * 0.5;
    rightArm.shoulder.rotation.x += weight.walk * stride * 0.5;
    leftArm.shoulder.rotation.z = lerp(leftArm.shoulder.rotation.z, weight.held > weight.jump ? -2.6 : -2.1, airborne);
    rightArm.shoulder.rotation.z = lerp(rightArm.shoulder.rotation.z, weight.held > weight.jump ? 2.6 : 2.1, airborne);

    rig.position.y += Math.abs(stride) * 0.06 * weight.walk;
    root.rotation.y += turn.value * 0.7;
    root.rotation.z = swing.value * 0.35 * weight.held;

    // Dance: bouncy hops, hip wiggle and alternating arm pumps.
    const d = (now - danceAt) / DANCE_SECONDS;
    const dancing = d >= 0 && d <= 1 ? clamp01(Math.min(d, 1 - d) * 6) : 0;
    if (dancing > 0) {
      const beat = now * 9;
      const leftUp = (Math.sin(beat) + 1) / 2;
      rig.position.y += Math.abs(Math.sin(beat)) * 0.12 * dancing;
      rig.rotation.z += Math.sin(beat) * 0.12 * dancing;
      leftArm.shoulder.rotation.z = lerp(leftArm.shoulder.rotation.z, lerp(-0.6, -2.5, leftUp), dancing);
      rightArm.shoulder.rotation.z = lerp(rightArm.shoulder.rotation.z, lerp(0.6, 2.5, 1 - leftUp), dancing);
    }

    // Point: one straight arm out toward the target, body turned a little that way.
    pointWeight = lerp(pointWeight, pointDir ? 1 : 0, 0.15);
    if (pointWeight > 0.01) {
      const side = pointDir || 1;
      const arm = side > 0 ? rightArm : leftArm;
      arm.shoulder.rotation.z = lerp(arm.shoulder.rotation.z, side * (1.05 + Math.sin(now * 6) * 0.08), pointWeight);
      arm.elbow.rotation.z = lerp(arm.elbow.rotation.z, side * 0.05, pointWeight);
      root.rotation.y += side * 0.35 * pointWeight;
    }

    // PR lift: crouch, pull the bar to the chest, hold it shaking, drop it, arms up.
    const l = (now - liftAt) / LIFT_SECONDS;
    const lifting = l >= 0 && l <= 1;
    barbell.visible = lifting && l < 0.82;
    let elbowBend = 0;
    if (lifting) {
      const crouch = smooth(l / 0.2);
      const pull = smooth((l - 0.2) / 0.2);
      const drop = smooth((l - 0.72) / 0.1);
      const victory = pulse((l - 0.82) / 0.18);
      const strain = l > 0.4 && l < 0.72 ? Math.sin(now * 40) * 0.015 : 0;
      barbell.position.set(strain, lerp(lerp(-0.72, -0.12, pull), -1.1, drop), 0.42);
      rig.position.y += -0.12 * crouch * (1 - pull) + victory * 0.2;
      const reach = lerp(-0.3 * crouch, -1.35, pull) * (1 - drop);
      leftArm.shoulder.rotation.x = reach;
      rightArm.shoulder.rotation.x = reach;
      elbowBend = -1 * pull * (1 - drop);
      leftArm.shoulder.rotation.z = lerp(-0.3, -2.5, victory);
      rightArm.shoulder.rotation.z = lerp(0.3, 2.5, victory);
      if (l > 0.4) head.rotation.z += Math.sin(now * 30) * 0.02 * (1 - drop);
    }
    leftArm.elbow.rotation.x = elbowBend;
    rightArm.elbow.rotation.x = elbowBend;

    if (airborne > 0.05 || dancing > 0.05 || lifting) {
      grin.visible = true;
      smile.visible = false;
    }
    (shadow.material as MeshBasicMaterial).opacity *= 1 - Math.max(weight.sit, airborne);
  };

  const tick = () => {
    const now = performance.now() / 1000;
    const t = now - start;
    const idle = t * IDLE_SPEED;

    // Pop: rise up and forward out of the background with an overshoot.
    const popLinear = clamp01(t / POP_SECONDS);
    const pop = easeOutBack(popLinear);
    root.position.set(0, lerp(-2.2, 0, pop), lerp(-4, 0, popLinear));
    root.rotation.y = lerp(-0.8, 0, pop);

    // Idle breathing, gentle sway, and a head bob (slower and sleepier on rest days).
    const sleepy = mood === 'rest';
    const breathe = Math.sin(idle * (sleepy ? 1.6 : 2.4));
    rig.position.y = breathe * 0.025;
    rig.rotation.z = Math.sin(idle * (sleepy ? 0.7 : 1.1)) * 0.035;
    head.rotation.z = Math.sin(idle * (sleepy ? 0.8 : 1.3) + 1) * (sleepy ? 0.09 : 0.05);
    torso.scale.set(1 + breathe * 0.015, 1 + breathe * 0.02, 0.82);

    // Cheer: hop with squash and stretch, then flex the left bicep.
    const c = (now - cheerAt) / CHEER_SECONDS;
    const hop = pulse(c / 0.4);
    rig.position.y += hop * 0.25;
    rig.scale.set(1 - hop * 0.05, 1 + hop * 0.06, 1);
    const flex = pulse(c);
    const flexPose = Math.min(flex * 1.6, 1);
    leftArm.shoulder.rotation.z = lerp(-0.35 - Math.sin(idle * 2.4) * 0.04, -1.5, flexPose);
    leftArm.elbow.rotation.z = lerp(-0.25, -2.1, flexPose);
    leftArm.bicep.scale.setScalar(0.01 + flexPose * (1 + Math.sin(now * 18) * 0.08));
    shadow.scale.setScalar(1 - hop * 0.35);
    (shadow.material as MeshBasicMaterial).opacity = popLinear;

    // Wave: right arm up beside the head, forearm wagging from the elbow.
    const w = (now - waveAt) / WAVE_SECONDS;
    const raise = Math.min(pulse(w * 1.1) * 1.5, 1);
    const wag = w >= 0 && w <= 1 ? Math.sin((now - waveAt) * 17) * 0.45 * raise : 0;
    rightArm.shoulder.rotation.z = lerp(0.35 + Math.sin(idle * 2.4) * 0.04, 2.25, raise);
    rightArm.shoulder.rotation.x = 0.3 * raise;
    rightArm.elbow.rotation.z = lerp(0.25, 0.75, raise) + wag;
    head.rotation.z += raise * -0.12;

    const excited = raise > 0.05 || flex > 0.05;
    grin.visible = excited;
    smile.visible = !excited;
    for (const b of brows) b.position.y = 0.17 + (excited ? 0.04 : sleepy ? -0.02 : 0);

    // Eyes follow the pointer; head and body turn a little toward it.
    look.x = lerp(look.x, look.tx, 0.08);
    look.y = lerp(look.y, look.ty, 0.08);
    root.rotation.y += look.x * 0.18;
    head.rotation.y = look.x * 0.3;
    head.rotation.x = look.y * 0.15;
    for (const p of pupils) p.position.set(look.x * 0.04, -look.y * 0.035, 0.06);

    // Blink every few seconds; rest days keep the eyes droopy.
    let lid = sleepy ? 0.4 : 1;
    if (now >= nextBlink) {
      const b = (now - nextBlink) / 0.16;
      if (b >= 1) nextBlink = now + 2.5 + Math.random() * 3;
      else lid *= 1 - Math.sin(Math.PI * b) * 0.9;
    }
    for (const e of eyes) e.scale.y = lid;

    sweat.position.y = 0.12 - ((t * 0.25) % 1) * 0.12;

    applyPose(now);

    renderer.render(scene, camera);
  };

  // Only animate while the buddy is on screen and the tab is visible.
  let onScreen = true;
  const sync = () => renderer.setAnimationLoop(onScreen && !document.hidden ? tick : null);
  const visibility = new IntersectionObserver(([entry]) => {
    onScreen = entry.isIntersecting;
    sync();
  });
  visibility.observe(canvas);
  document.addEventListener('visibilitychange', sync);
  sync();

  return {
    wave() {
      waveAt = performance.now() / 1000;
    },
    dance() {
      danceAt = performance.now() / 1000;
    },
    point(dir) {
      pointDir = Math.sign(dir);
    },
    lift() {
      liftAt = performance.now() / 1000;
    },
    cheer() {
      cheerAt = performance.now() / 1000;
    },
    setMood(state) {
      mood = state;
      sweat.visible = state === 'done';
    },
    setPose(next) {
      pose = next;
    },
    setFacing(dir) {
      turn.target = Math.sign(dir);
    },
    setSwing(amount) {
      swing.target = Math.max(-1, Math.min(1, amount));
    },
    dispose() {
      renderer.setAnimationLoop(null);
      window.removeEventListener('pointermove', onPointer);
      document.removeEventListener('visibilitychange', sync);
      visibility.disconnect();
      resizeObserver.disconnect();
      const materials = new Set<Material>();
      scene.traverse(obj => {
        if (obj instanceof Mesh) {
          obj.geometry.dispose();
          (Array.isArray(obj.material) ? obj.material : [obj.material]).forEach(m => materials.add(m));
        }
      });
      materials.forEach(m => m.dispose());
      shadowMap.dispose();
      envMap.dispose();
      pmrem.dispose();
      renderer.dispose();
    }
  };
}
