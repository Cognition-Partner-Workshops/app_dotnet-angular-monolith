import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const token = authService.getAccessToken();

  // Always set withCredentials so the browser sends cached Basic Auth
  // credentials (from the tunnel proxy URL) with every XHR request
  const headers: Record<string, string> = {};
  if (token) {
    headers['X-Authorization'] = `Bearer ${token}`;
  }

  const cloned = req.clone({
    withCredentials: true,
    setHeaders: headers
  });
  return next(cloned);
};
