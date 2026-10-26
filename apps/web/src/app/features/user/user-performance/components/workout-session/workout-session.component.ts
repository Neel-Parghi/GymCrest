import {
  ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal, untracked
} from '@angular/core';
import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { FIELD_LIMITS } from '../../../../../shared/constants/validation.constants';
import { catchError, map, of } from 'rxjs';
import { WorkoutMasterService } from '../../../../../core/services/workout-master.service';
import { WorkoutPlanService } from '../../../../../core/services/workout-plan.service';
import { NotificationService } from '../../../../../core/services/notification.service';
import { ConfirmationService } from '../../../../../core/services/confirmation.service';
import { CONSTANTS } from '../../../../../core/constants/constants';
import { DropdownComponent } from '../../../../../shared/components/dropdown/dropdown.component';
import { DropdownOption } from '../../../../../shared/models/dropdown.model';
import { DailyPlanner, WorkoutSessionLogDto } from '../../../../../shared/models/workout-plan.model';
import { SET_LIMITS, clampSetValue, isCardioExercise } from '../../../../../shared/utils/workout-session';
import { exerciseSetCount, toDateKey } from '../../../../../shared/utils/workout-schedule';
import { LastPerformance, PlanDayOption, SessionExerciseForm, SessionExerciseValue, SessionSetForm, SessionSetValue, SessionSheet, SessionWorkoutInput, SessionWorkoutResult, SetField } from '../../../../../shared/models/workout-session.model';

const TRACK = CONSTANTS.MEMBER_DETAIL_MODULE.TRACK_PERFORMANCE;
const STRENGTH_TARGET = '8-12 reps';
const CARDIO_TARGET = '20 mins';

