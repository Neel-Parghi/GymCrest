import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';
import { API_CONSTANTS } from '../constants/api-constants';
import { ApiResponse } from '../../shared/models/api-response.model';
import { FeedbackCategory, FeedbackItem } from '../../shared/models/feedback.model';

@Injectable({ providedIn: 'root' })
export class FeedbackService extends BaseApiService {
  submit(category: FeedbackCategory, message: string, pageUrl: string): Observable<ApiResponse<{ message: string }>> {
    return this.post<ApiResponse<{ message: string }>>(API_CONSTANTS.FEEDBACK.BASE, { category, message, pageUrl });
  }

  getAll(): Observable<ApiResponse<FeedbackItem[]>> {
    return this.get<ApiResponse<FeedbackItem[]>>(API_CONSTANTS.FEEDBACK.BASE);
  }

  setResolved(id: string, value: boolean): Observable<ApiResponse<unknown>> {
    return this.patch<ApiResponse<unknown>>(`${API_CONSTANTS.FEEDBACK.BASE}/${id}/resolved?value=${value}`, {});
  }
}
