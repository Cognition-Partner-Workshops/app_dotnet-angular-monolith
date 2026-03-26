import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CallLogDto, ContactDto } from '../models/interfaces';

@Injectable({ providedIn: 'root' })
export class CallService {
  private readonly API_URL = '/api/calls';

  constructor(private http: HttpClient) {}

  initiateCall(receiverId: number, callType: string = 'Audio'): Observable<CallLogDto> {
    return this.http.post<CallLogDto>(`${this.API_URL}/initiate`, { receiverId, callType });
  }

  endCall(callId: number): Observable<CallLogDto> {
    return this.http.post<CallLogDto>(`${this.API_URL}/${callId}/end`, {});
  }

  getHistory(page: number = 1, pageSize: number = 20): Observable<CallLogDto[]> {
    return this.http.get<CallLogDto[]>(`${this.API_URL}/history?page=${page}&pageSize=${pageSize}`);
  }

  getContacts(): Observable<ContactDto[]> {
    return this.http.get<ContactDto[]>(`${this.API_URL}/contacts`);
  }

  addContact(username: string, displayName?: string): Observable<ContactDto> {
    return this.http.post<ContactDto>(`${this.API_URL}/contacts`, { username, displayName });
  }

  removeContact(id: number): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/contacts/${id}`);
  }
}
