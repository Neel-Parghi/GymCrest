import { Injectable, inject } from '@angular/core';
import { AuthApiService } from './auth-api.service';
import { BaseApiService } from './base-api.service';
import { API_CONSTANTS } from '../constants/api-constants';
import { Observable, shareReplay, tap, catchError, of, forkJoin, map } from 'rxjs';
import { GymMember, MemberDashboardResponse, MemberSubscription, OnboardMemberRequest, RenewSubscriptionRequest } from '../../shared/models/member.model';
import { ActivePlanView, DateRange, MemberPlanAssignmentDto, WorkoutSessionLogDto, LogWorkoutSessionRequest, TrainingOverview } from '../../shared/models/workout-plan.model';
import { DietPlanDto, MemberDietAssignmentDto } from '../../shared/models/diet-plan.model';
import { ApiResponse } from '../../shared/models/api-response.model';
import { PagedResponse } from '../../shared/models/paged-response.model';
import { BranchContextService } from './branch-context.service';
import { WorkoutProgressService } from './workout-progress.service';

@Injectable({
  providedIn: 'root'
})
export class MemberService extends BaseApiService {

  private authService = inject(AuthApiService);
  private branchContextService = inject(BranchContextService);
  private workoutProgressService = inject(WorkoutProgressService);
  private membersListCache = new Map<string, Observable<ApiResponse<PagedResponse<GymMember>>>>();
  private memberCache = new Map<string, Observable<ApiResponse<GymMember>>>();
  private historyCache = new Map<string, Observable<ApiResponse<MemberSubscription[]>>>();
  private activePlanCache = new Map<string, Observable<ApiResponse<MemberPlanAssignmentDto | null>>>();
  private planAssignmentsCache = new Map<string, Observable<ApiResponse<MemberPlanAssignmentDto[]>>>();
  private workoutLogsCache = new Map<string, Observable<ApiResponse<WorkoutSessionLogDto[]>>>();
  private activeDietCache = new Map<string, Observable<ApiResponse<MemberDietAssignmentDto | null>>>();
  private dashboardCache$: Observable<ApiResponse<MemberDashboardResponse>> | null = null;

  constructor() {
    super();
    this.authService.userProfile$.subscribe(user => {
      if (!user) {
        this.clearCache();
      }
    });
    this.branchContextService.activeBranch$.subscribe(() => {
      this.clearCache();
    });
  }

  onboardMember(payload: OnboardMemberRequest): Observable<ApiResponse<GymMember>> {
    const branchId = this.branchContextService.getActiveBranchId();
    if (branchId && !payload.branchId) {
      payload.branchId = branchId;
    }
    return this.post<ApiResponse<GymMember>>(API_CONSTANTS.MEMBERS.ONBOARD, payload).pipe(
      tap(() => this.clearCache())
    );
  }

  getGymMembers(pageNumber: number = 1, pageSize: number = 10, search: string = '', status: string = 'all', planId: string = 'all', paymentStatus: string = 'all', forceRefresh = false, bypassPagination = false): Observable<ApiResponse<PagedResponse<GymMember>>> {
    const branchId = this.branchContextService.getActiveBranchId();

    const params: Record<string, string | number | boolean> = { pageNumber, pageSize };
    if (search)
      params['searchTerm'] = search;
    if (bypassPagination)
      params['bypassPagination'] = true;
    if (branchId)
      params['branchId'] = branchId;
    if (status !== 'all')
      params['status'] = status;
    if (planId !== 'all')
      params['planId'] = planId;
    if (paymentStatus !== 'all')
      params['paymentStatus'] = paymentStatus;

    if (search || bypassPagination || status !== 'all' || planId !== 'all' || paymentStatus !== 'all') {
      return this.get<ApiResponse<PagedResponse<GymMember>>>(API_CONSTANTS.MEMBERS.LIST, params);
    }

    const cacheKey = `${pageNumber}-${pageSize}-${branchId || 'all'}`;

    if (forceRefresh || !this.membersListCache.has(cacheKey)) {
      const request$ = this.get<ApiResponse<PagedResponse<GymMember>>>(API_CONSTANTS.MEMBERS.LIST, params).pipe(
        shareReplay(1)
      );
      this.membersListCache.set(cacheKey, request$);
    }

    return this.membersListCache.get(cacheKey)!;
  }

  getMemberById(id: string): Observable<ApiResponse<GymMember>> {
    if (this.memberCache.has(id)) {
      return this.memberCache.get(id)!;
    }

    const obs = this.get<ApiResponse<GymMember>>(`${API_CONSTANTS.MEMBERS.GET}/${id}`)
      .pipe(shareReplay(1));
    this.memberCache.set(id, obs);
    return obs;
  }

  toggleMemberStatus(id: string): Observable<ApiResponse<{ success: boolean }>> {
    return this.patch<ApiResponse<{ success: boolean }>>(API_CONSTANTS.MEMBERS.TOGGLE_STATUS.replace('{id}', id), {}).pipe(
      tap(() => this.clearCache())
    );
  }

