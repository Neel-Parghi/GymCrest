export type WorkoutPlanType = 'Split' | 'Weekly' | 'Daily';

export interface PlannerExercise {
  id?: string;
  name: string;
  sets: number;
  reps: string;
  notes?: string;
}

export interface SplitDay {
  id?: string;
  name: string;
  category: string;
  exercises: PlannerExercise[];
}

export interface BaseWorkoutPlan {
  id?: string;
  name: string;
  description: string;
  level: string;
  goal: string;
  type: WorkoutPlanType;
  gymId?: string;
  createdBy?: string;
  isCustom?: boolean;
  daysCount?: number;
  exercisesCount?: number;
  createdAt?: Date;
}

export interface SplitPlanner extends BaseWorkoutPlan {
  type: 'Split';
  days: SplitDay[];
}

export interface WeeklyDayPlan {
  id?: string;
  dayName: string;
  type: 'workout' | 'rest';
  targetCategory?: string;
  splitDayName?: string;
  exercises: PlannerExercise[];
}

export interface WeeklyPlanner extends BaseWorkoutPlan {
  type: 'Weekly';
  activeDaysCount: number;
  calendar: WeeklyDayPlan[];
}

export interface DailyPlanner extends BaseWorkoutPlan {
  type: 'Daily';
  /** Id of the plan's single workout day; what a weekday or date override points at. */
  dayId?: string;
  targetCategory: string;
  exercises: PlannerExercise[];
}

export type WorkoutPlan = SplitPlanner | WeeklyPlanner | DailyPlanner;

export interface CalendarDay {
  date: Date;
  dateKey: string;
  dayNum: number;
  isCurrentMonth: boolean;
  isToday: boolean;
  isPast: boolean;
  historySession: any;
  override: any;
  scheduledWorkout: {
    dayName: string;
    category?: string;
    exercises: any[];
    isRestDay: boolean;
  } | null;
  routineName: string;
  isRestDay: boolean;
  exercisesCount: number;
  templateDayName: string;
}


export interface WorkoutPlanDto {
  id: string;
  name: string;
  description: string;
  level: string;
  goal: string;
  type: string;
  gymId?: string;
  daysCount: number;
  exercisesCount: number;
  isCustom: boolean;
  createdBy: string;
  createdOn: Date | string;
  days: WorkoutPlanDayDto[];
}

export interface WorkoutPlanDayDto {
  id: string;
  workoutPlanId: string;
  dayName?: string;
  dayIndex: number;
  isRestDay: boolean;
  category?: string;
  freeTextNotes?: string;
  exercises: WorkoutPlanExerciseDto[];
}

export interface WorkoutPlanExerciseDto {
  id: string;
  workoutPlanDayId: string;
  exerciseId?: string;
  exerciseName: string;
  sets: number;
  reps: string;
  notes?: string;
  sortOrder: number;
}

export interface CreateWorkoutPlanRequest {
  name: string;
  description: string;
  level: string;
  goal: string;
  type: string;
  isCustom: boolean;
  days: CreateWorkoutPlanDayDto[];
}

export interface UpdateWorkoutPlanRequest {
  id: string;
  name: string;
  description: string;
  level: string;
  goal: string;
  type: string;
  isCustom: boolean;
  days: CreateWorkoutPlanDayDto[];
}

export interface CreateWorkoutPlanDayDto {
  dayName?: string;
  dayIndex: number;
  isRestDay: boolean;
  category?: string;
  freeTextNotes?: string;
  exercises: CreateWorkoutPlanExerciseDto[];
}

export interface CreateWorkoutPlanExerciseDto {
  exerciseId?: string;
  exerciseName: string;
  sets: number;
  reps: string;
  notes?: string;
  sortOrder: number;
}

export interface MemberPlanAssignmentDto {
  id: string;
  memberId?: string;
  userId?: string;
  workoutPlanId: string;
  assignedAt: string | Date;
  isActive: boolean;
  workoutPlan: WorkoutPlanDto;
}

/** Inclusive date range as YYYY-MM-DD keys. */
export interface DateRange {
  from: string;
  to: string;
}

export interface WorkoutSessionLogDto {
  id: string;
  memberId: string;
  date: string | Date;
  dayName: string;
  exercisesCompleted: number;
  totalSets: number;
  status: string;
  notes?: string;
  loggedExercises: LoggedExerciseDto[];
}

export interface LoggedExerciseDto {
  id: string;
  name: string;
  skipped: boolean;
  sortOrder: number;
  isCardio: boolean;
  loggedSets: LoggedSetDto[];
}

export interface LoggedSetDto {
  id: string;
  setNo: number;
  weight: number;
  reps: number;
  completed: boolean;
}

export interface LogWorkoutSessionRequest {
  date: string | Date;
  dayName: string;
  status: string;
  notes?: string;
  loggedExercises: LogLoggedExerciseDto[];
}

export interface LogLoggedExerciseDto {
  name: string;
  skipped: boolean;
  sortOrder: number;
  isCardio: boolean;
  loggedSets: LogLoggedSetDto[];
}

export interface LogLoggedSetDto {
  setNo: number;
  weight: number;
  reps: number;
  completed: boolean;
}

export interface ActivePlanView {
  name: string;
  days: ActivePlanDay[];
}

export interface ActivePlanDay {
  id?: string;
  dayName: string;
  dayIndex?: number;
  isRestDay?: boolean;
  category?: string;
  exercises?: { exerciseName: string; sets?: number | string; reps?: string; notes?: string }[];
}

export interface SchedulableDay {
  dayName: string;
  isRestDay?: boolean;
  category?: string;
}

export interface TrainingOverview {
  plan: ActivePlanView | null;
  logs: WorkoutSessionLogDto[];
}
