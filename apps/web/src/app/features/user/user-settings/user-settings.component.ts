import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { catchError, debounceTime, filter, map, merge, of, scan, startWith, switchMap } from 'rxjs';
import { UserService } from '../../../core/services/user.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ConfirmationService } from '../../../core/services/confirmation.service';
import { DropdownComponent } from '../../../shared/components/dropdown/dropdown.component';
import { KCAL_PER_GRAM, macroCalorieSplit } from '../../../shared/utils/nutrition';
import {
  ACTIVITY_LEVELS, DIET_TYPES, DietType, FITNESS_GOALS, FitnessGoalId, goalLabel, toGoalId
} from '../../../shared/constants/fitness-preferences';
import {
  DashboardForm, FitnessForm, FitnessValue, NotificationsForm, SuggestedTargets, UserPreferences
} from '../../../core/models/user-settings.model';

const WEIGHT_STEP = 0.5;

@Component({
  selector: 'app-user-settings',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule, DropdownComponent],
  templateUrl: './user-settings.component.html',
  styleUrl: './user-settings.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserSettingsComponent implements OnInit {
  private userService = inject(UserService);
  private notificationService = inject(NotificationService);
  private confirmationService = inject(ConfirmationService);
  private destroyRef = inject(DestroyRef);

  readonly goals = FITNESS_GOALS;
  readonly dietTypes = DIET_TYPES;
  readonly activityOptions = ACTIVITY_LEVELS.map(a => ({ label: `${a.label} · ${a.hint}`, value: a.value }));

  readonly macroFields = [
    { key: 'targetProtein', label: 'Protein', tone: 'protein' },
    { key: 'targetCarbs', label: 'Carbs', tone: 'carbs' },
    { key: 'targetFats', label: 'Fats', tone: 'fats' }
  ] as const;

  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  readonly isRebuilding = signal(false);

  readonly fitnessForm = new FormGroup<FitnessForm>({
    primaryGoal: new FormControl<FitnessGoalId | ''>('', { nonNullable: true }),
    activityLevel: new FormControl(null),
    dietPreference: new FormControl(null),
    targetWeight: new FormControl<number | null>(null, [Validators.min(0), Validators.max(500)]),
    targetCalories: new FormControl<number | null>(null, [Validators.min(0), Validators.max(10000)]),
    targetTrainingTime: new FormControl<number | null>(null, [Validators.min(0), Validators.max(600)]),
    targetProtein: new FormControl<number | null>(null, [Validators.min(0), Validators.max(1000)]),
    targetCarbs: new FormControl<number | null>(null, [Validators.min(0), Validators.max(1000)]),
    targetFats: new FormControl<number | null>(null, [Validators.min(0), Validators.max(1000)])
  });

  readonly notificationsForm = new FormGroup<NotificationsForm>({
    emailNotifications: new FormControl(true, { nonNullable: true }),
    workoutReminders: new FormControl(true, { nonNullable: true })
  });

  readonly dashboardForm = new FormGroup<DashboardForm>({
    showGymBuddy: new FormControl(true, { nonNullable: true })
  });

  private readonly fitness = toSignal(
    this.fitnessForm.valueChanges.pipe(map(() => this.fitnessForm.getRawValue()), startWith(this.fitnessForm.getRawValue())),
    { requireSync: true }
  );

  private readonly formChanges = toSignal(
    merge(this.fitnessForm.valueChanges, this.notificationsForm.valueChanges, this.dashboardForm.valueChanges)
      .pipe(scan(count => count + 1, 0)),
    { initialValue: 0 }
  );

  readonly isDirty = computed(() => {
    this.formChanges();
    return this.fitnessForm.dirty || this.notificationsForm.dirty || this.dashboardForm.dirty;
  });

  private readonly suggestedTargets = toSignal(
    merge(this.fitnessForm.controls.primaryGoal.valueChanges, this.fitnessForm.controls.activityLevel.valueChanges).pipe(
      filter(() => this.goalOrActivityChanged()),
      debounceTime(250),
      switchMap(() => {
        const { primaryGoal, activityLevel } = this.fitnessForm.getRawValue();
        if (!primaryGoal) return of(null);
        return this.userService.getSuggestedTargets(primaryGoal, activityLevel).pipe(
          map(res => (res?.data ?? null) as SuggestedTargets | null),
          catchError(() => of(null))
        );
      })
    ),
    { initialValue: null }
  );

  readonly suggestion = computed(() => {
    const targets = this.suggestedTargets();
    const f = this.fitness();
    if (!targets || !this.goalOrActivityChanged()) return null;
    const applied = f.targetCalories === targets.targetCalories && f.targetProtein === targets.targetProtein
      && f.targetCarbs === targets.targetCarbs && f.targetFats === targets.targetFats;
    return applied ? null : { ...targets, goal: goalLabel(f.primaryGoal) };
  });

  readonly macroCheck = computed(() => {
    const f = this.fitness();
    if (f.targetProtein == null && f.targetCarbs == null && f.targetFats == null) return null;
    const kcal = Math.round(
      (f.targetProtein ?? 0) * KCAL_PER_GRAM.protein +
      (f.targetCarbs ?? 0) * KCAL_PER_GRAM.carbs +
      (f.targetFats ?? 0) * KCAL_PER_GRAM.fats
    );
    const target = f.targetCalories ?? 0;
    const matches = target > 0 && Math.abs(kcal - target) / target <= 0.05;
    const note = target <= 0 ? '.' : matches ? ', matching your daily target.' : `. Your daily target is ${target} kcal.`;
    return { kcal, target, matches, note, split: this.macroSplit(f) };
  });

  ngOnInit(): void {
    this.userService.getPreferences().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => {
        const data = (res as { data?: UserPreferences })?.data;
        if (data) this.resetForms(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.notificationService.error('Failed to load preferences.');
      }
    });
  }

  private resetForms(data: UserPreferences): void {
    this.fitnessForm.reset({
      primaryGoal: toGoalId(data.primaryGoal),
      activityLevel: data.activityLevel ?? null,
      dietPreference: data.dietPreference ?? null,
      targetWeight: data.targetWeight ?? null,
      targetCalories: data.targetCalories ?? null,
      targetTrainingTime: data.targetTrainingTime ?? null,
      targetProtein: data.targetProtein ?? null,
      targetCarbs: data.targetCarbs ?? null,
      targetFats: data.targetFats ?? null
    });
    this.notificationsForm.reset({
      emailNotifications: data.emailNotificationsEnabled ?? true,
      workoutReminders: data.workoutRemindersEnabled ?? true
    });
    this.dashboardForm.reset({ showGymBuddy: data.showGymBuddy ?? true });
  }

  private goalOrActivityChanged(): boolean {
    const { primaryGoal, activityLevel } = this.fitnessForm.controls;
    return primaryGoal.dirty || activityLevel.dirty;
  }

  private macroSplit(f: FitnessValue): { tone: string; pct: number }[] {
    const split = macroCalorieSplit(f.targetProtein ?? 0, f.targetCarbs ?? 0, f.targetFats ?? 0);
    return [
      { tone: 'protein', pct: split.protein },
      { tone: 'carbs', pct: split.carbs },
      { tone: 'fats', pct: split.fats }
    ].filter(part => part.pct > 0);
  }

  selectGoal(value: FitnessGoalId): void {
    const control = this.fitnessForm.controls.primaryGoal;
    control.markAsDirty();
    control.setValue(value);
  }

  selectDiet(value: DietType): void {
    const control = this.fitnessForm.controls.dietPreference;
    control.markAsDirty();
    control.setValue(value);
  }

  applySuggestion(): void {
    const targets = this.suggestion();
    if (!targets) return;
    const { targetCalories, targetProtein, targetCarbs, targetFats } = this.fitnessForm.controls;
    [targetCalories, targetProtein, targetCarbs, targetFats].forEach(control => control.markAsDirty());
    this.fitnessForm.patchValue({
      targetCalories: targets.targetCalories,
      targetProtein: targets.targetProtein,
      targetCarbs: targets.targetCarbs,
      targetFats: targets.targetFats
    });
  }

  stepWeight(direction: 1 | -1): void {
    const control = this.fitnessForm.controls.targetWeight;
    const next = Math.round(((control.value ?? 0) + direction * WEIGHT_STEP) * 10) / 10;
    control.setValue(Math.min(Math.max(next, 0), 500));
    control.markAsDirty();
  }

  saveChanges(): void {
    if (this.fitnessForm.invalid) {
      this.fitnessForm.markAllAsTouched();
      this.notificationService.error('Please check the highlighted values.');
      return;
    }

    this.isSaving.set(true);
    const notifications = this.notificationsForm.getRawValue();
    const payload = {
      ...this.fitnessForm.getRawValue(),
      emailNotificationsEnabled: notifications.emailNotifications,
      workoutRemindersEnabled: notifications.workoutReminders,
      showGymBuddy: this.dashboardForm.getRawValue().showGymBuddy
    };

    this.userService.updatePreferences(payload).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.fitnessForm.markAsPristine();
        this.notificationsForm.markAsPristine();
        this.dashboardForm.markAsPristine();
        this.fitnessForm.updateValueAndValidity();
        this.notificationService.success('Settings saved.');
      },
      error: () => {
        this.isSaving.set(false);
        this.notificationService.error('Failed to save settings.');
      }
    });
  }

  async rebuildPlans(): Promise<void> {
    const confirmed = await this.confirmationService.confirm({
      title: 'Build new starter plans?',
      message: `We'll create a workout and diet plan for ${goalLabel(this.fitness().primaryGoal)} and make them your active plans. ` +
        'Your current plans stay in your library, and your calorie and macro targets are updated to match.',
      confirmText: 'Build plans',
      type: 'info'
    });
    if (!confirmed) return;

    this.isRebuilding.set(true);
    this.userService.regenerateStarterPlans().subscribe({
      next: () => {
        this.isRebuilding.set(false);
        this.userService.getPreferences(true).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res => {
          const data = (res as { data?: UserPreferences })?.data;
          if (data) this.resetForms(data);
        });
        this.notificationService.success('New starter plans are ready.');
      },
      error: err => {
        this.isRebuilding.set(false);
        this.notificationService.error(err?.error?.message || 'Failed to build new plans.');
      }
    });
  }
}
