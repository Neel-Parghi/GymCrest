import { FormControl } from '@angular/forms';
import { AssignedMealDto } from '../../shared/models/diet-tracking.model';

export interface DailyRoutineItem {
  id: string;
  title: string;
  time?: string;
  amount?: string;
  completed: boolean;
  isEditing?: boolean;
  order?: number;
}

export interface ActivePlanSummary {
  planId: string;
  name: string;
  type: string;
}

export interface PersonalRecord {
  name: string;
  weight: string;
  date: string;
}

export interface MuscleRecovery {
  name: string;
  pct: number;
  status: 'Ready' | 'Recovering' | string;
}

export interface MuscleHeatmapZone {
  name: string;
  level: 'High' | 'Medium' | 'Low' | string;
  label: string;
}

/** Response of GET user dashboard summary (UserDashboardSummaryDto). */
export interface UserDashboardSummary {
  greeting?: string;
  userName?: string;
  showGymBuddy: boolean;
  goalTitle?: string | null;
  goalProgressPct: number;
  targetWeight?: number | null;
  targetCalories: number;
  targetTrainingTime: number;
  targetCaloriesBurn: number;
  caloriesBurnedToday: number;
  activeTrainingTimeMinutes: number;
  cardioMinutesToday: number;
  strengthSetsToday: number;
  workoutStreak: number;
  streakAtRisk: boolean;
  monthlySessionCount: number;
  monthlySessionTarget: number;
  monthlyCompletionPct: number;
  currentWeight: number;
  bodyFat: number;
  bmi: number;
  activeWorkoutPlan?: ActivePlanSummary | null;
  activeDietPlan?: ActivePlanSummary | null;
  personalRecords: PersonalRecord[];
  muscleRecovery: MuscleRecovery[];
  dailyRoutines: DailyRoutineItem[];
  muscleHeatmap: MuscleHeatmapZone[];
}

export type WorkoutState = 'none' | 'rest' | 'done' | 'ready';

export interface BuddyFacts {
  firstName: string;
  isFirstTime: boolean;
  state: WorkoutState | null;
  workoutTitle: string;
  streak: number;
  streakAtRisk: boolean;
  monthlyCount: number;
  monthlyTarget: number;
  topRecord: { name: string; weight: string } | null;
  caloriesLeft: number | null;
  readyMuscle: string | null;
  goalTitle: string;
}

/** How the gym buddy reacts when it gets to the card. */
export type BuddyAct = 'dance' | 'lift' | 'point';

/** Something worth reacting to: the buddy goes to `target`, does `act` and says `text`. */
export interface BuddyEvent {
  act: BuddyAct;
  /** Selector, inside the buddy's roam area, of the card to go to. */
  target: string;
  /** Selector of the element to stand beside and point at. */
  focus?: string;
  text: string;
  icon: string;
}

export interface TodayWorkout {
  state: WorkoutState;
  planName: string;
  title: string;
  muscles: string;
  exerciseCount: number;
  setCount: number;
}

export type WeekDayState = 'done' | 'rest' | 'today' | 'missed' | 'upcoming';

export interface WeekDay {
  label: string;
  state: WeekDayState;
}

export interface MacroProgress {
  label: string;
  tone: 'protein' | 'carbs' | 'fats';
  value: number;
  target: number;
  pct: number;
}

export interface NutritionToday {
  eaten: number;
  target: number;
  left: number;
  pct: number;
  macros: MacroProgress[];
  nextMeal: AssignedMealDto | null;
}

export interface GoalRing {
  label: string;
  pct: number;
  value: string;
  unit: string;
}

export interface ActivityRing {
  key: 'calories' | 'active' | 'goal';
  radius: number;
  dash: string;
}

export interface RoutineForm {
  title: FormControl<string>;
  amount: FormControl<string>;
}
