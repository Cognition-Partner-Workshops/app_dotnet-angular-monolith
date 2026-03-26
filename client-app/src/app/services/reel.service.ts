import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ReelDto, ReelFeedResponse } from '../models/interfaces';

@Injectable({ providedIn: 'root' })
export class ReelService {
  private readonly API_URL = '/api/reels';

  constructor(private http: HttpClient) {}

  getFeed(page: number = 1, pageSize: number = 10): Observable<ReelFeedResponse> {
    return this.http.get<ReelFeedResponse>(`${this.API_URL}?page=${page}&pageSize=${pageSize}`);
  }

  getById(id: number): Observable<ReelDto> {
    return this.http.get<ReelDto>(`${this.API_URL}/${id}`);
  }

  toggleLike(id: number): Observable<{ isLiked: boolean }> {
    return this.http.post<{ isLiked: boolean }>(`${this.API_URL}/${id}/like`, {});
  }

  getDownloadInfo(id: number): Observable<{ downloadUrl: string; title: string; fileSizeBytes: number; durationSeconds: number }> {
    return this.http.get<{ downloadUrl: string; title: string; fileSizeBytes: number; durationSeconds: number }>(`${this.API_URL}/${id}/download`);
  }

  getMyReels(): Observable<ReelDto[]> {
    return this.http.get<ReelDto[]>(`${this.API_URL}/my`);
  }

  create(formData: FormData): Observable<ReelDto> {
    return this.http.post<ReelDto>(this.API_URL, formData);
  }
}
