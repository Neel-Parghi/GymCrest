import { DietType } from '../constants/fitness-preferences';

export interface DietPlanDto {
  id: string;
  name: string;
  description: string;
  protein: number;
  carbs: number;
  fats: number;
  calories: number;
  goal: string;
  dietType?: DietType | null;
  gymId?: string;
  createdBy: string;
  isCustom: boolean;
  isTemplate: boolean;
  meals: DietPlanMealDto[];
}

export interface DietPlanMealDto {
  id: string;
  dietPlanId: string;
  name: string;
  time: string;
  calories: number;
  protein: number;
  carbs: number;
  fats: number;
  items: string;
  sortOrder: number;
}

export interface CreateDietPlanRequest {
  name: string;
  description: string;
  protein: number;
  carbs: number;
  fats: number;
  goal: string;
  dietType?: DietType | null;
  isCustom: boolean;
  isTemplate: boolean;
  meals: CreateDietPlanMealDto[];
}

export interface UpdateDietPlanRequest {
  id: string;
  name: string;
  description: string;
  protein: number;
  carbs: number;
  fats: number;
  goal: string;
  dietType?: DietType | null;
  isCustom: boolean;
  isTemplate: boolean;
  meals: CreateDietPlanMealDto[];
}

export interface CreateDietPlanMealDto {
  name: string;
  time: string;
  calories: number;
  protein: number;
  carbs: number;
  fats: number;
  items: string;
  sortOrder: number;
}

export interface AssignDietPlanRequest {
  dietPlanId: string;
}

export interface MemberDietAssignmentDto {
  id: string;
  memberId?: string;
  userId?: string;
  dietPlanId: string;
  assignedAt: Date;
  isActive: boolean;
  dietPlan?: DietPlanDto;
}
