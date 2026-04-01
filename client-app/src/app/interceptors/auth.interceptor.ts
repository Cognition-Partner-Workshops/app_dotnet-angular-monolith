import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { ServerConfigService } from '../services/server-config.service';
import { retry } from 'rxjs/operators';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const serverConfig = inject(ServerConfigService);
  const token = authService.getAccessToken();

  const headers: Record<string, string> = {};
  if (token) {
    headers['X-Authorization'] = `Bearer ${token}`;
    // In local network mode, also send standard Authorization header
    // (no proxy to conflict with)
    if (serverConfig.isLocalMode) {
      headers['Authorization'] = `Bearer ${token}`;
    }
  }

  // Rewrite API URLs to point to the configured server
  let url = req.url;
  if (serverConfig.isLocalMode && url.startsWith('/')) {
    url = serverConfig.resolveUrl(url);
  }

  const cloned = req.clone({
    url,
    withCredentials: !serverConfig.isLocalMode, // no credentials needed for local
    setHeaders: headers
  });
  return next(cloned).pipe(
    retry({ count: 2, delay: 1000 })
  );
};
