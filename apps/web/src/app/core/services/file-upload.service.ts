import { Injectable } from '@angular/core';
import { BaseApiService } from './base-api.service';
import { API_CONSTANTS } from '../constants/api-constants';
import { Observable, from, switchMap } from 'rxjs';
import { ApiResponse } from '../../shared/models/api-response.model';
import { compressImage } from '../../shared/utils/image-compressor';

@Injectable({
  providedIn: 'root'
})
export class FileUploadService extends BaseApiService {

  uploadFile(file: File, folder: string = 'general'): Observable<ApiResponse<{ url: string }>> {
    const compressed = folder === 'logos' ? compressImage(file, 512, 0.9, file.type === 'image/jpeg' ? 'image/jpeg' : 'image/png') : compressImage(file);
    return from(compressed).pipe(
      switchMap(compressedFile => {
        const formData = new FormData();
        formData.append('file', compressedFile);
        return this.post<ApiResponse<{ url: string }>>(`${API_CONSTANTS.COMMON.UPLOAD}?folder=${folder}`, formData);
      })
    );
  }
}