@Component({
  selector: 'app-workout-session',
  standalone: true,
  imports: [DatePipe, NgTemplateOutlet, RouterLink, ReactiveFormsModule, DropdownComponent],
  templateUrl: './workout-session.component.html',
  styleUrl: './workout-session.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WorkoutSessionComponent {
  readonly limits = FIELD_LIMITS;

  private workoutMasterService = inject(WorkoutMasterService);
  private workoutPlanService = inject(WorkoutPlanService);
  private notification = inject(NotificationService);
  private confirmation = inject(ConfirmationService);

  readonly workout = input<SessionWorkoutInput | null>(null);
  readonly planDays = input<PlanDayOption[]>([]);
  readonly loggingDate = input<Date>(new Date());
  readonly history = input<WorkoutSessionLogDto[]>([]);

  readonly save = output<SessionWorkoutResult>();
  readonly exit = output<void>();

  readonly form = new FormArray<SessionExerciseForm>([]);
  readonly dayName = signal('');
  readonly isRestDay = signal(false);
  readonly currentIndex = signal(0);
  readonly sheet = signal<SessionSheet>(null);

  readonly exercises = toSignal(
    this.form.valueChanges.pipe(map(() => this.form.getRawValue() as SessionExerciseValue[])),
    { initialValue: [] as SessionExerciseValue[] }
  );

  readonly dailyPlans = toSignal(
    this.workoutPlanService.getPlans('Daily').pipe(
      map(plans => (plans as DailyPlanner[]).map(p => ({
        dayName: p.name,
        category: p.targetCategory,
        exercises: p.exercises
      }) as PlanDayOption)),
      catchError(() => of([] as PlanDayOption[]))
    ),
    { initialValue: [] as PlanDayOption[] }
  );

  readonly extraDays = computed(() => {
    const taken = new Set(this.planDays().map(d => d.dayName));
    return this.dailyPlans().filter(d => !taken.has(d.dayName));
  });

  readonly dayControl = new FormControl<string | null>(null);
  readonly dayOptions = computed<DropdownOption[]>(() => [
    ...this.planDays().map(d => ({
      label: d.category && !d.isRestDay ? `${d.dayName} · ${d.category}` : d.dayName,
      value: d.dayName,
      icon: 'fa-solid fa-calendar-day'
    })),
    ...this.extraDays().map(d => ({
      label: `${d.dayName} · Daily plan`,
      value: d.dayName,
      icon: 'fa-solid fa-bolt'
    }))
  ]);

  readonly exerciseNameControl = new FormControl('', { nonNullable: true, validators: [Validators.maxLength(FIELD_LIMITS.SHORT_TEXT)] });
  readonly useCustomName = signal(false);
  readonly catalogOptions = toSignal(
    this.workoutMasterService.getExercises().pipe(
      map(list => (list || []).map(e => ({ label: `${e.name} (${e.category})`, value: e.name }) as DropdownOption)),
      catchError(() => of([] as DropdownOption[]))
    ),
    { initialValue: [] as DropdownOption[] }
  );

  readonly isBackdating = computed(() => toDateKey(this.loggingDate()) !== toDateKey(new Date()));

  readonly current = computed(() => this.exercises()[this.currentIndex()] ?? null);

  /** The workout as it was handed in, so leaving only warns about changes made since. */
  private loadedState = '';

  readonly completedSetCount = computed(() =>
    this.exercises().reduce((sum, ex) => sum + (ex.skipped ? 0 : ex.sets.filter(s => s.completed).length), 0)
  );

  /** One segment per exercise for the progress bar. */
  readonly progressSegments = computed(() =>
    this.exercises().map((ex, i) => {
      if (ex.skipped) return 'skipped';
      if (ex.sets.length && ex.sets.every(s => s.completed)) return 'done';
      return i === this.currentIndex() ? 'current' : 'pending';
    })
  );

  /** Most recent earlier performance per exercise (lower-cased name). */
  private readonly lastPerformance = computed(() => {
    const before = toDateKey(this.loggingDate());
    const byName = new Map<string, LastPerformance>();
    const logs = [...this.history()]
      .filter(l => toDateKey(new Date(l.date)) < before)
      .sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());

    for (const log of logs) {
      for (const ex of log.loggedExercises ?? []) {
        const key = ex.name?.toLowerCase().trim();
        const sets = (ex.loggedSets ?? []).filter(s => s.completed).sort((a, b) => a.setNo - b.setNo);
        if (!key || ex.skipped || byName.has(key) || sets.length === 0) continue;

        const top = sets.reduce((best, s) => (s.weight > best.weight ? s : best), sets[0]);
        const summary = ex.isCardio
          ? `${top.reps} min${top.weight ? ` · ${top.weight} km` : ''}`
          : `${sets.length} × ${top.reps}${top.weight ? ` @ ${top.weight} kg` : ''}`;
        byName.set(key, { summary, sets: sets.map(s => ({ weight: s.weight, reps: s.reps })) });
      }
    }
    return byName;
  });

  readonly currentLast = computed(() => {
    const ex = this.current();
    return ex ? this.lastPerformance().get(ex.name.toLowerCase().trim()) ?? null : null;
  });

  readonly hasNext = computed(() => this.currentIndex() < this.exercises().length - 1);
  readonly hasPrevious = computed(() => this.currentIndex() > 0);

  /** True when every set of the current exercise is ticked. */
  readonly currentAllDone = computed(() => {
    const ex = this.current();
    return !!ex && ex.sets.length > 0 && ex.sets.every(s => s.completed);
  });

  constructor() {
    // Rebuild whenever Track Performance hands in a (new) workout.
    effect(() => {
      const workout = this.workout();
      untracked(() => this.load(workout));
    });

    this.dayControl.valueChanges.pipe(takeUntilDestroyed()).subscribe(name => this.pickDay(name));
  }

  private load(workout: SessionWorkoutInput | null): void {
    this.form.clear({ emitEvent: false });
    this.dayName.set(workout?.dayName ?? '');
    this.dayControl.setValue(this.plainDayName(workout?.dayName ?? ''), { emitEvent: false });
    this.isRestDay.set(!!workout?.isRestDay);
    this.currentIndex.set(0);

    for (const ex of workout?.exercises ?? []) {
      const isCardio = ex.isCardio ?? isCardioExercise(ex.name, ex.sets?.[0]?.target);
      this.form.push(this.exerciseGroup(ex.name, isCardio, !!ex.skipped, ex.sets ?? []), { emitEvent: false });
    }
    this.form.updateValueAndValidity();
    this.loadedState = JSON.stringify(this.form.getRawValue());

    // Open on the first exercise with work left.
    const firstOpen = this.exercises().findIndex(e => !e.skipped && e.sets.some(s => !s.completed));
    this.currentIndex.set(Math.max(firstOpen, 0));
  }

  private exerciseGroup(name: string, isCardio: boolean, skipped: boolean, sets: SessionWorkoutInput['exercises'][number]['sets']): SessionExerciseForm {
    const last = this.lastPerformance().get(name.toLowerCase().trim());
    const setGroups = sets.map((s, i) => {
      const weight = this.toNumber(s.weight);
      const reps = this.toNumber(s.reps);
      // Suggest last session's numbers for sets that haven't been filled in yet.
      const suggestion = !s.completed && !weight ? (last?.sets[i] ?? last?.sets[last.sets.length - 1]) : undefined;
      return this.setGroup({
        setNo: s.setNo ?? i + 1,
        target: s.target ?? (isCardio ? CARDIO_TARGET : STRENGTH_TARGET),
        weight: suggestion ? suggestion.weight : weight,
        reps: suggestion ? suggestion.reps : reps,
        completed: !!s.completed
      });
    });

    return new FormGroup({
      name: new FormControl(name, { nonNullable: true }),
      isCardio: new FormControl(isCardio, { nonNullable: true }),
      skipped: new FormControl(skipped, { nonNullable: true }),
      sets: new FormArray(setGroups)
    });
  }

  private setGroup(value: SessionSetValue): SessionSetForm {
    return new FormGroup({
      setNo: new FormControl(value.setNo, { nonNullable: true }),
      target: new FormControl(value.target, { nonNullable: true }),
      weight: new FormControl(value.weight, [Validators.min(0), Validators.max(SET_LIMITS.weight)]),
      reps: new FormControl(value.reps, [Validators.min(0), Validators.max(SET_LIMITS.reps)]),
      completed: new FormControl(value.completed, { nonNullable: true })
    });
  }

  private toNumber(value: number | string | null | undefined): number | null {
    if (value === '' || value === null || value === undefined) return null;
    const n = Number(value);
    return Number.isFinite(n) ? n : null;
  }

  doneSets(ex: SessionExerciseValue): number {
    return ex.sets.filter(s => s.completed).length;
  }

  exerciseForm(index: number): SessionExerciseForm {
    return this.form.at(index);
  }

  setForm(exerciseIndex: number, setIndex: number): SessionSetForm {
    return this.form.at(exerciseIndex).controls.sets.at(setIndex);
  }

  step(setIndex: number, field: SetField, direction: 1 | -1): void {
    const ex = this.current();
    if (!ex) return;
    const size = field === 'weight' ? (ex.isCardio ? 0.5 : 2.5) : 1;
    const control = this.setForm(this.currentIndex(), setIndex).controls[field];
    const next = Math.round(((control.value ?? 0) + direction * size) * 10) / 10;
    control.setValue(clampSetValue(next, SET_LIMITS[field]));
  }

  /** Keeps typed values inside the allowed range once the field loses focus. */
  clamp(setIndex: number, field: SetField): void {
    const control = this.setForm(this.currentIndex(), setIndex).controls[field];
    const clamped = clampSetValue(control.value, SET_LIMITS[field]);
    if (clamped !== control.value) control.setValue(clamped);
  }

  toggleSet(setIndex: number): void {
    const control = this.setForm(this.currentIndex(), setIndex).controls.completed;
    control.setValue(!control.value);
  }

  /** After a workout you usually log every set at once: tick (or untick) them all. */
  toggleAllSets(): void {
    const done = !this.currentAllDone();
    this.exerciseForm(this.currentIndex()).controls.sets.controls
      .forEach(set => set.controls.completed.setValue(done, { emitEvent: false }));
    this.form.updateValueAndValidity();
  }

  addSet(): void {
    const sets = this.exerciseForm(this.currentIndex()).controls.sets;
    const last = sets.at(sets.length - 1)?.getRawValue();
    const isCardio = this.current()?.isCardio ?? false;
    sets.push(this.setGroup({
      setNo: sets.length + 1,
      target: last?.target ?? (isCardio ? CARDIO_TARGET : STRENGTH_TARGET),
      weight: last?.weight ?? null,
      reps: last?.reps ?? (isCardio ? 20 : 10),
      completed: false
    }));
  }

  removeSet(setIndex: number): void {
    const sets = this.exerciseForm(this.currentIndex()).controls.sets;
    if (sets.length <= 1) {
      this.notification.warning(TRACK.MIN_SET_WARNING);
      return;
    }
    sets.removeAt(setIndex, { emitEvent: false });
    sets.controls.forEach((s, i) => s.controls.setNo.setValue(i + 1, { emitEvent: false }));
    this.form.updateValueAndValidity();
  }

  stepExercise(offset: 1 | -1): void {
    const next = this.currentIndex() + offset;
    if (next >= 0 && next < this.exercises().length) this.goTo(next);
  }

  goTo(index: number): void {
    this.currentIndex.set(index);
    this.sheet.set(null);
  }

  toggleSkip(): void {
    const control = this.exerciseForm(this.currentIndex()).controls.skipped;
    control.setValue(!control.value);
  }

  toggleCardio(): void {
    const control = this.exerciseForm(this.currentIndex()).controls.isCardio;
    control.setValue(!control.value);
  }

  openSheet(sheet: SessionSheet): void {
    if (sheet === 'add' || sheet === 'swap') {
      this.exerciseNameControl.setValue('');
      this.useCustomName.set(false);
    }
    this.sheet.set(sheet);
  }

  closeSheet(): void {
    this.sheet.set(null);
  }

  toggleCustomName(): void {
    this.useCustomName.update(v => !v);
    this.exerciseNameControl.setValue('');
  }

  confirmExerciseName(): void {
    const name = this.exerciseNameControl.value.trim();
    const mode = this.sheet();
    if (!name) {
      this.notification.warning(mode === 'swap' ? TRACK.INVALID_SWAP_EXERCISE_NAME : TRACK.INVALID_EXERCISE_NAME);
      return;
    }

    const isCardio = isCardioExercise(name);
    if (mode === 'swap') {
      const index = this.currentIndex();
      const old = this.exerciseForm(index).getRawValue();
      const sets = old.sets.map(s => ({
        setNo: s.setNo,
        target: isCardio !== old.isCardio ? (isCardio ? CARDIO_TARGET : STRENGTH_TARGET) : s.target,
        weight: null,
        reps: isCardio ? 20 : 10,
        completed: false
      }));
      this.form.setControl(index, this.exerciseGroup(name, isCardio, false, sets));
      this.notification.success(TRACK.SWAPPED_EXERCISE_PREFIX + old.name + TRACK.SWAPPED_EXERCISE_MID + name + TRACK.SWAPPED_EXERCISE_SUFFIX);
    } else {
      this.form.push(this.exerciseGroup(name, isCardio, false, [
        { setNo: 1, target: isCardio ? CARDIO_TARGET : STRENGTH_TARGET, weight: null, reps: isCardio ? 20 : 10, completed: false }
      ]));
      this.currentIndex.set(this.form.length - 1);
      this.isRestDay.set(false);
      this.notification.success(TRACK.ADDED_EXERCISE_PREFIX + name + TRACK.ADDED_EXERCISE_SUFFIX);
    }
    this.closeSheet();
  }

  /** Plan days are keyed by name; logged sessions may carry "Day - Category". */
  private plainDayName(name: string): string {
    return name.split(' - ')[0];
  }

  /** Switches plan day, asking first when sets have already been ticked. */
  async pickDay(name: string | null): Promise<void> {
    const day = this.planDays().find(d => d.dayName === name) ?? this.extraDays().find(d => d.dayName === name);
    if (!day || day.dayName === this.plainDayName(this.dayName())) return;

    if (this.completedSetCount() > 0) {
      const ok = await this.confirmation.confirm({
        title: 'Switch workout day?',
        message: 'The sets you have ticked for this day will be cleared.',
        confirmText: 'Switch day',
        cancelText: 'Keep current day',
        type: 'warning'
      });
      if (!ok) {
        this.dayControl.setValue(this.plainDayName(this.dayName()), { emitEvent: false });
        return;
      }
    }
    this.switchDay(day);
  }

  switchDay(day: PlanDayOption): void {
    this.load({
      dayName: day.dayName,
      isRestDay: !!day.isRestDay,
      exercises: (day.exercises ?? []).map(ex => {
        const count = exerciseSetCount(ex.sets);
        const target = typeof ex.reps === 'string' && ex.reps ? ex.reps : STRENGTH_TARGET;
        return {
          name: ex.name,
          isCardio: isCardioExercise(ex.name, ex.reps),
          sets: Array.from({ length: count }, (_, i) => ({ setNo: i + 1, target, weight: null, reps: 10, completed: false }))
        };
      })
    });
    this.closeSheet();
    this.notification.info(TRACK.SWITCH_WORKOUT_DAY_PREFIX + day.dayName);
  }

  trackAnyway(): void {
    this.isRestDay.set(false);
    if (this.form.length === 0) this.openSheet('add');
  }

  finish(): void {
    this.save.emit({ dayName: this.dayName(), isRestDay: this.isRestDay(), exercises: this.exercises() });
  }

  logRestDay(): void {
    this.save.emit({ dayName: this.dayName() || 'Rest Day', isRestDay: true, exercises: [] });
  }

  async leave(): Promise<void> {
    const changed = JSON.stringify(this.form.getRawValue()) !== this.loadedState;
    if (changed && this.completedSetCount() > 0) {
      const discard = await this.confirmation.confirm({
        title: 'Leave this workout?',
        message: 'Sets you have completed but not saved will be lost.',
        confirmText: 'Leave',
        cancelText: 'Keep training',
        type: 'warning'
      });
      if (!discard) return;
    }
    this.exit.emit();
  }
}
