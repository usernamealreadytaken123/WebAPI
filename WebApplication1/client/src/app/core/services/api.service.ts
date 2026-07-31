import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  ResultFilters,
  ResultResponse,
  UploadResponse,
  ValueResponse
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);

  uploadCsv(file: File): Observable<UploadResponse> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<UploadResponse>('/api/files/upload', formData);
  }

  getResults(filters: ResultFilters = {}): Observable<ResultResponse[]> {
    let params = new HttpParams();

    for (const [name, value] of Object.entries(filters)) {
      if (value !== undefined && value !== '') {
        params = params.set(name, String(value));
      }
    }

    return this.http.get<ResultResponse[]>('/api/results', { params });
  }

  getLatestValues(fileName: string): Observable<ValueResponse[]> {
    const params = new HttpParams().set('fileName', fileName.trim());

    return this.http.get<ValueResponse[]>('/api/values/latest', { params });
  }
}