  freezeMember(id: string, freezeUntil: string): Observable<ApiResponse<{ success: boolean }>> {
    return this.patch<ApiResponse<{ success: boolean }>>(API_CONSTANTS.MEMBERS.FREEZE.replace('{id}', id), { freezeUntil }).pipe(
      tap(() => this.clearCache())
    );
  }

  unfreezeMember(id: string): Observable<ApiResponse<{ success: boolean }>> {
    return this.patch<ApiResponse<{ success: boolean }>>(API_CONSTANTS.MEMBERS.UNFREEZE.replace('{id}', id), {}).pipe(
      tap(() => this.clearCache())
    );
  }

  renewSubscription(id: string, payload: RenewSubscriptionRequest): Observable<ApiResponse<GymMember>> {
    return this.post<ApiResponse<GymMember>>(API_CONSTANTS.MEMBERS.RENEW.replace('{id}', id), payload).pipe(
      tap(() => this.clearCache())
    );
  }

  updateMember(id: string, payload: Partial<OnboardMemberRequest>): Observable<ApiResponse<GymMember>> {
    return this.put<ApiResponse<GymMember>>(`${API_CONSTANTS.MEMBERS.UPDATE}/${id}`, payload).pipe(
      tap(() => this.clearCache())
    );
  }

  deleteMember(id: string): Observable<ApiResponse<{ success: boolean }>> {
    return this.delete<ApiResponse<{ success: boolean }>>(`${API_CONSTANTS.MEMBERS.DELETE}/${id}`).pipe(
      tap(() => this.clearCache())
    );
  }

  getSubscriptionHistory(id: string): Observable<ApiResponse<MemberSubscription[]>> {
    if (this.historyCache.has(id)) {
      return this.historyCache.get(id)!;
    }

    const obs = this.get<ApiResponse<MemberSubscription[]>>(API_CONSTANTS.MEMBERS.SUBSCRIPTION_HISTORY.replace('{id}', id))
      .pipe(shareReplay(1));
    this.historyCache.set(id, obs);
    return obs;
  }

  exportMembers(): Observable<Blob> {
    const branchId = this.branchContextService.getActiveBranchId();
    const params: Record<string, string> = {};
    if (branchId) {
      params['branchId'] = branchId;
    }
    return this.getBlob(API_CONSTANTS.MEMBERS.EXPORT, params);
  }

  getMemberDashboardData(forceRefresh = false): Observable<ApiResponse<MemberDashboardResponse>> {
    if (forceRefresh || !this.dashboardCache$) {
      this.dashboardCache$ = this.get<ApiResponse<MemberDashboardResponse>>(API_CONSTANTS.MEMBERS.DASHBOARD).pipe(
        shareReplay(1)
      );
    }
    return this.dashboardCache$;
  }

  getActivePlan(memberId: string, forceRefresh = false): Observable<ApiResponse<MemberPlanAssignmentDto | null>> {
    if (forceRefresh || !this.activePlanCache.has(memberId)) {
      const request$ = this.get<ApiResponse<MemberPlanAssignmentDto | null>>(API_CONSTANTS.MEMBERS.ACTIVE_PLAN.replace('{memberId}', memberId)).pipe(
        catchError(() => of({ success: true, data: null, message: 'No active plan', error: null, statusCode: 200, timestamp: new Date().toISOString() } as ApiResponse<MemberPlanAssignmentDto | null>)),
        shareReplay(1)
      );
      this.activePlanCache.set(memberId, request$);
    }
    return this.activePlanCache.get(memberId)!;
  }

  assignPlan(memberId: string, planId: string): Observable<ApiResponse<null>> {
    return this.post<ApiResponse<null>>(API_CONSTANTS.MEMBERS.ASSIGN_PLAN.replace('{memberId}', memberId), { workoutPlanId: planId }).pipe(
      tap(() => {
        this.activePlanCache.delete(memberId);
        this.planAssignmentsCache.delete(memberId);
        this.clearWorkoutLogsCache(memberId);
      })
    );
  }

  getPlanAssignments(memberId: string, forceRefresh = false): Observable<ApiResponse<MemberPlanAssignmentDto[]>> {
    if (forceRefresh || !this.planAssignmentsCache.has(memberId)) {
      const request$ = this.get<ApiResponse<MemberPlanAssignmentDto[]>>(API_CONSTANTS.MEMBERS.PLAN_ASSIGNMENTS.replace('{memberId}', memberId)).pipe(
        shareReplay(1)
      );
      this.planAssignmentsCache.set(memberId, request$);
    }
    return this.planAssignmentsCache.get(memberId)!;
  }

