import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { RfpDocument, RfpStatus } from '../models/rfp-document.model';
import { RfpResponse } from '../models/rfp-response.model';

@Injectable({ providedIn: 'root' })
export class RfpApiService {
  private baseUrl = '/api/rfp';

  constructor(private http: HttpClient) {}

  getAll(): Observable<RfpDocument[]> {
    return this.http.get<RfpDocument[]>(this.baseUrl);
  }

  upload(formData: FormData): Observable<RfpDocument> {
    return this.http.post<RfpDocument>(`${this.baseUrl}/upload`, formData);
  }

  getStatus(id: number): Observable<RfpStatus> {
    return this.http.get<RfpStatus>(`${this.baseUrl}/${id}/status`);
  }

  getResponse(id: number): Observable<RfpResponse> {
    return this.http.get<RfpResponse>(`${this.baseUrl}/${id}/response`);
  }

  getResponseSection(id: number, section: number): Observable<any> {
    return this.http.get(`${this.baseUrl}/${id}/response/${section}`);
  }

  regenerateSection(id: number, section: number): Observable<any> {
    return this.http.post(`${this.baseUrl}/${id}/regenerate/${section}`, {});
  }

  downloadDocx(rfpId: number): Observable<Blob> {
    return this.http.get(`/api/response/${rfpId}/download/docx`, { responseType: 'blob' });
  }

  downloadPdf(rfpId: number): Observable<Blob> {
    return this.http.get(`/api/response/${rfpId}/download/pdf`, { responseType: 'blob' });
  }
}
