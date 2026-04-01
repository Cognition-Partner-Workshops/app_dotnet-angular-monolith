import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { delay, retry } from 'rxjs/operators';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const token = authService.getAccessToken();

  const headers: Record<string, string> = {};
  if (token) {
    headers['X-Authorization'] = `Bearer ${token}`;
  }

  const cloned = req.clone({
    withCredentials: true,
    setHeaders: headers
  });
  return next(cloned).pipe(
    retry({ count: 2, delay: 1000 })
  );
};
