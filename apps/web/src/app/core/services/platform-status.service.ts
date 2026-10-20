import { Injectable } from '@angular/core';
import { Observable, catchError, map, of, shareReplay } from 'rxjs';
import { BaseApiService } from './base-api.service';
import { API_CONSTANTS } from '../constants/api-constants';
import { ApiResponse } from '../../shared/models/api-response.model';
import { PlatformStatus } from '../../shared/models/platform-status.model';

const EMPTY_STATUS: PlatformStatus = {
  supportEmail: null,
  supportPhone: null,
  gracePeriodDays: 0,
  maintenanceActive: false,
  maintenanceStartTime: null,
  maintenanceEndTime: null
};

/** Support contacts, grace period and maintenance state set by the super admin. */
@Injectable({ providedIn: 'root' })
export class PlatformStatusService extends BaseApiService {
  private cache$?: Observable<PlatformStatus>;

  getStatus(forceRefresh = false): Observable<PlatformStatus> {
    if (!this.cache$ || forceRefresh) {
      this.cache$ = this.get<ApiResponse<PlatformStatus>>(API_CONSTANTS.PLATFORM.STATUS).pipe(
        map(res => ({ ...EMPTY_STATUS, ...(res?.data ?? {}) })),
        // A status hiccup must never break the page that asked for it
        catchError(() => of(EMPTY_STATUS)),
        shareReplay(1)
      );
    }
    return this.cache$;
  }
}
