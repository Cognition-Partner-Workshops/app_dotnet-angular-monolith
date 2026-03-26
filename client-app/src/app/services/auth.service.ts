import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap, catchError, throwError } from 'rxjs';
import { AuthResponse, LoginRequest, RegisterRequest, UserDto } from '../models/interfaces';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly API_URL = '/api/auth';
  private currentUserSubject = new BehaviorSubject<UserDto | null>(null);
  private tokenExpiryTimer: ReturnType<typeof setTimeout> | null = null;

  currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {
    this.loadStoredUser();
  }

  get isAuthenticated(): boolean {
    return !!this.getAccessToken();
  }

  get currentUser(): UserDto | null {
    return this.currentUserSubject.value;
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.API_URL}/login`, request).pipe(
      tap(response => this.handleAuthResponse(response)),
      catchError(err => throwError(() => err))
    );
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.API_URL}/register`, request).pipe(
      tap(response => this.handleAuthResponse(response)),
      catchError(err => throwError(() => err))
    );
  }

  logout(): void {
    this.http.post(`${this.API_URL}/logout`, {}).subscribe();
    this.clearAuth();
  }

  refreshToken(): Observable<AuthResponse> {
    const refreshToken = localStorage.getItem('tc_refresh_token');
    if (!refreshToken) {
      return throwError(() => new Error('No refresh token'));
    }
    return this.http.post<AuthResponse>(`${this.API_URL}/refresh`, { refreshToken }).pipe(
      tap(response => this.handleAuthResponse(response)),
      catchError(err => {
        this.clearAuth();
        return throwError(() => err);
      })
    );
  }

  getAccessToken(): string | null {
    const token = localStorage.getItem('tc_access_token');
    const expiresAt = localStorage.getItem('tc_token_expires');
    if (!token || !expiresAt) return null;
    if (new Date(expiresAt) <= new Date()) {
      return null;
    }
    return token;
  }

  private handleAuthResponse(response: AuthResponse): void {
    localStorage.setItem('tc_access_token', response.accessToken);
    localStorage.setItem('tc_refresh_token', response.refreshToken);
    localStorage.setItem('tc_token_expires', response.expiresAt);
    localStorage.setItem('tc_user', JSON.stringify(response.user));
    this.currentUserSubject.next(response.user);
    this.scheduleTokenRefresh(response.expiresAt);
  }

  private loadStoredUser(): void {
    const userJson = localStorage.getItem('tc_user');
    if (userJson && this.getAccessToken()) {
      try {
        this.currentUserSubject.next(JSON.parse(userJson));
      } catch {
        this.clearAuth();
      }
    }
  }

  private clearAuth(): void {
    localStorage.removeItem('tc_access_token');
    localStorage.removeItem('tc_refresh_token');
    localStorage.removeItem('tc_token_expires');
    localStorage.removeItem('tc_user');
    this.currentUserSubject.next(null);
    if (this.tokenExpiryTimer) {
      clearTimeout(this.tokenExpiryTimer);
    }
  }

  private scheduleTokenRefresh(expiresAt: string): void {
    if (this.tokenExpiryTimer) {
      clearTimeout(this.tokenExpiryTimer);
    }
    const expiryTime = new Date(expiresAt).getTime();
    const now = Date.now();
    const refreshIn = expiryTime - now - 60000; // Refresh 1 minute before expiry
    if (refreshIn > 0) {
      this.tokenExpiryTimer = setTimeout(() => {
        this.refreshToken().subscribe();
      }, refreshIn);
    }
  }
}
