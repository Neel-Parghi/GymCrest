import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, PLATFORM_ID, computed, effect, inject, signal, viewChild } from '@angular/core';
import { DatePipe, DecimalPipe, isPlatformBrowser } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { FIELD_LIMITS } from '../../../shared/constants/validation.constants';
import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { catchError, filter, forkJoin, map, of, take } from 'rxjs';
import { AuthApiService } from '../../../core/services/auth-api.service';
import { MemberService } from '../../../core/services/member.service';
import { UserService } from '../../../core/services/user.service';
import { DietTrackingService } from '../../../core/services/diet-tracking.service';
import { AnnouncementService } from '../../../core/services/announcement.service';
import { DietLogDto } from '../../../shared/models/diet-tracking.model';
import { GymAnnouncementResponse } from '../../../shared/models/announcement.model';
import { ActivePlanView, WorkoutSessionLogDto } from '../../../shared/models/workout-plan.model';
import { PLANNED_STATUS, exerciseSetCount, loggedState, resolveScheduledDay, startOfWeek, toDateKey, weekRange } from '../../../shared/utils/workout-schedule';
import { mealTimeToMinutes } from '../../../shared/utils/meal-time';
import { NUTRITION_TARGET_FALLBACK, percentOf } from '../../../shared/utils/nutrition';
import { goalLabel, toGoalId } from '../../../shared/constants/fitness-preferences';
import { GymBuddyComponent } from './components/gym-buddy/gym-buddy.component';
import { recall, remember } from './components/gym-buddy/buddy-memory';
import {
  ActivityRing, BuddyEvent, BuddyFacts, DailyRoutineItem, GoalRing, MacroProgress, NutritionToday, RoutineForm, TodayWorkout,
  UserDashboardSummary, WeekDay, WeekDayState
} from '../../../core/models/user-dashboard.model';
import { greetingFor } from '../../../shared/utils/greeting';

const WEEK_LABELS = ['M', 'T', 'W', 'T', 'F', 'S', 'S'];
const RING_RADII = { calories: 58, active: 44, goal: 30 };
const ROUTINE_PREVIEW = 5;
/** From this hour the buddy points out a planned workout that hasn't been logged yet. */
const NUDGE_FROM_HOUR = 18;
/** Daily cardio the weight-loss and endurance goal rings aim for. */
const CARDIO_TARGET_MINUTES = 30;

