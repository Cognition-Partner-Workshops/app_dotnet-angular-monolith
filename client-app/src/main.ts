/**
 * Application bootstrap entry point for the OrderManager Angular SPA.
 *
 * Uses the standalone component bootstrap API (Angular 17+) to initialize the
 * application with the root {@link AppComponent} and configure essential providers:
 * - `provideHttpClient()` — Enables HttpClient for API communication with the .NET backend
 * - `provideRouter(routes)` — Configures the Angular Router with lazy-loaded feature routes
 */
import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { AppComponent } from './app/app.component';
import { routes } from './app/app.routes';

bootstrapApplication(AppComponent, {
  providers: [provideHttpClient(), provideRouter(routes)]
});
