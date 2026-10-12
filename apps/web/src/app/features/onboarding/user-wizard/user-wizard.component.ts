import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { catchError, map, of, startWith } from 'rxjs';
import { AuthApiService } from '../../../core/services/auth-api.service';
import { UserService } from '../../../core/services/user.service';
import { ProfileService } from '../../../core/services/profile.service';
import { DropdownComponent } from '../../../shared/components/dropdown/dropdown.component';
import { DateTimePickerComponent } from '../../../shared/components/date-time-picker/date-time-picker.component';
import { LoadingComponent } from '../../../shared/components/loading/loading.component';
import { ValidationMessage } from '../../../shared/components/validation-message/validation-message.component';
import {
  ACTIVITY_LEVELS, ActivityLevel, DIET_TYPES, DietType, FitnessGoalId
} from '../../../shared/constants/fitness-preferences';
import { SuggestedTargets } from '../../../core/models/user-settings.model';
import { STORAGE_KEYS } from '../../../shared/utils/storage-keys';

const DRAFT_KEY = STORAGE_KEYS.USER_WIZARD_DRAFT;
/** Drafts saved by the earlier three-step wizard; their step numbers no longer line up. */
const LEGACY_DRAFT_KEY = 'gymForge_userWizardDraft';
const STEP_COUNT = 4;
const DEFAULT_SESSION_MINUTES = 60;
const MIN_TRAINING_DAYS = 2;
const MAX_TRAINING_DAYS = 7;
const MIN_SESSION_MINUTES = 30;
const MAX_SESSION_MINUTES = 180;
/** Starter plan sizing; mirrors PlanGenerationService in the API. */
const MINUTES_PER_EXERCISE = 10;
const MIN_EXERCISES = 3;
const MAX_EXERCISES = 10;

const LIMITATIONS_MAX_LENGTH = 300;

type ExperienceLevel = 'Beginner' | 'Intermediate' | 'Advanced';

interface MetricsForm {
  gender: FormControl<string>;
  dob: FormControl<Date | string | null>;
  height: FormControl<number | null>;
  weight: FormControl<number | null>;
  targetWeight: FormControl<number | null>;
  activityLevel: FormControl<ActivityLevel | null>;
}

interface PlanForm {
  trainingDaysPerWeek: FormControl<number | null>;
  sessionLengthMinutes: FormControl<number>;
  dietPreference: FormControl<DietType | null>;
  limitations: FormControl<string>;
}

interface WizardDraft {
  currentStep: number;
  selectedGoal: FitnessGoalId | null;
  selectedExperienceLevel: ExperienceLevel | null;
  metricsForm: ReturnType<FormGroup<MetricsForm>['getRawValue']>;
  planForm: ReturnType<FormGroup<PlanForm>['getRawValue']>;
}

