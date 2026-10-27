import { Injectable } from '@angular/core';
import { Observable, shareReplay, tap } from 'rxjs';
import { API_CONSTANTS } from '../constants/api-constants';
import { ApiResponse } from '../../shared/models/api-response.model';
import { BaseApiService } from './base-api.service';
import { AnnouncementTemplateRequest, AnnouncementTemplateResponse } from '../../shared/models/announcement.model';

@Injectable({
  providedIn: 'root',
})
export class TemplateService extends BaseApiService {
  private templatesCache$: Observable<ApiResponse<AnnouncementTemplateResponse[]>> | null = null;

  constructor() {
    super();
  }

  getTemplates(): Observable<ApiResponse<AnnouncementTemplateResponse[]>> {
    if (!this.templatesCache$) {
      this.templatesCache$ = this.get<ApiResponse<AnnouncementTemplateResponse[]>>(API_CONSTANTS.ANNOUNCEMENT_TEMPLATES.BASE).pipe(
        shareReplay(1)
      );
    }
    return this.templatesCache$;
  }

  clearCache(): void {
    this.templatesCache$ = null;
  }

  getTemplateById(id: string): Observable<ApiResponse<AnnouncementTemplateResponse>> {
    return this.get<ApiResponse<AnnouncementTemplateResponse>>(`${API_CONSTANTS.ANNOUNCEMENT_TEMPLATES.BASE}/${id}`);
  }

  createTemplate(dto: AnnouncementTemplateRequest): Observable<ApiResponse<AnnouncementTemplateResponse>> {
    return this.post<ApiResponse<AnnouncementTemplateResponse>>(API_CONSTANTS.ANNOUNCEMENT_TEMPLATES.BASE, dto).pipe(
      tap(() => this.clearCache())
    );
  }

  updateTemplate(id: string, dto: AnnouncementTemplateRequest): Observable<ApiResponse<AnnouncementTemplateResponse>> {
    return this.put<ApiResponse<AnnouncementTemplateResponse>>(`${API_CONSTANTS.ANNOUNCEMENT_TEMPLATES.BASE}/${id}`, dto).pipe(
      tap(() => this.clearCache())
    );
  }

  deleteTemplate(id: string): Observable<ApiResponse<null>> {
    return this.delete<ApiResponse<null>>(`${API_CONSTANTS.ANNOUNCEMENT_TEMPLATES.BASE}/${id}`).pipe(
      tap(() => this.clearCache())
    );
  }
}
