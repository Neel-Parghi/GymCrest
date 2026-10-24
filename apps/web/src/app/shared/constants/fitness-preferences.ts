/** Canonical member preference ids; they mirror GymCrest.Shared.Constants.FitnessPreferences on the API. */

export type FitnessGoalId = 'weight_loss' | 'muscle_gain' | 'endurance' | 'flexibility' | 'general_fitness';
export type DietType = 'veg' | 'eggetarian' | 'non_veg' | 'vegan';
export type ActivityLevel = 'sedentary' | 'light' | 'moderate' | 'active' | 'very_active';

export interface PreferenceOption<T extends string> {
  value: T;
  label: string;
  hint: string;
  icon: string;
}

export const FITNESS_GOALS: PreferenceOption<FitnessGoalId>[] = [
  { value: 'weight_loss', label: 'Weight loss', hint: 'Burn fat and get leaner', icon: 'fa-solid fa-weight-scale' },
  { value: 'muscle_gain', label: 'Muscle gain', hint: 'Build size and strength', icon: 'fa-solid fa-dumbbell' },
  { value: 'endurance', label: 'Endurance', hint: 'Improve stamina and cardio', icon: 'fa-solid fa-person-running' },
  { value: 'flexibility', label: 'Flexibility', hint: 'Mobility and recovery', icon: 'fa-solid fa-child-reaching' },
  { value: 'general_fitness', label: 'General fitness', hint: 'Stay active and healthy', icon: 'fa-solid fa-heart-pulse' }
];

export const DIET_TYPES: PreferenceOption<DietType>[] = [
  { value: 'veg', label: 'Vegetarian', hint: 'Dairy, no eggs or meat', icon: 'fa-solid fa-leaf' },
  { value: 'eggetarian', label: 'Eggetarian', hint: 'Vegetarian plus eggs', icon: 'fa-solid fa-egg' },
  { value: 'non_veg', label: 'Non-vegetarian', hint: 'Includes meat and fish', icon: 'fa-solid fa-drumstick-bite' },
  { value: 'vegan', label: 'Vegan', hint: 'Fully plant-based', icon: 'fa-solid fa-seedling' }
];

export const ACTIVITY_LEVELS: PreferenceOption<ActivityLevel>[] = [
  { value: 'sedentary', label: 'Sedentary', hint: 'Desk job, little walking', icon: 'fa-solid fa-chair' },
  { value: 'light', label: 'Lightly active', hint: 'On your feet some of the day', icon: 'fa-solid fa-person-walking' },
  { value: 'moderate', label: 'Moderately active', hint: 'Regularly moving around', icon: 'fa-solid fa-person-hiking' },
  { value: 'active', label: 'Very active', hint: 'Physical job or lots of steps', icon: 'fa-solid fa-person-running' },
  { value: 'very_active', label: 'Extremely active', hint: 'Hard physical work daily', icon: 'fa-solid fa-person-digging' }
];

export const COMPATIBLE_DIET_TYPES: Record<DietType, DietType[]> = {
  vegan: ['vegan'],
  veg: ['veg', 'vegan'],
  eggetarian: ['eggetarian', 'veg', 'vegan'],
  non_veg: ['non_veg', 'eggetarian', 'veg', 'vegan']
};

export function toGoalId(value: string | null | undefined): FitnessGoalId | '' {
  const id = (value ?? '').trim().toLowerCase().replace(/[\s-]+/g, '_');
  return FITNESS_GOALS.some(g => g.value === id) ? id as FitnessGoalId : '';
}

export function goalLabel(value: string | null | undefined): string {
  return FITNESS_GOALS.find(g => g.value === toGoalId(value))?.label ?? 'General fitness';
}

export function dietTypeLabel(value: string | null | undefined): string {
  return DIET_TYPES.find(d => d.value === value)?.label ?? '';
}

export function suitsDiet(planType: string | null | undefined, preference: DietType | null | undefined): boolean {
  if (!preference || !planType) return true;
  return COMPATIBLE_DIET_TYPES[preference].includes(planType as DietType);
}