  /** Active plan + logged sessions; either side falls back to empty rather than failing. */
  getTrainingOverview(memberId: string, range?: DateRange): Observable<TrainingOverview> {
    return forkJoin({
      // The active-plan endpoint returns the plan itself (name + days), not the assignment wrapper.
      plan: this.getActivePlan(memberId).pipe(
        map(res => (res?.data ?? null) as unknown as ActivePlanView | null),
        catchError(() => of(null))
      ),
      logs: this.getWorkoutLogs(memberId, false, range).pipe(
        map(res => res?.data ?? []),
        catchError(() => of([] as WorkoutSessionLogDto[]))
      )
    });
  }

  /** Logged sessions, newest first. Pass a range to fetch only those dates instead of the full history. */
  getWorkoutLogs(memberId: string, forceRefresh = false, range?: DateRange): Observable<ApiResponse<WorkoutSessionLogDto[]>> {
    const key = range ? `${memberId}|${range.from}|${range.to}` : memberId;
    if (forceRefresh || !this.workoutLogsCache.has(key)) {
      const request$ = this.get<ApiResponse<WorkoutSessionLogDto[]>>(
        API_CONSTANTS.MEMBERS.WORKOUT_LOGS.replace('{memberId}', memberId),
        range ? { from: range.from, to: range.to } : undefined
      ).pipe(
        shareReplay(1)
      );
      this.workoutLogsCache.set(key, request$);
    }
    return this.workoutLogsCache.get(key)!;
  }

  /** Drops the member's full-history and date-range log caches. */
  private clearWorkoutLogsCache(memberId: string): void {
    for (const key of [...this.workoutLogsCache.keys()]) {
      if (key === memberId || key.startsWith(`${memberId}|`)) {
        this.workoutLogsCache.delete(key);
      }
    }
  }

  logWorkoutSession(memberId: string, payload: LogWorkoutSessionRequest): Observable<ApiResponse<WorkoutSessionLogDto>> {
    return this.post<ApiResponse<WorkoutSessionLogDto>>(API_CONSTANTS.MEMBERS.WORKOUT_LOGS.replace('{memberId}', memberId), payload).pipe(
      tap(() => {
        this.clearWorkoutLogsCache(memberId);
        this.workoutProgressService.clearCache();
      })
    );
  }

  saveRecurringOverride(memberId: string, payload: { dayOfWeek: string, workoutPlanDayId: string | null, isRestDay: boolean }): Observable<ApiResponse<null>> {
    return this.post<ApiResponse<null>>(API_CONSTANTS.MEMBERS.RECURRING_OVERRIDE.replace('{memberId}', memberId), payload).pipe(
      tap(() => {
        this.activePlanCache.delete(memberId);
        this.planAssignmentsCache.delete(memberId);
        this.clearWorkoutLogsCache(memberId);
      })
    );
  }

  getActiveDiet(memberId: string, forceRefresh = false): Observable<ApiResponse<MemberDietAssignmentDto | null>> {
    if (forceRefresh || !this.activeDietCache.has(memberId)) {
      const request$ = this.get<ApiResponse<MemberDietAssignmentDto | null>>(API_CONSTANTS.MEMBERS.ACTIVE_DIET.replace('{memberId}', memberId)).pipe(
        catchError(() => of({ success: true, data: null, message: 'No active diet', error: null, statusCode: 200, timestamp: new Date().toISOString() } as ApiResponse<MemberDietAssignmentDto | null>)),
        shareReplay(1)
      );
      this.activeDietCache.set(memberId, request$);
    }
    return this.activeDietCache.get(memberId)!;
  }

  assignDiet(memberId: string, dietPlanId: string): Observable<ApiResponse<null>> {
    return this.post<ApiResponse<null>>(API_CONSTANTS.MEMBERS.ASSIGN_DIET.replace('{memberId}', memberId), { dietPlanId }).pipe(
      tap(() => {
        this.activeDietCache.delete(memberId);
      })
    );
  }

  assignCustomDiet(memberId: string, payload: DietPlanDto): Observable<ApiResponse<null>> {
    return this.post<ApiResponse<null>>(API_CONSTANTS.MEMBERS.CUSTOM_DIET.replace('{memberId}', memberId), payload).pipe(
      tap(() => {
        this.activeDietCache.delete(memberId);
      })
    );
  }

  unassignActiveDiet(memberId: string): Observable<ApiResponse<null>> {
    return this.delete<ApiResponse<null>>(API_CONSTANTS.MEMBERS.ACTIVE_DIET.replace('{memberId}', memberId)).pipe(
      tap(() => {
        this.activeDietCache.delete(memberId);
      })
    );
  }

  clearCache(): void {
    this.membersListCache.clear();
    this.memberCache.clear();
    this.historyCache.clear();
    this.dashboardCache$ = null;
    this.activePlanCache.clear();
    this.planAssignmentsCache.clear();
    this.workoutLogsCache.clear();
    this.activeDietCache.clear();
  }

}