@Component({
  selector: 'app-user-dashboard',
  standalone: true,
  imports: [DatePipe, DecimalPipe, RouterLink, ReactiveFormsModule, DragDropModule, GymBuddyComponent],
  templateUrl: './user-dashboard.component.html',
  styleUrl: './user-dashboard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserDashboardComponent implements OnInit {
  readonly limits = FIELD_LIMITS;

  private authService = inject(AuthApiService);
  private memberService = inject(MemberService);
  private userService = inject(UserService);
  private dietTrackingService = inject(DietTrackingService);
  private announcementService = inject(AnnouncementService);
  private platformId = inject(PLATFORM_ID);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  readonly today = new Date();
  readonly greeting = greetingFor(this.today.getHours());
  readonly isFirstTime = signal(false);

  private readonly profile = toSignal(this.authService.userProfile$, { initialValue: null });
  private readonly buddy = viewChild(GymBuddyComponent);
  /** Reactions that happen before the buddy appears (it waits for the summary) play once it does. */
  private readonly pendingBuddyEvents: BuddyEvent[] = [];
  private readonly todayKey = toDateKey(this.today);

  readonly summary = signal<UserDashboardSummary | null>(null);
  readonly routines = signal<DailyRoutineItem[]>([]);
  readonly todayWorkout = signal<TodayWorkout | null>(null);
  readonly week = signal<WeekDay[]>(this.buildWeek([]));
  readonly nutrition = signal<NutritionToday | null>(null);
  readonly isAddingRoutine = signal(false);
  readonly showAllRoutines = signal(false);
  readonly announcements = signal<GymAnnouncementResponse[]>([]);
  readonly showAllAnnouncements = signal(false);

  readonly visibleAnnouncements = computed(() =>
    this.showAllAnnouncements() ? this.announcements() : this.announcements().slice(0, 1)
  );

  readonly routineForm = new FormGroup<RoutineForm>({
    title: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(FIELD_LIMITS.SHORT_TEXT)] }),
    amount: new FormControl('', { nonNullable: true, validators: [this.routineAmountValidator()] })
  });

  readonly myGym = toSignal(this.userService.getMyGym().pipe(
    map(res => (res?.data as { gymName?: string; gymLogoUrl?: string | null } | null) ?? null)
  ));

  useGymCrestLogo(event: Event): void {
    const img = event.target as HTMLImageElement;
    if (!img.src.endsWith('favicon.svg')) img.src = 'favicon.svg';
  }

  readonly userName = computed(() => this.summary()?.userName || this.profile()?.firstName || 'Member');

  readonly initials = computed(() => {
    const p = this.profile();
    const fromProfile = ((p?.firstName?.charAt(0) ?? '') + (p?.lastName?.charAt(0) ?? '')).toUpperCase();
    return fromProfile || this.userName().charAt(0).toUpperCase();
  });

  readonly goalTitle = computed(() => goalLabel(this.summary()?.goalTitle));

  /** The buddy appears once the summary has loaded, unless the member turned it off in settings. */
  readonly showBuddy = computed(() => {
    const summary = this.summary();
    return !!summary && summary.showGymBuddy !== false;
  });

  /**
   * Today's training step towards the member's goal. Food is left out on purpose: the nutrition card
   * already tracks calories and protein.
   */
  readonly goalRing = computed<GoalRing>(() => {
    const s = this.summary();
    switch (toGoalId(s?.goalTitle)) {
      case 'weight_loss':
      case 'endurance':
        return this.goalProgress('Cardio', s?.cardioMinutesToday ?? 0, CARDIO_TARGET_MINUTES, 'min');
      case 'flexibility': {
        const routines = this.routines();
        return routines.length
          ? this.goalProgress('Routines', this.doneRoutineCount(), routines.length, 'done')
          : this.exercisesRing();
      }
      default:
        return this.exercisesRing();
    }
  });

  readonly activity = computed(() => {
    const s = this.summary();
    const calories = s?.caloriesBurnedToday ?? 0;
    const calorieTarget = s?.targetCaloriesBurn || 400;
    const active = s?.activeTrainingTimeMinutes ?? 0;
    const activeTarget = s?.targetTrainingTime || 60;
    const goal = this.goalRing();
    return {
      calories,
      calorieTarget,
      active,
      activeTarget,
      goal,
      rings: [
        this.ring('calories', percentOf(calories, calorieTarget)),
        this.ring('active', percentOf(active, activeTarget)),
        this.ring('goal', goal.pct)
      ] as ActivityRing[]
    };
  });

  /** Long-term weight goal, shown with body composition rather than in today's rings. */
  readonly weightGoal = computed(() => {
    const s = this.summary();
    return s?.targetWeight ? { target: s.targetWeight, pct: Math.min(s.goalProgressPct ?? 0, 100) } : null;
  });

  /** Personal records as medal tiles: rank, split weight ("110 kg" → 110 + kg) and a rank icon. */
  readonly prTiles = computed(() =>
    (this.summary()?.personalRecords ?? []).map((pr, i) => {
      const match = /^\s*([\d.,]+)\s*(.*)$/.exec(pr.weight ?? '');
      const rank = i + 1;
      return {
        ...pr,
        rank,
        value: match ? match[1] : pr.weight,
        unit: match ? match[2] || 'kg' : '',
        icon: rank === 1 ? 'fa-trophy' : rank <= 3 ? 'fa-medal' : 'fa-dumbbell'
      };
    })
  );

  /** Everything the gym buddy can bring up in its speech bubble. */
  readonly buddyFacts = computed<BuddyFacts>(() => {
    const s = this.summary();
    const workout = this.todayWorkout();
    const top = this.prTiles()[0];
    return {
      firstName: this.userName().split(' ')[0],
      isFirstTime: this.isFirstTime(),
      state: workout?.state ?? null,
      workoutTitle: workout?.title ?? '',
      streak: s?.workoutStreak ?? 0,
      streakAtRisk: !!s?.streakAtRisk,
      monthlyCount: s?.monthlySessionCount ?? 0,
      monthlyTarget: s?.monthlySessionTarget ?? 0,
      topRecord: top ? { name: top.name, weight: top.weight } : null,
      caloriesLeft: this.nutrition()?.left ?? null,
      readyMuscle: s?.muscleRecovery?.find(m => m.status === 'Ready')?.name ?? null,
      goalTitle: this.goalTitle()
    };
  });

  readonly doneRoutineCount = computed(() => this.routines().filter(r => r.completed).length);

  /** Keeps the card a steady height: the first few routines, the rest behind "Show all". */
  readonly visibleRoutines = computed(() => {
    const all = this.routines();
    return this.showAllRoutines() || all.length <= ROUTINE_PREVIEW + 1 ? all : all.slice(0, ROUTINE_PREVIEW);
  });

  readonly hasMoreRoutines = computed(() => this.routines().length > ROUTINE_PREVIEW + 1);

  readonly bmiCategory = computed(() => {
    const bmi = this.summary()?.bmi ?? 0;
    if (!bmi) return null;
    if (bmi < 18.5) return { label: 'Underweight', tone: 'warn' };
    if (bmi < 25) return { label: 'Normal', tone: 'good' };
    if (bmi < 30) return { label: 'Overweight', tone: 'warn' };
    return { label: 'Obese', tone: 'bad' };
  });

  constructor() {
    effect(() => {
      const buddy = this.buddy();
      if (buddy) this.pendingBuddyEvents.splice(0).forEach(event => buddy.react(event));
    });
  }

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId) && sessionStorage.getItem('justFinishedOnboarding') === 'true') {
      this.isFirstTime.set(true);
      sessionStorage.removeItem('justFinishedOnboarding');
      setTimeout(() => this.triggerConfetti(), 500);
    }

    this.loadSummary();
    this.loadNutrition();
    this.loadAnnouncements();

    this.authService.userProfile$.pipe(
      filter(profile => !!profile?.id),
      take(1),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(profile => this.loadTraining(profile!.id));
  }

  private loadSummary(): void {
    this.userService.getDashboardSummary().pipe(
      map(res => (res?.data ?? null) as UserDashboardSummary | null),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: summary => {
        this.summary.set(summary);
        this.routines.set([...(summary?.dailyRoutines ?? [])]);
        this.checkNewRecord(summary?.personalRecords ?? []);
      },
      error: err => console.error('Error fetching dashboard summary:', err)
    });
  }

  private loadTraining(userId: string): void {
    // Only this week's logs are needed: the week strip and today's card.
    this.memberService.getTrainingOverview(userId, weekRange(this.today)).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(({ plan, logs }) => {
      this.week.set(this.buildWeek(logs));
      const workout = this.buildTodayWorkout(plan, logs);
      this.todayWorkout.set(workout);
      this.checkWorkout(workout);
    });
  }

  private loadAnnouncements(): void {
    this.announcementService.getMyGymAnnouncementList().pipe(
      catchError(() => of([] as GymAnnouncementResponse[])),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(list => this.announcements.set(list));
  }

  private loadNutrition(): void {
    this.dietTrackingService.getUserDietLog(toDateKey(this.today)).pipe(
      map(res => res?.data ?? null),
      catchError(() => of(null)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(log => {
      this.nutrition.set(log ? this.buildNutrition(log) : null);
      this.checkMealLogged(log?.mealEntries?.length ?? 0);
    });
  }

  private buildTodayWorkout(plan: ActivePlanView | null, logs: WorkoutSessionLogDto[]): TodayWorkout {
    const todayKey = toDateKey(this.today);
    const planName = plan?.name ?? '';
    const loggedToday = logs.find(l => toDateKey(new Date(l.date)) === todayKey);
    const loggedAs = loggedState(loggedToday?.status);

    if (loggedToday && loggedAs) {
      const isRest = loggedAs === 'rest';
      const planDay = this.planDayFor(plan, loggedToday.dayName);
      return {
        state: isRest ? 'rest' : 'done',
        planName,
        title: isRest ? 'Rest day logged' : loggedToday.dayName,
        muscles: '',
        exerciseCount: loggedToday.exercisesCompleted ?? 0,
        setCount: loggedToday.totalSets ?? 0,
        plannedExerciseCount: isRest ? 0 : planDay?.exercises?.length ?? loggedToday.loggedExercises?.length ?? 0
      };
    }

    // A workout planned for today from the calendar replaces the plan's usual day.
    if (loggedToday?.status === PLANNED_STATUS) {
      const exercises = loggedToday.loggedExercises ?? [];
      const planDay = this.planDayFor(plan, loggedToday.dayName);
      return {
        state: 'ready',
        planName,
        title: loggedToday.dayName,
        muscles: (planDay?.category ?? '').split(',').map(c => c.trim()).filter(Boolean).join(' · '),
        exerciseCount: exercises.length,
        setCount: exercises.reduce((sum, ex) => sum + (ex.loggedSets?.length ?? 0), 0),
        plannedExerciseCount: exercises.length
      };
    }

    if (!plan?.days?.length) {
      return { state: 'none', planName, title: 'No workout plan yet', muscles: '', exerciseCount: 0, setCount: 0, plannedExerciseCount: 0 };
    }

    const day = resolveScheduledDay(plan.days, this.today);
    if (!day || day.isRestDay) {
      return { state: 'rest', planName, title: 'Rest day', muscles: '', exerciseCount: 0, setCount: 0, plannedExerciseCount: 0 };
    }

    const exercises = day.exercises ?? [];
    return {
      state: 'ready',
      planName,
      title: day.dayName,
      muscles: (day.category ?? '').split(',').map(c => c.trim()).filter(Boolean).join(' · '),
      exerciseCount: exercises.length,
      setCount: exercises.reduce((sum, ex) => sum + exerciseSetCount(ex.sets), 0),
      plannedExerciseCount: exercises.length
    };
  }

  /** Logged sessions name the day "Push - Chest, Triceps" when the plan day has a category. */
  private planDayFor(plan: ActivePlanView | null, loggedDayName: string) {
    return plan?.days?.find(d =>
      d.dayName === loggedDayName || (!!d.category && `${d.dayName} - ${d.category}` === loggedDayName));
  }

  private buildWeek(logs: WorkoutSessionLogDto[]): WeekDay[] {
    const statusByDate = new Map(logs.map(l => [toDateKey(new Date(l.date)), loggedState(l.status)]));
    const todayKey = toDateKey(this.today);
    const monday = startOfWeek(this.today);

    return WEEK_LABELS.map((label, i) => {
      const date = new Date(monday);
      date.setDate(monday.getDate() + i);
      const key = toDateKey(date);
      const status = statusByDate.get(key);

      let state: WeekDayState;
      if (status) state = status;
      else if (key === todayKey) state = 'today';
      else state = key < todayKey ? 'missed' : 'upcoming';

      return { label, state };
    });
  }

  private buildNutrition(log: DietLogDto): NutritionToday {
    const target = log.targetCalories || NUTRITION_TARGET_FALLBACK.calories;
    const eaten = log.totalCalories ?? 0;
    const loggedMealIds = new Set((log.mealEntries ?? []).map(e => e.sourceDietPlanMealId).filter(Boolean));
    const nextMeal = [...(log.assignedMeals ?? [])]
      .sort((a, b) => mealTimeToMinutes(a.time) - mealTimeToMinutes(b.time))
      .find(m => !loggedMealIds.has(m.id)) ?? null;

    const macro = (label: string, tone: MacroProgress['tone'], value: number, macroTarget: number): MacroProgress =>
      ({ label, tone, value: value ?? 0, target: macroTarget, pct: percentOf(value ?? 0, macroTarget) });

    return {
      eaten,
      target,
      left: Math.max(target - eaten, 0),
      pct: percentOf(eaten, target),
      nextMeal,
      macros: [
        macro('Protein', 'protein', log.totalProtein, log.targetProtein || NUTRITION_TARGET_FALLBACK.protein),
        macro('Carbs', 'carbs', log.totalCarbs, log.targetCarbs || NUTRITION_TARGET_FALLBACK.carbs),
        macro('Fats', 'fats', log.totalFats, log.targetFats || NUTRITION_TARGET_FALLBACK.fats)
      ]
    };
  }

  /**
   * Exercises done against today's plan, e.g. 6 / 7 or 8 / 7 when extras were added. A rest day
   * counts as complete; without a planned workout, any logged exercise fills the ring.
   */
  private exercisesRing(): GoalRing {
    const workout = this.todayWorkout();
    const done = this.summary()?.exercisesDoneToday ?? 0;
    const planned = workout?.plannedExerciseCount ?? 0;
    if (workout?.state === 'rest' && !done) return { label: 'Exercises', pct: 100, value: 'Rest day', unit: '' };
    if (planned > 0) return this.goalProgress('Exercises', done, planned, 'done');
    return { label: 'Exercises', pct: done > 0 ? 100 : 0, value: `${done}`, unit: 'done' };
  }

  private goalProgress(label: string, value: number, target: number, unit: string): GoalRing {
    return { label, pct: percentOf(value, target), value: `${Math.round(value)} / ${Math.round(target)}`, unit };
  }

  private ring(key: ActivityRing['key'], pct: number): ActivityRing {
    const radius = RING_RADII[key];
    const circumference = 2 * Math.PI * radius;
    return { key, radius, dash: `${(circumference * pct / 100).toFixed(1)} ${circumference.toFixed(1)}` };
  }

  startTodaysWorkout(): void {
    this.router.navigate(['/user/performance']);
  }

  logMeal(): void {
    this.router.navigate(['/user/diet-tracker']);
  }

  toggleAllAnnouncements(): void {
    this.showAllAnnouncements.update(all => !all);
  }

  toggleShowAllRoutines(): void {
    this.showAllRoutines.update(all => !all);
  }

  toggleAddRoutine(): void {
    this.isAddingRoutine.update(open => !open);
  }

  toggleRoutine(item: DailyRoutineItem): void {
    this.routines.update(list => list.map(r => (r.id === item.id ? { ...r, completed: !r.completed } : r)));
    this.userService.toggleDailyRoutine(item.id).subscribe();

    if (!item.completed) {
      const allDone = this.routines().every(r => r.completed);
      this.buddyReact({
        act: 'dance',
        target: '.area-routines',
        text: allDone ? 'Every routine done today. Legend!' : `${item.title}, done. Nice one!`,
        icon: allDone ? 'fa-star' : 'fa-circle-check'
      });
    }
  }

  // ---------- gym buddy reactions ----------

  private buddyReact(event: BuddyEvent): void {
    if (!isPlatformBrowser(this.platformId) || this.summary()?.showGymBuddy === false) return;
    const buddy = this.buddy();
    if (buddy) buddy.react(event);
    else this.pendingBuddyEvents.push(event);
  }

  /** Per-user key, so two members sharing a browser don't share the buddy's memory. */
  private memoryKey(name: string): string {
    return `${this.profile()?.id ?? 'me'}.${name}`;
  }

  /** Dance on the hero once a day when today's workout is logged; otherwise, in the evening, point at it. */
  private checkWorkout(workout: TodayWorkout): void {
    if (!isPlatformBrowser(this.platformId)) return;
    if (workout.state === 'done' && recall(this.memoryKey('workout')) !== this.todayKey) {
      remember(this.memoryKey('workout'), this.todayKey);
      this.buddyReact({ act: 'dance', target: '.area-hero', text: `${workout.title} done. You crushed it!`, icon: 'fa-medal' });
    } else if (workout.state === 'ready' && this.today.getHours() >= NUDGE_FROM_HOUR
      && recall(this.memoryKey('nudge')) !== this.todayKey) {
      remember(this.memoryKey('nudge'), this.todayKey);
      this.buddyReact({
        act: 'point',
        target: '.area-hero',
        focus: '.area-hero .hero-cta',
        text: `${workout.title} is still waiting. Log it?`,
        icon: 'fa-hand-point-right'
      });
    }
  }

  /** Dance on the nutrition card when there are more meals logged today than last time he looked. */
  private checkMealLogged(count: number): void {
    if (!isPlatformBrowser(this.platformId)) return;
    const [day, seen] = (recall(this.memoryKey('meals')) ?? '').split('|');
    const before = day === this.todayKey ? Number(seen) || 0 : 0;
    remember(this.memoryKey('meals'), `${this.todayKey}|${count}`);
    if (count > before) {
      this.buddyReact({
        act: 'dance',
        target: '.area-nutrition',
        text: before === 0 && count === 1 ? 'First meal logged. Fuel up!' : 'Meal logged. Good fuel!',
        icon: 'fa-utensils'
      });
    }
  }

  /** Barbell party on the trophy card when a record is heavier than the last one he saw (or new to the top list). */
  private checkNewRecord(records: UserDashboardSummary['personalRecords']): void {
    if (!isPlatformBrowser(this.platformId)) return;
    const kg = (weight: string) => parseFloat((weight ?? '').replace(',', '')) || 0;
    const current = Object.fromEntries(records.map(r => [r.name, kg(r.weight)]));
    const stored = recall(this.memoryKey('prs'));
    remember(this.memoryKey('prs'), JSON.stringify(current));
    // First visit: just learn the records, nothing to celebrate yet.
    if (!stored) return;

    let previous: Record<string, number> = {};
    try {
      previous = JSON.parse(stored);
    } catch {
      return;
    }
    const record = records.find(r => kg(r.weight) > (previous[r.name] ?? 0));
    if (record) {
      this.buddyReact({ act: 'lift', target: '.area-prs', text: `New PR! ${record.name} at ${record.weight}!`, icon: 'fa-trophy' });
    }
  }

  addRoutine(): void {
    if (this.routineForm.invalid) {
      this.routineForm.markAllAsTouched();
      return;
    }

    const { title, amount } = this.routineForm.getRawValue();
    const dto = { title: title.trim(), amount: amount.trim() || null, order: this.routines().length };

    this.userService.createDailyRoutine(dto).subscribe(res => {
      const created = (res?.data ?? res) as DailyRoutineItem;
      this.routines.update(list => [...list, created]);
      // Make sure the routine just added is visible
      if (this.hasMoreRoutines()) this.showAllRoutines.set(true);
      this.routineForm.reset();
      this.isAddingRoutine.set(false);
    });
  }

  removeRoutine(item: DailyRoutineItem): void {
    this.userService.deleteDailyRoutine(item.id).subscribe(() => {
      this.routines.update(list => list.filter(r => r.id !== item.id));
    });
  }

  dropRoutine(event: CdkDragDrop<DailyRoutineItem[]>): void {
    if (event.previousIndex === event.currentIndex) return;

    const reordered = [...this.routines()];
    moveItemInArray(reordered, event.previousIndex, event.currentIndex);
    this.routines.set(reordered.map((r, index) => ({ ...r, order: index })));

    forkJoin(this.routines().map((routine, index) =>
      this.userService.updateDailyRoutine(routine.id, {
        title: routine.title,
        amount: routine.amount,
        time: routine.time,
        order: index
      })
    )).subscribe();
  }

  private routineAmountValidator(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const value = control.value;
      if (!value) return null;

      const strVal = value.toString().trim();
      if (strVal.length > 20) return { maxLengthExceeded: true };
      if (/^-?\d+$/.test(strVal) && strVal.replace('-', '').length > 5) return { maxDigitsExceeded: true };
      return null;
    };
  }

  private triggerConfetti(): void {
    if (!isPlatformBrowser(this.platformId)) return;

    import('canvas-confetti').then(module => {
      const confetti = module.default || module;
      const duration = 3 * 1000;
      const animationEnd = Date.now() + duration;
      const defaults = { startVelocity: 30, spread: 360, ticks: 60, zIndex: 9999 };
      const randomInRange = (min: number, max: number) => Math.random() * (max - min) + min;

      const interval = setInterval(() => {
        const timeLeft = animationEnd - Date.now();
        if (timeLeft <= 0) {
          clearInterval(interval);
          return;
        }

        const particleCount = 50 * (timeLeft / duration);
        confetti({ ...defaults, particleCount, origin: { x: randomInRange(0.1, 0.3), y: Math.random() - 0.2 } });
        confetti({ ...defaults, particleCount, origin: { x: randomInRange(0.7, 0.9), y: Math.random() - 0.2 } });
      }, 250);
    });
  }
}