@Component({
  selector: 'app-user-wizard',
  standalone: true,
  imports: [ReactiveFormsModule, DropdownComponent, DateTimePickerComponent, LoadingComponent, ValidationMessage],
  templateUrl: './user-wizard.component.html',
  styleUrls: ['./user-wizard.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserWizardComponent implements OnInit {
  private authService = inject(AuthApiService);
  private router = inject(Router);
  private toastr = inject(ToastrService);
  private userService = inject(UserService);
  private profileService = inject(ProfileService);
  private destroyRef = inject(DestroyRef);

  readonly limitationsMaxLength = LIMITATIONS_MAX_LENGTH;

  readonly steps = [
    { title: 'Goal', desc: 'What you want to achieve' },
    { title: 'About you', desc: 'Body and daily activity' },
    { title: 'Training & diet', desc: 'Schedule and food' },
    { title: 'Review', desc: 'Check your plan' }
  ];

  readonly goals: { id: FitnessGoalId; label: string; icon: string; desc: string; image: string }[] = [
    { id: 'weight_loss', label: 'Weight Loss', icon: 'fi-rr-fire', desc: 'Burn fat and get leaner', image: '/images/goals/weight_loss.png' },
    { id: 'muscle_gain', label: 'Muscle Gain', icon: 'fi-rr-gym', desc: 'Build size and strength', image: '/images/goals/muscle_gain.png' },
    { id: 'endurance', label: 'Endurance', icon: 'fi-rr-running', desc: 'Improve cardiovascular health', image: '/images/goals/endurance.png' },
    { id: 'flexibility', label: 'Flexibility', icon: 'fi-rr-person-praying', desc: 'Enhance mobility and recovery', image: '/images/goals/flexibility.png' },
    { id: 'general_fitness', label: 'General Fitness', icon: 'fi-rr-heart-pulse', desc: 'Stay active and healthy', image: '/images/goals/general_fitness.png' }
  ];

  readonly experienceLevels: { id: ExperienceLevel; label: string; desc: string; icon: string; days: number }[] = [
    { id: 'Beginner', label: 'Beginner', desc: 'New to structured training', icon: 'fa-solid fa-seedling', days: 3 },
    { id: 'Intermediate', label: 'Intermediate', desc: 'Training consistently for a while', icon: 'fa-solid fa-dumbbell', days: 4 },
    { id: 'Advanced', label: 'Advanced', desc: 'Experienced with structured programs', icon: 'fa-solid fa-fire-flame-curved', days: 5 }
  ];

  readonly activityLevels = ACTIVITY_LEVELS;
  readonly dietTypes = DIET_TYPES;
  readonly minTrainingDays = MIN_TRAINING_DAYS;
  readonly maxTrainingDays = MAX_TRAINING_DAYS;
  readonly minSessionMinutes = MIN_SESSION_MINUTES;
  readonly maxSessionMinutes = MAX_SESSION_MINUTES;
  readonly sessionScale = [30, 60, 90, 120, 150, 180];

  readonly genderOptions = [
    { label: 'Male', value: 'Male' },
    { label: 'Female', value: 'Female' },
    { label: 'Other', value: 'Other' }
  ];

  readonly maxDobDate: Date;
  readonly minDobDate: Date;

  readonly currentStep = signal(1);
  readonly isSubmitting = signal(false);
  readonly selectedGoal = signal<FitnessGoalId | null>(null);
  readonly selectedExperienceLevel = signal<ExperienceLevel | null>(null);
  /** Targets for the review step; undefined while loading, null when they couldn't be worked out. */
  readonly previewTargets = signal<SuggestedTargets | null | undefined>(undefined);

  readonly metricsForm = new FormGroup<MetricsForm>({
    gender: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    dob: new FormControl<Date | string | null>(null, [Validators.required]),
    height: new FormControl<number | null>(null, [Validators.required, Validators.min(50), Validators.max(300)]),
    weight: new FormControl<number | null>(null, [Validators.required, Validators.min(20), Validators.max(500)]),
    targetWeight: new FormControl<number | null>(null, [Validators.min(20), Validators.max(500)]),
    activityLevel: new FormControl<ActivityLevel | null>(null, [Validators.required])
  });

  readonly planForm = new FormGroup<PlanForm>({
    trainingDaysPerWeek: new FormControl<number | null>(null, [Validators.required]),
    sessionLengthMinutes: new FormControl(DEFAULT_SESSION_MINUTES, { nonNullable: true }),
    dietPreference: new FormControl<DietType | null>(null, [Validators.required]),
    limitations: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(LIMITATIONS_MAX_LENGTH)] })
  });

  private readonly metrics = toSignal(
    this.metricsForm.valueChanges.pipe(map(() => this.metricsForm.getRawValue()), startWith(this.metricsForm.getRawValue())),
    { requireSync: true }
  );

  private readonly plan = toSignal(
    this.planForm.valueChanges.pipe(map(() => this.planForm.getRawValue()), startWith(this.planForm.getRawValue())),
    { requireSync: true }
  );

  readonly calculatedAge = computed(() => {
    const dob = this.metrics().dob;
    if (!dob) return null;
    const birthDate = new Date(dob);
    const today = new Date();
    let age = today.getFullYear() - birthDate.getFullYear();
    const m = today.getMonth() - birthDate.getMonth();
    if (m < 0 || (m === 0 && today.getDate() < birthDate.getDate())) age--;
    return age;
  });

  readonly bmi = computed(() => {
    const { height, weight } = this.metrics();
    if (!height || !weight || height <= 0 || weight <= 0) return null;
    const heightM = height / 100;
    const value = parseFloat((weight / (heightM * heightM)).toFixed(1));
    const status = value < 18.5 ? 'under' : value >= 25 ? 'over' : 'normal';
    return { value, status };
  });

  readonly canContinue = computed(() => {
    this.metrics();
    this.plan();
    switch (this.currentStep()) {
      case 1: return !!this.selectedGoal() && !!this.selectedExperienceLevel();
      case 2: return this.metricsForm.valid;
      case 3: return this.planForm.valid;
      default: return true;
    }
  });

  /** What the review step lists, one row per answer, with the step to go back to for changes. */
  readonly exercisesPerSession = computed(() =>
    Math.min(Math.max(Math.floor(this.plan().sessionLengthMinutes / MINUTES_PER_EXERCISE), MIN_EXERCISES), MAX_EXERCISES));

  /** "1 hr 30 min" style label for the session slider. */
  readonly sessionLabel = computed(() => {
    const minutes = this.plan().sessionLengthMinutes;
    const hours = Math.floor(minutes / 60);
    const rest = minutes % 60;
    if (!hours) return { value: `${rest}`, unit: 'min' };
    return rest ? { value: `${hours}h ${rest}`, unit: 'min' } : { value: `${hours}`, unit: hours === 1 ? 'hour' : 'hours' };
  });

  readonly review = computed(() => {
    const m = this.metrics();
    const p = this.plan();
    const goal = this.goals.find(g => g.id === this.selectedGoal());
    const level = this.experienceLevels.find(l => l.id === this.selectedExperienceLevel());
    const activity = this.activityLevels.find(a => a.value === m.activityLevel);
    const diet = this.dietTypes.find(d => d.value === p.dietPreference);
    const body = [`${m.height ?? '-'} cm`, `${m.weight ?? '-'} kg`, m.targetWeight ? `target ${m.targetWeight} kg` : null]
      .filter(Boolean).join(' · ');
    return [
      { label: 'Goal', value: `${goal?.label ?? '-'} · ${level?.label ?? '-'}`, step: 1 },
      { label: 'Body', value: body, step: 2 },
      { label: 'Daily activity', value: activity?.label ?? '-', step: 2 },
      { label: 'Training', value: `${p.trainingDaysPerWeek ?? '-'} days a week · ${p.sessionLengthMinutes} min sessions`, step: 3 },
      { label: 'Diet', value: diet?.label ?? '-', step: 3 },
      ...(p.limitations.trim() ? [{ label: 'Limitations', value: p.limitations.trim(), step: 3 }] : [])
    ];
  });

  constructor() {
    const today = new Date();
    this.maxDobDate = new Date(today.getFullYear() - 10, today.getMonth(), today.getDate());
    this.minDobDate = new Date(today.getFullYear() - 100, today.getMonth(), today.getDate());
  }

  ngOnInit(): void {
    localStorage.removeItem(LEGACY_DRAFT_KEY);
    const hasDraft = this.loadDraft();
    const user = this.authService.getUserProfile();
    if (!hasDraft && user?.currentOnboardingStep && user.currentOnboardingStep > 1) {
      this.goToStep(Math.min(user.currentOnboardingStep, STEP_COUNT));
    }

    this.metricsForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.saveDraft());
    this.planForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.saveDraft());
  }

  allowNumbersOnly(event: KeyboardEvent): void {
    const isDigit = event.key.length === 1 && /[0-9.]/.test(event.key);
    if (event.key.length === 1 && !isDigit) event.preventDefault();
  }

  clampValue(control: FormControl<number | null>, max: number): void {
    if (control.value !== null && Number(control.value) > max) control.setValue(max);
  }

  selectGoal(goalId: FitnessGoalId): void {
    this.selectedGoal.set(goalId);
    this.saveDraft();
  }

  selectExperienceLevel(levelId: ExperienceLevel): void {
    this.selectedExperienceLevel.set(levelId);
    // Suggest a schedule that suits the level until the member picks one themselves.
    const days = this.planForm.controls.trainingDaysPerWeek;
    if (!days.dirty) days.setValue(this.experienceLevels.find(l => l.id === levelId)?.days ?? 3);
    this.saveDraft();
  }

  stepTrainingDays(delta: 1 | -1): void {
    const control = this.planForm.controls.trainingDaysPerWeek;
    const current = control.value ?? 3;
    this.selectOption(control, Math.min(Math.max(current + delta, MIN_TRAINING_DAYS), MAX_TRAINING_DAYS));
  }

  selectOption<T>(control: FormControl<T>, value: T): void {
    control.markAsDirty();
    control.setValue(value);
  }

  nextStep(): void {
    if (!this.canContinue()) {
      if (this.currentStep() === 2) this.metricsForm.markAllAsTouched();
      if (this.currentStep() === 3) this.planForm.markAllAsTouched();
      this.toastr.warning('Please complete this step to continue.');
      return;
    }
    const next = this.currentStep() + 1;
    this.goToStep(next);
    this.userService.saveOnboardingStep(next).subscribe();
  }

  prevStep(): void {
    if (this.currentStep() > 1) this.goToStep(this.currentStep() - 1);
  }

  goToStep(step: number): void {
    this.currentStep.set(step);
    if (step === STEP_COUNT) this.loadPreviewTargets();
    this.saveDraft();
  }

  logout(): void {
    localStorage.removeItem(DRAFT_KEY);
    this.authService.logout();
  }

  submit(): void {
    if (this.metricsForm.invalid || this.planForm.invalid || !this.selectedGoal()) {
      this.toastr.warning('Some answers are missing. Please go back and complete them.');
      return;
    }

    this.isSubmitting.set(true);
    const m = this.metricsForm.getRawValue();
    const p = this.planForm.getRawValue();
    const payload = {
      primaryGoal: this.selectedGoal(),
      experienceLevel: this.selectedExperienceLevel(),
      gender: m.gender,
      dob: m.dob,
      height: m.height,
      weight: m.weight,
      targetWeight: m.targetWeight || null,
      activityLevel: m.activityLevel,
      dietPreference: p.dietPreference,
      trainingDaysPerWeek: p.trainingDaysPerWeek,
      sessionLengthMinutes: p.sessionLengthMinutes,
      limitations: p.limitations.trim() || null
    };

    this.userService.completeOnboarding(payload).subscribe({
      next: () => {
        localStorage.removeItem(DRAFT_KEY);
        this.toastr.success('Your profile is set up!');

        const navigateToDashboard = () => {
          sessionStorage.setItem('justFinishedOnboarding', 'true');
          this.router.navigate(['/user/dashboard']);
        };

        this.authService.refreshToken().subscribe({
          next: () => {
            this.profileService.getProfile(true).subscribe({
              next: navigateToDashboard,
              error: navigateToDashboard
            });
          },
          error: navigateToDashboard
        });
      },
      error: () => {
        this.toastr.error('Failed to save your profile. Please try again.');
        this.isSubmitting.set(false);
      }
    });
  }

  private loadPreviewTargets(): void {
    const m = this.metricsForm.getRawValue();
    const p = this.planForm.getRawValue();
    if (!m.height || !m.weight || !m.dob) {
      this.previewTargets.set(null);
      return;
    }

    this.previewTargets.set(undefined);
    this.userService.previewOnboardingTargets({
      primaryGoal: this.selectedGoal(),
      experienceLevel: this.selectedExperienceLevel(),
      activityLevel: m.activityLevel,
      dietPreference: p.dietPreference,
      trainingDaysPerWeek: p.trainingDaysPerWeek,
      sessionLengthMinutes: p.sessionLengthMinutes,
      heightCm: m.height,
      weightKg: m.weight,
      dateOfBirth: m.dob,
      gender: m.gender
    }).pipe(
      map(res => (res?.data ?? null) as SuggestedTargets | null),
      catchError(() => of(null)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(targets => this.previewTargets.set(targets));
  }

  private saveDraft(): void {
    const draft: WizardDraft = {
      currentStep: this.currentStep(),
      selectedGoal: this.selectedGoal(),
      selectedExperienceLevel: this.selectedExperienceLevel(),
      metricsForm: this.metricsForm.getRawValue(),
      planForm: this.planForm.getRawValue()
    };
      localStorage.setItem(DRAFT_KEY, JSON.stringify(draft));
  }

  private loadDraft(): boolean {
    const draftStr = localStorage.getItem(DRAFT_KEY);
    if (!draftStr) return false;
    try {
      const draft = JSON.parse(draftStr) as Partial<WizardDraft>;
      if (draft.selectedGoal) this.selectedGoal.set(draft.selectedGoal);
      if (draft.selectedExperienceLevel) this.selectedExperienceLevel.set(draft.selectedExperienceLevel);
      if (draft.metricsForm) this.metricsForm.patchValue(draft.metricsForm);
      if (draft.planForm) {
        this.planForm.patchValue(draft.planForm);
        if (draft.planForm.trainingDaysPerWeek) this.planForm.controls.trainingDaysPerWeek.markAsDirty();
      }
      if (draft.currentStep) this.goToStep(Math.min(draft.currentStep, STEP_COUNT));
      return true;
    } catch (e) {
      console.error('Failed to parse wizard draft', e);
      return false;
    }
  }
}
