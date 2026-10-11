import { Injectable, inject } from '@angular/core';
import { BaseApiService } from './base-api.service';
import { API_CONSTANTS } from '../constants/api-constants';
import { Observable, shareReplay, tap } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiResponse } from '../../shared/models/api-response.model';
import { WorkoutPlan, WorkoutPlanType, SplitPlanner, WeeklyPlanner, DailyPlanner, WorkoutPlanDto, CreateWorkoutPlanRequest, UpdateWorkoutPlanRequest } from '../../shared/models/workout-plan.model';
import { BranchContextService } from './branch-context.service';

@Injectable({
  providedIn: 'root'
})
export class WorkoutPlanService extends BaseApiService {
  private branchContextService = inject(BranchContextService);
  private plansCache$: Observable<WorkoutPlan[]> | null = null;

  constructor() {
    super();
    this.branchContextService.activeBranch$.subscribe(() => {
      this.clearCache();
    });
  }

  getPlans(type?: string, forceRefresh = false): Observable<WorkoutPlan[]> {
    if (forceRefresh) {
      this.clearCache();
    }

    if (type) {
      const params = { type };
      return this.get<ApiResponse<WorkoutPlanDto[]>>(API_CONSTANTS.WORKOUT_PLAN.BASE, params).pipe(
        map(res => {
          const plans = res?.data || [];
          return plans.map((p: WorkoutPlanDto) => this.adaptFromApi(p));
        })
      );
    }

    if (!this.plansCache$) {
      this.plansCache$ = this.get<ApiResponse<WorkoutPlanDto[]>>(API_CONSTANTS.WORKOUT_PLAN.BASE).pipe(
        map(res => {
          const plans = res?.data || [];
          return plans.map((p: WorkoutPlanDto) => this.adaptFromApi(p));
        }),
        shareReplay(1)
      );
    }
    return this.plansCache$;
  }

  getPlanById(id: string): Observable<WorkoutPlan> {
    const url = API_CONSTANTS.WORKOUT_PLAN.BY_ID.replace('{id}', id);
    return this.get<ApiResponse<WorkoutPlanDto>>(url).pipe(
      map(res => this.adaptFromApi(res?.data as WorkoutPlanDto))
    );
  }

  createPlan(plan: WorkoutPlan): Observable<WorkoutPlan> {
    const payload = this.adaptToApi(plan);
    return this.post<ApiResponse<WorkoutPlanDto>>(API_CONSTANTS.WORKOUT_PLAN.BASE, payload).pipe(
      tap(() => this.clearCache()),
      map(res => this.adaptFromApi(res?.data as WorkoutPlanDto))
    );
  }

  updatePlan(id: string, plan: WorkoutPlan): Observable<WorkoutPlan> {
    const url = API_CONSTANTS.WORKOUT_PLAN.BY_ID.replace('{id}', id);
    const payload = this.adaptToApi(plan);
    return this.put<ApiResponse<WorkoutPlanDto>>(url, payload).pipe(
      tap(() => this.clearCache()),
      map(res => this.adaptFromApi(res?.data as WorkoutPlanDto))
    );
  }

  deletePlan(id: string): Observable<ApiResponse<null>> {
    const url = API_CONSTANTS.WORKOUT_PLAN.BY_ID.replace('{id}', id);
    return this.delete<ApiResponse<null>>(url).pipe(
      tap(() => this.clearCache())
    );
  }

  clearCache(): void {
    this.plansCache$ = null;
  }

