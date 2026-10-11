import { FormControl, FormGroup } from '@angular/forms';
import { ActivityLevel, DietType, FitnessGoalId } from '../../shared/constants/fitness-preferences';

export interface FitnessForm {
  primaryGoal: FormControl<FitnessGoalId | ''>;
  activityLevel: FormControl<ActivityLevel | null>;
  dietPreference: FormControl<DietType | null>;
  targetWeight: FormControl<number | null>;
  targetCalories: FormControl<number | null>;
  targetTrainingTime: FormControl<number | null>;
  targetProtein: FormControl<number | null>;
  targetCarbs: FormControl<number | null>;
  targetFats: FormControl<number | null>;
}

export interface NotificationsForm {
  emailNotifications: FormControl<boolean>;
  workoutReminders: FormControl<boolean>;
}

export interface DashboardForm {
  showGymBuddy: FormControl<boolean>;
}

export interface UserPreferences {
  primaryGoal?: string;
  activityLevel?: ActivityLevel | null;
  dietPreference?: DietType | null;
  trainingDaysPerWeek?: number | null;
  targetWeight?: number | null;
  targetCalories?: number | null;
  targetProtein?: number | null;
  targetCarbs?: number | null;
  targetFats?: number | null;
  targetTrainingTime?: number | null;
  emailNotificationsEnabled?: boolean;
  workoutRemindersEnabled?: boolean;
  showGymBuddy?: boolean;
}

export interface SuggestedTargets {
  targetCalories: number;
  targetProtein: number;
  targetCarbs: number;
  targetFats: number;
}

export type FitnessValue = ReturnType<FormGroup<FitnessForm>['getRawValue']>;
