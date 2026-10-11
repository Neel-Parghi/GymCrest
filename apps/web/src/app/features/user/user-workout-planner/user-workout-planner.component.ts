import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, effect, inject, signal, viewChild } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { filter, map, take } from 'rxjs';
import { WorkoutPlannerComponent } from '../../trainer/workout-planner/workout-planner.component';
import { SegmentedTabsComponent } from '../../../shared/components/segmented-tabs/segmented-tabs.component';
import { SegmentedTab } from '../../../shared/models/segmented-tab.model';
import { AuthApiService } from '../../../core/services/auth-api.service';
import { MemberService } from '../../../core/services/member.service';
import { ActivePlanView, WorkoutSessionLogDto } from '../../../shared/models/workout-plan.model';
import {
  PLANNED_STATUS,
  exerciseSetCount,
  loggedState,
  resolveScheduledDay,
  startOfWeek,
  toDateKey,
  weekRange
} from '../../../shared/utils/workout-schedule';
import { CtaKind, DayExercise, TrainingDay, TrainingView } from '../../../core/models/user-training.model';

@Component({
  selector: 'app-user-workout-planner',
  standalone: true,
  imports: [DatePipe, RouterLink, SegmentedTabsComponent, WorkoutPlannerComponent],
  templateUrl: './user-workout-planner.component.html',
  styleUrl: './user-workout-planner.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserWorkoutPlannerComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private authService = inject(AuthApiService);
  private memberService = inject(MemberService);
  private destroyRef = inject(DestroyRef);

  readonly tabs: SegmentedTab[] = [
    { id: 'plan', label: 'My plan' },
    { id: 'library', label: 'Library' }
  ];

  readonly today = new Date();
  readonly view = toSignal(
    this.route.queryParamMap.pipe(map(params => (params.get('view') === 'library' ? 'library' : 'plan') as TrainingView)),
    { initialValue: 'plan' as TrainingView }
  );

  readonly loading = signal(true);
  readonly plan = signal<ActivePlanView | null>(null);
  private readonly logs = signal<WorkoutSessionLogDto[]>([]);
  readonly selectedIndex = signal((this.today.getDay() + 6) % 7);

  /** The embedded library (only rendered on the Library view) owns the plan creators. */
  private readonly library = viewChild(WorkoutPlannerComponent);
  private readonly pendingCreate = signal(false);

  constructor() {
    effect(() => {
      const library = this.library();
      if (library && this.pendingCreate()) {
        this.pendingCreate.set(false);
        library.openCreatorModal();
      }
    });
  }

  readonly week = computed<TrainingDay[]>(() => {
    const plan = this.plan();
    const logByDate = new Map(this.logs().map(l => [toDateKey(new Date(l.date)), l]));
    const todayKey = toDateKey(this.today);
    const monday = startOfWeek(this.today);

    return Array.from({ length: 7 }, (_, i) => {
      const date = new Date(monday);
      date.setDate(monday.getDate() + i);
      const key = toDateKey(date);
      const log = logByDate.get(key);
      // A day planned from the calendar replaces the plan's usual day for that date.
      const planned = log?.status === PLANNED_STATUS ? plan?.days?.find(d => d.dayName === log.dayName) : undefined;
      const scheduled = planned ?? (plan?.days?.length ? resolveScheduledDay(plan.days, date) : null);
      const day = scheduled && !scheduled.isRestDay ? scheduled : null;

      return {
        date,
        day,
        tag: day ? (day.category?.split(',')[0].trim() || day.dayName) : 'Rest',
        isToday: key === todayKey,
        isFuture: key > todayKey,
        logged: loggedState(log?.status)
      };
    });
  });

  readonly selected = computed(() => this.week()[this.selectedIndex()]);

  readonly selectedExercises = computed<DayExercise[]>(() =>
    (this.selected().day?.exercises ?? []).map(ex => ({
      name: ex.exerciseName,
      sets: exerciseSetCount(ex.sets),
      reps: ex.reps || '',
      notes: ex.notes || ''
    }))
  );

  readonly selectedSetCount = computed(() => this.selectedExercises().reduce((sum, ex) => sum + ex.sets, 0));

  readonly cta = computed<{ kind: CtaKind; label: string } | null>(() => {
    const s = this.selected();
    if (s.logged === 'done') return { kind: 'view', label: 'View in calendar' };
    if (!s.day || s.isFuture) return null;
    if (s.isToday) return { kind: 'start', label: `Log ${s.day.dayName}` };
    return { kind: 'log', label: 'Log this workout' };
  });

  ngOnInit(): void {
    this.authService.userProfile$.pipe(
      filter(profile => !!profile?.id),
      take(1),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(profile => this.loadPlan(profile!.id));
  }

  private loadPlan(userId: string): void {
    // The planner shows the current week, so only this week's logs are fetched.
    this.memberService.getTrainingOverview(userId, weekRange(this.today)).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(({ plan, logs }) => {
      this.plan.set(plan?.days?.length ? plan : null);
      this.logs.set(logs);
      this.loading.set(false);
    });
  }

  setView(view: string): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { view: view === 'library' ? 'library' : null },
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }

  createPlan(): void {
    const library = this.library();
    if (library) {
      library.openCreatorModal();
      return;
    }
    this.pendingCreate.set(true);
    this.setView('library');
  }

  selectDay(index: number): void {
    this.selectedIndex.set(index);
  }

  runCta(): void {
    const cta = this.cta();
    const s = this.selected();
    if (!cta) return;

    if (cta.kind === 'view') {
      this.router.navigate(['/user/workout-calendar']);
      return;
    }

    this.router.navigate(['/user/performance'], {
      state: {
        routineName: s.day?.dayName,
        templateDayName: s.day?.dayName,
        ...(cta.kind === 'log' ? { loggingDate: s.date.toISOString() } : {})
      }
    });
  }
}
