import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { BehaviorSubject, catchError, filter, map, merge, of, scan, startWith, switchMap, take } from 'rxjs';
import { UserService } from '../../../core/services/user.service';
import { AuthApiService } from '../../../core/services/auth-api.service';
import { MemberService } from '../../../core/services/member.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ConfirmationService } from '../../../core/services/confirmation.service';
import { DropdownComponent } from '../../../shared/components/dropdown/dropdown.component';
import { macroCalorieSplit } from '../../../shared/utils/nutrition';
import {
  ACTIVITY_LEVELS, DIET_TYPES, DietType, FITNESS_GOALS, FitnessGoalId, goalLabel, toGoalId
} from '../../../shared/constants/fitness-preferences';
import {
  DashboardForm, FitnessForm, NotificationsForm, UserPreferences
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
  private authService = inject(AuthApiService);
  private memberService = inject(MemberService);
  private destroyRef = inject(DestroyRef);

  readonly goals = FITNESS_GOALS;
  readonly dietTypes = DIET_TYPES;
  readonly activityOptions = ACTIVITY_LEVELS.map(a => ({ label: `${a.label} · ${a.hint}`, value: a.value }));

  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  readonly isRebuilding = signal(false);

  readonly fitnessForm = new FormGroup<FitnessForm>({
    primaryGoal: new FormControl<FitnessGoalId | ''>('', { nonNullable: true }),
    activityLevel: new FormControl(null),
    dietPreference: new FormControl(null),
    targetWeight: new FormControl<number | null>(null, [Validators.min(0), Validators.max(500)]),
    targetTrainingTime: new FormControl<number | null>(null, [Validators.min(0), Validators.max(600)]),
    // Empty = automatic burn goal
    targetCaloriesBurn: new FormControl<number | null>(null, [Validators.min(50), Validators.max(5000)]),
    trainingDaysPerWeek: new FormControl<number | null>(null, [Validators.min(1), Validators.max(7)])
  });

  readonly notificationsForm = new FormGroup<NotificationsForm>({
    emailNotifications: new FormControl(true, { nonNullable: true })
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

  /**
   * What to eat comes from the active diet plan only (its meals add up to these numbers), so Settings shows it
   * read-only instead of keeping a second target that could disagree. Reloaded after "Build new plans".
   */
  private readonly reloadDiet$ = new BehaviorSubject<void>(undefined);
  readonly dietPlan = toSignal(
    this.authService.userProfile$.pipe(
      filter(profile => !!profile),
      take(1),
      switchMap(profile => this.reloadDiet$.pipe(
        switchMap(() => this.memberService.getActiveDiet(profile!.id, true)),
        map(res => {
          const plan = res?.data?.dietPlan;
          if (!plan) return null;
          const split = macroCalorieSplit(plan.protein ?? 0, plan.carbs ?? 0, plan.fats ?? 0);
          return {
            name: plan.name,
            calories: plan.calories,
            macros: [
              { label: 'Protein', tone: 'protein', grams: Math.round(plan.protein ?? 0), pct: split.protein },
              { label: 'Carbs', tone: 'carbs', grams: Math.round(plan.carbs ?? 0), pct: split.carbs },
              { label: 'Fats', tone: 'fats', grams: Math.round(plan.fats ?? 0), pct: split.fats }
            ]
          };
        }),
        catchError(() => of(null))
      ))
    ),
    { initialValue: undefined }
  );

  /** Goal, diet or activity changed: the current plans were built for the old ones. */
  readonly plansOutdated = computed(() => {
    this.formChanges();
    return this.goalOrActivityChanged() || this.fitnessForm.controls.dietPreference.dirty;
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

  /** Automatic burn goal per training minute (from the server, which knows the latest body weight). */
  private autoBurnPerMinute = 0;

  /** Placeholder for the burn goal box: the automatic value for the training time currently in the form. */
  autoBurnHint(): number | null {
    if (!this.autoBurnPerMinute) return null;
    const minutes = this.fitnessForm.controls.targetTrainingTime.value || 60;
    return Math.round(this.autoBurnPerMinute * minutes);
  }

  private resetForms(data: UserPreferences): void {
    const savedMinutes = data.targetTrainingTime || 60;
    this.autoBurnPerMinute = data.autoCaloriesBurn ? data.autoCaloriesBurn / savedMinutes : 0;
    this.fitnessForm.reset({
      primaryGoal: toGoalId(data.primaryGoal),
      activityLevel: data.activityLevel ?? null,
      dietPreference: data.dietPreference ?? null,
      targetWeight: data.targetWeight ?? null,
      targetTrainingTime: data.targetTrainingTime ?? null,
      targetCaloriesBurn: data.targetCaloriesBurn ?? null,
      trainingDaysPerWeek: data.trainingDaysPerWeek ?? null
    });
    this.notificationsForm.reset({
      emailNotifications: data.emailNotificationsEnabled ?? true
    });
    this.dashboardForm.reset({ showGymBuddy: data.showGymBuddy ?? true });
  }

  private goalOrActivityChanged(): boolean {
    const { primaryGoal, activityLevel } = this.fitnessForm.controls;
    return primaryGoal.dirty || activityLevel.dirty;
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
        'Your current plans stay in your library. Your daily calories and macros then come from the new diet plan.',
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
        this.reloadDiet$.next();
        this.notificationService.success('New starter plans are ready.');
      },
      error: err => {
        this.isRebuilding.set(false);
        this.notificationService.error(err?.error?.message || 'Failed to build new plans.');
      }
    });
  }
}
