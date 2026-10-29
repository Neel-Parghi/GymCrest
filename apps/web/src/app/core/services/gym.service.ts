import { inject, Injectable } from "@angular/core";
import { BaseApiService } from "./base-api.service";
import { API_CONSTANTS } from "../constants/api-constants";
import { Observable, Subject, shareReplay, tap, catchError, of, timer, concatMap, map, takeWhile, last } from "rxjs";
import { ApiResponse } from "../../shared/models/api-response.model";
import { OnboardGymRequest, GymOwnerResponse, GymListResponse, UpdateGymOwnerRequest, UpdateMyGymRequest, BranchDto } from "../../shared/models/gym.model";
import { PagedResponse } from "../../shared/models/paged-response.model";
import { AuthApiService } from "./auth-api.service";

@Injectable({
    providedIn: 'root',
})
export class GymService extends BaseApiService {
    private authService = inject(AuthApiService);

    private gymOwnersCache$?: Observable<ApiResponse<PagedResponse<GymOwnerResponse>>> | null;
    private gymListCache$?: Observable<ApiResponse<PagedResponse<GymListResponse>>> | null;
    private branchesCache = new Map<string, Observable<ApiResponse<BranchDto[]>>>();
    private myBranchesCache$: Observable<ApiResponse<BranchDto[]>> | null = null;
    private myGymCache$: Observable<ApiResponse<GymListResponse>> | null = null;
    private myGymUpdatedSubject = new Subject<void>();
    readonly myGymUpdated$ = this.myGymUpdatedSubject.asObservable();

    constructor() {
        super();
        this.authService.userProfile$.subscribe(user => {
            if (!user) {
                this.clearCache();
            }
        });
    }

    clearCache(): void {
        this.gymOwnersCache$ = null;
        this.gymListCache$ = null;
        this.branchesCache.clear();
        this.myBranchesCache$ = null;
        this.myGymCache$ = null;
    }

    clearGymOwnersCache(): void {
        this.gymOwnersCache$ = null;
    }

    clearGymListCache(): void {
        this.gymListCache$ = null;
        this.branchesCache.clear();
        this.myBranchesCache$ = null;
        this.myGymCache$ = null;
    }

    clearMyGymCache(): void {
        this.myGymCache$ = null;
    }

    /**
     * Whether the gym's plan is active, asking the server a few times. Razorpay's webhook can activate a plan
     * a moment after the checkout popup closes without reporting success, so one check is not enough.
     */
    confirmSubscriptionActive(attempts = 4, intervalMs = 2000): Observable<boolean> {
        return timer(0, intervalMs).pipe(
            concatMap(() => this.getMyGym(true)),
            map(res => !!res?.data?.hasActiveSubscription),
            takeWhile((active, i) => !active && i < attempts - 1, true),
            last()
        );
    }

    clearMyBranchesCache(): void {
        this.myBranchesCache$ = null;
    }

    clearBranchesCache(gymId?: string): void {
        if (gymId) {
            this.branchesCache.delete(gymId);
        } else {
            this.branchesCache.clear();
        }
    }

    onboardGym(payload: OnboardGymRequest): Observable<ApiResponse<{ message: string; gymId: string }>> {
        return this.post<ApiResponse<{ message: string; gymId: string }>>(API_CONSTANTS.GYM.ONBOARD, payload).pipe(
            tap(() => this.clearGymListCache())
        );
    }

    getGymOwnersList(pageNumber: number = 1, pageSize: number = 10, search: string = '', filters: Record<string, string> = {}, forceRefresh = false): Observable<ApiResponse<PagedResponse<GymOwnerResponse>>> {
        const activeFilters = Object.entries(filters).filter(([, value]) => !!value);
        if (search || activeFilters.length || pageNumber !== 1 || pageSize !== 10 || forceRefresh) {
            const params: Record<string, string | number> = { pageNumber, pageSize };
            if (search)
                params['searchTerm'] = search;
            activeFilters.forEach(([key, value]) => params[key] = value);
            return this.get<ApiResponse<PagedResponse<GymOwnerResponse>>>(API_CONSTANTS.GYM_OWNER.LIST, params);
        }

        if (!this.gymOwnersCache$) {
            this.gymOwnersCache$ = this.get<ApiResponse<PagedResponse<GymOwnerResponse>>>(API_CONSTANTS.GYM_OWNER.LIST, { pageNumber: 1, pageSize: 10 }).pipe(
                shareReplay(1)
            );
        }

        return this.gymOwnersCache$;
    }

    getGymList(pageNumber: number = 1, pageSize: number = 10, search: string = '', filters: Record<string, string> = {}, forceRefresh = false): Observable<ApiResponse<PagedResponse<GymListResponse>>> {
        const activeFilters = Object.entries(filters).filter(([, value]) => !!value);
        if (search || activeFilters.length || pageNumber !== 1 || pageSize !== 10 || forceRefresh) {
            const params: Record<string, string | number> = { pageNumber, pageSize };
            if (search)
                params['searchTerm'] = search;
            activeFilters.forEach(([key, value]) => params[key] = value);
            return this.get<ApiResponse<PagedResponse<GymListResponse>>>(API_CONSTANTS.GYM.LIST, params);
        }

        if (!this.gymListCache$) {
            this.gymListCache$ = this.get<ApiResponse<PagedResponse<GymListResponse>>>(API_CONSTANTS.GYM.LIST, { pageNumber: 1, pageSize: 10 }).pipe(
                shareReplay(1)
            );
        }

        return this.gymListCache$;
    }

