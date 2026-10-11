import { Injectable } from '@angular/core';
import { Observable, shareReplay, tap, catchError, of } from 'rxjs';
import { API_CONSTANTS } from '../constants/api-constants';
import { BaseApiService } from './base-api.service';
import { ApiResponse } from '../../shared/models/api-response.model';

@Injectable({
  providedIn: 'root',
})
export class UserService extends BaseApiService {
  constructor() {
    super();
  }

  inviteOwner(dto: unknown): Observable<ApiResponse> {
    return this.post<ApiResponse>(API_CONSTANTS.USER.INVITE_OWNER, dto);
  }

  reInviteOwner(userId: string): Observable<ApiResponse> {
    return this.post<ApiResponse>(`${API_CONSTANTS.USER.RE_INVITE}/${userId}`, {});
  }

  setPassword(dto: unknown): Observable<ApiResponse> {
    return this.post<ApiResponse>(API_CONSTANTS.USER.SET_PASSWORD, dto);
  }

  validateInvitation(token: string): Observable<ApiResponse> {
    return this.get<ApiResponse>(`${API_CONSTANTS.USER.VALIDATE_INVITATION}/${token}`, {});
  }

  private preferencesCache$: Observable<ApiResponse> | null = null;
  private subscriptionsCache$: Observable<ApiResponse> | null = null;

  getPreferences(forceRefresh = false): Observable<ApiResponse> {
    if (forceRefresh || !this.preferencesCache$) {
      this.preferencesCache$ = this.get<ApiResponse>(API_CONSTANTS.USER.PREFERENCES).pipe(
        catchError(() => of({ success: true, data: null, message: 'No preferences found', error: null, statusCode: 200, timestamp: new Date().toISOString() })),
        shareReplay(1)
      );
    }
    return this.preferencesCache$;
  }

  updatePreferences(dto: unknown): Observable<ApiResponse> {
    return this.put<ApiResponse>(API_CONSTANTS.USER.PREFERENCES, dto).pipe(
      tap(() => {
        this.preferencesCache$ = null;
        this.clearCache();
      })
    );
  }

  getSuggestedTargets(goal: string, activityLevel?: string | null): Observable<ApiResponse> {
    return this.get<ApiResponse>(API_CONSTANTS.USER.SUGGESTED_TARGETS, { goal, activityLevel });
  }

  regenerateStarterPlans(): Observable<ApiResponse> {
    return this.post<ApiResponse>(API_CONSTANTS.USER.REGENERATE_STARTER_PLANS, {}).pipe(
      tap(() => {
        this.preferencesCache$ = null;
        this.clearCache();
      })
    );
  }

  getMySubscriptions(forceRefresh = false): Observable<ApiResponse> {
    if (forceRefresh || !this.subscriptionsCache$) {
      this.subscriptionsCache$ = this.get<ApiResponse>(API_CONSTANTS.USER.MY_SUBSCRIPTIONS).pipe(
        catchError(() => of({ success: true, data: [], message: 'No subscriptions found', error: null, statusCode: 200, timestamp: new Date().toISOString() })),
        shareReplay(1)
      );
    }
    return this.subscriptionsCache$;
  }

  private myGymCache$: Observable<ApiResponse> | null = null;

  getMyGym(forceRefresh = false): Observable<ApiResponse> {
    if (forceRefresh || !this.myGymCache$) {
      this.myGymCache$ = this.get<ApiResponse>(API_CONSTANTS.USER.MY_GYM).pipe(
        catchError(() => of({ success: true, data: null, message: 'No gym found', error: null, statusCode: 200, timestamp: new Date().toISOString() })),
        shareReplay(1)
      );
    }
    return this.myGymCache$;
  }

  getAvailablePlans(): Observable<ApiResponse> {
    return this.get<ApiResponse>(API_CONSTANTS.USER.AVAILABLE_PLANS);
  }

  initiateUpgradeCheckout(planId: string): Observable<ApiResponse> {
    return this.post<ApiResponse>(API_CONSTANTS.MEMBER_PAYMENT.INITIATE_CHECKOUT, { planId });
  }

  verifyUpgradeCheckout(payload: unknown): Observable<ApiResponse> {
    return this.post<ApiResponse>(API_CONSTANTS.MEMBER_PAYMENT.VERIFY_CHECKOUT, payload);
  }

  initiateOfflineCheckout(planId: string): Observable<ApiResponse> {
    return this.post<ApiResponse>(API_CONSTANTS.MEMBER_PAYMENT.OFFLINE_CHECKOUT, { planId });
  }

  getStandaloneUsers(pageNumber: number = 1, pageSize: number = 10, searchTerm: string = ''): Observable<ApiResponse> {
    const params: Record<string, string | number> = { pageNumber, pageSize };
    if (searchTerm) {
      params['searchTerm'] = searchTerm;
    }
    return this.get<ApiResponse>(API_CONSTANTS.USER.STANDALONE, params);
  }

  saveOnboardingStep(step: number): Observable<ApiResponse> {
    return this.post<ApiResponse>(`${API_CONSTANTS.USER.SAVE_ONBOARDING_STEP}/${step}`, {});
  }

  completeOnboarding(payload: unknown): Observable<ApiResponse> {
    return this.post<ApiResponse>(API_CONSTANTS.USER.COMPLETE_ONBOARDING, payload);
  }

  previewOnboardingTargets(profile: unknown): Observable<ApiResponse> {
    return this.post<ApiResponse>(API_CONSTANTS.USER.ONBOARDING_PREVIEW_TARGETS, profile);
  }

  scheduleAccountDeletion(): Observable<ApiResponse> {
    return this.delete<ApiResponse>(API_CONSTANTS.AUTH.REQUEST_DELETION);
  }

  private dashboardCache$: Observable<ApiResponse> | null = null;
  private routinesCache$: Observable<ApiResponse> | null = null;

  getDashboardSummary(forceRefresh = false): Observable<ApiResponse> {
    if (forceRefresh || !this.dashboardCache$) {
      this.dashboardCache$ = this.get<ApiResponse>(API_CONSTANTS.USER.DASHBOARD).pipe(shareReplay(1));
    }
    return this.dashboardCache$;
  }

  getDailyRoutines(forceRefresh = false): Observable<ApiResponse> {
    if (forceRefresh || !this.routinesCache$) {
      this.routinesCache$ = this.get<ApiResponse>(API_CONSTANTS.USER.ROUTINES).pipe(shareReplay(1));
    }
    return this.routinesCache$;
  }

  clearCache(): void {
    this.dashboardCache$ = null;
    this.routinesCache$ = null;
    this.myGymCache$ = null;
  }

  createDailyRoutine(dto: unknown): Observable<ApiResponse> {
    return this.post<ApiResponse>(API_CONSTANTS.USER.ROUTINES, dto).pipe(tap(() => this.clearCache()));
  }

  updateDailyRoutine(id: string, dto: unknown): Observable<ApiResponse> {
    return this.put<ApiResponse>(`${API_CONSTANTS.USER.ROUTINES}/${id}`, dto).pipe(tap(() => this.clearCache()));
  }

  deleteDailyRoutine(id: string): Observable<ApiResponse> {
    return this.delete<ApiResponse>(`${API_CONSTANTS.USER.ROUTINES}/${id}`).pipe(tap(() => this.clearCache()));
  }

  toggleDailyRoutine(id: string): Observable<ApiResponse> {
    return this.patch<ApiResponse>(`${API_CONSTANTS.USER.ROUTINES}/${id}/toggle`, {}).pipe(tap(() => this.clearCache()));
  }
}
