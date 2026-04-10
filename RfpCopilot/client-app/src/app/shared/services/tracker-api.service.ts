import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TrackerEntry } from '../models/tracker-entry.model';

@Injectable({ providedIn: 'root' })
export class TrackerApiService {
  private baseUrl = '/api/tracker';

  constructor(private http: HttpClient) {}

  getAll(): Observable<TrackerEntry[]> {
    return this.http.get<TrackerEntry[]>(this.baseUrl);
  }

  getByRfpId(rfpId: string): Observable<TrackerEntry> {
    return this.http.get<TrackerEntry>(`${this.baseUrl}/${rfpId}`);
  }

  update(rfpId: string, entry: Partial<TrackerEntry>): Observable<TrackerEntry> {
    return this.http.put<TrackerEntry>(`${this.baseUrl}/${rfpId}`, entry);
  }

  exportToExcel(): Observable<Blob> {
    return this.http.post(`${this.baseUrl}/export`, {}, { responseType: 'blob' });
  }
}