    deleteGymOwner(ownerId: string): Observable<ApiResponse<unknown>> {
        return this.delete<ApiResponse<unknown>>(`${API_CONSTANTS.GYM_OWNER.DELETE}/${ownerId}`).pipe(
            tap(() => this.clearGymOwnersCache())
        );
    }

    approveOwnerDeletion(ownerId: string): Observable<ApiResponse<unknown>> {
        return this.post<ApiResponse<unknown>>(API_CONSTANTS.GYM_OWNER.APPROVE_DELETION.replace('{id}', ownerId), {}).pipe(
            tap(() => {
                this.clearGymOwnersCache();
                this.clearGymListCache();
            })
        );
    }

    declineOwnerDeletion(ownerId: string): Observable<ApiResponse<unknown>> {
        return this.post<ApiResponse<unknown>>(API_CONSTANTS.GYM_OWNER.DECLINE_DELETION.replace('{id}', ownerId), {}).pipe(
            tap(() => this.clearGymOwnersCache())
        );
    }

    updateGymOwner(ownerId: string, payload: UpdateGymOwnerRequest): Observable<ApiResponse<unknown>> {
        return this.put<ApiResponse<unknown>>(`${API_CONSTANTS.GYM_OWNER.UPDATE}/${ownerId}`, payload).pipe(
            tap(() => {
                this.clearGymOwnersCache();
                this.clearGymListCache();
            })
        );
    }

    updateGym(gymId: string, payload: unknown): Observable<ApiResponse<unknown>> {
        return this.put<ApiResponse<unknown>>(`${API_CONSTANTS.GYM.UPDATE}/${gymId}`, payload).pipe(
            tap(() => this.clearGymListCache())
        );
    }

    deleteGym(gymId: string): Observable<ApiResponse<unknown>> {
        return this.delete<ApiResponse<unknown>>(`${API_CONSTANTS.GYM.DELETE}/${gymId}`).pipe(
            tap(() => {
                this.clearGymListCache();
                this.clearGymOwnersCache();
            })
        );
    }

    getGymBranches(gymId: string): Observable<ApiResponse<BranchDto[]>> {
        if (this.branchesCache.has(gymId)) {
            return this.branchesCache.get(gymId)!;
        }
        const url = API_CONSTANTS.GYM.BRANCHES.replace('{id}', gymId);
        const obs = this.get<ApiResponse<BranchDto[]>>(url).pipe(shareReplay(1));
        this.branchesCache.set(gymId, obs);
        return obs;
    }

    addGymBranch(gymId: string, payload: Partial<BranchDto>): Observable<ApiResponse<BranchDto>> {
        const url = API_CONSTANTS.GYM.BRANCHES.replace('{id}', gymId);
        return this.post<ApiResponse<BranchDto>>(url, payload).pipe(
            tap(() => this.clearBranchesCache(gymId))
        );
    }

    getMyGym(forceRefresh = false): Observable<ApiResponse<GymListResponse>> {
        if (forceRefresh || !this.myGymCache$) {
            this.myGymCache$ = this.get<ApiResponse<GymListResponse>>(API_CONSTANTS.GYM.MY_GYM).pipe(
                catchError(() => of({ success: true, data: null as unknown as GymListResponse, message: 'No gym found', error: null, statusCode: 200, timestamp: new Date().toISOString() } as ApiResponse<GymListResponse>)),
                shareReplay(1)
            );
        }
        return this.myGymCache$;
    }

    updateMyGym(payload: UpdateMyGymRequest): Observable<ApiResponse<unknown>> {
        return this.put<ApiResponse<unknown>>(API_CONSTANTS.GYM.MY_GYM, payload).pipe(
            tap(() => {
                this.clearMyGymCache();
                this.myGymUpdatedSubject.next();
            })
        );
    }

    getMyBranches(forceRefresh = false): Observable<ApiResponse<BranchDto[]>> {
        if (forceRefresh || !this.myBranchesCache$) {
            this.myBranchesCache$ = this.get<ApiResponse<BranchDto[]>>(API_CONSTANTS.GYM.MY_BRANCHES).pipe(
                catchError(() => of({ success: true, data: [], message: 'No branches found', error: null, statusCode: 200, timestamp: new Date().toISOString() } as ApiResponse<BranchDto[]>)),
                shareReplay(1)
            );
        }
        return this.myBranchesCache$;
    }

    addMyBranch(payload: Partial<BranchDto>): Observable<ApiResponse<BranchDto>> {
        return this.post<ApiResponse<BranchDto>>(API_CONSTANTS.GYM.MY_BRANCHES, payload).pipe(
            tap(() => this.clearMyBranchesCache())
        );
    }

    updateMyBranch(branchId: string, payload: Partial<BranchDto>): Observable<ApiResponse<BranchDto>> {
        return this.put<ApiResponse<BranchDto>>(`${API_CONSTANTS.GYM.MY_BRANCHES}/${branchId}`, payload).pipe(
            tap(() => this.clearMyBranchesCache())
        );
    }
}