  private adaptFromApi(apiPlanData: WorkoutPlanDto): WorkoutPlan {
    if (!apiPlanData) {
      throw new Error('Null API plan cannot be adapted');
    }
    const apiPlan = apiPlanData;

    const basePlan = {
      id: apiPlan.id,
      name: apiPlan.name,
      description: apiPlan.description,
      level: apiPlan.level,
      goal: apiPlan.goal,
      type: apiPlan.type as WorkoutPlanType,
      gymId: apiPlan.gymId,
      isCustom: apiPlan.isCustom ?? false,
      daysCount: apiPlan.daysCount,
      exercisesCount: apiPlan.exercisesCount,
      createdBy: apiPlan.createdBy,
      createdAt: apiPlan.createdOn ? new Date(apiPlan.createdOn) : new Date()
    };

    if (apiPlan.type === 'Split') {
      return {
        ...basePlan,
        type: 'Split',
        days: [...(apiPlan.days || [])]
          .sort((a, b) => (a.dayIndex || 0) - (b.dayIndex || 0))
          .map((day: any) => ({
            id: day.id,
            name: day.dayName,
            category: day.category,
            exercises: [...(day.exercises || [])]
              .sort((a, b) => (a.sortOrder || 0) - (b.sortOrder || 0))
              .map((ex: any) => ({
                id: ex.id,
                name: ex.exerciseName,
                sets: ex.sets,
                reps: ex.reps,
                notes: ex.notes
              }))
          }))
      } as SplitPlanner;
    } else if (apiPlan.type === 'Weekly') {
      const weekdayOrder: { [key: string]: number } = {
        'monday': 1,
        'tuesday': 2,
        'wednesday': 3,
        'thursday': 4,
        'friday': 5,
        'saturday': 6,
        'sunday': 7
      };
      return {
        ...basePlan,
        type: 'Weekly',
        activeDaysCount: apiPlan.exercisesCount > 0 ? (apiPlan.days || []).filter((d: any) => !d.isRestDay).length : 0,
        calendar: [...(apiPlan.days || [])]
          .sort((a, b) => {
            const orderA = weekdayOrder[(a.dayName || '').toLowerCase()] || a.dayIndex || 0;
            const orderB = weekdayOrder[(b.dayName || '').toLowerCase()] || b.dayIndex || 0;
            return orderA - orderB;
          })
          .map((day: any) => ({
            id: day.id,
            dayName: day.dayName,
            type: day.isRestDay ? 'rest' : 'workout',
            targetCategory: day.category || '',
            splitDayName: day.dayName + ' Focus',
            exercises: [...(day.exercises || [])]
              .sort((a, b) => (a.sortOrder || 0) - (b.sortOrder || 0))
              .map((ex: any) => ({
                id: ex.id,
                name: ex.exerciseName,
                sets: ex.sets,
                reps: ex.reps,
                notes: ex.notes
              }))
          }))
      } as WeeklyPlanner;
    } else { // Daily
      const virtualDay = apiPlan.days?.[0] || { category: '', exercises: [] };
      return {
        ...basePlan,
        type: 'Daily',
        dayId: virtualDay.id,
        targetCategory: virtualDay.category || '',
        exercises: [...(virtualDay.exercises || [])]
          .sort((a, b) => (a.sortOrder || 0) - (b.sortOrder || 0))
          .map((ex: any) => ({
            id: ex.id,
            name: ex.exerciseName,
            sets: ex.sets,
            reps: ex.reps,
            notes: ex.notes
          }))
      } as DailyPlanner;
    }
  }

  private adaptToApi(fePlan: WorkoutPlan): CreateWorkoutPlanRequest | UpdateWorkoutPlanRequest | null {
    if (!fePlan) return null;

    const basePlan: Record<string, unknown> = {
      name: fePlan.name,
      description: fePlan.description,
      level: fePlan.level,
      goal: fePlan.goal,
      type: fePlan.type,
      isCustom: fePlan.isCustom ?? false
    };

    if (fePlan.id) {
      basePlan['id'] = fePlan.id;
    }

    if (fePlan.type === 'Split') {
      const p = fePlan as SplitPlanner;
      basePlan['days'] = (p.days || []).map((day, dIdx) => ({
        dayName: day.name,
        dayIndex: dIdx + 1,
        isRestDay: false,
        category: day.category,
        exercises: (day.exercises || []).map((ex, exIdx) => ({
          exerciseName: ex.name,
          sets: ex.sets,
          reps: ex.reps,
          notes: ex.notes,
          sortOrder: exIdx
        }))
      }));
    } else if (fePlan.type === 'Weekly') {
      const p = fePlan as WeeklyPlanner;
      basePlan['days'] = (p.calendar || []).map((day, dIdx) => ({
        dayName: day.dayName,
        dayIndex: dIdx + 1,
        isRestDay: day.type === 'rest',
        category: day.targetCategory,
        exercises: day.type === 'rest' ? [] : (day.exercises || []).map((ex, exIdx) => ({
          exerciseName: ex.name,
          sets: ex.sets,
          reps: ex.reps,
          notes: ex.notes,
          sortOrder: exIdx
        }))
      }));
    } else if (fePlan.type === 'Daily') {
      const p = fePlan as DailyPlanner;
      basePlan['days'] = [{
        dayName: 'Workout Day',
        dayIndex: 1,
        isRestDay: false,
        category: p.targetCategory,
        exercises: (p.exercises || []).map((ex, exIdx) => ({
          exerciseName: ex.name,
          sets: ex.sets,
          reps: ex.reps,
          notes: ex.notes,
          sortOrder: exIdx
        }))
      }];
    }

    return basePlan as any;
  }
}
