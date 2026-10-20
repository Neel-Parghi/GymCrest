import { Injectable } from '@angular/core';
import { BaseApiService } from './base-api.service';
import { Observable } from 'rxjs';
import { ApiResponse } from '../../shared/models/api-response.model';
import { API_CONSTANTS } from '../constants/api-constants';
import { shareReplay, tap } from 'rxjs/operators';
import { SaaSConfigurationDto } from '../../shared/models/config.model';

@Injectable({
  providedIn: 'root'
})
export class ConfigurationService extends BaseApiService {
  private cache$?: Observable<ApiResponse<SaaSConfigurationDto>>;

  constructor() {
    super();
  }

  getConfig(forceRefresh = false): Observable<ApiResponse<SaaSConfigurationDto>> {
    if (!this.cache$ || forceRefresh) {
      this.cache$ = this.get<ApiResponse<SaaSConfigurationDto>>(API_CONSTANTS.SUPER_ADMIN.CONFIG).pipe(
        shareReplay(1)
      );
    }
    return this.cache$;
  }

  updateConfig(config: SaaSConfigurationDto): Observable<ApiResponse<SaaSConfigurationDto>> {
    return this.post<ApiResponse<SaaSConfigurationDto>>(API_CONSTANTS.SUPER_ADMIN.CONFIG, config).pipe(
      tap(() => this.cache$ = undefined)
    );
  }

  /** Rebuilds the public demo member's plans and two weeks of logged history. */
  resetDemoMember(): Observable<ApiResponse<DemoDataResetResult>> {
    return this.post<ApiResponse<DemoDataResetResult>>(API_CONSTANTS.SUPER_ADMIN.RESET_DEMO_MEMBER, {});
  }
}

export interface DemoDataResetResult {
  email: string;
  workoutsLogged: number;
  restDaysLogged: number;
  daysOfMealsLogged: number;
  mealEntriesLogged: number;
  healthCheckIns: number;
}
